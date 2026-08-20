using System.Collections.ObjectModel;
using ApiLoadTester.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiLoadTester.App.ViewModels;

public partial class RequestConfigViewModel : ObservableObject
{
    public static readonly string[] HttpMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];

    [ObservableProperty]
    private string _targetUrl = "";

    [ObservableProperty]
    private string _httpMethod = "GET";

    [ObservableProperty]
    private string? _bodyTemplate;

    [ObservableProperty]
    private string _contentType = "application/json";

    public ObservableCollection<HeaderEntryViewModel> Headers { get; } = new();

    [RelayCommand]
    private void AddHeader() => Headers.Add(new HeaderEntryViewModel());

    [RelayCommand]
    private void RemoveHeader(HeaderEntryViewModel? header)
    {
        if (header is not null)
            Headers.Remove(header);
    }

    public void ApplyTo(TestConfiguration config)
    {
        config.TargetUrl = TargetUrl.Trim();
        config.HttpMethod = HttpMethod;
        config.BodyTemplate = string.IsNullOrWhiteSpace(BodyTemplate) ? null : BodyTemplate;
        config.ContentType = ContentType;
        config.Headers = Headers
            .Where(h => !string.IsNullOrWhiteSpace(h.Key))
            .Select(h => new HeaderEntry { Key = h.Key.Trim(), Value = h.Value })
            .ToList();
    }

    public void LoadFrom(TestConfiguration config)
    {
        TargetUrl = config.TargetUrl;
        HttpMethod = config.HttpMethod;
        BodyTemplate = config.BodyTemplate;
        ContentType = config.ContentType;

        Headers.Clear();
        foreach (var h in config.Headers)
            Headers.Add(new HeaderEntryViewModel { Key = h.Key, Value = h.Value });
    }

    public string? Validate() =>
        string.IsNullOrWhiteSpace(TargetUrl) ? "Target URL is required."
        : !Uri.TryCreate(TargetUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")
            ? "Target URL must be a valid absolute http(s) URL."
        : null;
}
