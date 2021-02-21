using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class SourceGeneratorTests
    {
        [Fact]
        public void Execute_Success()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
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

            var generator = new SourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator) as GeneratorDriver;

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Empty(diagnostics);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            var diag = outputCompilation.GetDiagnostics().ToList();
            Assert.Empty(outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden));

            var result = driver.GetRunResult();

            var tree = Assert.Single(result.GeneratedTrees);
            Assert.Empty(result.Diagnostics);

            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
        public static UrlActions Actions(this IUrlHelper urlHelper)
            => new UrlActions(urlHelper);

        public class UrlActions
        {
            private readonly IUrlHelper urlHelper;
            public UrlActions(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

            public HomeControllerActions Home
                => new HomeControllerActions(urlHelper);

            public class HomeControllerActions
            {
                private readonly IUrlHelper urlHelper;
                public HomeControllerActions(IUrlHelper urlHelper)
                {
                    this.urlHelper = urlHelper;
                }

                public string Index()
                    => urlHelper.Action(""Index"", ""Home"", new { area = """" });
            }
        }

    }
}
", tree.ToString());
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
