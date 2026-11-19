namespace AdminKit.Web.Models;

public sealed class Customer
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Company { get; init; }
    public required string City { get; init; }
    public DateOnly JoinedOn { get; init; }
    public bool IsActive { get; init; }
}
