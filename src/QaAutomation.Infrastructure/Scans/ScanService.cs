using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QaAutomation.Core.Scans;
using QaAutomation.Core.Targets;
using QaAutomation.Infrastructure.Persistence;

namespace QaAutomation.Infrastructure.Scans;

public sealed class ScanService(QaAutomationDbContext db, IScanJobQueue queue, TimeProvider clock,
    IOptions<ScannerOptions> options) : IScanService
{
    public async Task<StartScanResponse> StartAsync(Guid targetId, StartScanRequest? request, CancellationToken token)
    {
        var target = await db.Targets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == targetId, token)
            ?? throw Error("targetId", "Target was not found.");
        if (!target.IsEnabled) throw Error("targetId", "Disabled targets cannot be scanned.");
        ScanUrlSafety.Validate(target.StartingUrl, target.AllowedHost);
        if (await db.Scans.AnyAsync(x => x.TargetId == targetId && (x.Status == ScanStatus.Queued || x.Status == ScanStatus.Running), token))
            throw Error("targetId", "This target already has an active scan.");

        var settings = ValidateSettings(request?.Settings);
        var scan = new Scan
        {
            Id = Guid.NewGuid(),
            TargetId = targetId,
            Status = ScanStatus.Queued,
            Stage = "Waiting for scanner",
            RequestedAtUtc = clock.GetUtcNow(),
            StartingUrl = target.StartingUrl,
            BrowserName = "Chromium",
            ViewportWidth = settings.ViewportWidth,
            ViewportHeight = settings.ViewportHeight,
            OverallTimeoutSeconds = settings.OverallTimeoutSeconds,
            NavigationTimeoutMilliseconds = settings.NavigationTimeoutMilliseconds,
            ActionTimeoutMilliseconds = settings.ActionTimeoutMilliseconds,
            MaximumDetectedElements = settings.MaximumDetectedElements,
            MaximumDiagnosticRecords = settings.MaximumDiagnosticRecords,
            ElementScreenshotPadding = settings.ElementScreenshotPadding
        };
        db.Scans.Add(scan);
        await db.SaveChangesAsync(token);
        await queue.QueueAsync(scan.Id, token);
        return new(scan.Id, scan.Status, scan.Stage);
    }

    public async Task<PagedResponse<ScanSummaryResponse>> ListAsync(ScanHistoryQuery query, CancellationToken token)
    {
        ValidatePage(query.PageNumber, query.PageSize);
        var scans = db.Scans.AsNoTracking().Include(x => x.Target).AsQueryable();
        if (query.TargetId is { } targetId) scans = scans.Where(x => x.TargetId == targetId);
        if (query.Status is { } status) scans = scans.Where(x => x.Status == status);
        if (query.RequestedFromUtc is { } from) scans = scans.Where(x => x.RequestedAtUtc >= from);
        if (query.RequestedToUtc is { } to) scans = scans.Where(x => x.RequestedAtUtc <= to);
        if (!string.IsNullOrWhiteSpace(query.PageTitle)) scans = scans.Where(x => x.PageTitle != null && x.PageTitle.Contains(query.PageTitle.Trim()));
        if (!string.IsNullOrWhiteSpace(query.Url))
        {
            var url = query.Url.Trim();
            scans = scans.Where(x => x.StartingUrl.Contains(url) || x.FinalUrl != null && x.FinalUrl.Contains(url));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            scans = scans.Where(x => x.Target.Name.Contains(search) || x.StartingUrl.Contains(search) ||
                x.FinalUrl != null && x.FinalUrl.Contains(search) || x.PageTitle != null && x.PageTitle.Contains(search));
        }
        var total = await scans.CountAsync(token);
        var scanItems = await scans.OrderByDescending(x => x.RequestedAtUtc).Skip(Skip(query.PageNumber, query.PageSize)).Take(query.PageSize).ToListAsync(token);
        var items = scanItems.Select(Summary).ToList();
        return new(items, query.PageNumber, query.PageSize, total);
    }

    public async Task<ScanDetailsResponse?> GetAsync(Guid id, CancellationToken token)
    {
        var scan = await db.Scans.AsNoTracking().Include(x => x.Target).Include(x => x.Diagnostics)
            .Include(x => x.Pages).ThenInclude(x => x.Elements).ThenInclude(x => x.SelectorCandidates)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, token);
        if (scan is null) return null;
        var pages = scan.Pages.OrderBy(x => x.DiscoveryOrder).Select(PageResponse).ToList();
        var diagnostics = scan.Diagnostics.OrderByDescending(x => x.CreatedAtUtc).Select(DiagnosticResponse).ToList();
        return new(Summary(scan), pages, diagnostics, Settings(scan));
    }

    public async Task<PagedResponse<ElementResponse>> QueryElementsAsync(Guid scanId, ElementQuery query, CancellationToken token)
    {
        ValidatePage(query.PageNumber, query.PageSize);
        if (!await db.Scans.AnyAsync(x => x.Id == scanId, token)) throw Error("scanId", "Scan was not found.");
        var elements = db.DetectedElements.AsNoTracking().Include(x => x.Page).Include(x => x.SelectorCandidates)
            .Where(x => x.Page.ScanId == scanId);
        if (query.PageId is { } pageId) elements = elements.Where(x => x.PageId == pageId);
        if (query.Classification is { } classification) elements = elements.Where(x => (x.ClassificationOverride ?? x.Classification) == classification);
        if (query.IsActionable is { } actionable) elements = elements.Where(x => x.IsActionable == actionable);
        if (query.IsEnabled is { } enabled) elements = elements.Where(x => x.IsEnabled == enabled);
        if (query.IsPotentiallyDestructive is { } destructive) elements = elements.Where(x => x.IsPotentiallyDestructive == destructive);
        if (query.HasCrop is { } hasCrop) elements = elements.Where(x => (x.CropPath != null) == hasCrop);
        if (query.CropFailed is { } failed) elements = elements.Where(x => (x.ScreenshotError != null) == failed);
        if (query.HasUniqueSelector is { } unique) elements = elements.Where(x => x.SelectorCandidates.Any(s => s.WasUnique) == unique);
        if (query.HasManualReview is { } reviewed) elements = elements.Where(x => (x.UserDisplayName != null || x.ClassificationOverride != null) == reviewed);
        if (query.HasManualSelector is { } manual) elements = elements.Where(x => (x.ManualPreferredSelectorCandidateId != null) == manual);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            elements = elements.Where(x => x.UserDisplayName != null && x.UserDisplayName.Contains(search) ||
                x.AccessibleName != null && x.AccessibleName.Contains(search) ||
                x.VisibleText != null && x.VisibleText.Contains(search) ||
                x.AssociatedLabel != null && x.AssociatedLabel.Contains(search) ||
                x.Placeholder != null && x.Placeholder.Contains(search) ||
                x.HtmlId != null && x.HtmlId.Contains(search) ||
                x.NameAttribute != null && x.NameAttribute.Contains(search) ||
                x.TestId != null && x.TestId.Contains(search));
        }
        var total = await elements.CountAsync(token);
        var elementItems = await elements.OrderBy(x => x.Page.DiscoveryOrder).ThenBy(x => x.DiscoveryOrder)
            .Skip(Skip(query.PageNumber, query.PageSize)).Take(query.PageSize).ToListAsync(token);
        var items = elementItems.Select(ElementResponse).ToList();
        return new(items, query.PageNumber, query.PageSize, total);
    }

    public async Task<PagedResponse<DiagnosticResponse>> QueryDiagnosticsAsync(Guid scanId, DiagnosticQuery query, CancellationToken token)
    {
        ValidatePage(query.PageNumber, query.PageSize);
        if (!await db.Scans.AnyAsync(x => x.Id == scanId, token)) throw Error("scanId", "Scan was not found.");
        var diagnostics = db.ScanDiagnostics.AsNoTracking().Where(x => x.ScanId == scanId);
        if (query.Category is { } category) diagnostics = diagnostics.Where(x => x.Category == category);
        if (query.Severity is { } severity) diagnostics = diagnostics.Where(x => x.Severity == severity);
        if (query.StatusCode is { } statusCode) diagnostics = diagnostics.Where(x => x.StatusCode == statusCode);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            diagnostics = diagnostics.Where(x => x.Message.Contains(search) || x.Url != null && x.Url.Contains(search));
        }
        var total = await diagnostics.CountAsync(token);
        diagnostics = query.Descending ? diagnostics.OrderByDescending(x => x.CreatedAtUtc) : diagnostics.OrderBy(x => x.CreatedAtUtc);
        var diagnosticItems = await diagnostics.Skip(Skip(query.PageNumber, query.PageSize)).Take(query.PageSize).ToListAsync(token);
        var items = diagnosticItems.Select(DiagnosticResponse).ToList();
        return new(items, query.PageNumber, query.PageSize, total);
    }

    public async Task<PageResponse?> UpdatePageReviewAsync(Guid scanId, Guid pageId, UpdatePageReviewRequest request, CancellationToken token)
    {
        var page = await db.ScannedPages.Include(x => x.Elements).ThenInclude(x => x.SelectorCandidates)
            .SingleOrDefaultAsync(x => x.Id == pageId && x.ScanId == scanId, token);
        if (page is null) return null;
        page.UserDisplayName = NormalizeManualName(request.DisplayName, nameof(request.DisplayName));
        page.ReviewUpdatedAtUtc = clock.GetUtcNow();
        await db.SaveChangesAsync(token);
        return PageResponse(page);
    }

    public async Task<ElementResponse?> UpdateElementReviewAsync(Guid scanId, Guid elementId, UpdateElementReviewRequest request, CancellationToken token)
    {
        var element = await db.DetectedElements.Include(x => x.Page).Include(x => x.SelectorCandidates)
            .SingleOrDefaultAsync(x => x.Id == elementId && x.Page.ScanId == scanId, token);
        if (element is null) return null;
        element.UserDisplayName = NormalizeManualName(request.DisplayName, nameof(request.DisplayName));
        element.ClassificationOverride = request.ClassificationOverride;
        element.ReviewUpdatedAtUtc = clock.GetUtcNow();
        await db.SaveChangesAsync(token);
        return ElementResponse(element);
    }

    public async Task<ElementResponse?> SelectManualSelectorAsync(Guid scanId, Guid elementId, SelectManualSelectorRequest request, CancellationToken token)
    {
        var element = await db.DetectedElements.Include(x => x.Page).Include(x => x.SelectorCandidates)
            .SingleOrDefaultAsync(x => x.Id == elementId && x.Page.ScanId == scanId, token);
        if (element is null) return null;
        if (request.SelectorCandidateId is { } selectorId && element.SelectorCandidates.All(x => x.Id != selectorId))
            throw Error("selectorCandidateId", "The selected selector does not belong to this element.");
        element.ManualPreferredSelectorCandidateId = request.SelectorCandidateId;
        element.ReviewUpdatedAtUtc = clock.GetUtcNow();
        await db.SaveChangesAsync(token);
        return ElementResponse(element);
    }

    public async Task<CancelScanResponse> CancelAsync(Guid id, CancellationToken token)
    {
        var scan = await db.Scans.Include(x => x.Target).SingleOrDefaultAsync(x => x.Id == id, token);
        if (scan is null) return new(ScanCancellationOutcome.NotFound, null, "Scan was not found.");
        if (scan.Status == ScanStatus.Cancelled) return new(ScanCancellationOutcome.AlreadyCancelled, Summary(scan), "Scan was already cancelled.");
        if (!scan.IsActive) return new(ScanCancellationOutcome.NotCancellable, Summary(scan), $"Scan is {scan.Status} and can no longer be cancelled.");
        var now = clock.GetUtcNow(); scan.CancellationRequested = true; scan.CancellationRequestedAtUtc ??= now; scan.CancellationReason = "Cancelled by user";
        queue.RequestCancellation(id);
        if (scan.Status == ScanStatus.Queued) scan.Cancel(now, "Cancelled by user");
        await db.SaveChangesAsync(token);
        return new(ScanCancellationOutcome.CancellationRequested, Summary(scan), "Cancellation was requested.");
    }

    public async Task<ScanDashboardSummary> GetDashboardSummaryAsync(CancellationToken token)
    {
        var latest = await db.Scans.AsNoTracking().OrderByDescending(x => x.RequestedAtUtc).Select(x => new { x.Status, x.RequestedAtUtc }).FirstOrDefaultAsync(token);
        return new(await db.Scans.CountAsync(token), latest?.Status, latest?.RequestedAtUtc);
    }

    public Task<ScannerSettingsMetadata> GetSettingsMetadataAsync(CancellationToken token) => Task.FromResult(new ScannerSettingsMetadata(
        new(options.Value.OverallTimeoutSeconds, ScannerOptions.MinOverallTimeoutSeconds, ScannerOptions.MaxOverallTimeoutSeconds),
        new(options.Value.NavigationTimeoutMilliseconds, ScannerOptions.MinNavigationTimeoutMilliseconds, ScannerOptions.MaxNavigationTimeoutMilliseconds),
        new(options.Value.ActionTimeoutMilliseconds, ScannerOptions.MinActionTimeoutMilliseconds, ScannerOptions.MaxActionTimeoutMilliseconds),
        new(options.Value.MaximumDetectedElements, ScannerOptions.MinMaximumDetectedElements, ScannerOptions.MaxMaximumDetectedElements),
        new(options.Value.MaximumDiagnosticRecords, ScannerOptions.MinMaximumDiagnosticRecords, ScannerOptions.MaxMaximumDiagnosticRecords),
        new(options.Value.ElementScreenshotPadding, ScannerOptions.MinElementScreenshotPadding, ScannerOptions.MaxElementScreenshotPadding),
        new(options.Value.ViewportWidth, ScannerOptions.MinViewportWidth, ScannerOptions.MaxViewportWidth),
        new(options.Value.ViewportHeight, ScannerOptions.MinViewportHeight, ScannerOptions.MaxViewportHeight),
        [
            "One starting page only", "No link clicking", "No form submission", "No typing", "No uploads", "No downloads",
            "No CAPTCHA bypass", "No authentication bypass", "Main-frame navigation is restricted to the allowed host",
            "Third-party resources may load only to render the approved page", "TLS certificate errors are not ignored"
        ]));

    public async Task<int> RecoverInterruptedAsync(CancellationToken token)
    {
        var scans = await db.Scans.Where(x => x.Status == ScanStatus.Queued || x.Status == ScanStatus.Running).ToListAsync(token);
        foreach (var scan in scans) scan.Fail(clock.GetUtcNow(), "Scan was interrupted because the application stopped unexpectedly.");
        await db.SaveChangesAsync(token); return scans.Count;
    }

    private ScanSettingsResponse ValidateSettings(ScanSettingsRequest? request)
    {
        var configured = options.Value;
        var settings = new ScanSettingsResponse(
            request?.OverallTimeoutSeconds ?? configured.OverallTimeoutSeconds,
            request?.NavigationTimeoutMilliseconds ?? configured.NavigationTimeoutMilliseconds,
            request?.ActionTimeoutMilliseconds ?? configured.ActionTimeoutMilliseconds,
            request?.MaximumDetectedElements ?? configured.MaximumDetectedElements,
            request?.MaximumDiagnosticRecords ?? configured.MaximumDiagnosticRecords,
            request?.ElementScreenshotPadding ?? configured.ElementScreenshotPadding,
            request?.ViewportWidth ?? configured.ViewportWidth,
            request?.ViewportHeight ?? configured.ViewportHeight);
        var errors = new Dictionary<string, string[]>();
        Range(settings.OverallTimeoutSeconds, ScannerOptions.MinOverallTimeoutSeconds, ScannerOptions.MaxOverallTimeoutSeconds, "settings.overallTimeoutSeconds", errors);
        Range(settings.NavigationTimeoutMilliseconds, ScannerOptions.MinNavigationTimeoutMilliseconds, ScannerOptions.MaxNavigationTimeoutMilliseconds, "settings.navigationTimeoutMilliseconds", errors);
        Range(settings.ActionTimeoutMilliseconds, ScannerOptions.MinActionTimeoutMilliseconds, ScannerOptions.MaxActionTimeoutMilliseconds, "settings.actionTimeoutMilliseconds", errors);
        Range(settings.MaximumDetectedElements, ScannerOptions.MinMaximumDetectedElements, ScannerOptions.MaxMaximumDetectedElements, "settings.maximumDetectedElements", errors);
        Range(settings.MaximumDiagnosticRecords, ScannerOptions.MinMaximumDiagnosticRecords, ScannerOptions.MaxMaximumDiagnosticRecords, "settings.maximumDiagnosticRecords", errors);
        Range(settings.ElementScreenshotPadding, ScannerOptions.MinElementScreenshotPadding, ScannerOptions.MaxElementScreenshotPadding, "settings.elementScreenshotPadding", errors);
        Range(settings.ViewportWidth, ScannerOptions.MinViewportWidth, ScannerOptions.MaxViewportWidth, "settings.viewportWidth", errors);
        Range(settings.ViewportHeight, ScannerOptions.MinViewportHeight, ScannerOptions.MaxViewportHeight, "settings.viewportHeight", errors);
        if (errors.Count > 0) throw new DomainValidationException(errors);
        return settings;
    }

    private static PageResponse PageResponse(ScannedPage p) => new(p.Id, p.OriginalUrl, p.FinalUrl, p.Route, p.OriginalPageTitle,
        p.MainHeading, p.GeneratedDisplayName, p.UserDisplayName, PageDisplayName(p), p.DiscoveryOrder, p.ScreenshotPath is not null,
        p.ThumbnailPath is not null, p.ScreenshotWidth, p.ScreenshotHeight, p.ReviewUpdatedAtUtc,
        p.Elements.OrderBy(e => e.DiscoveryOrder).Select(e => ElementResponse(e)).ToList());

    private static ElementResponse ElementResponse(DetectedElement e)
    {
        var effectiveSelectorId = e.ManualPreferredSelectorCandidateId ?? e.SelectorCandidates.OrderBy(x => x.Priority).FirstOrDefault(x => x.IsPreferred)?.Id;
        var pageName = PageDisplayName(e.Page);
        return new(e.Id, e.PageId, pageName, e.DiscoveryOrder, e.TagName, e.InputType, e.AccessibleRole, e.AccessibleName,
            e.VisibleText, e.AssociatedLabel, e.Placeholder, e.NameAttribute, e.HtmlId, e.TestId, e.Classification,
            e.ClassificationOverride ?? e.Classification, e.ClassificationOverride, e.UserDisplayName, ElementDisplayName(e),
            e.UserDisplayName is not null || e.ClassificationOverride is not null, e.IsActionable, e.IsVisible, e.IsEnabled,
            e.IsPotentiallyDestructive, e.CropPath is not null, e.ScreenshotError, e.ManualPreferredSelectorCandidateId,
            e.SelectorCandidates.OrderBy(s => s.Priority).Select(s => new SelectorResponse(s.Id, s.SelectorType, s.SelectorValue,
                s.Priority, s.WasUnique, s.Confidence, s.Id == effectiveSelectorId, s.IsPreferred,
                e.ManualPreferredSelectorCandidateId == s.Id, s.Id == effectiveSelectorId)).ToList());
    }

    private static DiagnosticResponse DiagnosticResponse(ScanDiagnostic x) => new(x.Id, x.Category, x.Severity, x.Message, x.Url, x.Method, x.StatusCode, x.CreatedAtUtc);
    private static ScanSummaryResponse Summary(Scan x) => new(x.Id, x.TargetId, x.Target.Name, x.Status, x.Stage, x.RequestedAtUtc, x.StartedAtUtc, x.CompletedAtUtc, x.StartingUrl, x.FinalUrl, x.PageTitle, x.DetectedPageCount, x.DetectedElementCount, x.WarningCount, x.ErrorCount, x.FailureSummary, x.CancellationRequested);
    private ScanSettingsResponse Settings(Scan scan) => new(scan.OverallTimeoutSeconds ?? options.Value.OverallTimeoutSeconds, scan.NavigationTimeoutMilliseconds ?? options.Value.NavigationTimeoutMilliseconds, scan.ActionTimeoutMilliseconds ?? options.Value.ActionTimeoutMilliseconds, scan.MaximumDetectedElements ?? options.Value.MaximumDetectedElements, scan.MaximumDiagnosticRecords ?? options.Value.MaximumDiagnosticRecords, scan.ElementScreenshotPadding ?? options.Value.ElementScreenshotPadding, scan.ViewportWidth, scan.ViewportHeight);
    private static string PageDisplayName(ScannedPage p) => string.IsNullOrWhiteSpace(p.UserDisplayName) ? p.GeneratedDisplayName : p.UserDisplayName.Trim();
    private static string ElementDisplayName(DetectedElement e) => TrimOrNull(e.UserDisplayName) ?? TrimOrNull(e.AccessibleName) ?? TrimOrNull(e.AssociatedLabel) ?? TrimOrNull(e.VisibleText) ?? TrimOrNull(e.Placeholder) ?? e.TagName;
    private static string? NormalizeManualName(string? value, string key) { var trimmed = TrimOrNull(value); if (trimmed is null) return null; if (trimmed.Length > 500) throw Error(key, "Manual names must be 500 characters or fewer."); return trimmed; }
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ValidatePage(int pageNumber, int pageSize) { var errors = new Dictionary<string, string[]>(); if (pageNumber < 1) errors["pageNumber"] = ["Page number must be 1 or greater."]; if (pageSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."]; if (errors.Count > 0) throw new DomainValidationException(errors); }
    private static int Skip(int pageNumber, int pageSize) => (pageNumber - 1) * pageSize;
    private static void Range(int value, int min, int max, string key, Dictionary<string, string[]> errors) { if (value < min || value > max) errors[key] = [$"Value must be between {min} and {max}."]; }
    private static DomainValidationException Error(string key, string message) => new(new Dictionary<string, string[]> { [key] = [message] });
}
