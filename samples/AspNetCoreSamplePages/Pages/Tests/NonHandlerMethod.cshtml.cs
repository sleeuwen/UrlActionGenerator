using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCoreSamplePages.Pages.Tests
{
    public class NonHandlerMethod : PageModel
    {
        public void OnGet()
        {
        }

        [NonHandler]
        public void OnPostSomething(string str)
        {
        }
    }
}
