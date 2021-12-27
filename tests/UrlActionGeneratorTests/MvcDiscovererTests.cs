using System;
using System.Collections.Immutable;
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
    public class MvcDiscovererTests
    {
        [Fact]
        public void DiscoverAreaControllerActions_ImplicitControllerNoArea()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Empty(area.Controllers[0].Actions.Single().Parameters);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerNoArea()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Empty(area.Controllers[0].Actions.Single().Parameters);
        }

        [Fact]
        public void DiscoverAreaControllerActions_MultiControllerNoArea()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var classSyntaxes = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().ToList();
            var classSymbols = classSyntaxes.Select(classSyntax => compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax)).ToList();

            var generatorContext = new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty);
            var areas = classSymbols.Select(symbol => MvcDiscoverer.DiscoverAreaControllerActions(symbol, generatorContext));
            var area = MvcDiscoverer.CombineAreas(areas).Single();

            Assert.Equal("", area.Name);
            Assert.Equal(2, area.Controllers.Count);

            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Empty(area.Controllers[0].Actions.Single().Parameters);

            Assert.Equal("Contact", area.Controllers[1].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Empty(area.Controllers[0].Actions.Single().Parameters);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerNoAreaAsyncAction()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> IndexAsync()
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Empty(area.Controllers[0].Actions.Single().Parameters);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerArea()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    [Area(""Admin"")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("Admin", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Empty(area.Controllers[0].Actions.Single().Parameters);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerActionParameters()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index(string search, int page)
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Equal(2, area.Controllers[0].Actions.Single().Parameters.Count);

            Assert.Equal("search", area.Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("string", area.Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(area.Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(area.Controllers[0].Actions.Single().Parameters[0].DefaultValue);

            Assert.Equal("page", area.Controllers[0].Actions.Single().Parameters[1].Name);
            Assert.Equal("int", area.Controllers[0].Actions.Single().Parameters[1].Type);
            Assert.False(area.Controllers[0].Actions.Single().Parameters[1].HasDefaultValue);
            Assert.Null(area.Controllers[0].Actions.Single().Parameters[1].DefaultValue);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerActionParametersDefaultValue()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index(string search = """", int page = 1)
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Equal(2, area.Controllers[0].Actions.Single().Parameters.Count);

            Assert.Equal("search", area.Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("string", area.Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.True(area.Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Equal("", area.Controllers[0].Actions.Single().Parameters[0].DefaultValue);

            Assert.Equal("page", area.Controllers[0].Actions.Single().Parameters[1].Name);
            Assert.Equal("int", area.Controllers[0].Actions.Single().Parameters[1].Type);
            Assert.True(area.Controllers[0].Actions.Single().Parameters[1].HasDefaultValue);
            Assert.Equal(1, area.Controllers[0].Actions.Single().Parameters[1].DefaultValue);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerActionGenericParameter()
        {
            var compilation = CreateCompilation(@"
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index(List<Dictionary<string, string>> parameter)
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Single(area.Controllers[0].Actions.Single().Parameters);

            Assert.Equal("parameter", area.Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>", area.Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(area.Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(area.Controllers[0].Actions.Single().Parameters[0].DefaultValue);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerActionArrayParameter()
        {
            var compilation = CreateCompilation(@"
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index(string[] strings)
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Single(area.Controllers[0].Actions.Single().Parameters);

            Assert.Equal("strings", area.Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("string[]", area.Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(area.Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(area.Controllers[0].Actions.Single().Parameters[0].DefaultValue);
        }

        [Fact]
        public void DiscoverAreaControllerActions_NestedClassActionParameters()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public class NestedClass
        {
            public int Number { get; set; }
        }

        public IActionResult Index(NestedClass param)
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Single(area.Controllers[0].Actions);
            Assert.Equal("Index", area.Controllers[0].Actions.Single().Name);
            Assert.Single(area.Controllers[0].Actions.Single().Parameters);

            Assert.Equal("param", area.Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("TestCode.HomeController.NestedClass", area.Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(area.Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(area.Controllers[0].Actions.Single().Parameters[0].DefaultValue);
        }

        [Fact]
        public void DiscoverAreaControllerActions_PartialClassController()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public partial class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    public partial class HomeController
    {
        public IActionResult About()
        {
            return View();
        }
    }
}");

            var classSyntaxes = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().ToList();
            var classSymbols = classSyntaxes.Select(classSyntax => compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax)).ToList();

            var generatorContext = new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty);
            var areas = classSymbols.Select(symbol => MvcDiscoverer.DiscoverAreaControllerActions(symbol, generatorContext));
            var area = MvcDiscoverer.CombineAreas(areas).Single();

            Assert.Equal("", area.Name);
            Assert.Single(area.Controllers);
            Assert.Equal("Home", area.Controllers[0].Name);
            Assert.Equal(2, area.Controllers[0].Actions.Count);
            Assert.Equal("Index", area.Controllers[0].Actions.First().Name);
            Assert.Empty(area.Controllers[0].Actions.First().Parameters);
            Assert.Equal("About", area.Controllers[0].Actions.Skip(1).First().Name);
            Assert.Empty(area.Controllers[0].Actions.Skip(1).First().Parameters);
        }

        [Fact]
        public void DiscoverAreaControllerActions_SingleControllerNonAction()
        {
            var compilation = CreateCompilation(@"
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        [NonAction]
        public IActionResult Index(string[] strings)
        {
            return View();
        }
    }
}");

            var classSyntax = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            var classSymbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);

            var area = MvcDiscoverer.DiscoverAreaControllerActions(classSymbol, new GeneratorContext(compilation, ImmutableArray<ITypeSymbol>.Empty));

            Assert.Empty(area.Name);
            Assert.Empty(area.Controllers);
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
