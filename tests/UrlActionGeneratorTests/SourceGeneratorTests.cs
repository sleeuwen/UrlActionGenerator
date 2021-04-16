using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class SourceGeneratorTests
    {
        [Fact]
        public void Execute_MVC_Success()
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

            diagnostics.Should().BeEmpty();
            outputCompilation.SyntaxTrees.Should().HaveCount(2);

            var hiddenDiagnostics = outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden);
            hiddenDiagnostics.Should().BeEmpty();

            var result = driver.GetRunResult();

            result.GeneratedTrees.Should().ContainSingle();
            result.Diagnostics.Should().BeEmpty();

            var tree = result.GeneratedTrees.Single();

            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlHelperExtensions
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
", tree.ToString(), false, true);
        }

        [Fact]
        public void Execute_MVC_KeywordParameter_Success()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index(string @return)
        {
            return View(new { value = @return });
        }
    }
}");

            var generator = new SourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator) as GeneratorDriver;

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            diagnostics.Should().BeEmpty();
            outputCompilation.SyntaxTrees.Should().HaveCount(2);

            var hiddenDiagnostics = outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden);
            hiddenDiagnostics.Should().BeEmpty();

            var result = driver.GetRunResult();

            result.GeneratedTrees.Should().ContainSingle();
            result.Diagnostics.Should().BeEmpty();

            var tree = result.GeneratedTrees.Single();

            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlHelperExtensions
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

                public string Index(string @return)
                    => urlHelper.Action(""Index"", ""Home"", new { area = """", @return });
            }
        }

    }
}
", tree.ToString(), false, true);
        }

        [Fact]
        public void Execute_Pages_Success()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSamplePages.Pages.Feature
{
    public class Page : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string GetProperty { get; set; }

        [BindProperty(SupportsGet = true, Name = ""NamedGetProperty"")]
        public string GetProperty2 { get; set; }

        [BindProperty]
        public string PostProperty { get; set; }

        [FromQuery]
        public string QueryParameter { get; set; }

        [FromQuery(Name = ""NamedQueryParameter"")]
        public string QueryParameter2 { get; set; }

        public void OnGet(int page, int pageSize)
        {
        }

        public async Task OnPostAsync(string str)
        {
            await Task.Delay(1);
        }

        public async Task OnGetHandlerAsync()
        {
            await Task.Delay(1);
        }
    }
}");

            var generator = new SourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator) as GeneratorDriver;
            driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("/AspNetCoreSamplePages/Pages/Feature/Page.cshtml", @"@page ""{id:int}""
@model AspNetCoreSamplePages.Pages.Feature.Page

<h1>Hello, World!</h1>
")));

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            diagnostics.Should().BeEmpty();
            outputCompilation.SyntaxTrees.Should().HaveCount(2);

            var hiddenDiagnostics = outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden);
            hiddenDiagnostics.Should().BeEmpty();

            var result = driver.GetRunResult();

            result.GeneratedTrees.Should().ContainSingle();
            result.Diagnostics.Should().BeEmpty();

            var tree = result.GeneratedTrees.Single();
            Console.WriteLine(tree.ToString());

            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlHelperExtensions
    {
        public static UrlPages Pages(this IUrlHelper urlHelper)
            => new UrlPages(urlHelper);

        public class UrlPages
        {
            private readonly IUrlHelper urlHelper;
            public UrlPages(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

            public FeaturePagesFolder Feature
                => new FeaturePagesFolder(urlHelper);

            public class FeaturePagesFolder
            {
                private readonly IUrlHelper urlHelper;
                public FeaturePagesFolder(IUrlHelper urlHelper)
                {
                    this.urlHelper = urlHelper;
                }

                public string Page(int @id, int @page, int @pageSize, string @getProperty = default, string @namedGetProperty = default, string @queryParameter = default, string @namedQueryParameter = default)
                    => urlHelper.Page(""/Feature/Page"", new { area = """", pageHandler = """", @id, @page, @pageSize, @getProperty, @namedGetProperty, @queryParameter, @namedQueryParameter });

                public string Page(int @id, string @str, string @getProperty = default, string @namedGetProperty = default, string @queryParameter = default, string @namedQueryParameter = default)
                    => urlHelper.Page(""/Feature/Page"", new { area = """", pageHandler = """", @id, @str, @getProperty, @namedGetProperty, @queryParameter, @namedQueryParameter });

                public string PageHandler(int @id, string @getProperty = default, string @namedGetProperty = default, string @queryParameter = default, string @namedQueryParameter = default)
                    => urlHelper.Page(""/Feature/Page"", new { area = """", pageHandler = ""Handler"", @id, @getProperty, @namedGetProperty, @queryParameter, @namedQueryParameter });
            }
        }

    }
}
", tree.ToString(), false, true);
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

    internal class InMemoryAdditionalText : AdditionalText
    {
        private string _text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _text = text;
        }

        public override SourceText GetText(CancellationToken cancellationToken = new CancellationToken())
        {
            return SourceText.From(_text);
        }

        public override string Path { get; }
    }
}
