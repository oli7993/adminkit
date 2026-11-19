using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminKit.Web.Pages;

/// <summary>Visual only - there is no authentication in this demo.</summary>
public class LoginModel : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnPost() => RedirectToPage("Index");
}
