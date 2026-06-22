namespace QaAutomation.Core.Targets;

public sealed record SaveTargetRequest(
    string? Name,
    string? StartingUrl,
    string? AllowedHost,
    TargetEnvironment? Environment,
    string? Description,
    bool IsEnabled = true);

public sealed record TargetResponse(
    Guid Id,
    string Name,
    string StartingUrl,
    string AllowedHost,
    TargetEnvironment Environment,
    string? Description,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record DashboardSummary(int TotalTargets, int EnabledTargets);
