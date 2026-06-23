namespace QaAutomation.Core.Scans;

public sealed record StartScanResponse(Guid Id, ScanStatus Status, string Stage);
public sealed record ScanSummaryResponse(Guid Id, Guid TargetId, string TargetName, ScanStatus Status, string Stage,
    DateTimeOffset RequestedAtUtc, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc, string StartingUrl,
    string? FinalUrl, string? PageTitle, int DetectedPageCount, int DetectedElementCount, int WarningCount,
    int ErrorCount, string? FailureSummary, bool CancellationRequested);
public sealed record SelectorResponse(Guid Id, string Type, string Value, int Priority, bool WasUnique, decimal Confidence, bool IsPreferred);
public sealed record ElementResponse(Guid Id, int DiscoveryOrder, string TagName, string? InputType, string? AccessibleRole,
    string? AccessibleName, string? VisibleText, string? AssociatedLabel, string? Placeholder, string? NameAttribute,
    string? HtmlId, string? TestId, ElementClassification Classification, bool IsActionable, bool IsVisible,
    bool IsEnabled, bool IsPotentiallyDestructive, bool HasCrop, string? ScreenshotError, IReadOnlyList<SelectorResponse> Selectors);
public sealed record PageResponse(Guid Id, string OriginalUrl, string FinalUrl, string Route, string? Title,
    string? MainHeading, string DisplayName, int DiscoveryOrder, bool HasScreenshot, bool HasThumbnail,
    int? ScreenshotWidth, int? ScreenshotHeight, IReadOnlyList<ElementResponse> Elements);
public sealed record DiagnosticResponse(Guid Id, DiagnosticCategory Category, DiagnosticSeverity Severity, string Message,
    string? Url, string? Method, int? StatusCode, DateTimeOffset CreatedAtUtc);
public sealed record ScanDetailsResponse(ScanSummaryResponse Summary, IReadOnlyList<PageResponse> Pages,
    IReadOnlyList<DiagnosticResponse> Diagnostics);
public sealed record ScanDashboardSummary(int TotalScans, ScanStatus? LatestStatus, DateTimeOffset? LatestRequestedAtUtc);
public enum ScanCancellationOutcome { CancellationRequested, AlreadyCancelled, NotFound, NotCancellable }
public sealed record CancelScanResponse(ScanCancellationOutcome Outcome, ScanSummaryResponse? Scan, string Message);

public interface IScanService
{
    Task<StartScanResponse> StartAsync(Guid targetId, CancellationToken token);
    Task<IReadOnlyList<ScanSummaryResponse>> ListAsync(int limit, CancellationToken token);
    Task<ScanDetailsResponse?> GetAsync(Guid id, CancellationToken token);
    Task<CancelScanResponse> CancelAsync(Guid id, CancellationToken token);
    Task<ScanDashboardSummary> GetDashboardSummaryAsync(CancellationToken token);
    Task<int> RecoverInterruptedAsync(CancellationToken token);
}

public interface IScanJobQueue
{
    ValueTask QueueAsync(Guid scanId, CancellationToken token);
    ValueTask<Guid> DequeueAsync(CancellationToken token);
    CancellationToken GetCancellationToken(Guid scanId);
    bool RequestCancellation(Guid scanId);
    void Complete(Guid scanId);
}

public interface IScanExecutor { Task ExecuteAsync(Guid scanId, CancellationToken token); }

public interface IManagedArtifactStorage
{
    string GetRelativePath(Guid scanId, string fileName);
    string GetAbsoluteWritePath(string relativePath);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken token);
    Task DeleteScanArtifactsAsync(Guid scanId, CancellationToken token);
}
