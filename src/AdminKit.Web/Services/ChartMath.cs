using System.Globalization;

namespace AdminKit.Web.Services;

public sealed record DonutSegment(string Label, decimal Value, double Share, string Path, string Color);

public sealed record BarMark(string Label, decimal Value, string Path, double LabelX, double LabelY);

/// <summary>
/// Builds SVG path data for the dashboard charts. Kept server-side so the markup
/// arrives complete - no chart library, no client-side layout pass, no flash of
/// an empty box before JavaScript runs.
/// </summary>
public static class ChartMath
{
    private static readonly CultureInfo Svg = CultureInfo.InvariantCulture;

    /// <param name="gapDegrees">Visual separation between wedges, on top of the 2px surface stroke.</param>
    public static IReadOnlyList<DonutSegment> Donut(
        IReadOnlyList<(string Label, decimal Value, string Color)> data,
        double cx,
        double cy,
        double outerRadius,
        double innerRadius,
        double gapDegrees = 1.2)
    {
        var total = data.Sum(d => d.Value);
        if (total <= 0) return [];

        var segments = new List<DonutSegment>(data.Count);
        var cursor = -90.0;

        foreach (var (label, value, color) in data)
        {
            if (value <= 0) continue;

            var share = (double)(value / total);
            var sweep = share * 360.0;

            // Only inset by the gap when the wedge is wide enough to survive it.
            var inset = sweep > gapDegrees * 3 ? gapDegrees / 2 : 0;
            var start = cursor + inset;
            var end = cursor + sweep - inset;

            segments.Add(new DonutSegment(
                label,
                value,
                share,
                Arc(cx, cy, outerRadius, innerRadius, start, end),
                color));

            cursor += sweep;
        }

        return segments;
    }

    private static string Arc(double cx, double cy, double rOuter, double rInner, double startDeg, double endDeg)
    {
        var largeArc = endDeg - startDeg > 180 ? 1 : 0;

        var (x1, y1) = Point(cx, cy, rOuter, startDeg);
        var (x2, y2) = Point(cx, cy, rOuter, endDeg);
        var (x3, y3) = Point(cx, cy, rInner, endDeg);
        var (x4, y4) = Point(cx, cy, rInner, startDeg);

        return string.Create(Svg, $"M {x1:0.##} {y1:0.##} " +
                                  $"A {rOuter:0.##} {rOuter:0.##} 0 {largeArc} 1 {x2:0.##} {y2:0.##} " +
                                  $"L {x3:0.##} {y3:0.##} " +
                                  $"A {rInner:0.##} {rInner:0.##} 0 {largeArc} 0 {x4:0.##} {y4:0.##} Z");
    }

    private static (double X, double Y) Point(double cx, double cy, double r, double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    /// <summary>Columns with 4px rounded tops, anchored flat on the baseline.</summary>
    public static IReadOnlyList<BarMark> Bars(
        IReadOnlyList<(string Label, decimal Value)> data,
        double width,
        double height,
        double gap = 10,
        double radius = 4)
    {
        if (data.Count == 0) return [];

        var max = data.Max(d => d.Value);
        if (max <= 0) max = 1;

        var slot = width / data.Count;
        var barWidth = Math.Max(6, slot - gap);
        var marks = new List<BarMark>(data.Count);

        foreach (var (item, index) in data.Select((d, i) => (d, i)))
        {
            var barHeight = Math.Max(2, (double)(item.Value / max) * height);
            var x = index * slot + (slot - barWidth) / 2;
            var y = height - barHeight;

            marks.Add(new BarMark(
                item.Label,
                item.Value,
                RoundedTop(x, y, barWidth, barHeight, Math.Min(radius, barWidth / 2)),
                x + barWidth / 2,
                height));
        }

        return marks;
    }

    private static string RoundedTop(double x, double y, double w, double h, double r)
    {
        if (h <= r) r = h / 2;

        return string.Create(Svg, $"M {x:0.##} {y + h:0.##} " +
                                  $"L {x:0.##} {y + r:0.##} " +
                                  $"Q {x:0.##} {y:0.##} {x + r:0.##} {y:0.##} " +
                                  $"L {x + w - r:0.##} {y:0.##} " +
                                  $"Q {x + w:0.##} {y:0.##} {x + w:0.##} {y + r:0.##} " +
                                  $"L {x + w:0.##} {y + h:0.##} Z");
    }

    /// <summary>Polyline points for a KPI sparkline, normalised into the given box.</summary>
    public static string Sparkline(IReadOnlyList<decimal> values, double width, double height)
    {
        if (values.Count < 2) return string.Empty;

        var min = values.Min();
        var max = values.Max();
        var span = max - min;
        var step = width / (values.Count - 1);

        var points = values.Select((value, index) =>
        {
            var ratio = span == 0 ? 0.5 : (double)((value - min) / span);
            var y = height - (ratio * (height - 4)) - 2;
            return string.Create(Svg, $"{index * step:0.##},{y:0.##}");
        });

        return string.Join(" ", points);
    }

    public static string N(double value) => value.ToString("0.##", Svg);
}
