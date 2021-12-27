using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class PagesDiscovererTests
    {
        [Fact]
        public void DiscoverAreaPages_SameRouteModelParameter()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestCode.Pages
{
    public class Index : PageModel
    {
        [FromRoute]
        public int PageNumber { get; set; }

        public void OnGet()
        {
        }
    }
}
");
            var razorPageItem = new RazorPageItem(new InMemoryAdditionalText("/Pages/Index.cshtml", @"@page ""{pageNumber}""
@model TestCode.Pages.Index"));

            // Act
            var generatorContext = new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty);
            var area = PagesDiscoverer.DiscoverAreaPages(razorPageItem, new GeneratorContext(generatorContext, ImmutableDictionary<string, ImmutableArray<string>>.Empty));

            // Assert
            compilation.GetDiagnostics().Should().HaveCount(0);

            Assert.NotNull(area);
            area.Pages.Should().HaveCount(1);
            var page = area.Pages.Single();

            page.Parameters.Should().HaveCount(1);
            var parameter = page.Parameters.Single();

            parameter.Name.Should().Be("pageNumber");
            parameter.Type.Should().Be("int");
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[]
                {
                    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ValueTuple<>).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Controller).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(AreaAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(RouteValueAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IUrlHelper).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(PageModel).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
