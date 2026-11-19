using AdminKit.Web.Models;

namespace AdminKit.Web.Services;

public sealed record OrderEdit(
    string CustomerName,
    string CustomerEmail,
    DateOnly PlacedOn,
    decimal Amount,
    OrderStatus Status,
    string? Note);

public interface IOrderStore
{
    IReadOnlyList<Order> Orders { get; }
    IReadOnlyList<Customer> Customers { get; }

    OrderPage Query(OrderQuery query);
    Order? Find(int id);
    bool Update(int id, OrderEdit edit);
    bool SetStatus(int id, OrderStatus status);
}
