using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QaAutomation.Core.Scans;
using QaAutomation.Core.Targets;
using QaAutomation.Infrastructure.Persistence;

namespace QaAutomation.Infrastructure.Scans;

public sealed class ScanService(QaAutomationDbContext db, IScanJobQueue queue, TimeProvider clock,
    IOptions<ScannerOptions> options) : IScanService
{
    public async Task<StartScanResponse> StartAsync(Guid targetId, CancellationToken token)
    {
        var target = await db.Targets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == targetId, token)
            ?? throw Error("targetId", "Target was not found.");
        if (!target.IsEnabled) throw Error("targetId", "Disabled targets cannot be scanned.");
        ScanUrlSafety.Validate(target.StartingUrl, target.AllowedHost);
        if (await db.Scans.AnyAsync(x => x.TargetId == targetId && (x.Status == ScanStatus.Queued || x.Status == ScanStatus.Running), token))
            throw Error("targetId", "This target already has an active scan.");

        var scan = new Scan { Id = Guid.NewGuid(), TargetId = targetId, Status = ScanStatus.Queued, Stage = "Waiting for scanner", RequestedAtUtc = clock.GetUtcNow(), StartingUrl = target.StartingUrl, BrowserName = "Chromium", ViewportWidth = options.Value.ViewportWidth, ViewportHeight = options.Value.ViewportHeight };
        db.Scans.Add(scan); await db.SaveChangesAsync(token); await queue.QueueAsync(scan.Id, token);
        return new(scan.Id, scan.Status, scan.Stage);
    }

    public async Task<IReadOnlyList<ScanSummaryResponse>> ListAsync(int limit, CancellationToken token) =>
        await db.Scans.AsNoTracking().OrderByDescending(x => x.RequestedAtUtc).Take(Math.Clamp(limit, 1, 100))
            .Select(x => new ScanSummaryResponse(x.Id, x.TargetId, x.Target.Name, x.Status, x.Stage, x.RequestedAtUtc,
                x.StartedAtUtc, x.CompletedAtUtc, x.StartingUrl, x.FinalUrl, x.PageTitle, x.DetectedPageCount,
                x.DetectedElementCount, x.WarningCount, x.ErrorCount, x.FailureSummary, x.CancellationRequested))
            .ToListAsync(token);

    public async Task<ScanDetailsResponse?> GetAsync(Guid id, CancellationToken token)
    {
        var scan = await db.Scans.AsNoTracking().Include(x => x.Target).Include(x => x.Diagnostics)
            .Include(x => x.Pages).ThenInclude(x => x.Elements).ThenInclude(x => x.SelectorCandidates)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, token);
        if (scan is null) return null;
        var pages = scan.Pages.OrderBy(x => x.DiscoveryOrder).Select(p => new PageResponse(p.Id, p.OriginalUrl, p.FinalUrl, p.Route,
            p.OriginalPageTitle, p.MainHeading, p.GeneratedDisplayName, p.DiscoveryOrder, p.ScreenshotPath is not null,
            p.ThumbnailPath is not null, p.ScreenshotWidth, p.ScreenshotHeight, p.Elements.OrderBy(e => e.DiscoveryOrder).Select(e =>
                new ElementResponse(e.Id, e.DiscoveryOrder, e.TagName, e.InputType, e.AccessibleRole, e.AccessibleName,
                    e.VisibleText, e.AssociatedLabel, e.Placeholder, e.NameAttribute, e.HtmlId, e.TestId, e.Classification,
                    e.IsActionable, e.IsVisible, e.IsEnabled, e.IsPotentiallyDestructive, e.CropPath is not null,
                    e.ScreenshotError, e.SelectorCandidates.OrderBy(s => s.Priority).Select(s => new SelectorResponse(s.Id,
                        s.SelectorType, s.SelectorValue, s.Priority, s.WasUnique, s.Confidence, s.IsPreferred)).ToList())).ToList())).ToList();
        var diagnostics = scan.Diagnostics.OrderBy(x => x.CreatedAtUtc).Select(x => new DiagnosticResponse(x.Id, x.Category,
            x.Severity, x.Message, x.Url, x.Method, x.StatusCode, x.CreatedAtUtc)).ToList();
        return new(Summary(scan), pages, diagnostics);
    }

    public async Task<CancelScanResponse> CancelAsync(Guid id, CancellationToken token)
    {
        var scan = await db.Scans.Include(x => x.Target).SingleOrDefaultAsync(x => x.Id == id, token);
        if (scan is null)
            return new(ScanCancellationOutcome.NotFound, null, "Scan was not found.");

        if (scan.Status == ScanStatus.Cancelled)
            return new(ScanCancellationOutcome.AlreadyCancelled, Summary(scan), "Scan was already cancelled.");

        if (!scan.IsActive)
            return new(ScanCancellationOutcome.NotCancellable, Summary(scan), $"Scan is {scan.Status} and can no longer be cancelled.");

        var now = clock.GetUtcNow();
        scan.CancellationRequested = true;
        scan.CancellationRequestedAtUtc ??= now;
        scan.CancellationReason = "Cancelled by user";

        queue.RequestCancellation(id);

        if (scan.Status == ScanStatus.Queued)
            scan.Cancel(now, "Cancelled by user");

        await db.SaveChangesAsync(token);
        return new(ScanCancellationOutcome.CancellationRequested, Summary(scan), "Cancellation was requested.");
    }

    public async Task<ScanDashboardSummary> GetDashboardSummaryAsync(CancellationToken token)
    {
        var latest = await db.Scans.AsNoTracking().OrderByDescending(x => x.RequestedAtUtc).Select(x => new { x.Status, x.RequestedAtUtc }).FirstOrDefaultAsync(token);
        return new(await db.Scans.CountAsync(token), latest?.Status, latest?.RequestedAtUtc);
    }

    public async Task<int> RecoverInterruptedAsync(CancellationToken token)
    {
        var scans = await db.Scans.Where(x => x.Status == ScanStatus.Queued || x.Status == ScanStatus.Running).ToListAsync(token);
        foreach (var scan in scans) scan.Fail(clock.GetUtcNow(), "Scan was interrupted because the application stopped unexpectedly.");
        await db.SaveChangesAsync(token); return scans.Count;
    }

    private static ScanSummaryResponse Summary(Scan x) => new(x.Id, x.TargetId, x.Target.Name, x.Status, x.Stage,
        x.RequestedAtUtc, x.StartedAtUtc, x.CompletedAtUtc, x.StartingUrl, x.FinalUrl, x.PageTitle, x.DetectedPageCount,
        x.DetectedElementCount, x.WarningCount, x.ErrorCount, x.FailureSummary, x.CancellationRequested);
    private static DomainValidationException Error(string key, string message) => new(new Dictionary<string, string[]> { [key] = [message] });
}
