using Microsoft.AspNetCore.Mvc;
using QaAutomation.Core.Targets;

namespace QaAutomation.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ITargetService targetService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> Summary(CancellationToken token) =>
        Ok(await targetService.GetDashboardSummaryAsync(token));
}
