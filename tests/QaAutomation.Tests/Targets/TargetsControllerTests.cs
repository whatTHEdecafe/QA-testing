using Microsoft.AspNetCore.Mvc;
using QaAutomation.Api.Controllers;
using QaAutomation.Core.Targets;

namespace QaAutomation.Tests.Targets;

public sealed class TargetsControllerTests
{
    [Fact]
    public async Task Get_ReturnsNotFound_WhenTargetDoesNotExist()
    {
        var controller = new TargetsController(new StubTargetService());
        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtGet_WithPersistedTarget()
    {
        var service = new StubTargetService();
        var controller = new TargetsController(service);
        var result = await controller.Create(new SaveTargetRequest("Acme", "https://example.com",
            "example.com", TargetEnvironment.Staging, null), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(TargetsController.Get), created.ActionName);
        Assert.IsType<TargetResponse>(created.Value);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentOnlyWhenTargetWasDeleted()
    {
        var service = new StubTargetService { DeleteResult = true };
        var controller = new TargetsController(service);
        Assert.IsType<NoContentResult>(await controller.Delete(Guid.NewGuid(), CancellationToken.None));
        service.DeleteResult = false;
        Assert.IsType<NotFoundResult>(await controller.Delete(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class StubTargetService : ITargetService
    {
        public bool DeleteResult { get; set; }
        public Task<IReadOnlyList<TargetResponse>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<TargetResponse>>([]);
        public Task<TargetResponse?> GetAsync(Guid id, CancellationToken token) => Task.FromResult<TargetResponse?>(null);
        public Task<TargetResponse> CreateAsync(SaveTargetRequest request, CancellationToken token)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new TargetResponse(Guid.NewGuid(), request.Name!, request.StartingUrl!, request.AllowedHost!,
                request.Environment!.Value, request.Description, request.IsEnabled, now, now));
        }
        public Task<TargetResponse?> UpdateAsync(Guid id, SaveTargetRequest request, CancellationToken token) => Task.FromResult<TargetResponse?>(null);
        public Task<bool> DeleteAsync(Guid id, CancellationToken token) => Task.FromResult(DeleteResult);
        public Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken token) => Task.FromResult(new DashboardSummary(0, 0));
    }
}
