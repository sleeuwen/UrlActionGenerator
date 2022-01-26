using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetCoreSample.Controllers
{
    [Route("[controller]/[action]")]
    public class TestsController : Controller
    {
        public IActionResult Index() => View("Index");

        public IActionResult Parameters(string str, int i) => View("Index");

        [ActionName("DifferentActionName")]
        public IActionResult CustomActionName() => View("Index");

        [Route("{id:int}")]
        public IActionResult RouteParameterOnly() => View("Index");

        [Route("{id:int}")]
        public IActionResult RouteParameterAndMethodParameter(string str) => View("Index");

        [Route("{id}")]
        public IActionResult DuplicateRouteParameter(int id) => View("Index");

        [NonAction]
        public IActionResult NonAction() => View("Index");

        [Route("{id}")]
        public IActionResult ComplexParameter(int id, string @string, ComplexParameterModel model) => View("Index");

        [HttpPost]
        public IActionResult PostComplexParameter(ComplexParameterModel model) => View("Index");

        [HttpGet]
        public IActionResult ExcludedParameters(ExcludedComplexParameterModel model, IFormFile formFile, CancellationToken cancellationToken) => View("Index");

        [HttpGet]
        public IActionResult ExcludesFromAttributes([FromBody] string a, [FromHeader] string b, [FromForm] string c, [FromServices] ILogger logger) => View("Index");
    }

    public class ComplexParameterModel
    {
        public string String { get; set; }

        public NestedModel Model { get; set; }

        public class NestedModel
        {
            public int Int { get; set; }
        }
    }

    public class ExcludedComplexParameterModel
    {
        public string String { get; set; }
    }
}
