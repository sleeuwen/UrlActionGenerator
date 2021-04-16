using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (IsRazorViewsAssembly(context))
                return;

            CodeGenMvc(context);
            CodeGenRazorPages(context);
        }

        private static void CodeGenMvc(GeneratorExecutionContext context)
        {
            var syntaxReceiver = (MySyntaxReceiver)context.SyntaxReceiver;

            var sw = Stopwatch.StartNew();

            Log(context.Compilation, "MVC");
            try
            {
                var areas = MvcDiscoverer.DiscoverAreaControllerActions(context.Compilation, syntaxReceiver.PossibleControllers).ToList();

                if (areas.Any())
                {
                    using var sourceWriter = new StringWriter();
                    using var writer = new IndentedTextWriter(sourceWriter, "    ");

                    CodeGenerator.WriteUrlActions(writer, areas);

                    context.AddSource("UrlSourceGenerator.Mvc.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                // TODO: Show diagnostic
                Log(context.Compilation, $"Exception during generating MVC: {ex.Message}\n{ex.StackTrace}");
            }

            sw.Stop();
            Log(context.Compilation, $"MVC step took: {sw.Elapsed.TotalMilliseconds}ms");
        }

        private static void CodeGenRazorPages(GeneratorExecutionContext context)
        {
            var sw = Stopwatch.StartNew();

            Log(context.Compilation, "Razor Pages");
            try
            {
                var pages = PagesDiscoverer.DiscoverAreaPages(context.Compilation, context.AdditionalFiles).ToList();

                if (pages.Any())
                {
                    using var sourceWriter = new StringWriter();
                    using var writer = new IndentedTextWriter(sourceWriter, "    ");

                    CodeGenerator.WriteUrlPages(writer, pages);

                    context.AddSource("UrlSourceGenerator.Pages.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                // TODO: Show diagnostic
                Log(context.Compilation, $"Exception during generating Razor Pages: {ex.Message}\n{ex.StackTrace}");
            }

            sw.Stop();
            Log(context.Compilation, $"Razor Pages step took: {sw.Elapsed.TotalMilliseconds}ms");
        }

        [Conditional("DEBUG")]
        internal static void Log(Compilation compilation, string message)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                File.AppendAllText("/home/stephan/Urls.txt", compilation.AssemblyName + ":\t" + message.Trim() + "\n");
            }
        }

        private static bool IsRazorViewsAssembly(GeneratorExecutionContext context)
        {
            if (context.Compilation.AssemblyName?.EndsWith(".Views") != true)
                return false;

            var attributes = context.Compilation.Assembly.GetAttributes();
            var applicationPartFactoryAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass?.ToString() == "Microsoft.AspNetCore.Mvc.ApplicationParts.ProvideApplicationPartFactoryAttribute");
            if (applicationPartFactoryAttribute == null)
                return false;

            var factoryType = applicationPartFactoryAttribute.ConstructorArguments.FirstOrDefault().Value as string;
            if (factoryType == null)
                return false;

            if (factoryType != "Microsoft.AspNetCore.Mvc.ApplicationParts.CompiledRazorAssemblyApplicationPartFactory, Microsoft.AspNetCore.Mvc.Razor")
                return false;

            return true;
        }
    }
}
