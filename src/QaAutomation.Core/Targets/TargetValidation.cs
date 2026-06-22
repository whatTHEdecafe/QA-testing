namespace QaAutomation.Core.Targets;

public sealed record ValidatedTarget(
    string Name,
    string StartingUrl,
    string AllowedHost,
    TargetEnvironment Environment,
    string? Description,
    bool IsEnabled);

public sealed class DomainValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public static class TargetValidator
{
    public static ValidatedTarget Validate(SaveTargetRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var name = request.Name?.Trim() ?? string.Empty;
        var startingUrl = request.StartingUrl?.Trim() ?? string.Empty;
        var allowedHost = request.AllowedHost?.Trim().TrimEnd('.') ?? string.Empty;
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        AddLengthError(errors, "name", name, 2, 120, "Customer or deployment name");
        if (startingUrl.Length > 2048)
            Add(errors, "startingUrl", "Starting URL cannot exceed 2,048 characters.");
        if (allowedHost.Length > 253)
            Add(errors, "allowedHost", "Allowed host cannot exceed 253 characters.");
        if (description?.Length > 1000)
            Add(errors, "description", "Description or notes cannot exceed 1,000 characters.");

        Uri? uri = null;
        if (!Uri.TryCreate(startingUrl, UriKind.Absolute, out uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            Add(errors, "startingUrl", "Starting URL must be a complete HTTP or HTTPS URL.");
        }
        else if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            Add(errors, "startingUrl", "Starting URL cannot contain a username or password.");
        }

        if (string.IsNullOrWhiteSpace(allowedHost) || allowedHost.Contains('/') || allowedHost.Contains(':') ||
            Uri.CheckHostName(allowedHost) == UriHostNameType.Unknown)
        {
            Add(errors, "allowedHost", "Allowed host must be a valid host name without a scheme, port, or path.");
        }
        else if (uri is not null && !HostIsAllowed(uri.Host, allowedHost))
        {
            Add(errors, "allowedHost", "Allowed host must match the starting URL host or one of its parent domains.");
        }

        if (request.Environment is null || !Enum.IsDefined(request.Environment.Value))
            Add(errors, "environment", "Select a valid environment.");

        if (errors.Count > 0)
            throw new DomainValidationException(errors.ToDictionary(x => x.Key, x => x.Value.ToArray()));

        return new ValidatedTarget(name, uri!.AbsoluteUri, allowedHost.ToLowerInvariant(),
            request.Environment!.Value, description, request.IsEnabled);
    }

    private static bool HostIsAllowed(string host, string allowedHost) =>
        host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith($".{allowedHost}", StringComparison.OrdinalIgnoreCase);

    private static void AddLengthError(Dictionary<string, List<string>> errors, string key, string value,
        int minimum, int maximum, string label)
    {
        if (value.Length < minimum || value.Length > maximum)
            Add(errors, key, $"{label} must be between {minimum} and {maximum} characters.");
    }

    private static void Add(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages)) errors[key] = messages = [];
        messages.Add(message);
    }
}
