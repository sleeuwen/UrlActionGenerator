using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    [Generator]
    public class SourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            CreateMvcPipeline(context);
            CreateRazorPagesPipeline(context);
        }

        private static void CreateMvcPipeline(IncrementalGeneratorInitializationContext context)
        {
            var controllers = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is TypeDeclarationSyntax classSyntax && MvcFacts.CanBeController(classSyntax),
                    static (ctx, _) => GetSemanticModelForGeneration(ctx))
                .Where(static m => m is not null);

            var controllerActions = controllers.Combine(context.CompilationProvider)
                .Where(static tup => tup.Right.AssemblyName?.EndsWith(".Views") != true)
                .Select((tup, _) => MvcDiscoverer.DiscoverAreaControllerActions(tup.Left))
                .Where(static area => area.Controllers.Count > 0);

            var areaDescriptors = controllerActions.Collect()
                .Select(static (areas, _) => MvcDiscoverer.CombineAreas(areas).ToList());

            context.RegisterSourceOutput(areaDescriptors, static (context, areas) =>
            {
                if (areas.Count == 0)
                    return;

                using var sourceWriter = new StringWriter();
                using var writer = new IndentedTextWriter(sourceWriter, "    ");

                CodeGenerator.WriteUrlActions(writer, areas);

                context.AddSource("UrlActionGenerator_UrlHelperExtensions.Mvc.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
            });
        }

        private static void CreateRazorPagesPipeline(IncrementalGeneratorInitializationContext context)
        {
            var razorPages = context.AdditionalTextsProvider
                .Where(static txt =>
                {
                    if (!txt.Path.EndsWith(".cshtml"))
                        return false;

                    var lines = txt.GetText()?.Lines;
                    if (lines is null or { Count: 0 })
                        return false;

                    return lines[0].ToString().Contains("@page");
                })
                .Select(static (txt, _) => new RazorPageItem(txt));

            var implicitlyImportedUsings = razorPages
                .Where(PagesFacts.IsImplicitlyIncludedFile)
                .Collect()
                .Select(static (pages, _) => PagesDiscoverer.GatherImplicitUsings(pages));

            var allPages = razorPages.Combine(implicitlyImportedUsings).Combine(context.CompilationProvider)
                .Select(static (tup, _) => (Page: tup.Left.Left, ImplicitlyImportedUsings: tup.Left.Right, Compilation: tup.Right))
                .Where(static tup => tup.Compilation.AssemblyName?.EndsWith(".Views") != true)
                .Select(static (tup, _) => PagesDiscoverer.DiscoverAreaPages(tup.Page, tup.ImplicitlyImportedUsings, tup.Compilation))
                .Where(static area => area.Pages.Count > 0 || area.Folders.Count > 0);

            var allPageAreas = allPages.Collect()
                .Select((pages, _) => PagesDiscoverer.CombineAreas(pages).ToList());

            context.RegisterSourceOutput(allPageAreas, static (context, areas) =>
            {
                if (areas.Count == 0)
                    return;

                using var sourceWriter = new StringWriter();
                using var writer = new IndentedTextWriter(sourceWriter, "    ");

                CodeGenerator.WriteUrlPages(writer, areas.ToList());

                context.AddSource("UrlActionGenerator_UrlHelperExtensions.Pages.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
            });
        }

        private static INamedTypeSymbol? GetSemanticModelForGeneration(GeneratorSyntaxContext context)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);

            if (symbol is INamedTypeSymbol typeSymbol && MvcFacts.IsController(typeSymbol))
                return typeSymbol;

            return null;
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
