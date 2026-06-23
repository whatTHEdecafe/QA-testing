using QaAutomation.Core.Scans;
using QaAutomation.Infrastructure.Scans;

namespace QaAutomation.Tests.Scans;

public sealed class ScanStateAndQueueTests
{
    [Fact]
    public void Scan_EnforcesStateTransitions()
    {
        var now=DateTimeOffset.UtcNow;var scan=new Scan{Status=ScanStatus.Queued};scan.Start(now);Assert.Equal(ScanStatus.Running,scan.Status);scan.Complete(now.AddSeconds(1));Assert.Equal(ScanStatus.Completed,scan.Status);Assert.Throws<InvalidOperationException>(()=>scan.Start(now));
    }

    [Fact]
    public async Task Queue_CancellationTokenIsSignalledAndJobCompletes()
    {
        var queue=new ScanJobQueue();var id=Guid.NewGuid();await queue.QueueAsync(id,CancellationToken.None);Assert.Equal(id,await queue.DequeueAsync(CancellationToken.None));var token=queue.GetCancellationToken(id);Assert.True(queue.RequestCancellation(id));Assert.True(token.IsCancellationRequested);queue.Complete(id);Assert.False(queue.RequestCancellation(id));
    }
}
