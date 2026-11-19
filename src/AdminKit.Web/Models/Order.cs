namespace AdminKit.Web.Models;

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Completed,
    Cancelled
}

public sealed class Order
{
    public int Id { get; init; }
    public required string Reference { get; init; }
    public int CustomerId { get; init; }
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public DateOnly PlacedOn { get; set; }
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; }
    public int ItemCount { get; set; }
    public string? Note { get; set; }
}

public static class OrderStatusDisplay
{
    public static string Label(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.Processing => "Processing",
        OrderStatus.Shipped => "Shipped",
        OrderStatus.Completed => "Completed",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    /// <summary>Modifier suffix for the .badge CSS class.</summary>
    public static string Slug(this OrderStatus status) => status.ToString().ToLowerInvariant();
}
