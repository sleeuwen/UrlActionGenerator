using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UrlActionGenerator;
using UrlActionGeneratorTests.TestFiles.PagesFactsTests;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class PagesFactsTests
    {
        #region IsPageMethod
        private static readonly Type TestIsPageMethodType = typeof(OnGetTestIsPageMethod);

        [Fact]
        public void IsPageMethod_ReturnsFalseForConstructor() => IsPageMethodReturnsFalse(TestIsPageMethodType, ".ctor");

        [Fact]
        public void IsPageMethod_ReturnsFalseForStaticConstructor() => IsPageMethodReturnsFalse(TestIsPageMethodType, ".cctor");

        [Fact]
        public void IsPageMethod_ReturnsFalseForPrivateMethod() => IsPageMethodReturnsFalse(TestIsPageMethodType, "OnGetPrivateMethod");

        [Fact]
        public void IsPageMethod_ReturnsFalseForProtectedMethod() => IsPageMethodReturnsFalse(TestIsPageMethodType, "OnGetProtectedMethod");

        [Fact]
        public void IsPageMethod_ReturnsFalseForInternalMethod() => IsPageMethodReturnsFalse(TestIsPageMethodType, nameof(OnGetTestIsPageMethod.OnGetInternalMethod));

        [Fact]
        public void IsPageMethod_ReturnsFalseForGenericMethod() => IsPageMethodReturnsFalse(TestIsPageMethodType, nameof(OnGetTestIsPageMethod.OnGetGenericMethod));

        [Fact]
        public void IsPageMethod_ReturnsFalseForStaticMethod() => IsPageMethodReturnsFalse(TestIsPageMethodType, nameof(OnGetTestIsPageMethod.OnGetStaticMethod));

        [Fact]
        public void IsPageMethod_ReturnsFalseForNonHandlerMethod() => IsPageMethodReturnsFalse(TestIsPageMethodType, nameof(OnGetTestIsPageMethod.OnGetNonHandler));

        [Fact]
        public void IsPageMethod_ReturnsFalseForOverriddenNonHandlerMethod() => IsPageMethodReturnsFalse(TestIsPageMethodType, nameof(OnGetTestIsPageMethod.OnGetNonHandlerBase));

        [Fact]
        public void IsPageMethod_ReturnsFalseForAbstractMethods() => IsPageMethodReturnsFalse(typeof(OnGetTestIsPageMethodBase), nameof(OnGetTestIsPageMethodBase.OnGetAbstractMethod));

        [Fact]
        public void IsPageMethod_ReturnsFalseForObjectEquals() => IsPageMethodReturnsFalse(typeof(object), nameof(object.Equals));

        [Fact]
        public void IsPageMethod_ReturnsFalseForObjectHashCode() => IsPageMethodReturnsFalse(typeof(object), nameof(object.GetHashCode));

        [Fact]
        public void IsPageMethod_ReturnsFalseForObjectToString() => IsPageMethodReturnsFalse(typeof(object), nameof(object.ToString));

        [Fact]
        public void IsPageMethod_ReturnsFalseForMissingPrefix() => IsPageMethodReturnsFalse(typeof(OnGetTestIsPageMethod), nameof(OnGetTestIsPageMethod.NoOnPrefix));

        [Fact]
        public void IsPageMethod_ReturnsFalseForOnTraceHandler() => IsPageMethodReturnsFalse(typeof(OnGetTestIsPageMethod), nameof(OnGetTestIsPageMethod.OnTraceHandler));

        [Fact]
        public void IsPageMethod_ReturnsFalseForOnConnectHandler() => IsPageMethodReturnsFalse(typeof(OnGetTestIsPageMethod), nameof(OnGetTestIsPageMethod.OnConnectHandler));

        private void IsPageMethodReturnsFalse(Type type, string methodName)
        {
            var compilation = GetIsPageMethodCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
            var method = (IMethodSymbol)typeSymbol.GetMembers(methodName).First();

            // Act
            var isControllerAction = PagesFacts.IsPageMethod(method);

            // Assert
            Assert.False(isControllerAction);
        }

        [Fact]
        public void IsPageMethod_ThrowsForNullMethodSymbol()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var isControllerAction = PagesFacts.IsPageMethod(null);
            });
        }

        [Fact]
        public void IsPageMethod_ReturnsTrueForOrdinaryHandler() => IsPageMethodReturnsTrue(TestIsPageMethodType, nameof(OnGetTestIsPageMethod.OnGetOrdinary));

        [Fact]
        public void IsPageMethod_ReturnsTrueForOverriddenMethod() => IsPageMethodReturnsTrue(TestIsPageMethodType, nameof(OnGetTestIsPageMethod.OnGetAbstractMethod));

        private void IsPageMethodReturnsTrue(Type type, string methodName)
        {
            var compilation = GetIsPageMethodCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
            var method = (IMethodSymbol)typeSymbol.GetMembers(methodName).First();

            // Act
            var isControllerAction = PagesFacts.IsPageMethod(method);

            // Assert
            Assert.True(isControllerAction);
        }

        private Compilation GetIsPageMethodCompilation() => GetCompilation("IsPageMethodTests");

        private Compilation GetCompilation(string testMethod)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "TestFiles", GetType().Name, testMethod + ".cs");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"TestFile {testMethod} could not be found at {filePath}.", filePath);

            var testSource = File.ReadAllText(filePath);

            var compilation = CreateCompilation(testSource);
            return compilation;
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
        #endregion

        [Fact]
        public void IsRazorPage_ReturnsTrue()
        {
            var file = new InMemoryAdditionalText("", "@page");

            // Act
            var result = PagesFacts.IsRazorPage(file);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsRazorPage_ReturnsFalse()
        {
            var file = new InMemoryAdditionalText("", "");

            // Act
            var result = PagesFacts.IsRazorPage(file);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("/Pages/Index.cshtml", false)]
        [InlineData("/Pages/Nested/Folder/Index.cshtml", false)]
        [InlineData("/Pages/_ViewStart.cshtml", true)]
        [InlineData("/Pages/_ViewImports.cshtml", true)]
        [InlineData("/Pages/Nested/Folder/_ViewStart.cshtml", true)]
        [InlineData("/Pages/Nested/Folder/_ViewImports.cshtml", true)]
        public void IsImplicitlyIncludedFile(string path, bool expected)
        {
            var file = new InMemoryAdditionalText(path, "");
            var pageData = new RazorPageItem(file);

            // Act
            var result = PagesFacts.IsImplicitlyIncludedFile(pageData);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractUsings_ReturnsEmpty_WhenNoUsings()
        {
            var file = new InMemoryAdditionalText("", "");
            var pageData = new RazorPageItem(file);

            // Act
            var result = PagesFacts.ExtractUsings(pageData);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractUsings_ReturnsUsings()
        {
            var file = new InMemoryAdditionalText("", @"@using System
@using System.Text");
            var pageData = new RazorPageItem(file);

            // Act
            var result = PagesFacts.ExtractUsings(pageData);

            // Assert
            result.Should().BeEquivalentTo(new[]
            {
                "System",
                "System.Text",
            });
        }
    }
}
