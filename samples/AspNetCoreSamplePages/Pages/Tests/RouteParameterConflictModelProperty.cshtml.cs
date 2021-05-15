using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSamplePages.Pages.Tests
{
    public class RouteParameterConflictModelProperty : PageModel
    {
        [FromRoute]
        public int Id { get; set; }

        public void OnGet()
        {

        }
    }
}
