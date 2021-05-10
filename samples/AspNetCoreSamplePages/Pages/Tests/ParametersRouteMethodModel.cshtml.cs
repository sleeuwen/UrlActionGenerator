using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSamplePages.Pages.Tests
{
    public class ParametersRouteMethodModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string QueryParameter { get; set; }

        public void OnGet(string str)
        {

        }
    }
}
