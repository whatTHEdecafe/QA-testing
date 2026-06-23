namespace QaAutomation.Core.Scans;

public static class ScanUrlSafety
{
    public static Uri Validate(string url, string allowedHost)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw Validation("URL must be a complete HTTP or HTTPS URL.");
        if (!string.IsNullOrEmpty(uri.UserInfo)) throw Validation("URLs containing credentials are not allowed.");
        if (!IsHostAllowed(uri.Host, allowedHost)) throw Validation("URL is outside the target's allowed host.");
        return uri;
    }

    public static bool IsHostAllowed(string host, string allowedHost)
    {
        var normalizedHost = host.Trim().TrimEnd('.');
        var normalizedAllowed = allowedHost.Trim().TrimEnd('.');
        return normalizedHost.Equals(normalizedAllowed, StringComparison.OrdinalIgnoreCase) ||
            normalizedHost.EndsWith('.' + normalizedAllowed, StringComparison.OrdinalIgnoreCase);
    }

    private static Exception Validation(string message) =>
        new Targets.DomainValidationException(new Dictionary<string, string[]> { ["target"] = [message] });
}

public static class SelectorPriority
{
    public static int For(string type) => type switch
    {
        "TestId" => 10, "Role" => 20, "Label" => 30, "Placeholder" => 40,
        "Id" => 50, "Name" => 60, "Text" => 70, "Css" => 80, "XPath" => 90, _ => 100
    };

    public static SelectorCandidate? MarkPreferred(IEnumerable<SelectorCandidate> candidates)
    {
        var materialized = candidates.ToList();
        foreach (var candidate in materialized) candidate.IsPreferred = false;
        var preferred = materialized.Where(x => x.WasUnique).OrderBy(x => x.Priority).FirstOrDefault();
        if (preferred is not null) preferred.IsPreferred = true;
        return preferred;
    }
}
