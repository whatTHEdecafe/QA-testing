using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using QaAutomation.Core.Scans;
using QaAutomation.Infrastructure.Persistence;

namespace QaAutomation.Infrastructure.Scans;

public sealed class PlaywrightScanExecutor(QaAutomationDbContext db, IManagedArtifactStorage storage,
    IOptions<ScannerOptions> configured, TimeProvider clock, ILogger<PlaywrightScanExecutor> logger) : IScanExecutor
{
    private readonly ScannerOptions _options = configured.Value;

    public async Task ExecuteAsync(Guid scanId, CancellationToken externalToken)
    {
        var scan = await db.Scans.Include(x => x.Target).SingleOrDefaultAsync(x => x.Id == scanId, externalToken);
        if (scan is null || !scan.IsActive) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.OverallTimeoutSeconds));
        var token = timeout.Token;
        var pending = new ConcurrentQueue<PendingDiagnostic>();
        IPlaywright? playwright = null; IBrowser? browser = null; IBrowserContext? context = null;
        try
        {
            token.ThrowIfCancellationRequested(); scan.Start(clock.GetUtcNow()); await db.SaveChangesAsync(CancellationToken.None);
            var startUri = ScanUrlSafety.Validate(scan.StartingUrl, scan.Target.AllowedHost);
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new() { Headless = _options.Headless });
            using var cancellationRegistration = token.Register(() =>
            {
                try { browser.CloseAsync().GetAwaiter().GetResult(); }
                catch { /* The normal scan path records the resulting cancellation or browser error. */ }
            });
            await Stage(scan, "Opening isolated browser context");
            context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = _options.ViewportWidth, Height = _options.ViewportHeight }, IgnoreHTTPSErrors = false });
            context.SetDefaultTimeout(_options.ActionTimeoutMilliseconds); context.SetDefaultNavigationTimeout(_options.NavigationTimeoutMilliseconds);
            var page = await context.NewPageAsync(); AttachDiagnostics(page, pending);
            await page.RouteAsync("**/*", async route =>
            {
                var request = route.Request;
                if (request.IsNavigationRequest && request.Frame == page.MainFrame && !IsAllowedRequest(request.Url, scan.Target.AllowedHost))
                {
                    pending.Enqueue(new(DiagnosticCategory.NavigationError, DiagnosticSeverity.Error, "Blocked main-frame navigation outside the allowed host.", RedactUrl(request.Url), request.Method, null));
                    await route.AbortAsync("blockedbyclient");
                }
                else await route.ContinueAsync();
            });

            await Stage(scan, "Navigating to authorized starting page");
            await page.GotoAsync(startUri.AbsoluteUri, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = _options.NavigationTimeoutMilliseconds });
            token.ThrowIfCancellationRequested();
            try { await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = Math.Min(5000, _options.ActionTimeoutMilliseconds) }); } catch (TimeoutException) { pending.Enqueue(new(DiagnosticCategory.ScannerWarning, DiagnosticSeverity.Warning, "Page remained network-active; scanning continued after the bounded wait.", RedactUrl(page.Url), null, null)); }
            var finalUri = ScanUrlSafety.Validate(page.Url, scan.Target.AllowedHost);
            scan.FinalUrl = finalUri.AbsoluteUri; scan.PageTitle = Trim(await page.TitleAsync(), 500);

            await Stage(scan, "Capturing page reference image");
            var pageRecord = new ScannedPage { Id = Guid.NewGuid(), ScanId = scan.Id, OriginalUrl = scan.StartingUrl,
                FinalUrl = finalUri.AbsoluteUri, Route = finalUri.PathAndQuery, OriginalPageTitle = scan.PageTitle,
                MainHeading = Trim(await ReadMainHeading(page), 500), DiscoveryOrder = 1, CreatedAtUtc = clock.GetUtcNow() };
            pageRecord.GeneratedDisplayName = GeneratePageName(pageRecord);
            var screenshotRelative = storage.GetRelativePath(scan.Id, "page-full.png");
            var screenshotPath = storage.GetAbsoluteWritePath(screenshotRelative);
            var screenshotBytes = await page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true, Type = ScreenshotType.Png });
            pageRecord.ScreenshotPath = screenshotRelative;
            var dimensions = await page.EvaluateAsync<Dimensions>("() => ({ width: Math.max(document.documentElement.scrollWidth, innerWidth), height: Math.max(document.documentElement.scrollHeight, innerHeight) })");
            pageRecord.ScreenshotWidth = dimensions.Width; pageRecord.ScreenshotHeight = dimensions.Height;
            try
            {
                var thumbRelative = storage.GetRelativePath(scan.Id, "page-thumbnail.png");
                await CreateThumbnail(context, screenshotBytes, storage.GetAbsoluteWritePath(thumbRelative));
                pageRecord.ThumbnailPath = thumbRelative;
            }
            catch (Exception ex) { pending.Enqueue(new(DiagnosticCategory.ScreenshotError, DiagnosticSeverity.Warning, $"Thumbnail capture failed: {SafeMessage(ex)}", null, null, null)); }

            await Stage(scan, "Detecting visible page elements");
            var detected = await DetectElements(page);
            if (detected.Count > _options.MaximumDetectedElements)
                pending.Enqueue(new(DiagnosticCategory.ScannerWarning, DiagnosticSeverity.Warning, $"Element limit reached; only the first {_options.MaximumDetectedElements} useful elements were retained.", finalUri.AbsoluteUri, null, null));
            var discoveryOrder = 0;
            foreach (var item in detected.Take(_options.MaximumDetectedElements))
            {
                token.ThrowIfCancellationRequested();
                var element = MapElement(item, pageRecord.Id, ++discoveryOrder);
                await CaptureCrop(page, scan.Id, item.ScanKey, element, pending);
                element.SelectorCandidates = await BuildSelectors(page, item, element.Id);
                pageRecord.Elements.Add(element);
            }
            db.ScannedPages.Add(pageRecord); scan.DetectedPageCount = 1; scan.DetectedElementCount = pageRecord.Elements.Count;
            FlushDiagnostics(scan, pending); scan.WarningCount = scan.Diagnostics.Count(x => x.Severity == DiagnosticSeverity.Warning); scan.ErrorCount = scan.Diagnostics.Count(x => x.Severity == DiagnosticSeverity.Error);
            scan.Complete(clock.GetUtcNow()); await db.SaveChangesAsync(CancellationToken.None);
            await context.CloseAsync();
            logger.LogInformation("Scan {ScanId} completed with {ElementCount} elements", scan.Id, scan.DetectedElementCount);
        }
        catch (Exception ex) when (token.IsCancellationRequested || ex is OperationCanceledException || ex.Message.Contains("closed", StringComparison.OrdinalIgnoreCase) && externalToken.IsCancellationRequested)
        {
            db.ChangeTracker.Clear(); var terminal = await db.Scans.Include(x => x.Diagnostics).SingleAsync(x => x.Id == scanId, CancellationToken.None);
            FlushDiagnostics(terminal, pending); if (terminal.IsActive) terminal.Cancel(clock.GetUtcNow(), externalToken.IsCancellationRequested ? "Cancelled by user" : "Scan timed out");
            await db.SaveChangesAsync(CancellationToken.None); logger.LogInformation("Scan {ScanId} cancelled", scanId);
        }
        catch (Exception ex)
        {
            pending.Enqueue(new(DiagnosticCategory.NavigationError, DiagnosticSeverity.Error, SafeMessage(ex), scan.FinalUrl ?? scan.StartingUrl, null, null));
            db.ChangeTracker.Clear(); var terminal = await db.Scans.Include(x => x.Diagnostics).SingleAsync(x => x.Id == scanId, CancellationToken.None);
            FlushDiagnostics(terminal, pending); terminal.ErrorCount = terminal.Diagnostics.Count(x => x.Severity == DiagnosticSeverity.Error); terminal.WarningCount = terminal.Diagnostics.Count(x => x.Severity == DiagnosticSeverity.Warning);
            if (terminal.IsActive) terminal.Fail(clock.GetUtcNow(), SafeMessage(ex));
            await db.SaveChangesAsync(CancellationToken.None); logger.LogError(ex, "Scan {ScanId} failed", scanId);
        }
        finally { if (context is not null) try { await context.CloseAsync(); } catch { } if (browser is not null) try { await browser.CloseAsync(); } catch { } playwright?.Dispose(); }
    }

    private void AttachDiagnostics(IPage page, ConcurrentQueue<PendingDiagnostic> pending)
    {
        page.Console += (_, message) => { if (message.Type is "error" or "warning") pending.Enqueue(new(message.Type == "error" ? DiagnosticCategory.BrowserConsoleError : DiagnosticCategory.BrowserConsoleWarning, message.Type == "error" ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning, Trim(message.Text, 4000)!, null, null, null)); };
        page.PageError += (_, error) => pending.Enqueue(new(DiagnosticCategory.PageError, DiagnosticSeverity.Error, Trim(error, 4000)!, RedactUrl(page.Url), null, null));
        page.RequestFailed += (_, request) => pending.Enqueue(new(DiagnosticCategory.FailedNetworkRequest, DiagnosticSeverity.Error, Trim(request.Failure, 4000) ?? "Network request failed.", RedactUrl(request.Url), request.Method, null));
        page.Response += (_, response) => { if (response.Status >= 400) pending.Enqueue(new(DiagnosticCategory.HttpResponseError, response.Status >= 500 ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning, $"HTTP {response.Status} response", RedactUrl(response.Url), response.Request.Method, response.Status)); };
    }

    private async Task<List<ElementSnapshot>> DetectElements(IPage page)
    {
        var result = await page.EvaluateAsync<JsonElement>(@"() => {
      const candidates=[...document.querySelectorAll('a,button,input,textarea,select,[role],[tabindex],[contenteditable=""true""],summary')];
      const useful=[]; const destructive=/\b(delete|remove|purchase|pay|confirm booking|send|submit order|cancel account)\b/i;
      for(const [index,el] of candidates.entries()){const r=el.getBoundingClientRect();const s=getComputedStyle(el);if(r.width<=0||r.height<=0||s.visibility==='hidden'||s.display==='none')continue;
        const tag=el.tagName.toLowerCase(),type=(el.getAttribute('type')||'').toLowerCase(),role=el.getAttribute('role')||({a:'link',button:'button',select:'combobox',textarea:'textbox'}[tag]??(tag==='input'?(type==='checkbox'?'checkbox':type==='radio'?'radio':'textbox'):''));
        const label=(el.labels&&el.labels[0]?.innerText)||el.getAttribute('aria-label')||''; const text=(el.innerText||el.value||'').trim().replace(/\s+/g,' ').slice(0,2000); const name=(el.getAttribute('aria-label')||label||text||el.getAttribute('placeholder')||el.getAttribute('title')||'').slice(0,1000);
        const action=['a','button','input','textarea','select','summary'].includes(tag)||['button','link','textbox','checkbox','radio','combobox','tab'].includes(role)||el.hasAttribute('contenteditable')||el.tabIndex>=0;
        if(!action&&!name)continue; const key='qa-'+index;el.setAttribute('data-qa-scan-key',key);
        const segments=[];let node=el;while(node&&node.nodeType===1&&node!==document.documentElement){let segment=node.tagName.toLowerCase();const siblings=node.parentElement?[...node.parentElement.children].filter(x=>x.tagName===node.tagName):[];if(siblings.length>1)segment+=`:nth-of-type(${siblings.indexOf(node)+1})`;segments.unshift(segment);node=node.parentElement;}const cssPath=segments.join(' > ');
        useful.push({scanKey:key,cssPath,tagName:tag,inputType:type||null,role:role||null,accessibleName:name||null,visibleText:text||null,label:label||null,placeholder:el.getAttribute('placeholder'),nameAttribute:el.getAttribute('name'),htmlId:el.id||null,testId:el.getAttribute('data-testid')||el.getAttribute('data-test-id')||el.getAttribute('data-test'),enabled:!(el.disabled||el.getAttribute('aria-disabled')==='true'),actionable:action,destructive:destructive.test(name+' '+text),x:r.x+scrollX,y:r.y+scrollY,width:r.width,height:r.height});
      } return useful; }");
        return JsonSerializer.Deserialize<List<ElementSnapshot>>(result.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private async Task CaptureCrop(IPage page, Guid scanId, string key, DetectedElement element, ConcurrentQueue<PendingDiagnostic> pending)
    {
        try { var locator = page.Locator($"[data-qa-scan-key=\"{key}\"]"); await locator.ScrollIntoViewIfNeededAsync(); var box = await locator.BoundingBoxAsync() ?? throw new InvalidOperationException("Element bounding box is unavailable."); var bounds = await page.EvaluateAsync<Dimensions>("() => ({ width: Math.max(document.documentElement.scrollWidth, innerWidth), height: Math.max(document.documentElement.scrollHeight, innerHeight) })"); var pad = _options.ElementScreenshotPadding; var x = Math.Clamp(box.X - pad, 0, Math.Max(0, bounds.Width - 1)); var y = Math.Clamp(box.Y - pad, 0, Math.Max(0, bounds.Height - 1)); var width = Math.Clamp(box.Width + pad * 2, 1, Math.Max(1, bounds.Width - x)); var height = Math.Clamp(box.Height + pad * 2, 1, Math.Max(1, bounds.Height - y)); var relative = storage.GetRelativePath(scanId, $"element-{element.DiscoveryOrder:000}.png"); await page.ScreenshotAsync(new() { Path = storage.GetAbsoluteWritePath(relative), Clip = new() { X = (float)x, Y = (float)y, Width = (float)width, Height = (float)height }, Type = ScreenshotType.Png }); element.CropPath = relative; element.BoundingX = box.X; element.BoundingY = box.Y; element.BoundingWidth = box.Width; element.BoundingHeight = box.Height; }
        catch (Exception ex) { element.ScreenshotError = SafeMessage(ex); pending.Enqueue(new(DiagnosticCategory.ScreenshotError, DiagnosticSeverity.Warning, $"Element {element.DiscoveryOrder} crop failed: {element.ScreenshotError}", null, null, null)); }
    }

    private async Task<List<SelectorCandidate>> BuildSelectors(IPage page, ElementSnapshot item, Guid elementId)
    {
        var candidates = new List<(string Type, string Value, ILocator Locator, decimal Confidence)>();
        if (!string.IsNullOrWhiteSpace(item.TestId)) candidates.Add(("TestId", item.TestId, page.GetByTestId(item.TestId), .98m));
        if (!string.IsNullOrWhiteSpace(item.Role) && !string.IsNullOrWhiteSpace(item.AccessibleName)) candidates.Add(("Role", JsonSerializer.Serialize(new { role=item.Role,name=item.AccessibleName }), page.Locator($"role={item.Role}[name=\"{EscapeSelector(item.AccessibleName)}\"]"), .92m));
        if (!string.IsNullOrWhiteSpace(item.Label)) candidates.Add(("Label", item.Label, page.GetByLabel(item.Label), .90m));
        if (!string.IsNullOrWhiteSpace(item.Placeholder)) candidates.Add(("Placeholder", item.Placeholder, page.GetByPlaceholder(item.Placeholder), .84m));
        if (!string.IsNullOrWhiteSpace(item.HtmlId)) candidates.Add(("Id", item.HtmlId, page.Locator("#" + CssEscape(item.HtmlId)), .82m));
        if (!string.IsNullOrWhiteSpace(item.NameAttribute)) candidates.Add(("Name", item.NameAttribute, page.Locator($"[name=\"{EscapeSelector(item.NameAttribute)}\"]"), .76m));
        if (!string.IsNullOrWhiteSpace(item.VisibleText) && item.VisibleText.Length <= 200) candidates.Add(("Text", item.VisibleText, page.GetByText(item.VisibleText, new() { Exact = true }), .65m));
        if (!string.IsNullOrWhiteSpace(item.CssPath)) candidates.Add(("Css", item.CssPath, page.Locator(item.CssPath), .50m));
        var result = new List<SelectorCandidate>();
        foreach (var candidate in candidates) { bool unique; try { unique = await candidate.Locator.CountAsync() == 1; } catch { unique = false; } result.Add(new() { Id = Guid.NewGuid(), ElementId = elementId, SelectorType = candidate.Type, SelectorValue = candidate.Value, Priority = SelectorPriority.For(candidate.Type), WasUnique = unique, Confidence = unique ? candidate.Confidence : candidate.Confidence / 2 }); }
        SelectorPriority.MarkPreferred(result); return result;
    }

    private DetectedElement MapElement(ElementSnapshot x, Guid pageId, int order) => new() { Id = Guid.NewGuid(), PageId = pageId, DiscoveryOrder = order, TagName = x.TagName, InputType = x.InputType, AccessibleRole = Trim(x.Role,100), AccessibleName = Trim(x.AccessibleName,1000), VisibleText = Trim(x.VisibleText,2000), AssociatedLabel = Trim(x.Label,1000), Placeholder = Trim(x.Placeholder,1000), NameAttribute = Trim(x.NameAttribute,500), HtmlId = Trim(x.HtmlId,500), TestId = Trim(x.TestId,500), Classification = Classify(x), IsActionable = x.Actionable, IsVisible = true, IsEnabled = x.Enabled, IsPotentiallyDestructive = x.Destructive, BoundingX=x.X, BoundingY=x.Y, BoundingWidth=x.Width, BoundingHeight=x.Height, CreatedAtUtc=clock.GetUtcNow() };
    private static ElementClassification Classify(ElementSnapshot x) { if (x.Destructive) return ElementClassification.PotentiallyDestructive; if (x.InputType == "file") return ElementClassification.Upload; if (x.InputType is "date" or "time" or "datetime-local") return ElementClassification.DateOrTime; if (x.InputType == "submit") return ElementClassification.Submission; if (x.TagName is "input" or "textarea" or "select" || x.Role is "textbox" or "checkbox" or "radio" or "combobox") return ElementClassification.Input; if (x.TagName == "a" || x.Role == "link") return ElementClassification.Navigational; if (x.TagName == "button" || x.Role is "button" or "tab") return ElementClassification.Action; return x.Actionable ? ElementClassification.UnknownCustomControl : ElementClassification.Informational; }
    private async Task CreateThumbnail(IBrowserContext context, byte[] bytes, string path) { var thumb = await context.NewPageAsync(); await thumb.SetViewportSizeAsync(400, 300); await thumb.SetContentAsync($"<style>body{{margin:0}}img{{display:block;width:360px;height:auto}}</style><img src='data:image/png;base64,{Convert.ToBase64String(bytes)}'>"); await thumb.Locator("img").ScreenshotAsync(new() { Path = path, Type = ScreenshotType.Png }); await thumb.CloseAsync(); }
    private async Task Stage(Scan scan, string stage) { scan.Stage = stage; await db.SaveChangesAsync(CancellationToken.None); }
    private void FlushDiagnostics(Scan scan, ConcurrentQueue<PendingDiagnostic> pending) { while (scan.Diagnostics.Count < _options.MaximumDiagnosticRecords && pending.TryDequeue(out var d)) { var diagnostic=new ScanDiagnostic { Id=Guid.NewGuid(), ScanId=scan.Id, Category=d.Category, Severity=d.Severity, Message=Trim(d.Message,4000)!, Url=Trim(d.Url,2048), Method=Trim(d.Method,20), StatusCode=d.StatusCode, CreatedAtUtc=clock.GetUtcNow() }; scan.Diagnostics.Add(diagnostic); db.Entry(diagnostic).State=EntityState.Added; } }
    private static bool IsAllowedRequest(string value, string host) { try { ScanUrlSafety.Validate(value, host); return true; } catch { return false; } }
    private static string? RedactUrl(string? value) { if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null; return new UriBuilder(uri) { UserName="", Password="", Query=string.IsNullOrEmpty(uri.Query)?"":"?redacted" }.Uri.AbsoluteUri; }
    private static string GeneratePageName(ScannedPage p) => Trim(p.OriginalPageTitle,120) ?? Trim(p.MainHeading,120) ?? (string.IsNullOrWhiteSpace(p.Route.Trim('/')) ? "Home" : p.Route.Trim('/').Split('/').Last().Replace('-', ' '));
    private static async Task<string?> ReadMainHeading(IPage page) { try { var heading = page.Locator("h1:visible").First; return await heading.CountAsync() == 0 ? null : await heading.TextContentAsync(new() { Timeout=2000 }); } catch { return null; } }
    private static string SafeMessage(Exception ex) => Trim(ex.Message.Replace(Environment.NewLine," "),2000) ?? "Unknown scanner error.";
    private static string? Trim(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length,max)];
    private static string EscapeSelector(string value) => value.Replace("\\","\\\\").Replace("\"","\\\"");
    private static string CssEscape(string value) => value.Replace("\\","\\\\").Replace("\"","\\\"").Replace("#","\\#").Replace(".","\\.");
    private sealed record PendingDiagnostic(DiagnosticCategory Category, DiagnosticSeverity Severity, string Message, string? Url, string? Method, int? StatusCode);
    private sealed class Dimensions { public int Width { get; set; } public int Height { get; set; } }
    private sealed class ElementSnapshot { public string ScanKey { get; set; }=""; public string CssPath { get; set; }=""; public string TagName { get; set; }=""; public string? InputType { get; set; } public string? Role { get; set; } public string? AccessibleName { get; set; } public string? VisibleText { get; set; } public string? Label { get; set; } public string? Placeholder { get; set; } public string? NameAttribute { get; set; } public string? HtmlId { get; set; } public string? TestId { get; set; } public bool Enabled { get; set; } public bool Actionable { get; set; } public bool Destructive { get; set; } public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } }
}
