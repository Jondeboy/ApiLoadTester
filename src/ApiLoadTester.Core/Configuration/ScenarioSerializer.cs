using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Core.Configuration;

/// <summary>Saves/loads a TestConfiguration as a scenario JSON file. The certificate password is
/// excluded by default (safer default) and only round-tripped when the caller explicitly opts in
/// via rememberPassword, in which case it is DPAPI-encrypted - see PasswordProtector.</summary>
public static class ScenarioSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void Save(TestConfiguration config, string path, bool rememberPassword)
    {
        var dto = new ScenarioDto
        {
            TargetUrl = config.TargetUrl,
            HttpMethod = config.HttpMethod,
            Headers = config.Headers,
            BodyTemplate = config.BodyTemplate,
            ContentType = config.ContentType,
            CertificateKind = config.Certificate.Kind,
            PfxFilePath = config.Certificate.PfxFilePath,
            StoreThumbprintOrSubject = config.Certificate.StoreThumbprintOrSubject,
            EncryptedPassword = rememberPassword && config.Certificate.Password is { Length: > 0 } pwd
                ? PasswordProtector.Protect(pwd)
                : null,
            MinimumTlsProtocol = config.Tls.MinimumProtocol.ToString(),
            ValidateServerCertificate = config.Tls.ValidateServerCertificate,
            ExtraTrustedCaPath = config.Tls.ExtraTrustedCaPath,
            Concurrency = config.Concurrency,
            DurationSeconds = config.Duration?.TotalSeconds,
            MaxRequestCount = config.MaxRequestCount,
            RequestTimeoutSeconds = config.RequestTimeout.TotalSeconds,
            RampInDelayMs = config.RampInDelay.TotalMilliseconds,
            ReportTitle = config.ReportTitle,
            CustomerName = config.CustomerName
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static TestConfiguration Load(string path)
    {
        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<ScenarioDto>(json, JsonOptions)
            ?? throw new InvalidDataException("Scenario file could not be parsed.");

        SecureString? password = null;
        if (!string.IsNullOrEmpty(dto.EncryptedPassword))
        {
            try
            {
                password = PasswordProtector.Unprotect(dto.EncryptedPassword);
            }
            catch (CryptographicException)
            {
                // Written by a different Windows account/machine - fall through with no password;
                // the UI will prompt the user to re-enter it rather than failing the whole load.
            }
        }

        return new TestConfiguration
        {
            TargetUrl = dto.TargetUrl,
            HttpMethod = dto.HttpMethod,
            Headers = dto.Headers,
            BodyTemplate = dto.BodyTemplate,
            ContentType = dto.ContentType,
            Certificate = new CertificateSource
            {
                Kind = dto.CertificateKind,
                PfxFilePath = dto.PfxFilePath,
                StoreThumbprintOrSubject = dto.StoreThumbprintOrSubject,
                Password = password
            },
            Tls = new TlsOptions
            {
                MinimumProtocol = Enum.Parse<SslProtocols>(dto.MinimumTlsProtocol),
                ValidateServerCertificate = dto.ValidateServerCertificate,
                ExtraTrustedCaPath = dto.ExtraTrustedCaPath
            },
            Concurrency = dto.Concurrency,
            Duration = dto.DurationSeconds is { } s ? TimeSpan.FromSeconds(s) : null,
            MaxRequestCount = dto.MaxRequestCount,
            RequestTimeout = TimeSpan.FromSeconds(dto.RequestTimeoutSeconds),
            RampInDelay = TimeSpan.FromMilliseconds(dto.RampInDelayMs),
            ReportTitle = dto.ReportTitle,
            CustomerName = dto.CustomerName
        };
    }
}
