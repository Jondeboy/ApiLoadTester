using ApiLoadTester.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace ApiLoadTester.App.ViewModels;

public partial class LiveResultsViewModel : ObservableObject
{
    private static readonly OxyColor PrimaryColor = OxyColor.FromRgb(0x2E, 0x5C, 0xE6);
    private static readonly OxyColor ErrorColor = OxyColor.FromRgb(0xD6, 0x33, 0x3C);

    private LineSeries _throughputSeries = null!;
    private LineSeries _latencyAvgSeries = null!;
    private LineSeries _latencyP95Series = null!;

    public PlotModel ThroughputModel { get; private set; } = null!;
    public PlotModel LatencyModel { get; private set; } = null!;

    [ObservableProperty] private TimeSpan _elapsed;
    [ObservableProperty] private long _totalRequests;
    [ObservableProperty] private long _successCount;
    [ObservableProperty] private long _errorCount;
    [ObservableProperty] private double _currentRequestsPerSecond;
    [ObservableProperty] private double _overallRequestsPerSecond;
    [ObservableProperty] private double _minLatencyMs;
    [ObservableProperty] private double _avgLatencyMs;
    [ObservableProperty] private double _medianLatencyMs;
    [ObservableProperty] private double _p90LatencyMs;
    [ObservableProperty] private double _p95LatencyMs;
    [ObservableProperty] private double _p99LatencyMs;
    [ObservableProperty] private double _maxLatencyMs;

    public double SuccessRatePercent => TotalRequests == 0 ? 0 : 100.0 * SuccessCount / TotalRequests;

    public LiveResultsViewModel() => Reset();

    public void Reset()
    {
        ThroughputModel = BuildPlotModel("Throughput", "Requests / second");
        _throughputSeries = new LineSeries { Color = PrimaryColor, StrokeThickness = 2, MarkerType = MarkerType.None };
        ThroughputModel.Series.Add(_throughputSeries);

        LatencyModel = BuildPlotModel("Latency", "ms");
        _latencyAvgSeries = new LineSeries { Title = "Avg", Color = PrimaryColor, StrokeThickness = 2, MarkerType = MarkerType.None };
        _latencyP95Series = new LineSeries { Title = "P95", Color = ErrorColor, StrokeThickness = 2, MarkerType = MarkerType.None, LineStyle = LineStyle.Dash };
        LatencyModel.Series.Add(_latencyAvgSeries);
        LatencyModel.Series.Add(_latencyP95Series);
        LatencyModel.IsLegendVisible = true;
        LatencyModel.Legends.Add(new OxyPlot.Legends.Legend { LegendPosition = OxyPlot.Legends.LegendPosition.TopRight });

        OnPropertyChanged(nameof(ThroughputModel));
        OnPropertyChanged(nameof(LatencyModel));

        Elapsed = TimeSpan.Zero;
        TotalRequests = 0;
        SuccessCount = 0;
        ErrorCount = 0;
        CurrentRequestsPerSecond = 0;
        OverallRequestsPerSecond = 0;
        MinLatencyMs = AvgLatencyMs = MedianLatencyMs = P90LatencyMs = P95LatencyMs = P99LatencyMs = MaxLatencyMs = 0;
    }

    /// <summary>Called via IProgress&lt;LiveStatsSnapshot&gt;, which captures the WPF UI thread's
    /// SynchronizationContext at construction - this always runs on the UI thread already, no manual
    /// Dispatcher.Invoke needed.</summary>
    public void OnSnapshot(LiveStatsSnapshot snapshot)
    {
        Elapsed = snapshot.Elapsed;
        TotalRequests = snapshot.TotalRequests;
        SuccessCount = snapshot.SuccessCount;
        ErrorCount = snapshot.ErrorCount;
        CurrentRequestsPerSecond = snapshot.CurrentRequestsPerSecond;
        OverallRequestsPerSecond = snapshot.OverallRequestsPerSecond;
        MinLatencyMs = snapshot.Latency.MinMs;
        AvgLatencyMs = snapshot.Latency.AvgMs;
        MedianLatencyMs = snapshot.Latency.MedianMs;
        P90LatencyMs = snapshot.Latency.P90Ms;
        P95LatencyMs = snapshot.Latency.P95Ms;
        P99LatencyMs = snapshot.Latency.P99Ms;
        MaxLatencyMs = snapshot.Latency.MaxMs;
        OnPropertyChanged(nameof(SuccessRatePercent));

        var t = snapshot.Elapsed.TotalSeconds;
        _throughputSeries.Points.Add(new DataPoint(t, snapshot.CurrentRequestsPerSecond));
        _latencyAvgSeries.Points.Add(new DataPoint(t, snapshot.Latency.AvgMs));
        _latencyP95Series.Points.Add(new DataPoint(t, snapshot.Latency.P95Ms));

        ThroughputModel.InvalidatePlot(true);
        LatencyModel.InvalidatePlot(true);
    }

    private static PlotModel BuildPlotModel(string title, string yAxisTitle)
    {
        var model = new PlotModel { Title = title };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Elapsed (s)", Minimum = 0 });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = yAxisTitle, Minimum = 0, MinimumPadding = 0.05 });
        return model;
    }
}
