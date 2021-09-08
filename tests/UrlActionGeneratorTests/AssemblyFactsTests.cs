using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class AssemblyFactsTests
    {
        [Fact]
        public void IsRazorViewsAssembly_ReturnsFalse_WhenAssemblyNameDoesNotEndInViews()
        {
            var compilation = CreateCompilation("AspNetCoreSample");

            var assemblyAttributes = compilation.SyntaxTrees.SelectMany(st => st.GetRoot().DescendantNodes())
                .OfType<AttributeListSyntax>()
                .Where(attr => attr.Target?.Identifier.ToString() == "assembly")
                .SelectMany(attr => attr.Attributes)
                .ToList();

            // Act
            var result = AssemblyFacts.IsRazorViewsAssembly(compilation.AssemblyName, assemblyAttributes, compilation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRazorViewsAssembly_ReturnsFalse_WhenNoApplicationPartFactoryAttributeIsPresent()
        {
            var compilation = CreateCompilation("AspNetCoreSample.Views");

            var assemblyAttributes = compilation.SyntaxTrees.SelectMany(st => st.GetRoot().DescendantNodes())
                .OfType<AttributeListSyntax>()
                .Where(attr => attr.Target?.Identifier.ToString() == "assembly")
                .SelectMany(attr => attr.Attributes)
                .ToList();

            // Act
            var result = AssemblyFacts.IsRazorViewsAssembly(compilation.AssemblyName, assemblyAttributes, compilation);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\"Microsoft.AspNetCore.Mvc.ApplicationParts.ConsolidatedAssemblyApplicationPartFactory, Microsoft.AspNetCore.Mvc.Razor\"")] // .NET 6 consolidated assembly
        public void IsRazorViewsAssembly_ReturnsFalse_WhenApplicationPartFactoryAttributeIsNotCompiledRazorProvider(string typeInfo)
        {
            var source = $@"[assembly: global::Microsoft.AspNetCore.Mvc.ApplicationParts.ProvideApplicationPartFactoryAttribute({typeInfo})]";
            var compilation = CreateCompilation("AspNetCoreSample.Views", source);

            var assemblyAttributes = compilation.SyntaxTrees.SelectMany(st => st.GetRoot().DescendantNodes())
                .OfType<AttributeListSyntax>()
                .Where(attr => attr.Target?.Identifier.ToString() == "assembly")
                .SelectMany(attr => attr.Attributes)
                .ToList();

            // Act
            var result = AssemblyFacts.IsRazorViewsAssembly(compilation.AssemblyName, assemblyAttributes, compilation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRazorViewsAssembly_ReturnsTrue_ForRazorViewsAssembly()
        {
            var source = @"[assembly: global::Microsoft.AspNetCore.Mvc.ApplicationParts.ProvideApplicationPartFactoryAttribute(""Microsoft.AspNetCore.Mvc.ApplicationParts.CompiledRazorAssemblyApplicationPartFactory, Microsoft.AspNetCore.Mvc.Razor"")]";
            var compilation = CreateCompilation("AspNetCoreSample.Views", source);

            var assemblyAttributes = compilation.SyntaxTrees.SelectMany(st => st.GetRoot().DescendantNodes())
                .OfType<AttributeListSyntax>()
                .Where(attr => attr.Target?.Identifier.ToString() == "assembly")
                .SelectMany(attr => attr.Attributes)
                .ToList();

            // Act
            var result = AssemblyFacts.IsRazorViewsAssembly(compilation.AssemblyName, assemblyAttributes, compilation);

            // Assert
            Assert.True(result);
        }

        private static Compilation CreateCompilation(string assemblyName, string source = "")
            => CSharpCompilation.Create(assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source) },
                new MetadataReference[]
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
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
