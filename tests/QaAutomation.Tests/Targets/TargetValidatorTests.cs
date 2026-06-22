using QaAutomation.Core.Targets;

namespace QaAutomation.Tests.Targets;

public sealed class TargetValidatorTests
{
    [Fact]
    public void Validate_NormalizesAValidTarget()
    {
        var result = TargetValidator.Validate(new SaveTargetRequest("  Acme staging  ",
            "https://booking.example.com/start", "Example.COM.", TargetEnvironment.Staging,
            "  Booking flow  ", true));

        Assert.Equal("Acme staging", result.Name);
        Assert.Equal("example.com", result.AllowedHost);
        Assert.Equal("Booking flow", result.Description);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("https://user:secret@example.com")]
    public void Validate_RejectsUnsafeStartingUrls(string url)
    {
        var exception = Assert.Throws<DomainValidationException>(() => TargetValidator.Validate(
            new SaveTargetRequest("Acme", url, "example.com", TargetEnvironment.Development, null)));

        Assert.Contains("startingUrl", exception.Errors.Keys);
    }

    [Fact]
    public void Validate_RejectsAllowedHostOutsideStartingUrl()
    {
        var exception = Assert.Throws<DomainValidationException>(() => TargetValidator.Validate(
            new SaveTargetRequest("Acme", "https://example.com", "unrelated.test",
                TargetEnvironment.Production, null)));

        Assert.Contains("allowedHost", exception.Errors.Keys);
    }
}
