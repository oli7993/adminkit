namespace AdminKit.Web.Models;

/// <summary>
/// Everything the orders table can be narrowed by. Mirrors the query string so the
/// table works with JavaScript disabled and every view is a shareable URL.
/// </summary>
public sealed record OrderQuery(
    string? Search = null,
    OrderStatus? Status = null,
    string Sort = "date_desc",
    int Page = 1,
    int PageSize = 12);

public sealed record OrderPage(
    IReadOnlyList<Order> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstRow => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastRow => Math.Min(Page * PageSize, TotalCount);
}
