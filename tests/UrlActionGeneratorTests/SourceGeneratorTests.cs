using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using UrlActionGenerator;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace UrlActionGeneratorTests
{
    [UsesVerify]
    public class SourceGeneratorTests
    {
        private static readonly VerifySettings _settings;

        static SourceGeneratorTests()
        {
            _settings = new VerifySettings();
            _settings.UseDirectory("Verify");
        }

        [Fact]
        public Task Execute_MVC_Success()
        {
            return RunAndVerify(new[]
            {
                ("HomeController.cs", SourceText.From(@"
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
}")),
            });
        }

        [Fact]
        public Task Execute_MVC_KeywordParameter_Success()
        {
            return RunAndVerify(
                new[]
                {
                    ("HomeController.cs", SourceText.From(@"
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
}", Encoding.UTF8)),
                },
                null);
        }

        [Fact]
        public Task Execute_Pages_Success()
        {
            return RunAndVerify(new[]
                {
                    ("Pages/Feature/Page.cs", SourceText.From(@"
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
}")),
                },
                new AdditionalText[]
                {
                    new InMemoryAdditionalText("Pages/Feature/Page.cshtml", @"@page ""{id:int}""
@model AspNetCoreSamplePages.Pages.Feature.Page

<h1>Hello, World!</h1>
"),
                });
        }

        [Fact]
        public Task Execute_RazorViewsAssembly_DoesNothing()
        {
            return RunAndVerify(new[]
                {
                    ("Pages/Feature/Page.cs", SourceText.From(@"
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
", Encoding.UTF8)),
                },
                new AdditionalText[]
                {
                    new InMemoryAdditionalText("Pages/Feature/Page.cshtml", @"@page ""{id:int}""
@model AspNetCoreSamplePages.Pages.Feature.Page

<h1>Hello, World!</h1>
"),
                },
                "TestProject.Views");
        }

        [Fact]
        public Task Execute_ExcludedParameterTypeAttribute()
        {
            return RunAndVerify(
                new[]
                {
                    ("HomeController.cs", SourceText.From(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrlActionGenerator;
using TestCode;

[assembly: UrlActionGenerator.ExcludedParameterTypeAttribute(typeof(TestCode.Model))]
[assembly: UrlActionGenerator.ExcludedParameterType(typeof(TestCode.AnotherModel))]
[assembly: ExcludedParameterType(typeof(TestCode.ThirdModel))]
[assembly: global::UrlActionGenerator.ExcludedParameterType(typeof(FourthModel))]

namespace TestCode
{
    public class HomeController : Controller
    {
        public IActionResult Index(Model model, AnotherModel anotherModel, int param, ThirdModel thirdModel, System.Threading.CancellationToken cancellationToken, IFormFile file, FourthModel fourthModel, IFormFile[] files, System.Collections.Generic.List<IFormFile> files)
        {
            return View();
        }
    }

    public class Model {}
    public class AnotherModel {}
    public class ThirdModel {}
    public class FourthModel {}
}", Encoding.UTF8)),
                },
                null);
        }

        private static Task RunAndVerify(IEnumerable<(string Path, SourceText Source)> sources,
            IEnumerable<AdditionalText> additionalTexts = null, string assemblyName = null)
        {
            var compilation = CreateCompilation(sources, assemblyName);

            var generator = new SourceGenerator();

            var driver = (GeneratorDriver)CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, additionalTexts);

            driver = driver.RunGenerators(compilation);

            return Verifier.Verify(driver, _settings);
        }

        private static Compilation CreateCompilation(IEnumerable<(string Path, SourceText source)> sources,
            string assemblyName = null)
        {
            var syntaxTrees = sources
                .Select(x => CSharpSyntaxTree.ParseText(x.source, path: x.Path))
                .ToList();

            var compilation = CSharpCompilation.Create(assemblyName ?? "compilation",
                syntaxTrees,
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
                    MetadataReference.CreateFromFile(typeof(IFormFile).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }
    }
}
