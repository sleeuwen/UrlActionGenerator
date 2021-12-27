using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UrlActionGenerator.Extensions;
using Xunit;

namespace UrlActionGeneratorTests.Extensions
{
    public class SymbolExtensionsTests
    {
        [Theory]
        [InlineData("System.String", "System.String")]
        [InlineData("System.String?", "System.String")]
        [InlineData("System.Nullable<System.String>", "System.String")]
        [InlineData("System.String[]", "System.String")]
        [InlineData("System.Collections.Generic.List<System.String>", "System.String")]
        [InlineData("System.Collections.Generic.IList<System.String>", "System.String")]
        [InlineData("System.Collections.Generic.ICollection<System.String>", "System.String")]
        [InlineData("System.Collections.Generic.IEnumerable<System.String>", "System.String")]
        public void GetUnderlyingType(string type, string expected)
        {
            var compilation = CreateCompilation($@"
public class TypeHolder
{{
    public TypeHolder({type} type)
    {{
    }}
}}");

            var classSyntax = compilation.SyntaxTrees.SelectMany(st => st.GetRoot().DescendantNodes()).OfType<ClassDeclarationSyntax>().Single();
            var classSymbol = (ITypeSymbol)ModelExtensions.GetDeclaredSymbol(compilation.GetSemanticModel(classSyntax.SyntaxTree), classSyntax);

            var method = classSymbol.GetMembers().OfType<IMethodSymbol>().Single();
            var typeSymbol = method.Parameters.Single().Type;

            var underlyingType = typeSymbol.GetUnderlyingType();

            Assert.Equal(expected, underlyingType.GetFullNamespacedName());
        }

        [Theory]
        [InlineData("System.Nullable<System.Int32>", "int?")]
        public void GetTypeName(string type, string expected)
        {
            var compilation = CreateCompilation(@$"
public class TypeHolder
{{
    public TypeHolder({type} type)
    {{
    }}
}}");

            var classSyntax = compilation.SyntaxTrees.SelectMany(st => st.GetRoot().DescendantNodes()).OfType<ClassDeclarationSyntax>().Single();
            var classSymbol = (ITypeSymbol)ModelExtensions.GetDeclaredSymbol(compilation.GetSemanticModel(classSyntax.SyntaxTree), classSyntax);

            var method = classSymbol.GetMembers().OfType<IMethodSymbol>().Single();
            var result = method.Parameters.Single().Type.GetTypeName();

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("System.String", "string")]
        [InlineData("System.Byte", "byte")]
        [InlineData("System.SByte", "sbyte")]
        [InlineData("System.Char", "char")]
        [InlineData("System.Int16", "short")]
        [InlineData("System.Int32", "int")]
        [InlineData("System.Int64", "long")]
        [InlineData("System.UInt16", "ushort")]
        [InlineData("System.UInt32", "uint")]
        [InlineData("System.UInt64", "ulong")]
        [InlineData("System.Boolean", "bool")]
        [InlineData("System.Decimal", "decimal")]
        [InlineData("System.Single", "float")]
        [InlineData("System.Double", "double")]
        [InlineData("System.Object", "object")]
        [InlineData("System.DateTime", "System.DateTime")]
        [InlineData("System.DateTimeOffset", "System.DateTimeOffset")]
        public void GetSimpleTypeName(string type, string expected)
        {
            var compilation = CreateCompilation(@$"
public class TypeHolder
{{
    public TypeHolder({type} type)
    {{
    }}
}}");

            var classSyntax = compilation.SyntaxTrees.SelectMany(st => st.GetRoot().DescendantNodes()).OfType<ClassDeclarationSyntax>().Single();
            var classSymbol = (ITypeSymbol)ModelExtensions.GetDeclaredSymbol(compilation.GetSemanticModel(classSyntax.SyntaxTree), classSyntax);

            var method = classSymbol.GetMembers().OfType<IMethodSymbol>().Single();
            var result = method.Parameters.Single().Type.GetSimpleTypeName();

            Assert.Equal(expected, result);
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
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
