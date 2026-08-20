# API Load Tester

A Windows desktop application for load-testing an HTTP(S) API that requires mutual TLS (client
certificate) authentication, producing a polished PDF capacity report suitable for customers asking
about server capacity.

Built with WPF on .NET 10. Constant-load testing only (fixed concurrency, run for a duration and/or
a request budget) - not ramp-up/spike/soak.

## Solution layout

```
ApiLoadTester.slnx
src/
  ApiLoadTester.Core/          Load-test engine, models, certificate loading, metrics (net10.0)
  ApiLoadTester.Reporting/     PDF + CSV report generation (net10.0, no WPF dependency)
  ApiLoadTester.App/           WPF UI, MVVM (net10.0-windows)
    Certificates/               Drop your .pfx files here (never committed - see its README)
tests/
  ApiLoadTester.Core.Tests/    xunit tests for the engine, incl. a hermetic loopback HTTP server
```

## Requirements

- Windows 10/11
- .NET 10 SDK (build/dev only - the published exe is self-contained and needs nothing installed)

## Build & test

```bash
dotnet build ApiLoadTester.slnx -c Release
dotnet test tests/ApiLoadTester.Core.Tests
```

## Run (dev)

```bash
dotnet run --project src/ApiLoadTester.App
```

## Publish a standalone exe

```bash
dotnet publish src/ApiLoadTester.App -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o publish/
```

This produces a single `ApiLoadTester.App.exe` (~70 MB, self-contained, no .NET runtime install
required on the target machine) plus a `Certificates/` folder next to it. **Code signing is not
done here** - sign the published exe with your organization's own certificate before distributing it
internally (`signtool.exe`).

## Using a client certificate

The Certificate tab supports two sources:

- **.pfx file** - drop it in `Certificates/` next to the exe (the file picker opens there by
  default), or browse anywhere. The password is never written to disk in plain text; an opt-in
  "remember password" checkbox stores it DPAPI-encrypted, tied to your Windows account and machine.
- **Windows certificate store** - select a certificate already imported into your Windows account's
  store (`CurrentUser\My`). Use this when your organization's policy prohibits exporting private
  keys to a file; the key never leaves the store.

## Enterprise / offline notes

- No runtime network calls other than the HTTP(S) requests you explicitly configure against your
  target API - the app itself makes no telemetry or "phone home" calls.
- All third-party dependencies are MIT-licensed (PdfSharp, OxyPlot, SkiaSharp,
  CommunityToolkit.Mvvm) - no revenue-gated or AGPL components.
- Reports render with Segoe UI / Consolas, which ship with every supported Windows version, so no
  fonts are embedded or redistributed.
- Scenario files (saved test configurations) are plain JSON and never contain a plaintext password.

## Report output

Each run can export:
- **PDF capacity report** - cover page, executive summary with KPIs, full test configuration &
  methodology (flags prominently if server certificate validation was disabled), results overview
  with latency percentiles, throughput/latency charts, error breakdown, and a methodology appendix.
  Sensitive headers (Authorization, Cookie, etc.) are automatically masked.
- **CSV** - full per-request raw data (timestamp, latency, status code, error info, response size)
  for your own analysis or a support engineer.
