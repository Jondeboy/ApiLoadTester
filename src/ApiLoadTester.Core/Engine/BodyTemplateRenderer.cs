using System.Text.RegularExpressions;

namespace ApiLoadTester.Core.Engine;

/// <summary>Renders a request body template, substituting a small set of per-request tokens so
/// concurrent requests can carry unique payloads (e.g. unique idempotency keys).</summary>
public static partial class BodyTemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*(guid|seq|timestamp)\s*\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex TokenPattern();

    public static string? Render(string? template, long sequenceNumber)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return TokenPattern().Replace(template, match =>
        {
            var token = match.Groups[1].Value.ToLowerInvariant();
            return token switch
            {
                "guid" => Guid.NewGuid().ToString(),
                "seq" => sequenceNumber.ToString(),
                "timestamp" => DateTimeOffset.UtcNow.ToString("O"),
                _ => match.Value
            };
        });
    }
}
