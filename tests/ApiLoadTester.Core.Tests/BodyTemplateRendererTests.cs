using ApiLoadTester.Core.Engine;

namespace ApiLoadTester.Core.Tests;

public class BodyTemplateRendererTests
{
    [Fact]
    public void Render_NullOrEmptyTemplate_ReturnsAsIs()
    {
        Assert.Null(BodyTemplateRenderer.Render(null, 1));
        Assert.Equal("", BodyTemplateRenderer.Render("", 1));
    }

    [Fact]
    public void Render_NoTokens_ReturnsUnchanged()
    {
        var result = BodyTemplateRenderer.Render("{\"static\": true}", 1);
        Assert.Equal("{\"static\": true}", result);
    }

    [Fact]
    public void Render_SeqToken_SubstitutesSequenceNumber()
    {
        var result = BodyTemplateRenderer.Render("{\"seq\": {{seq}}}", 42);
        Assert.Equal("{\"seq\": 42}", result);
    }

    [Fact]
    public void Render_GuidToken_ProducesDifferentValuesEachCall()
    {
        var first = BodyTemplateRenderer.Render("{{guid}}", 1);
        var second = BodyTemplateRenderer.Render("{{guid}}", 1);

        Assert.NotEqual(first, second);
        Assert.True(Guid.TryParse(first, out _));
    }

    [Fact]
    public void Render_TimestampToken_ProducesParsableIsoTimestamp()
    {
        var result = BodyTemplateRenderer.Render("{{timestamp}}", 1);
        Assert.True(DateTimeOffset.TryParse(result, out _));
    }

    [Fact]
    public void Render_MultipleTokensAndCaseInsensitivity_AllSubstituted()
    {
        var result = BodyTemplateRenderer.Render("{\"id\":\"{{GUID}}\",\"n\":{{Seq}},\"t\":\"{{timestamp}}\"}", 7);

        Assert.DoesNotContain("{{", result);
        Assert.Contains("\"n\":7", result);
    }
}
