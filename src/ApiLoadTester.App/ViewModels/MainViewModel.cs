using System.Threading;
using ApiLoadTester.Core.Configuration;
using ApiLoadTester.Core.Engine;
using ApiLoadTester.Core.Models;
using ApiLoadTester.Reporting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ApiLoadTester.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILoadTestEngine _engine = new LoadTestEngine();
    private CancellationTokenSource? _runCts;

    public RequestConfigViewModel RequestConfig { get; } = new();
    public CertificateConfigViewModel CertificateConfig { get; } = new();
    public LoadOptionsViewModel LoadOptions { get; } = new();
    public LiveResultsViewModel LiveResults { get; } = new();
    public RunHistoryViewModel History { get; } = new();

    [ObservableProperty]
    private string? _reportTitle = "API Load Test Report";

    [ObservableProperty]
    private string? _customerName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    private TestSummary? _lastSummary;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    private bool CanStart() => !IsRunning;
    private bool CanStop() => IsRunning;
    private bool CanExport() => LastSummary is not null;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        var validationError = RequestConfig.Validate() ?? CertificateConfig.Validate() ?? LoadOptions.Validate();
        if (validationError is not null)
        {
            StatusMessage = validationError;
            return;
        }

        var config = new TestConfiguration { TargetUrl = RequestConfig.TargetUrl.Trim() };
        RequestConfig.ApplyTo(config);
        LoadOptions.ApplyTo(config);
        config.Certificate = CertificateConfig.ToSource();
        config.ReportTitle = string.IsNullOrWhiteSpace(ReportTitle) ? "API Load Test Report" : ReportTitle;
        config.CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? null : CustomerName;

        LiveResults.Reset();
        _runCts = new CancellationTokenSource();
        IsRunning = true;
        StatusMessage = "Running...";

        var progress = new Progress<LiveStatsSnapshot>(LiveResults.OnSnapshot);

        try
        {
            var summary = await _engine.RunAsync(config, progress, _runCts.Token);
            LastSummary = summary;
            History.Add(new TestRunHistoryItem { Summary = summary });
            StatusMessage = $"Finished: {summary.TotalRequests:N0} requests, {summary.SuccessRate:P1} success, {summary.OverallRequestsPerSecond:0.#} req/s.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Test failed to start: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            _runCts?.Dispose();
            _runCts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _runCts?.Cancel();
        StatusMessage = "Stopping...";
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void ExportPdf()
    {
        if (LastSummary is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Export capacity report",
            Filter = "PDF report (*.pdf)|*.pdf",
            FileName = SuggestFileName(LastSummary, "pdf")
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            new PdfReportBuilder().Save(LastSummary, dialog.FileName);
            StatusMessage = $"Report saved to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save report: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void ExportCsv()
    {
        if (LastSummary is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Export raw results",
            Filter = "CSV file (*.csv)|*.csv",
            FileName = SuggestFileName(LastSummary, "csv")
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            CsvReportWriter.Write(LastSummary, dialog.FileName);
            StatusMessage = $"Raw results saved to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save CSV: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveScenario()
    {
        var dialog = new SaveFileDialog { Title = "Save scenario", Filter = "Scenario file (*.json)|*.json" };
        if (dialog.ShowDialog() != true) return;

        var config = new TestConfiguration { TargetUrl = RequestConfig.TargetUrl.Trim() };
        RequestConfig.ApplyTo(config);
        LoadOptions.ApplyTo(config);
        config.Certificate = CertificateConfig.ToSource();
        config.ReportTitle = ReportTitle;
        config.CustomerName = CustomerName;

        try
        {
            ScenarioSerializer.Save(config, dialog.FileName, CertificateConfig.RememberPassword);
            StatusMessage = $"Scenario saved to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save scenario: {ex.Message}";
        }
    }

    [RelayCommand]
    private void LoadScenario()
    {
        var dialog = new OpenFileDialog { Title = "Load scenario", Filter = "Scenario file (*.json)|*.json" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var config = ScenarioSerializer.Load(dialog.FileName);
            RequestConfig.LoadFrom(config);
            LoadOptions.LoadFrom(config);
            CertificateConfig.LoadFrom(config.Certificate, config.Certificate.Password);
            ReportTitle = config.ReportTitle;
            CustomerName = config.CustomerName;
            StatusMessage = $"Scenario loaded from {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load scenario: {ex.Message}";
        }
    }

    private static string SuggestFileName(TestSummary summary, string extension)
    {
        var host = Uri.TryCreate(summary.Configuration.TargetUrl, UriKind.Absolute, out var uri) ? uri.Host : "loadtest";
        return $"{host}-{summary.StartedAt.ToLocalTime():yyyyMMdd-HHmmss}.{extension}";
    }
}
