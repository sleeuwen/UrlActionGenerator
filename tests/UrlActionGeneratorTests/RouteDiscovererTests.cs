using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UrlActionGenerator;
using UrlActionGenerator.Descriptors;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class RouteDiscovererTests
    {
        public static object[][] DiscoverRouteParameters_Data => new[]
        {
            new object[] { "", Array.Empty<ParameterDescriptor>() },
            new object[] { "/page", Array.Empty<ParameterDescriptor>() },
            new object[] { "/{id}", new[] { new ParameterDescriptor("id", "string", false, null) } },
            new object[] { "/{id:int}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:int:min(1)}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:min(1)}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:min(1):max(4)}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:minlength(1)}", new[] { new ParameterDescriptor("id", "string", false, null) } },
            new object[] { "/{id:alpha}", new[] { new ParameterDescriptor("id", "string", false, null) } },
            new object[] { "/{id:bool}", new[] { new ParameterDescriptor("id", "bool", false, null) } },
            new object[] { "/{id:long}", new[] { new ParameterDescriptor("id", "long", false, null) } },
            new object[] { "/{id:float}", new[] { new ParameterDescriptor("id", "float", false, null) } },
            new object[] { "/{id:double}", new[] { new ParameterDescriptor("id", "double", false, null) } },
            new object[] { "/{id:decimal}", new[] { new ParameterDescriptor("id", "decimal", false, null) } },
            new object[] { "/{id:guid}", new[] { new ParameterDescriptor("id", "System.Guid", false, null) } },
            new object[] { "/{id:datetime}", new[] { new ParameterDescriptor("id", "System.DateTime", false, null) } },
            new object[] { "/{id:int}/{other}", new[] { new ParameterDescriptor("id", "int", false, null), new ParameterDescriptor("other", "string", false, null) } },
            new object[] { "/{culture:culture}", new[] { new ParameterDescriptor("culture", "string", false, null) } },
        };

        [Theory]
        [MemberData(nameof(DiscoverRouteParameters_Data))]
        public void DiscoverRouteParameters_ReturnsTheCorrectParameters(string route, IEnumerable<ParameterDescriptor> expectedParameters)
        {
            // Act
            var parameters = RouteDiscoverer.DiscoverRouteParameters(route);

            // Assert
            parameters.Should().BeEquivalentTo(expectedParameters);
        }

        [Fact]
        public void DiscoverActionName_FromActionNameAttribute()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

public class TestClass
{
    [ActionName(""CustomActionName"")]
    public void ActionMethod() {}
}");

            var method = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var methodSymbol = compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method);

            var result = RouteDiscoverer.DiscoverActionName(methodSymbol);

            Assert.Equal("CustomActionName", result);
        }

        [Fact]
        public void DiscoverActionName_FromMethodName()
        {
            var compilation = CreateCompilation(@"
public class TestClass
{
    public void ActionMethod() {}
}");

            var method = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var methodSymbol = compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method);

            var result = RouteDiscoverer.DiscoverActionName(methodSymbol);

            Assert.Equal("ActionMethod", result);
        }

        [Fact]
        public void DiscoverActionName_FromMethodNameAsync()
        {
            var compilation = CreateCompilation(@"
public class TestClass
{
    public void ActionMethodAsync() {}
}");

            var method = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var methodSymbol = compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method);

            var result = RouteDiscoverer.DiscoverActionName(methodSymbol);

            Assert.Equal("ActionMethod", result);
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
