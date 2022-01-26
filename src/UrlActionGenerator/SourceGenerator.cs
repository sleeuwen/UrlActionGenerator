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
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    [Generator]
    public class SourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(PostInitialize);

            var generatorContext = CreateGeneratorContextProvider(context);

            CreateMvcPipeline(context, generatorContext);
            CreateRazorPagesPipeline(context, generatorContext);
        }

        private static void PostInitialize(IncrementalGeneratorPostInitializationContext context)
        {
            context.AddSource("ExcludedParameterTypeAttribute.cs", @"namespace UrlActionGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class ExcludedParameterTypeAttribute : System.Attribute
    {
        public ExcludedParameterTypeAttribute(System.Type type)
        {
        }
    }
}");
        }

        private static IncrementalValueProvider<GeneratorContext> CreateGeneratorContextProvider(IncrementalGeneratorInitializationContext context)
        {
            var excludedParameterTypes = CreateExcludedParameterTypesValueProvider(context);

            return context.CompilationProvider.Combine(excludedParameterTypes)
                .Select(static (tup, _) => new GeneratorContext(tup.Left, tup.Right));
        }

        private static IncrementalValueProvider<ImmutableArray<ITypeSymbol>> CreateExcludedParameterTypesValueProvider(IncrementalGeneratorInitializationContext context)
        {
            var attributes = context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => FilterExcludedParameterTypeAttributes(node),
                    static (context, _) => GetTypeSymbolForExcludedParameterType(context))
                .Where(x => x != null)
                .Collect();

            return attributes;

            static bool FilterExcludedParameterTypeAttributes(SyntaxNode node)
            {
                if (node is not AttributeSyntax attributeSyntax)
                    return false;

                if (attributeSyntax.ArgumentList?.Arguments.Count != 1)
                    return false;

                if ((attributeSyntax.Parent as AttributeListSyntax)?.Target?.Identifier.ToString() != "assembly")
                    return false;

                var attributeName = attributeSyntax.Name.ToString();
                if (!attributeName.EndsWith("ExcludedParameterTypeAttribute") && !attributeName.EndsWith("ExcludedParameterType"))
                    return false;

                return true;
            }

            static ITypeSymbol GetTypeSymbolForExcludedParameterType(GeneratorSyntaxContext context)
            {
                var attribute = (AttributeSyntax)context.Node;
                if (attribute.ArgumentList?.Arguments.Count != 1)
                    return null;

                var attributeName = attribute.Name.ToString();
                if (!attributeName.EndsWith("ExcludedParameterTypeAttribute") && !attributeName.EndsWith("ExcludedParameterType"))
                    return null;

                var excludedParameterTypeAttributeType = context.SemanticModel.Compilation.GetTypeByMetadataName("UrlActionGenerator.ExcludedParameterTypeAttribute");

                var attributeType = context.SemanticModel.GetTypeInfo(attribute).Type;
                if (attributeType == null || !attributeType.Equals(excludedParameterTypeAttributeType))
                    return null;

                var typeSyntax = (attribute.ArgumentList.Arguments[0].Expression as TypeOfExpressionSyntax)?.Type;
                if (typeSyntax == null)
                    return null;

                return context.SemanticModel.GetTypeInfo(typeSyntax).Type;
            }
        }

        private static void CreateMvcPipeline(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<GeneratorContext> generatorContextProvider)
        {
            var controllers = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is TypeDeclarationSyntax classSyntax && MvcFacts.CanBeController(classSyntax),
                    static (ctx, _) => GetSemanticModelForGeneration(ctx))
                .Where(static m => m is not null);

            var controllerActions = controllers.Combine(generatorContextProvider)
                .Where(static (tup) => !tup.Right.IsViewsAssembly)
                .Select((tup, _) => MvcDiscoverer.DiscoverAreaControllerActions(tup.Left, tup.Right))
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

        private static void CreateRazorPagesPipeline(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<GeneratorContext> generatorContextProvider)
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

            var pagesGeneratorContextProvider = generatorContextProvider.Combine(implicitlyImportedUsings)
                .Select(static (tup, _) => new GeneratorPagesContext(tup.Left, tup.Right));

            var allPages = razorPages.Combine(pagesGeneratorContextProvider)
                .Where(static tup => !tup.Right.IsViewsAssembly)
                .Select(static (tup, _) => PagesDiscoverer.DiscoverAreaPages(tup.Left, tup.Right))
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
