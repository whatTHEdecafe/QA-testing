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
    public async Task<ActionResult<StartScanResponse>> Start(Guid targetId, CancellationToken token)
    {
        var result = await scans.StartAsync(targetId, token);
        return AcceptedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScanSummaryResponse>>> List([FromQuery] int limit = 50,
        CancellationToken token = default) => Ok(await scans.ListAsync(limit, token));

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

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken token)
    {
        var result = await scans.CancelAsync(id, token);
        return result.Outcome switch
        {
            ScanCancellationOutcome.CancellationRequested => Accepted(result.Scan),
            ScanCancellationOutcome.AlreadyCancelled => Ok(result.Scan),
            ScanCancellationOutcome.NotFound => NotFound(new ProblemDetails
            {
                Title = "Scan was not found",
                Detail = result.Message,
                Status = StatusCodes.Status404NotFound
            }),
            _ => Conflict(new ProblemDetails
            {
                Title = "Scan cannot be cancelled",
                Detail = result.Message,
                Status = StatusCodes.Status409Conflict
            })
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
