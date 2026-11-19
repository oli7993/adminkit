using AdminKit.Web.Models;

namespace AdminKit.Web.Services;

/// <summary>
/// Stand-in for a real data layer. The seed uses a fixed Random seed so the dashboard
/// renders identical numbers on every run - the screenshots in the README stay accurate.
/// </summary>
public sealed class InMemoryOrderStore : IOrderStore
{
    private const int Seed = 20240115;

    private readonly List<Order> _orders;
    private readonly List<Customer> _customers;
    private readonly Lock _gate = new();

    public InMemoryOrderStore()
    {
        _customers = BuildCustomers();
        _orders = BuildOrders(_customers);
    }

    public IReadOnlyList<Order> Orders
    {
        get { lock (_gate) return _orders.ToList(); }
    }

    public IReadOnlyList<Customer> Customers => _customers;

    public Order? Find(int id)
    {
        lock (_gate) return _orders.FirstOrDefault(o => o.Id == id);
    }

    public OrderPage Query(OrderQuery query)
    {
        lock (_gate)
        {
            IEnumerable<Order> result = _orders;

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                result = result.Where(o =>
                    o.Reference.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    o.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    o.CustomerEmail.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Status is { } status)
            {
                result = result.Where(o => o.Status == status);
            }

            result = query.Sort switch
            {
                "date_asc" => result.OrderBy(o => o.PlacedOn).ThenBy(o => o.Id),
                "amount_desc" => result.OrderByDescending(o => o.Amount).ThenBy(o => o.Id),
                "amount_asc" => result.OrderBy(o => o.Amount).ThenBy(o => o.Id),
                "customer_asc" => result.OrderBy(o => o.CustomerName).ThenBy(o => o.Id),
                "customer_desc" => result.OrderByDescending(o => o.CustomerName).ThenBy(o => o.Id),
                "status_asc" => result.OrderBy(o => o.Status).ThenByDescending(o => o.PlacedOn),
                _ => result.OrderByDescending(o => o.PlacedOn).ThenByDescending(o => o.Id)
            };

            var matches = result.ToList();
            // The high ceiling lets the CSV export ask for everything in one page.
            var pageSize = query.PageSize <= 0 ? 12 : Math.Min(query.PageSize, 5000);
            var totalPages = matches.Count == 0 ? 1 : (int)Math.Ceiling(matches.Count / (double)pageSize);
            var page = Math.Clamp(query.Page, 1, totalPages);

            var items = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new OrderPage(items, matches.Count, page, pageSize);
        }
    }

    public bool Update(int id, OrderEdit edit)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order is null) return false;

            order.CustomerName = edit.CustomerName.Trim();
            order.CustomerEmail = edit.CustomerEmail.Trim();
            order.PlacedOn = edit.PlacedOn;
            order.Amount = edit.Amount;
            order.Status = edit.Status;
            order.Note = string.IsNullOrWhiteSpace(edit.Note) ? null : edit.Note.Trim();
            return true;
        }
    }

    public bool SetStatus(int id, OrderStatus status)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order is null) return false;

            order.Status = status;
            return true;
        }
    }

    private static List<Customer> BuildCustomers()
    {
        var rows = new (string Name, string Company, string City)[]
        {
            ("Amara Okafor", "Northwind Supply", "Manchester"),
            ("Tobias Lund", "Lund & Vik", "Oslo"),
            ("Priya Raman", "Meridian Labs", "Bristol"),
            ("Caleb Fenwick", "Fenwick Retail", "Leeds"),
            ("Sofia Marchetti", "Marchetti Foods", "Milan"),
            ("Daniel Ashby", "Ashby Logistics", "Dublin"),
            ("Hana Suzuki", "Kite Interiors", "Rotterdam"),
            ("Owen Brennan", "Brennan Tools", "Cork"),
            ("Lucia Ferreira", "Atlas Textiles", "Porto"),
            ("Marcus Webb", "Webb Instruments", "Sheffield"),
            ("Ines Duarte", "Duarte Ceramics", "Lisbon"),
            ("Noah Kristensen", "Harbour Print", "Aarhus"),
            ("Zara Haddad", "Cedar Organics", "Lyon"),
            ("Felix Brandt", "Brandt Werke", "Hamburg"),
            ("Eliza Monroe", "Monroe Stationery", "Glasgow"),
            ("Rohan Mehta", "Signal Components", "Birmingham"),
            ("Agnes Thorpe", "Thorpe Bakery", "York"),
            ("Viktor Novak", "Novak Optics", "Brno")
        };

        var random = new Random(Seed);
        var today = new DateOnly(2026, 11, 1);

        return rows.Select((row, index) => new Customer
        {
            Id = index + 1,
            Name = row.Name,
            Email = $"{row.Name.Split(' ')[0].ToLowerInvariant()}@{Slugify(row.Company)}.com",
            Company = row.Company,
            City = row.City,
            JoinedOn = today.AddDays(-random.Next(60, 900)),
            IsActive = random.Next(10) > 1
        }).ToList();
    }

    private static List<Order> BuildOrders(List<Customer> customers)
    {
        var random = new Random(Seed);
        var today = new DateOnly(2026, 11, 18);

        // Order volume is roughly flat across the period and the status follows from
        // the order's age, the way it would in a real pipeline. Modelling it this way
        // rather than picking a status at random keeps the period-on-period deltas on
        // the dashboard believable, and still leaves every status filter with rows.
        const int days = 200;
        const int count = 400;

        var orders = new List<Order>(count);

        for (var i = 0; i < count; i++)
        {
            var customer = customers[random.Next(customers.Count)];

            // Evenly spaced rather than randomly drawn: a random draw over 200 days
            // leaves the three short-lived stages at the mercy of clustering, and a
            // demo whose "Pending" filter shows one row is not demonstrating anything.
            // The small jitter keeps period-on-period figures from landing on a
            // suspiciously exact 0% while leaving each stage's share intact.
            var age = Math.Clamp((int)(i * (days + 1.0) / count) + random.Next(-2, 3), 0, days);

            var status = random.NextDouble() < 0.07
                ? OrderStatus.Cancelled
                : age switch
                {
                    // Roughly a fortnight from order to delivered.
                    <= 3 => OrderStatus.Pending,
                    <= 7 => OrderStatus.Processing,
                    <= 14 => OrderStatus.Shipped,
                    _ => OrderStatus.Completed
                };

            orders.Add(new Order
            {
                Id = i + 1,
                Reference = $"ORD-{2600 + i + 1}",
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerEmail = customer.Email,
                PlacedOn = today.AddDays(-age),
                Amount = Math.Round((decimal)(random.NextDouble() * 4200 + 85), 2),
                Status = status,
                ItemCount = random.Next(1, 14),
                Note = random.Next(5) == 0 ? "Customer asked for split delivery." : null
            });
        }

        return orders.OrderByDescending(o => o.PlacedOn).ToList();
    }

    private static string Slugify(string value) =>
        new string(value.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
}
