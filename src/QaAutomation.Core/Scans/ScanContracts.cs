namespace QaAutomation.Core.Scans;

public sealed record StartScanRequest(ScanSettingsRequest? Settings);
public sealed record StartScanResponse(Guid Id, ScanStatus Status, string Stage);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);

public sealed record ScanHistoryQuery(
    Guid? TargetId,
    ScanStatus? Status,
    DateTimeOffset? RequestedFromUtc,
    DateTimeOffset? RequestedToUtc,
    string? PageTitle,
    string? Url,
    string? Search,
    int PageNumber = 1,
    int PageSize = 25);

public sealed record ElementQuery(
    string? Search,
    Guid? PageId,
    ElementClassification? Classification,
    bool? IsActionable,
    bool? IsEnabled,
    bool? IsPotentiallyDestructive,
    bool? HasCrop,
    bool? CropFailed,
    bool? HasUniqueSelector,
    bool? HasManualReview,
    bool? HasManualSelector,
    int PageNumber = 1,
    int PageSize = 25);

public sealed record DiagnosticQuery(
    string? Search,
    DiagnosticCategory? Category,
    DiagnosticSeverity? Severity,
    int? StatusCode,
    bool Descending = true,
    int PageNumber = 1,
    int PageSize = 25);

public sealed record ScanSummaryResponse(Guid Id, Guid TargetId, string TargetName, ScanStatus Status, string Stage,
    DateTimeOffset RequestedAtUtc, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc, string StartingUrl,
    string? FinalUrl, string? PageTitle, int DetectedPageCount, int DetectedElementCount, int WarningCount,
    int ErrorCount, string? FailureSummary, bool CancellationRequested);

public sealed record SelectorResponse(Guid Id, string Type, string Value, int Priority, bool WasUnique,
    decimal Confidence, bool IsPreferred, bool IsScannerPreferred, bool IsManualPreferred, bool IsEffectivePreferred);

public sealed record ElementResponse(Guid Id, Guid PageId, string PageDisplayName, int DiscoveryOrder, string TagName,
    string? InputType, string? AccessibleRole, string? AccessibleName, string? VisibleText, string? AssociatedLabel,
    string? Placeholder, string? NameAttribute, string? HtmlId, string? TestId, ElementClassification Classification,
    ElementClassification EffectiveClassification, ElementClassification? ClassificationOverride, string? UserDisplayName,
    string DisplayName, bool HasManualReview, bool IsActionable, bool IsVisible, bool IsEnabled,
    bool IsPotentiallyDestructive, bool HasCrop, string? ScreenshotError, Guid? ManualPreferredSelectorCandidateId,
    IReadOnlyList<SelectorResponse> Selectors);

public sealed record PageResponse(Guid Id, string OriginalUrl, string FinalUrl, string Route, string? Title,
    string? MainHeading, string GeneratedDisplayName, string? UserDisplayName, string DisplayName, int DiscoveryOrder,
    bool HasScreenshot, bool HasThumbnail, int? ScreenshotWidth, int? ScreenshotHeight, DateTimeOffset? ReviewUpdatedAtUtc,
    IReadOnlyList<ElementResponse> Elements);

public sealed record DiagnosticResponse(Guid Id, DiagnosticCategory Category, DiagnosticSeverity Severity, string Message,
    string? Url, string? Method, int? StatusCode, DateTimeOffset CreatedAtUtc);

public sealed record ScanSettingsResponse(int OverallTimeoutSeconds, int NavigationTimeoutMilliseconds,
    int ActionTimeoutMilliseconds, int MaximumDetectedElements, int MaximumDiagnosticRecords,
    int ElementScreenshotPadding, int ViewportWidth, int ViewportHeight);

public sealed record ScanSettingLimit<T>(T Default, T Min, T Max);
public sealed record ScannerSettingsMetadata(
    ScanSettingLimit<int> OverallTimeoutSeconds,
    ScanSettingLimit<int> NavigationTimeoutMilliseconds,
    ScanSettingLimit<int> ActionTimeoutMilliseconds,
    ScanSettingLimit<int> MaximumDetectedElements,
    ScanSettingLimit<int> MaximumDiagnosticRecords,
    ScanSettingLimit<int> ElementScreenshotPadding,
    ScanSettingLimit<int> ViewportWidth,
    ScanSettingLimit<int> ViewportHeight,
    IReadOnlyList<string> FixedSafetyRules);

public sealed record ScanSettingsRequest(int? OverallTimeoutSeconds = null, int? NavigationTimeoutMilliseconds = null,
    int? ActionTimeoutMilliseconds = null, int? MaximumDetectedElements = null, int? MaximumDiagnosticRecords = null,
    int? ElementScreenshotPadding = null, int? ViewportWidth = null, int? ViewportHeight = null);

public sealed record ScanDetailsResponse(ScanSummaryResponse Summary, IReadOnlyList<PageResponse> Pages,
    IReadOnlyList<DiagnosticResponse> Diagnostics, ScanSettingsResponse Settings);

public sealed record ScanDashboardSummary(int TotalScans, ScanStatus? LatestStatus, DateTimeOffset? LatestRequestedAtUtc);
public enum ScanCancellationOutcome { CancellationRequested, AlreadyCancelled, NotFound, NotCancellable }
public sealed record CancelScanResponse(ScanCancellationOutcome Outcome, ScanSummaryResponse? Scan, string Message);

public sealed record UpdatePageReviewRequest(string? DisplayName);
public sealed record UpdateElementReviewRequest(string? DisplayName, ElementClassification? ClassificationOverride);
public sealed record SelectManualSelectorRequest(Guid? SelectorCandidateId);

public interface IScanService
{
    Task<StartScanResponse> StartAsync(Guid targetId, StartScanRequest? request, CancellationToken token);
    Task<PagedResponse<ScanSummaryResponse>> ListAsync(ScanHistoryQuery query, CancellationToken token);
    Task<ScanDetailsResponse?> GetAsync(Guid id, CancellationToken token);
    Task<PagedResponse<ElementResponse>> QueryElementsAsync(Guid scanId, ElementQuery query, CancellationToken token);
    Task<PagedResponse<DiagnosticResponse>> QueryDiagnosticsAsync(Guid scanId, DiagnosticQuery query, CancellationToken token);
    Task<PageResponse?> UpdatePageReviewAsync(Guid scanId, Guid pageId, UpdatePageReviewRequest request, CancellationToken token);
    Task<ElementResponse?> UpdateElementReviewAsync(Guid scanId, Guid elementId, UpdateElementReviewRequest request, CancellationToken token);
    Task<ElementResponse?> SelectManualSelectorAsync(Guid scanId, Guid elementId, SelectManualSelectorRequest request, CancellationToken token);
    Task<CancelScanResponse> CancelAsync(Guid id, CancellationToken token);
    Task<ScanDashboardSummary> GetDashboardSummaryAsync(CancellationToken token);
    Task<ScannerSettingsMetadata> GetSettingsMetadataAsync(CancellationToken token);
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
