using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    //[Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (AssemblyFacts.IsRazorViewsAssembly(context.Compilation.Assembly))
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

                using var sourceWriter = new StringWriter();
                using var writer = new IndentedTextWriter(sourceWriter, "    ");

                CodeGenerator.WriteUrlActions(writer, areas);

                context.AddSource("UrlActionGenerator_UrlHelperExtensions.Mvc.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MvcCodeGenException, Location.None, ex.Message, ex.StackTrace));
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
                var pages = PagesDiscoverer.DiscoverAreaPages(context.Compilation, context.AdditionalFiles, context.AnalyzerConfigOptions.GlobalOptions).ToList();

                using var sourceWriter = new StringWriter();
                using var writer = new IndentedTextWriter(sourceWriter, "    ");

                CodeGenerator.WriteUrlPages(writer, pages);

                context.AddSource("UrlActionGenerator_UrlHelperExtensions.Pages.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.RazorPagesCodeGenException, Location.None, ex.Message, ex.StackTrace));
                Log(context.Compilation, $"Exception during generating Razor Pages: {ex.Message}\n{ex.StackTrace}");
            }

            sw.Stop();
            Log(context.Compilation, $"Razor Pages step took: {sw.Elapsed.TotalMilliseconds}ms");
        }

        [Conditional("DEBUG")]
        [ExcludeFromCodeCoverage]
        internal static void Log(Compilation compilation, string message)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    File.AppendAllText("/home/stephan/Urls.txt", compilation.AssemblyName + ":\t" + message.Trim() + "\n");
                }
                catch (Exception)
                {
                    // Ignored
                }
            }
        }
    }
}
