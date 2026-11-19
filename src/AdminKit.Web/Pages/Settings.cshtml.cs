using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminKit.Web.Pages;

public class SettingsModel : PageModel
{
    [BindProperty]
    public SettingsInput Input { get; set; } = new();

    public sealed class SettingsInput
    {
        [Required(ErrorMessage = "Enter a display name.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Use between 2 and 60 characters.")]
        public string DisplayName { get; set; } = "Dana Albu";

        [Required(ErrorMessage = "Enter an email address.")]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        public string Email { get; set; } = "dana@northwindsupply.com";

        public string TimeZone { get; set; } = "Europe/London";

        public string DateFormat { get; set; } = "d MMM yyyy";

        [Range(1, 200, ErrorMessage = "Pick a page size between 1 and 200.")]
        public int RowsPerPage { get; set; } = 12;

        public bool EmailOnNewOrder { get; set; } = true;

        public bool WeeklyDigest { get; set; }
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();

        // No persistence in the demo - the point is the round trip and the confirmation.
        TempData["ToastTitle"] = "Settings saved";
        TempData["Toast"] = "Your preferences were updated.";
        return RedirectToPage();
    }

    public IEnumerable<string> ErrorsFor(string key) =>
        ModelState.TryGetValue($"Input.{key}", out var entry)
            ? entry.Errors.Select(e => e.ErrorMessage)
            : [];

    public bool HasError(string key) => ErrorsFor(key).Any();
}
