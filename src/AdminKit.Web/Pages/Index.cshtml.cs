using AdminKit.Web.Models;
using AdminKit.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminKit.Web.Pages;

/// <summary>
/// A headline figure. <paramref name="Delta"/> is null when there is no honest
/// like-for-like comparison to draw, in which case the tile shows context instead
/// of a percentage.
/// </summary>
public sealed record Kpi(
    string Label,
    string Value,
    double? Delta,
    string Comparison,
    string Sparkline,
    bool HigherIsBetter,
    string? Detail = null);

public class IndexModel : PageModel
{
    private readonly IOrderStore _store;

    public IndexModel(IOrderStore store) => _store = store;

    public IReadOnlyList<Kpi> Kpis { get; private set; } = [];
    public IReadOnlyList<DonutSegment> StatusSegments { get; private set; } = [];
    public IReadOnlyList<(string Label, decimal Value, string Color)> StatusData { get; private set; } = [];
    public IReadOnlyList<BarMark> RevenueBars { get; private set; } = [];
    public IReadOnlyList<Order> Recent { get; private set; } = [];
    public int OpenOrders { get; private set; }

    public void OnGet()
    {
        var orders = _store.Orders;
        var today = orders.Max(o => o.PlacedOn);

        var current = orders.Where(o => o.PlacedOn > today.AddDays(-30)).ToList();
        var previous = orders.Where(o => o.PlacedOn <= today.AddDays(-30) && o.PlacedOn > today.AddDays(-60)).ToList();

        var open = orders.Where(o => o.Status is OrderStatus.Pending or OrderStatus.Processing).ToList();
        OpenOrders = open.Count;

        // An open-order count has no meaningful "same figure, previous period" to sit
        // beside it, so the tile reports how long the oldest one has been waiting.
        var oldestWait = open.Count == 0 ? 0 : today.DayNumber - open.Min(o => o.PlacedOn).DayNumber;

        Kpis =
        [
            new Kpi(
                "Orders",
                current.Count.ToString("N0"),
                Change(current.Count, previous.Count),
                $"{previous.Count:N0} in the prior 30 days",
                ChartMath.Sparkline(WeeklyCounts(orders, today), 180, 30),
                HigherIsBetter: true),

            new Kpi(
                "Revenue",
                current.Sum(o => o.Amount).ToString("C0"),
                Change((double)current.Sum(o => o.Amount), (double)previous.Sum(o => o.Amount)),
                $"{previous.Sum(o => o.Amount):C0} in the prior 30 days",
                ChartMath.Sparkline(WeeklyRevenue(orders, today), 180, 30),
                HigherIsBetter: true),

            new Kpi(
                "Average order",
                (current.Count == 0 ? 0 : current.Average(o => o.Amount)).ToString("C0"),
                Change(
                    current.Count == 0 ? 0 : (double)current.Average(o => o.Amount),
                    previous.Count == 0 ? 0 : (double)previous.Average(o => o.Amount)),
                "vs. prior 30 days",
                ChartMath.Sparkline(WeeklyAverage(orders, today), 180, 30),
                HigherIsBetter: true),

            new Kpi(
                "Awaiting action",
                OpenOrders.ToString("N0"),
                Delta: null,
                oldestWait == 0
                    ? "nothing is waiting"
                    : $"oldest has waited {oldestWait} day{(oldestWait == 1 ? "" : "s")}",
                string.Empty,
                HigherIsBetter: false,
                Detail: $"{open.Count(o => o.Status == OrderStatus.Pending)} pending "
                      + $"· {open.Count(o => o.Status == OrderStatus.Processing)} processing")
        ];

        // Scoped to the same 30 days as the rest of the page. Across all time this
        // would be ~80% completed - true, but it says nothing about work in hand.
        // Ordinal ramp: the pipeline runs light to dark, cancelled sits outside it in grey.
        StatusData =
        [
            ("Pending", current.Count(o => o.Status == OrderStatus.Pending), "var(--c-1)"),
            ("Processing", current.Count(o => o.Status == OrderStatus.Processing), "var(--c-2)"),
            ("Shipped", current.Count(o => o.Status == OrderStatus.Shipped), "var(--c-3)"),
            ("Completed", current.Count(o => o.Status == OrderStatus.Completed), "var(--c-4)"),
            ("Cancelled", current.Count(o => o.Status == OrderStatus.Cancelled), "var(--c-off)")
        ];

        StatusSegments = ChartMath.Donut(StatusData, cx: 110, cy: 110, outerRadius: 100, innerRadius: 66);

        var months = Enumerable.Range(0, 6)
            .Select(i => new DateOnly(today.Year, today.Month, 1).AddMonths(-5 + i))
            .Select(start => (
                Label: start.ToString("MMM"),
                Value: orders
                    .Where(o => o.PlacedOn.Year == start.Year && o.PlacedOn.Month == start.Month)
                    .Sum(o => o.Amount)))
            .ToList();

        RevenueBars = ChartMath.Bars(months, width: 460, height: 150);

        Recent = orders.OrderByDescending(o => o.PlacedOn).Take(5).ToList();
    }

    private static double Change(double now, double before)
    {
        if (before == 0) return now == 0 ? 0 : 100;
        return (now - before) / before * 100.0;
    }

    private static List<decimal> WeeklyCounts(IReadOnlyList<Order> orders, DateOnly today) =>
        Enumerable.Range(0, 8)
            .Select(i => (decimal)orders.Count(o =>
                o.PlacedOn > today.AddDays(-7 * (8 - i)) && o.PlacedOn <= today.AddDays(-7 * (7 - i))))
            .ToList();

    private static List<decimal> WeeklyAverage(IReadOnlyList<Order> orders, DateOnly today) =>
        Enumerable.Range(0, 8)
            .Select(i =>
            {
                var week = orders
                    .Where(o => o.PlacedOn > today.AddDays(-7 * (8 - i)) && o.PlacedOn <= today.AddDays(-7 * (7 - i)))
                    .ToList();
                return week.Count == 0 ? 0m : week.Average(o => o.Amount);
            })
            .ToList();

    private static List<decimal> WeeklyRevenue(IReadOnlyList<Order> orders, DateOnly today) =>
        Enumerable.Range(0, 8)
            .Select(i => orders
                .Where(o => o.PlacedOn > today.AddDays(-7 * (8 - i)) && o.PlacedOn <= today.AddDays(-7 * (7 - i)))
                .Sum(o => o.Amount))
            .ToList();
}
