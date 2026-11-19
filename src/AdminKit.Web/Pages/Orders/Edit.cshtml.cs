using System.ComponentModel.DataAnnotations;
using AdminKit.Web.Models;
using AdminKit.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminKit.Web.Pages.Orders;

public class EditModel : PageModel
{
    private readonly IOrderStore _store;

    public EditModel(IOrderStore store) => _store = store;

    public Order? Order { get; private set; }

    [BindProperty]
    public OrderInput Input { get; set; } = new();

    public sealed class OrderInput : IValidatableObject
    {
        [Required(ErrorMessage = "Enter the customer name.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Use between 2 and 80 characters.")]
        [Display(Name = "Customer name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an email address.")]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pick the date the order was placed.")]
        [DataType(DataType.Date)]
        [Display(Name = "Placed on")]
        public DateOnly? PlacedOn { get; set; }

        [Required(ErrorMessage = "Enter the order total.")]
        [Range(0.01, 100000, ErrorMessage = "Enter an amount between 0.01 and 100,000.")]
        public decimal? Amount { get; set; }

        public OrderStatus Status { get; set; }

        [StringLength(280, ErrorMessage = "Keep the note under 280 characters.")]
        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (PlacedOn is { } date && date > DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult(
                    "An order cannot be placed in the future.",
                    [nameof(PlacedOn)]);
            }

            if (Status == OrderStatus.Completed && Amount is <= 0)
            {
                yield return new ValidationResult(
                    "A completed order needs a total above zero.",
                    [nameof(Amount)]);
            }
        }
    }

    public IActionResult OnGet(int id)
    {
        Order = _store.Find(id);
        if (Order is null) return NotFound();

        Input = new OrderInput
        {
            CustomerName = Order.CustomerName,
            CustomerEmail = Order.CustomerEmail,
            PlacedOn = Order.PlacedOn,
            Amount = Order.Amount,
            Status = Order.Status,
            Note = Order.Note
        };

        return Page();
    }

    public IActionResult OnPost(int id)
    {
        Order = _store.Find(id);
        if (Order is null) return NotFound();

        if (!ModelState.IsValid) return Page();

        _store.Update(id, new OrderEdit(
            Input.CustomerName,
            Input.CustomerEmail,
            Input.PlacedOn!.Value,
            Input.Amount!.Value,
            Input.Status,
            Input.Note));

        TempData["ToastTitle"] = "Order saved";
        TempData["Toast"] = $"{Order.Reference} was updated.";

        // Redirect after post so a refresh does not resubmit the form.
        return RedirectToPage("Index");
    }

    public IEnumerable<string> ErrorsFor(string key) =>
        ModelState.TryGetValue($"Input.{key}", out var entry)
            ? entry.Errors.Select(e => e.ErrorMessage)
            : [];

    public bool HasError(string key) => ErrorsFor(key).Any();
}
