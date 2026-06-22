using Microsoft.AspNetCore.Mvc;
using QaAutomation.Core.Targets;

namespace QaAutomation.Api.Controllers;

[ApiController]
[Route("api/targets")]
public sealed class TargetsController(ITargetService targetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TargetResponse>>> List(CancellationToken token) =>
        Ok(await targetService.ListAsync(token));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TargetResponse>> Get(Guid id, CancellationToken token)
    {
        var target = await targetService.GetAsync(id, token);
        return target is null ? NotFound() : Ok(target);
    }

    [HttpPost]
    public async Task<ActionResult<TargetResponse>> Create(SaveTargetRequest request, CancellationToken token)
    {
        var target = await targetService.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = target.Id }, target);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TargetResponse>> Update(Guid id, SaveTargetRequest request, CancellationToken token)
    {
        var target = await targetService.UpdateAsync(id, request, token);
        return target is null ? NotFound() : Ok(target);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) =>
        await targetService.DeleteAsync(id, token) ? NoContent() : NotFound();
}
