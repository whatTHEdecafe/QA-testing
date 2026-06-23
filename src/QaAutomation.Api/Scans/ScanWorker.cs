using QaAutomation.Core.Scans;

namespace QaAutomation.Api.Scans;

public sealed class ScanWorker(IServiceScopeFactory scopeFactory, IScanJobQueue queue,
    ILogger<ScanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var recovered = await scope.ServiceProvider.GetRequiredService<IScanService>().RecoverInterruptedAsync(stoppingToken);
                if (recovered > 0) logger.LogWarning("Marked {Count} interrupted scans as failed", recovered);
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                var id = await queue.DequeueAsync(stoppingToken);
                try
                {
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, queue.GetCancellationToken(id));
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IScanExecutor>().ExecuteAsync(id, linked.Token);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex) { logger.LogError(ex, "Unexpected scan worker failure for {ScanId}", id); }
                finally { queue.Complete(id); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
