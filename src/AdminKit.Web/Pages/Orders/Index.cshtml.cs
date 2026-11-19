using System.Text;
using AdminKit.Web.Models;
using AdminKit.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminKit.Web.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly IOrderStore _store;

    public IndexModel(IOrderStore store) => _store = store;

    // Every filter lives in the query string: the table works without JavaScript,
    // the back button behaves, and any view can be pasted to a colleague.
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true, Name = "status")]
    public OrderStatus? Status { get; set; }

    [BindProperty(SupportsGet = true, Name = "sort")]
    public string Sort { get; set; } = "date_desc";

    // Named "p" because "page" is reserved by Razor Pages routing.
    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; } = 1;

    public OrderPage Result { get; private set; } = new([], 0, 1, 12);

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || Status is not null;

    public void OnGet() => Load();

    /// <summary>Exports exactly what the current filters show - not the whole table.</summary>
    public IActionResult OnGetExport()
    {
        var all = _store.Query(new OrderQuery(Search, Status, Sort, Page: 1, PageSize: int.MaxValue));

        var csv = new StringBuilder();
        csv.AppendLine("Reference,Customer,Email,Placed,Status,Items,Amount");

        foreach (var order in all.Items)
        {
            csv.AppendLine(string.Join(',',
                Escape(order.Reference),
                Escape(order.CustomerName),
                Escape(order.CustomerEmail),
                order.PlacedOn.ToString("yyyy-MM-dd"),
                order.Status,
                order.ItemCount,
                order.Amount.ToString("0.00")));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "orders.csv");
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    public IActionResult OnPostAdvance(int id)
    {
        var order = _store.Find(id);
        if (order is null) return NotFound();

        var next = NextStage(order.Status);
        if (next is null)
        {
            TempData["ToastTitle"] = "No change";
            TempData["Toast"] = $"{order.Reference} is already {order.Status.Label().ToLowerInvariant()}.";
        }
        else
        {
            _store.SetStatus(id, next.Value);
            TempData["ToastTitle"] = "Status updated";
            TempData["Toast"] = $"{order.Reference} moved to {next.Value.Label().ToLowerInvariant()}.";
        }

        return Redirect(CurrentUrl());
    }

    public static OrderStatus? NextStage(OrderStatus status) => status switch
    {
        OrderStatus.Pending => OrderStatus.Processing,
        OrderStatus.Processing => OrderStatus.Shipped,
        OrderStatus.Shipped => OrderStatus.Completed,
        _ => null
    };

    /// <summary>Link for a sortable column header - clicking the active column flips direction.</summary>
    public string SortUrl(string column)
    {
        var descending = $"{column}_desc";
        var ascending = $"{column}_asc";
        var next = Sort == descending ? ascending : descending;
        return BuildUrl(sort: next, page: 1);
    }

    public string SortState(string column) =>
        Sort == $"{column}_desc" ? "desc" : Sort == $"{column}_asc" ? "asc" : "none";

    public string PageUrl(int page) => BuildUrl(sort: Sort, page: page);

    /// <summary>Current filter state, re-attached to row-action posts so we can return here.</summary>
    public Dictionary<string, string> RouteValues()
    {
        var values = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Search)) values["q"] = Search;
        if (Status is not null) values["status"] = Status.ToString()!;
        if (Sort != "date_desc") values["sort"] = Sort;
        if (PageNumber > 1) values["p"] = PageNumber.ToString();
        return values;
    }

    public string WithoutSearch() => BuildUrl(sort: Sort, page: 1, search: null, status: Status, dropSearch: true);

    public string WithoutStatus() => BuildUrl(sort: Sort, page: 1, search: Search, status: null, dropStatus: true);

    public string ClearAll() => "/Orders";

    // After a row action we return to exactly the view the user was looking at.
    private string CurrentUrl() => BuildUrl(Sort, Math.Max(PageNumber, 1));

    private string BuildUrl(
        string sort,
        int page,
        string? search = null,
        OrderStatus? status = null,
        bool dropSearch = false,
        bool dropStatus = false)
    {
        search ??= dropSearch ? null : Search;
        status ??= dropStatus ? null : Status;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) parts.Add($"q={Uri.EscapeDataString(search)}");
        if (status is not null) parts.Add($"status={status}");
        if (sort != "date_desc") parts.Add($"sort={sort}");
        if (page > 1) parts.Add($"p={page}");

        return parts.Count == 0 ? "/Orders" : "/Orders?" + string.Join("&", parts);
    }

    private void Load() =>
        Result = _store.Query(new OrderQuery(Search, Status, Sort, PageNumber));
}
