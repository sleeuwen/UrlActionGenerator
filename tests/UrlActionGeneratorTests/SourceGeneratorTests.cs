using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
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
            outputCompilation.SyntaxTrees.Should().HaveCount(3);

            var nonHiddenDiagnostics = outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden);
            nonHiddenDiagnostics.Should().BeEmpty();

            var result = driver.GetRunResult();

            result.GeneratedTrees.Should().HaveCount(2);
            result.Diagnostics.Should().BeEmpty();

            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
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
                {
                    var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
                        new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
                    });
                    return urlHelper.Action(""Index"", ""Home"", __routeValues);
                }

            }

        }

    }
}
", result.GeneratedTrees[0].ToString(), false, true);

            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
    {
    }
}
", result.GeneratedTrees[1].ToString(), false, true);
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
            outputCompilation.SyntaxTrees.Should().HaveCount(3);

            var nonHiddenDiagnostics = outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden);
            nonHiddenDiagnostics.Should().BeEmpty();

            var result = driver.GetRunResult();

            result.GeneratedTrees.Should().HaveCount(2);
            result.Diagnostics.Should().BeEmpty();

            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
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
                {
                    var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
                        new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
                        new System.Collections.Generic.KeyValuePair<string, object>(""return"", @return),
                    });
                    return urlHelper.Action(""Index"", ""Home"", __routeValues);
                }

            }

        }

    }
}
", result.GeneratedTrees[0].ToString(), false, true);

            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
    {
    }
}
", result.GeneratedTrees[1].ToString(), false, true);
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
            outputCompilation.SyntaxTrees.Should().HaveCount(3);

            var nonHiddenDiagnostics = outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden);
            nonHiddenDiagnostics.Should().BeEmpty();

            var result = driver.GetRunResult();

            result.GeneratedTrees.Should().HaveCount(2);
            result.Diagnostics.Should().BeEmpty();

            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
    {
    }
}
", result.GeneratedTrees[0].ToString(), false, true);

            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
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
                {
                    var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
                        new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
                        new System.Collections.Generic.KeyValuePair<string, object>(""handler"", """"),
                        new System.Collections.Generic.KeyValuePair<string, object>(""id"", @id),
                        new System.Collections.Generic.KeyValuePair<string, object>(""page"", @page),
                        new System.Collections.Generic.KeyValuePair<string, object>(""pageSize"", @pageSize),
                        new System.Collections.Generic.KeyValuePair<string, object>(""getProperty"", @getProperty),
                        new System.Collections.Generic.KeyValuePair<string, object>(""namedGetProperty"", @namedGetProperty),
                        new System.Collections.Generic.KeyValuePair<string, object>(""queryParameter"", @queryParameter),
                        new System.Collections.Generic.KeyValuePair<string, object>(""namedQueryParameter"", @namedQueryParameter),
                    });
                    return urlHelper.Page(""/Feature/Page"", __routeValues);
                }

                public string Page(int @id, string @str, string @getProperty = default, string @namedGetProperty = default, string @queryParameter = default, string @namedQueryParameter = default)
                {
                    var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
                        new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
                        new System.Collections.Generic.KeyValuePair<string, object>(""handler"", """"),
                        new System.Collections.Generic.KeyValuePair<string, object>(""id"", @id),
                        new System.Collections.Generic.KeyValuePair<string, object>(""str"", @str),
                        new System.Collections.Generic.KeyValuePair<string, object>(""getProperty"", @getProperty),
                        new System.Collections.Generic.KeyValuePair<string, object>(""namedGetProperty"", @namedGetProperty),
                        new System.Collections.Generic.KeyValuePair<string, object>(""queryParameter"", @queryParameter),
                        new System.Collections.Generic.KeyValuePair<string, object>(""namedQueryParameter"", @namedQueryParameter),
                    });
                    return urlHelper.Page(""/Feature/Page"", __routeValues);
                }

                public PagePageHandlers PageHandlers
                    => new PagePageHandlers(urlHelper);

                public class PagePageHandlers
                {
                    private readonly IUrlHelper urlHelper;
                    public PagePageHandlers(IUrlHelper urlHelper)
                    {
                        this.urlHelper = urlHelper;
                    }

                    public string Handler(int @id, string @getProperty = default, string @namedGetProperty = default, string @queryParameter = default, string @namedQueryParameter = default)
                    {
                        var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
                            new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
                            new System.Collections.Generic.KeyValuePair<string, object>(""handler"", ""Handler""),
                            new System.Collections.Generic.KeyValuePair<string, object>(""id"", @id),
                            new System.Collections.Generic.KeyValuePair<string, object>(""getProperty"", @getProperty),
                            new System.Collections.Generic.KeyValuePair<string, object>(""namedGetProperty"", @namedGetProperty),
                            new System.Collections.Generic.KeyValuePair<string, object>(""queryParameter"", @queryParameter),
                            new System.Collections.Generic.KeyValuePair<string, object>(""namedQueryParameter"", @namedQueryParameter),
                        });
                        return urlHelper.Page(""/Feature/Page"", __routeValues);
                    }

                }

            }

        }

    }
}
", result.GeneratedTrees[1].ToString(), false, true);
        }

        [Fact]
        public void Execute_RazorViewsAssembly_DoesNothing()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[assembly: global::Microsoft.AspNetCore.Mvc.ApplicationParts.ProvideApplicationPartFactoryAttribute(""Microsoft.AspNetCore.Mvc.ApplicationParts.CompiledRazorAssemblyApplicationPartFactory, Microsoft.AspNetCore.Mvc.Razor"")]

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
}
", "AspNetCoreSamplePages.Views");

            var generator = new SourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator) as GeneratorDriver;
            driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("/AspNetCoreSamplePages/Pages/Feature/Page.cshtml", @"@page ""{id:int}""
@model AspNetCoreSamplePages.Pages.Feature.Page

<h1>Hello, World!</h1>
")));

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            diagnostics.Should().BeEmpty();
            outputCompilation.SyntaxTrees.Should().HaveCount(1);

            var hiddenDiagnostics = outputCompilation.GetDiagnostics().Where(x => x.Severity != DiagnosticSeverity.Hidden);
            hiddenDiagnostics.Should().BeEmpty();

            var result = driver.GetRunResult();

            result.GeneratedTrees.Should().BeEmpty();
            result.Diagnostics.Should().BeEmpty();
        }

        private static Compilation CreateCompilation(string source, string assemblyName = "compilation")
            => CSharpCompilation.Create(assemblyName,
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
                    MetadataReference.CreateFromFile(typeof(RouteValueDictionary).Assembly.Location),
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
