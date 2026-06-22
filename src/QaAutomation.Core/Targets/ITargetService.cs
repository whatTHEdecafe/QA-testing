namespace QaAutomation.Core.Targets;

public interface ITargetService
{
    Task<IReadOnlyList<TargetResponse>> ListAsync(CancellationToken cancellationToken);
    Task<TargetResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<TargetResponse> CreateAsync(SaveTargetRequest request, CancellationToken cancellationToken);
    Task<TargetResponse?> UpdateAsync(Guid id, SaveTargetRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken);
}
