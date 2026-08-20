using ApiLoadTester.Core.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

namespace ApiLoadTester.Reporting;

/// <summary>Headlessly rasterizes report charts to PNG bytes via OxyPlot's SkiaSharp exporter - no
/// WPF visual tree or STA thread required, so report generation can run on a background thread.</summary>
public static class ChartImageRenderer
{
    public static byte[] RenderThroughputChart(TestSummary summary)
    {
        var buckets = TimeSeriesBuilder.BuildPerSecondBuckets(summary);
        var model = NewModel("Throughput Over Time", "Requests / second");

        var series = new LineSeries
        {
            Color = ChartTheme.Primary,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };
        foreach (var b in buckets)
            series.Points.Add(new DataPoint(b.SecondOffset, b.RequestCount));
        model.Series.Add(series);

        return Rasterize(model);
    }

    public static byte[] RenderLatencyChart(TestSummary summary)
    {
        var buckets = TimeSeriesBuilder.BuildPerSecondBuckets(summary);
        var model = NewModel("Latency Over Time", "Latency (ms)");

        var avg = new LineSeries { Title = "Avg", Color = ChartTheme.Primary, StrokeThickness = 2, MarkerType = MarkerType.None };
        var p95 = new LineSeries { Title = "P95", Color = ChartTheme.Error, StrokeThickness = 2, MarkerType = MarkerType.None, LineStyle = LineStyle.Dash };
        foreach (var b in buckets)
        {
            avg.Points.Add(new DataPoint(b.SecondOffset, b.AvgLatencyMs));
            p95.Points.Add(new DataPoint(b.SecondOffset, b.P95LatencyMs));
        }
        model.Series.Add(avg);
        model.Series.Add(p95);
        model.IsLegendVisible = true;
        model.Legends.Add(new OxyPlot.Legends.Legend { LegendPosition = OxyPlot.Legends.LegendPosition.TopRight });

        return Rasterize(model);
    }

    private static PlotModel NewModel(string title, string yAxisTitle)
    {
        var model = new PlotModel
        {
            Title = title,
            TextColor = ChartTheme.Text,
            PlotAreaBorderColor = ChartTheme.GridLine,
            Background = OxyColors.White
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Elapsed (s)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = ChartTheme.GridLine,
            TextColor = ChartTheme.Text,
            TitleColor = ChartTheme.Text
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = yAxisTitle,
            MinimumPadding = 0,
            AbsoluteMinimum = 0,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = ChartTheme.GridLine,
            TextColor = ChartTheme.Text,
            TitleColor = ChartTheme.Text
        });

        return model;
    }

    private static byte[] Rasterize(PlotModel model)
    {
        var exporter = new PngExporter
        {
            Width = ChartTheme.ChartPixelWidth,
            Height = ChartTheme.ChartPixelHeight,
            Dpi = 144
        };

        using var stream = new MemoryStream();
        exporter.Export(model, stream);
        return stream.ToArray();
    }
}
