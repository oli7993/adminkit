using AdminKit.Web.Models;
using AdminKit.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminKit.Web.Pages.Customers;

public sealed record CustomerRow(Customer Customer, int Orders, decimal Spent, DateOnly? LastOrder);

public class IndexModel : PageModel
{
    private readonly IOrderStore _store;

    public IndexModel(IOrderStore store) => _store = store;

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    public IReadOnlyList<CustomerRow> Rows { get; private set; } = [];

    public void OnGet()
    {
        var orders = _store.Orders;

        var rows = _store.Customers.Select(customer =>
        {
            var theirs = orders.Where(o => o.CustomerId == customer.Id).ToList();
            return new CustomerRow(
                customer,
                theirs.Count,
                theirs.Sum(o => o.Amount),
                theirs.Count == 0 ? null : theirs.Max(o => o.PlacedOn));
        });

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            rows = rows.Where(r =>
                r.Customer.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Customer.Company.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Customer.City.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Rows = rows.OrderByDescending(r => r.Spent).ToList();
    }
}
