using QaAutomation.Core.Scans;
using QaAutomation.Core.Targets;

namespace QaAutomation.Tests.Scans;

public sealed class ScanSafetyTests
{
    [Theory]
    [InlineData("moovez.ca")]
    [InlineData("www.moovez.ca")]
    [InlineData("booking.moovez.ca")]
    public void IsHostAllowed_AcceptsExactAndTrueSubdomains(string host) => Assert.True(ScanUrlSafety.IsHostAllowed(host, "moovez.ca"));

    [Theory]
    [InlineData("badmoovez.ca")]
    [InlineData("moovez.ca.example.com")]
    [InlineData("example.com")]
    public void IsHostAllowed_RejectsLookalikes(string host) => Assert.False(ScanUrlSafety.IsHostAllowed(host, "moovez.ca"));

    [Theory]
    [InlineData("ftp://moovez.ca")]
    [InlineData("moovez.ca")]
    [InlineData("https://user:secret@moovez.ca")]
    public void Validate_RejectsInvalidOrCredentialUrls(string url) => Assert.Throws<DomainValidationException>(() => ScanUrlSafety.Validate(url, "moovez.ca"));

    [Fact]
    public void SelectorRules_PrioritizeTheFirstUniqueCandidate()
    {
        var candidates = new[] { Candidate("TestId", false), Candidate("Role", true), Candidate("Css", true) };
        var preferred = SelectorPriority.MarkPreferred(candidates);
        Assert.Equal("Role", preferred!.SelectorType); Assert.Single(candidates, x => x.IsPreferred);
        Assert.True(SelectorPriority.For("TestId") < SelectorPriority.For("Role"));
    }

    private static SelectorCandidate Candidate(string type, bool unique) => new() { SelectorType=type, Priority=SelectorPriority.For(type), WasUnique=unique };
}
