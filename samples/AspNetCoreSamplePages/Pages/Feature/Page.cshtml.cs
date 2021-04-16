using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSamplePages.Pages.Feature
{
    public class Page : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string GetProperty { get; set; }

        [BindProperty]
        public string PostProperty { get; set; }

        [FromQuery]
        public string QueryParameter { get; set; }

        public void OnGet(int page, int pageSize)
        {
        }
    }
}
