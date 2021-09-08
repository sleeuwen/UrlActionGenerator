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
{/*
    public class MvcDiscovererTests
    {
        [Fact]
        public void DiscoverAreaControllerActions_NoControllerNoArea()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class Home
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Empty(areas);
        }

        [Fact]
        public void DiscoverAreaControllerActions_PrivateControllerNoArea()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    private class HomeController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Empty(areas);
        }

        [Fact]
        public void DiscoverAreaControllerActions_GenericControllerNoArea()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController<T>
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Empty(areas);
        }

        [Fact]
        public void DiscoverAreaControllerActions_AbstractControllerNoArea()
        {
            var compilation = CreateCompilation(@"
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public abstract class HomeController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}");

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Empty(areas);
        }

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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.Single().Parameters);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.Single().Parameters);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Equal(2, areas[0].Controllers.Count);

            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.Single().Parameters);

            Assert.Equal("Contact", areas[0].Controllers[1].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.Single().Parameters);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.Single().Parameters);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("Admin", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.Single().Parameters);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Equal(2, areas[0].Controllers[0].Actions.Single().Parameters.Count);

            Assert.Equal("search", areas[0].Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("string", areas[0].Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(areas[0].Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(areas[0].Controllers[0].Actions.Single().Parameters[0].DefaultValue);

            Assert.Equal("page", areas[0].Controllers[0].Actions.Single().Parameters[1].Name);
            Assert.Equal("int", areas[0].Controllers[0].Actions.Single().Parameters[1].Type);
            Assert.False(areas[0].Controllers[0].Actions.Single().Parameters[1].HasDefaultValue);
            Assert.Null(areas[0].Controllers[0].Actions.Single().Parameters[1].DefaultValue);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Equal(2, areas[0].Controllers[0].Actions.Single().Parameters.Count);

            Assert.Equal("search", areas[0].Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("string", areas[0].Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.True(areas[0].Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Equal("", areas[0].Controllers[0].Actions.Single().Parameters[0].DefaultValue);

            Assert.Equal("page", areas[0].Controllers[0].Actions.Single().Parameters[1].Name);
            Assert.Equal("int", areas[0].Controllers[0].Actions.Single().Parameters[1].Type);
            Assert.True(areas[0].Controllers[0].Actions.Single().Parameters[1].HasDefaultValue);
            Assert.Equal(1, areas[0].Controllers[0].Actions.Single().Parameters[1].DefaultValue);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Single(areas[0].Controllers[0].Actions.Single().Parameters);

            Assert.Equal("parameter", areas[0].Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>", areas[0].Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(areas[0].Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(areas[0].Controllers[0].Actions.Single().Parameters[0].DefaultValue);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Single(areas[0].Controllers[0].Actions.Single().Parameters);

            Assert.Equal("strings", areas[0].Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("string[]", areas[0].Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(areas[0].Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(areas[0].Controllers[0].Actions.Single().Parameters[0].DefaultValue);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Single(areas[0].Controllers[0].Actions);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.Single().Name);
            Assert.Single(areas[0].Controllers[0].Actions.Single().Parameters);

            Assert.Equal("param", areas[0].Controllers[0].Actions.Single().Parameters[0].Name);
            Assert.Equal("TestCode.HomeController.NestedClass", areas[0].Controllers[0].Actions.Single().Parameters[0].Type);
            Assert.False(areas[0].Controllers[0].Actions.Single().Parameters[0].HasDefaultValue);
            Assert.Null(areas[0].Controllers[0].Actions.Single().Parameters[0].DefaultValue);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Single(areas);
            Assert.Equal("", areas[0].Name);
            Assert.Single(areas[0].Controllers);
            Assert.Equal("Home", areas[0].Controllers[0].Name);
            Assert.Equal(2, areas[0].Controllers[0].Actions.Count);
            Assert.Equal("Index", areas[0].Controllers[0].Actions.First().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.First().Parameters);
            Assert.Equal("About", areas[0].Controllers[0].Actions.Skip(1).First().Name);
            Assert.Empty(areas[0].Controllers[0].Actions.Skip(1).First().Parameters);
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

            var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<TypeDeclarationSyntax>().ToList();
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(compilation, allTypes).ToList();

            Assert.Empty(areas);
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
    }*/
}
