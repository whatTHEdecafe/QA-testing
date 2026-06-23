using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QaAutomation.Core.Scans;
using QaAutomation.Infrastructure.Persistence;

namespace QaAutomation.Api.Controllers;

[ApiController]
[Route("api/scans")]
public sealed class ScansController(IScanService scans, QaAutomationDbContext db,
    IManagedArtifactStorage storage) : ControllerBase
{
    [HttpPost("/api/targets/{targetId:guid}/scans")]
    public async Task<ActionResult<StartScanResponse>> Start(Guid targetId, [FromBody] StartScanRequest? request, CancellationToken token)
    {
        var result = await scans.StartAsync(targetId, request, token);
        return AcceptedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ScanSummaryResponse>>> List([FromQuery] ScanHistoryQuery query,
        [FromQuery] int? limit, CancellationToken token = default)
    {
        var effective = limit is null ? query : query with { PageSize = limit.Value };
        return Ok(await scans.ListAsync(effective, token));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ScannerSettingsMetadata>> Settings(CancellationToken token) =>
        Ok(await scans.GetSettingsMetadataAsync(token));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScanDetailsResponse>> Get(Guid id, CancellationToken token)
    {
        var result = await scans.GetAsync(id, token); return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<ScanSummaryResponse>> Status(Guid id, CancellationToken token)
    {
        var result = await scans.GetAsync(id, token); return result is null ? NotFound() : Ok(result.Summary);
    }

    [HttpGet("{id:guid}/elements")]
    public async Task<ActionResult<PagedResponse<ElementResponse>>> Elements(Guid id, [FromQuery] ElementQuery query, CancellationToken token) =>
        Ok(await scans.QueryElementsAsync(id, query, token));

    [HttpGet("{id:guid}/diagnostics")]
    public async Task<ActionResult<PagedResponse<DiagnosticResponse>>> Diagnostics(Guid id, [FromQuery] DiagnosticQuery query, CancellationToken token) =>
        Ok(await scans.QueryDiagnosticsAsync(id, query, token));

    [HttpPut("{scanId:guid}/pages/{pageId:guid}/review")]
    public async Task<ActionResult<PageResponse>> UpdatePageReview(Guid scanId, Guid pageId, UpdatePageReviewRequest request, CancellationToken token)
    {
        var result = await scans.UpdatePageReviewAsync(scanId, pageId, request, token);
        return result is null ? NotFound(new ProblemDetails { Title = "Page was not found", Detail = "The page was not found in this scan.", Status = 404 }) : Ok(result);
    }

    [HttpPut("{scanId:guid}/elements/{elementId:guid}/review")]
    public async Task<ActionResult<ElementResponse>> UpdateElementReview(Guid scanId, Guid elementId, UpdateElementReviewRequest request, CancellationToken token)
    {
        var result = await scans.UpdateElementReviewAsync(scanId, elementId, request, token);
        return result is null ? NotFound(new ProblemDetails { Title = "Element was not found", Detail = "The element was not found in this scan.", Status = 404 }) : Ok(result);
    }

    [HttpPut("{scanId:guid}/elements/{elementId:guid}/manual-selector")]
    public async Task<ActionResult<ElementResponse>> SelectManualSelector(Guid scanId, Guid elementId, SelectManualSelectorRequest request, CancellationToken token)
    {
        var result = await scans.SelectManualSelectorAsync(scanId, elementId, request, token);
        return result is null ? NotFound(new ProblemDetails { Title = "Element was not found", Detail = "The element was not found in this scan.", Status = 404 }) : Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken token)
    {
        var result = await scans.CancelAsync(id, token);
        return result.Outcome switch
        {
            ScanCancellationOutcome.CancellationRequested => Accepted(result.Scan),
            ScanCancellationOutcome.AlreadyCancelled => Ok(result.Scan),
            ScanCancellationOutcome.NotFound => NotFound(new ProblemDetails { Title = "Scan was not found", Detail = result.Message, Status = StatusCodes.Status404NotFound }),
            _ => Conflict(new ProblemDetails { Title = "Scan cannot be cancelled", Detail = result.Message, Status = StatusCodes.Status409Conflict })
        };
    }

    [HttpGet("pages/{pageId:guid}/screenshot")]
    public async Task<IActionResult> PageScreenshot(Guid pageId, CancellationToken token) =>
        await Artifact(await db.ScannedPages.AsNoTracking().Where(x => x.Id == pageId).Select(x => x.ScreenshotPath).SingleOrDefaultAsync(token), token);

    [HttpGet("pages/{pageId:guid}/thumbnail")]
    public async Task<IActionResult> PageThumbnail(Guid pageId, CancellationToken token) =>
        await Artifact(await db.ScannedPages.AsNoTracking().Where(x => x.Id == pageId).Select(x => x.ThumbnailPath).SingleOrDefaultAsync(token), token);

    [HttpGet("elements/{elementId:guid}/crop")]
    public async Task<IActionResult> ElementCrop(Guid elementId, CancellationToken token) =>
        await Artifact(await db.DetectedElements.AsNoTracking().Where(x => x.Id == elementId).Select(x => x.CropPath).SingleOrDefaultAsync(token), token);

    private async Task<IActionResult> Artifact(string? path, CancellationToken token)
    {
        if (path is null) return NotFound();
        var stream = await storage.OpenReadAsync(path, token); return stream is null ? NotFound() : File(stream, "image/png");
    }
}
