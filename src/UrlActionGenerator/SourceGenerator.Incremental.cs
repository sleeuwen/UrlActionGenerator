using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    [Generator]
    public class IncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var controllers = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is TypeDeclarationSyntax classSyntax && MvcFacts.CanBeController(classSyntax),
                    static (ctx, _) => GetSemanticModelForGeneration(ctx))
                .Where(static m => m is not null);

            var controllerActions = controllers
                .Select((symbol, _) => MvcDiscoverer.DiscoverAreaControllerActions(symbol))
                .Where(static area => area.Controllers.Count > 0);

            var allAreas = controllerActions.Collect()
                .Select((areas, _) => MvcDiscoverer.CombineAreas(areas));

            context.RegisterSourceOutput(allAreas, static (context, areas) =>
            {
                using var sourceWriter = new StringWriter();
                using var writer = new IndentedTextWriter(sourceWriter, "    ");

                CodeGenerator.WriteUrlActions(writer, areas.ToList());

                context.AddSource("UrlActionGenerator_UrlHelperExtensions.Mvc.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
            });

            var razorPages = context.AdditionalTextsProvider
                .Where(static txt =>
                    txt.Path.EndsWith(".cshtml") && txt.GetText().Lines.First().ToString().Contains("@page"))
                .Select(static (txt, _) => new RazorPageItem(txt));

            var implicitlyImportedPages = razorPages
                .Where(PagesFacts.IsImplicitlyIncludedFile)
                .Collect()
                .Select(static (pages, _) => PagesDiscoverer.GatherImplicitUsings(pages));

            var allPages = razorPages.Combine(implicitlyImportedPages).Combine(context.CompilationProvider)
                .Select(static (tup, _) => (Page: tup.Left.Left, ImplicitlyImportedUsings: tup.Left.Right, Compilation: tup.Right))
                .Select(static (tup, _) => PagesDiscoverer.DiscoverAreaPages(tup.Page, tup.ImplicitlyImportedUsings, tup.Compilation))
                .Where(static area => area.Pages.Count > 0 || area.Folders.Count > 0);

            var allPageAreas = allPages.Collect()
                .Select((pages, _) => PagesDiscoverer.CombineAreas(pages));

            context.RegisterSourceOutput(allPageAreas, static (context, areas) =>
            {
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

        public static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> controllers, SourceProductionContext context)
        {
            if (controllers.IsDefaultOrEmpty)
                return;
            if (AssemblyFacts.IsRazorViewsAssembly(compilation.Assembly))
                return;

            var sw = Stopwatch.StartNew();

            Log(compilation, "MVC");
            try
            {
                var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, controllers.Distinct().ToList()).ToList();

                using var sourceWriter = new StringWriter();
                using var writer = new IndentedTextWriter(sourceWriter, "    ");

                CodeGenerator.WriteUrlActions(writer, areas);

                context.AddSource("UrlActionGenerator_UrlHelperExtensions.Mvc.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MvcCodeGenException, Location.None, ex.Message, ex.StackTrace));
                Log(compilation, $"Exception during generating MVC: {ex.Message}\n{ex.StackTrace}");
            }

            sw.Stop();
            Log(compilation, $"MVC step took: {sw.Elapsed.TotalMilliseconds}ms");
        }

        public static void Execute(Compilation compilation, AnalyzerConfigOptionsProvider options, ImmutableArray<AdditionalText> additionalFiles, SourceProductionContext context)
        {
            if (additionalFiles.IsDefaultOrEmpty)
                return;
            if (AssemblyFacts.IsRazorViewsAssembly(compilation.Assembly))
                return;

            var sw = Stopwatch.StartNew();

            Log(compilation, "Razor Pages");
            try
            {
                var pages = PagesDiscoverer.DiscoverAreaPages(compilation, additionalFiles, options.GlobalOptions).ToList();

                using var sourceWriter = new StringWriter();
                using var writer = new IndentedTextWriter(sourceWriter, "    ");

                CodeGenerator.WriteUrlPages(writer, pages);

                context.AddSource("UrlActionGenerator_UrlHelperExtensions.Pages.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.RazorPagesCodeGenException, Location.None, ex.Message, ex.StackTrace));
                Log(compilation, $"Exception during generating Razor Pages: {ex.Message}\n{ex.StackTrace}");
            }

            sw.Stop();
            Log(compilation, $"Razor Pages step took: {sw.Elapsed.TotalMilliseconds}ms");
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
