using System.Security.Authentication;
using ApiLoadTester.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ApiLoadTester.App.ViewModels;

public partial class LoadOptionsViewModel : ObservableObject
{
    public static readonly SslProtocols[] TlsProtocolOptions = [SslProtocols.Tls12, SslProtocols.Tls13];

    [ObservableProperty]
    private int _concurrency = 10;

    [ObservableProperty]
    private bool _useDuration = true;

    [ObservableProperty]
    private double _durationSeconds = 30;

    [ObservableProperty]
    private bool _useMaxRequestCount;

    [ObservableProperty]
    private int _maxRequestCount = 1000;

    [ObservableProperty]
    private double _requestTimeoutSeconds = 30;

    [ObservableProperty]
    private double _rampInDelayMs;

    [ObservableProperty]
    private SslProtocols _minimumTlsProtocol = SslProtocols.Tls12;

    [ObservableProperty]
    private bool _validateServerCertificate = true;

    [ObservableProperty]
    private string? _extraTrustedCaPath;

    [RelayCommand]
    private void BrowseExtraCa()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select extra trusted CA certificate",
            Filter = "Certificate files (*.cer;*.crt;*.pem)|*.cer;*.crt;*.pem|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
            ExtraTrustedCaPath = dialog.FileName;
    }

    public void ApplyTo(TestConfiguration config)
    {
        config.Concurrency = Concurrency;
        config.Duration = UseDuration ? TimeSpan.FromSeconds(DurationSeconds) : null;
        config.MaxRequestCount = UseMaxRequestCount ? MaxRequestCount : null;
        config.RequestTimeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
        config.RampInDelay = TimeSpan.FromMilliseconds(RampInDelayMs);
        config.Tls.MinimumProtocol = MinimumTlsProtocol;
        config.Tls.ValidateServerCertificate = ValidateServerCertificate;
        config.Tls.ExtraTrustedCaPath = string.IsNullOrWhiteSpace(ExtraTrustedCaPath) ? null : ExtraTrustedCaPath;
    }

    public void LoadFrom(TestConfiguration config)
    {
        Concurrency = config.Concurrency;
        UseDuration = config.Duration is not null;
        DurationSeconds = config.Duration?.TotalSeconds ?? 30;
        UseMaxRequestCount = config.MaxRequestCount is not null;
        MaxRequestCount = config.MaxRequestCount ?? 1000;
        RequestTimeoutSeconds = config.RequestTimeout.TotalSeconds;
        RampInDelayMs = config.RampInDelay.TotalMilliseconds;
        MinimumTlsProtocol = config.Tls.MinimumProtocol;
        ValidateServerCertificate = config.Tls.ValidateServerCertificate;
        ExtraTrustedCaPath = config.Tls.ExtraTrustedCaPath;
    }

    public string? Validate() =>
        Concurrency < 1 ? "Concurrency must be at least 1."
        : !UseDuration && !UseMaxRequestCount ? "Set a duration and/or a max request count."
        : UseDuration && DurationSeconds <= 0 ? "Duration must be greater than zero."
        : UseMaxRequestCount && MaxRequestCount < 1 ? "Max request count must be at least 1."
        : RequestTimeoutSeconds <= 0 ? "Request timeout must be greater than zero."
        : null;
}
