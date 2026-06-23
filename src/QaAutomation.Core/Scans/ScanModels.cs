using QaAutomation.Core.Targets;

namespace QaAutomation.Core.Scans;

public enum ScanStatus { Queued, Running, Completed, Failed, Cancelled }
public enum ElementClassification { Informational, Navigational, Input, Action, Submission, Upload, DateOrTime, PotentiallyDestructive, UnknownCustomControl }
public enum DiagnosticCategory { BrowserConsoleError, BrowserConsoleWarning, PageError, FailedNetworkRequest, HttpResponseError, NavigationError, ScreenshotError, ScannerWarning }
public enum DiagnosticSeverity { Information, Warning, Error }

public sealed class Scan
{
    public Guid Id { get; set; }
    public Guid TargetId { get; set; }
    public Target Target { get; set; } = null!;
    public ScanStatus Status { get; set; }
    public string Stage { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string StartingUrl { get; set; } = string.Empty;
    public string? FinalUrl { get; set; }
    public string? PageTitle { get; set; }
    public string BrowserName { get; set; } = "Chromium";
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public int? OverallTimeoutSeconds { get; set; }
    public int? NavigationTimeoutMilliseconds { get; set; }
    public int? ActionTimeoutMilliseconds { get; set; }
    public int? MaximumDetectedElements { get; set; }
    public int? MaximumDiagnosticRecords { get; set; }
    public int? ElementScreenshotPadding { get; set; }
    public int DetectedPageCount { get; set; }
    public int DetectedElementCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public string? FailureSummary { get; set; }
    public bool CancellationRequested { get; set; }
    public DateTimeOffset? CancellationRequestedAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public List<ScannedPage> Pages { get; set; } = [];
    public List<ScanDiagnostic> Diagnostics { get; set; } = [];

    public bool IsActive => Status is ScanStatus.Queued or ScanStatus.Running;
    public void Start(DateTimeOffset now) { if (Status != ScanStatus.Queued) throw new InvalidOperationException("Only queued scans can start."); Status = ScanStatus.Running; Stage = "Launching Chromium"; StartedAtUtc = now; }
    public void Complete(DateTimeOffset now) { if (Status != ScanStatus.Running) throw new InvalidOperationException("Only running scans can complete."); Status = ScanStatus.Completed; Stage = "Scan completed"; CompletedAtUtc = now; }
    public void Fail(DateTimeOffset now, string summary) { if (!IsActive) throw new InvalidOperationException("Only active scans can fail."); Status = ScanStatus.Failed; Stage = "Scan failed"; FailureSummary = summary; CompletedAtUtc = now; }
    public void Cancel(DateTimeOffset now, string reason) { if (!IsActive) throw new InvalidOperationException("Only active scans can be cancelled."); Status = ScanStatus.Cancelled; Stage = "Scan cancelled"; CancellationRequested = true; CancellationRequestedAtUtc ??= now; CancellationReason = reason; CompletedAtUtc = now; }
}

public sealed class ScannedPage
{
    public Guid Id { get; set; }
    public Guid ScanId { get; set; }
    public Scan Scan { get; set; } = null!;
    public string OriginalUrl { get; set; } = string.Empty;
    public string FinalUrl { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? OriginalPageTitle { get; set; }
    public string? MainHeading { get; set; }
    public string GeneratedDisplayName { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public DateTimeOffset? ReviewUpdatedAtUtc { get; set; }
    public int DiscoveryOrder { get; set; }
    public string? ScreenshotPath { get; set; }
    public string? ThumbnailPath { get; set; }
    public int? ScreenshotWidth { get; set; }
    public int? ScreenshotHeight { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<DetectedElement> Elements { get; set; } = [];
}

public sealed class DetectedElement
{
    public Guid Id { get; set; }
    public Guid PageId { get; set; }
    public ScannedPage Page { get; set; } = null!;
    public int DiscoveryOrder { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? InputType { get; set; }
    public string? AccessibleRole { get; set; }
    public string? AccessibleName { get; set; }
    public string? VisibleText { get; set; }
    public string? AssociatedLabel { get; set; }
    public string? Placeholder { get; set; }
    public string? NameAttribute { get; set; }
    public string? HtmlId { get; set; }
    public string? TestId { get; set; }
    public ElementClassification Classification { get; set; }
    public string? UserDisplayName { get; set; }
    public ElementClassification? ClassificationOverride { get; set; }
    public DateTimeOffset? ReviewUpdatedAtUtc { get; set; }
    public Guid? ManualPreferredSelectorCandidateId { get; set; }
    public SelectorCandidate? ManualPreferredSelectorCandidate { get; set; }
    public bool IsActionable { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPotentiallyDestructive { get; set; }
    public double? BoundingX { get; set; }
    public double? BoundingY { get; set; }
    public double? BoundingWidth { get; set; }
    public double? BoundingHeight { get; set; }
    public string? CropPath { get; set; }
    public string? ScreenshotError { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<SelectorCandidate> SelectorCandidates { get; set; } = [];
}

public sealed class SelectorCandidate
{
    public Guid Id { get; set; }
    public Guid ElementId { get; set; }
    public DetectedElement Element { get; set; } = null!;
    public string SelectorType { get; set; } = string.Empty;
    public string SelectorValue { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool WasUnique { get; set; }
    public decimal Confidence { get; set; }
    public bool IsPreferred { get; set; }
}

public sealed class ScanDiagnostic
{
    public Guid Id { get; set; }
    public Guid ScanId { get; set; }
    public Scan Scan { get; set; } = null!;
    public DiagnosticCategory Category { get; set; }
    public DiagnosticSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
