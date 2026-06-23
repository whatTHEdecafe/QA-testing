using Microsoft.EntityFrameworkCore;
using QaAutomation.Core.Targets;
using QaAutomation.Infrastructure.Persistence;

namespace QaAutomation.Infrastructure.Targets;

public sealed class TargetService(QaAutomationDbContext dbContext, TimeProvider timeProvider) : ITargetService
{
    public async Task<IReadOnlyList<TargetResponse>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Targets.AsNoTracking().OrderBy(x => x.Name).Select(x => Map(x))
            .ToListAsync(cancellationToken);

    public async Task<TargetResponse?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Targets.AsNoTracking().Where(x => x.Id == id).Select(x => Map(x))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TargetResponse> CreateAsync(SaveTargetRequest request, CancellationToken cancellationToken)
    {
        var input = TargetValidator.Validate(request);
        var now = timeProvider.GetUtcNow();
        var target = new Target { Id = Guid.NewGuid(), CreatedAtUtc = now, UpdatedAtUtc = now };
        Apply(target, input);
        dbContext.Targets.Add(target);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(target);
    }

    public async Task<TargetResponse?> UpdateAsync(Guid id, SaveTargetRequest request, CancellationToken cancellationToken)
    {
        var input = TargetValidator.Validate(request);
        var target = await dbContext.Targets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (target is null) return null;
        Apply(target, input);
        target.UpdatedAtUtc = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(target);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var target = await dbContext.Targets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (target is null) return false;
        if (await dbContext.Scans.AnyAsync(x => x.TargetId == id, cancellationToken))
            throw new DomainValidationException(new Dictionary<string, string[]>
                { ["target"] = ["Targets with saved scan history cannot be deleted."] });
        dbContext.Targets.Remove(target);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken) =>
        new(await dbContext.Targets.CountAsync(cancellationToken),
            await dbContext.Targets.CountAsync(x => x.IsEnabled, cancellationToken));

    private static void Apply(Target target, ValidatedTarget input)
    {
        target.Name = input.Name;
        target.StartingUrl = input.StartingUrl;
        target.AllowedHost = input.AllowedHost;
        target.Environment = input.Environment;
        target.Description = input.Description;
        target.IsEnabled = input.IsEnabled;
    }

    private static TargetResponse Map(Target target) => new(target.Id, target.Name, target.StartingUrl,
        target.AllowedHost, target.Environment, target.Description, target.IsEnabled,
        target.CreatedAtUtc, target.UpdatedAtUtc);
}
