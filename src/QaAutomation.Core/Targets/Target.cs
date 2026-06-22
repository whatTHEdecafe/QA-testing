namespace QaAutomation.Core.Targets;

public sealed class Target
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartingUrl { get; set; } = string.Empty;
    public string AllowedHost { get; set; } = string.Empty;
    public TargetEnvironment Environment { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
