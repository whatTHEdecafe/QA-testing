using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QaAutomation.Core.Scans;
using QaAutomation.Core.Targets;
using QaAutomation.Infrastructure.Persistence;
using QaAutomation.Infrastructure.Scans;

namespace QaAutomation.Tests.Scans;

public sealed class ScanServiceTests
{
    [Fact]
    public async Task Start_RejectsDisabledTarget()
    {
        await using var db=Database();var target=Target(false);db.Targets.Add(target);await db.SaveChangesAsync();
        var service=Service(db,new ScanJobQueue());await Assert.ThrowsAsync<DomainValidationException>(()=>service.StartAsync(target.Id,null,CancellationToken.None));
    }

    [Fact]
    public async Task Start_PreventsDuplicateActiveScans()
    {
        await using var db=Database();var target=Target(true);db.Targets.Add(target);await db.SaveChangesAsync();var service=Service(db,new ScanJobQueue());
        await service.StartAsync(target.Id,null,CancellationToken.None);await Assert.ThrowsAsync<DomainValidationException>(()=>service.StartAsync(target.Id,null,CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_QueuedScan_PersistsCancelledTerminalState()
    {
        await using var db=Database();var target=Target(true);db.Targets.Add(target);await db.SaveChangesAsync();var service=Service(db,new ScanJobQueue());
        var queued=await service.StartAsync(target.Id,null,CancellationToken.None);var result=await service.CancelAsync(queued.Id,CancellationToken.None);
        Assert.Equal(ScanCancellationOutcome.CancellationRequested,result.Outcome);Assert.Equal(ScanStatus.Cancelled,result.Scan!.Status);Assert.Equal(ScanStatus.Cancelled,(await db.Scans.FindAsync(queued.Id))!.Status);
    }

    [Fact]
    public async Task Cancel_RunningScan_RequestsCancellationWithoutCorruptingState()
    {
        await using var db=Database();var target=Target(true);db.Targets.Add(target);var scan=RunningScan(target.Id);db.Scans.Add(scan);await db.SaveChangesAsync();var queue=new ScanJobQueue();await queue.QueueAsync(scan.Id,CancellationToken.None);_ = await queue.DequeueAsync(CancellationToken.None);var token=queue.GetCancellationToken(scan.Id);var service=Service(db,queue);
        var result=await service.CancelAsync(scan.Id,CancellationToken.None);
        Assert.Equal(ScanCancellationOutcome.CancellationRequested,result.Outcome);Assert.Equal(ScanStatus.Running,result.Scan!.Status);Assert.True(result.Scan.CancellationRequested);Assert.True(token.IsCancellationRequested);
        var saved=await db.Scans.FindAsync(scan.Id);Assert.Equal(ScanStatus.Running,saved!.Status);Assert.True(saved.CancellationRequested);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledScan_IsIdempotentSuccess()
    {
        await using var db=Database();var target=Target(true);var scan=QueuedScan(target.Id);scan.Cancel(DateTimeOffset.UtcNow,"Cancelled by user");db.Targets.Add(target);db.Scans.Add(scan);await db.SaveChangesAsync();var service=Service(db,new ScanJobQueue());
        var result=await service.CancelAsync(scan.Id,CancellationToken.None);
        Assert.Equal(ScanCancellationOutcome.AlreadyCancelled,result.Outcome);Assert.Equal(ScanStatus.Cancelled,result.Scan!.Status);Assert.True(result.Scan.CancellationRequested);
    }

    [Fact]
    public async Task Cancel_CompletedScan_ReturnsConsistentNotCancellableResult()
    {
        await using var db=Database();var target=Target(true);var scan=RunningScan(target.Id);scan.Complete(DateTimeOffset.UtcNow.AddSeconds(2));db.Targets.Add(target);db.Scans.Add(scan);await db.SaveChangesAsync();var service=Service(db,new ScanJobQueue());
        var result=await service.CancelAsync(scan.Id,CancellationToken.None);
        Assert.Equal(ScanCancellationOutcome.NotCancellable,result.Outcome);Assert.Equal(ScanStatus.Completed,result.Scan!.Status);Assert.False((await db.Scans.FindAsync(scan.Id))!.CancellationRequested);
    }

    [Fact]
    public async Task Cancel_FailedScan_ReturnsConsistentNotCancellableResult()
    {
        await using var db=Database();var target=Target(true);var scan=RunningScan(target.Id);scan.Fail(DateTimeOffset.UtcNow.AddSeconds(2),"Navigation failed.");db.Targets.Add(target);db.Scans.Add(scan);await db.SaveChangesAsync();var service=Service(db,new ScanJobQueue());
        var result=await service.CancelAsync(scan.Id,CancellationToken.None);
        Assert.Equal(ScanCancellationOutcome.NotCancellable,result.Outcome);Assert.Equal(ScanStatus.Failed,result.Scan!.Status);Assert.False((await db.Scans.FindAsync(scan.Id))!.CancellationRequested);
    }

    [Fact]
    public async Task Cancel_WhenWorkerAlreadyPersistedCancelled_ReturnsSuccessfulCancelledState()
    {
        await using var db=Database();var target=Target(true);var scan=RunningScan(target.Id);db.Targets.Add(target);db.Scans.Add(scan);await db.SaveChangesAsync();
        var workerScan=await db.Scans.FindAsync(scan.Id);workerScan!.Cancel(DateTimeOffset.UtcNow.AddSeconds(1),"Cancelled by user");await db.SaveChangesAsync();
        var result=await Service(db,new ScanJobQueue()).CancelAsync(scan.Id,CancellationToken.None);
        Assert.Equal(ScanCancellationOutcome.AlreadyCancelled,result.Outcome);Assert.Equal(ScanStatus.Cancelled,result.Scan!.Status);
    }

    [Fact]
    public async Task Recovery_PersistsInterruptedScansAsFailed()
    {
        await using var db=Database();var target=Target(true);db.Targets.Add(target);await db.SaveChangesAsync();var service=Service(db,new ScanJobQueue());
        var interrupted=await service.StartAsync(target.Id,null,CancellationToken.None);Assert.Equal(1,await service.RecoverInterruptedAsync(CancellationToken.None));Assert.Equal(ScanStatus.Failed,(await db.Scans.FindAsync(interrupted.Id))!.Status);
    }

    private static ScanService Service(QaAutomationDbContext db,IScanJobQueue queue)=>new(db,queue,TimeProvider.System,Options.Create(new ScannerOptions()));
    private static QaAutomationDbContext Database()=>new(new DbContextOptionsBuilder<QaAutomationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Target Target(bool enabled)=>new(){Id=Guid.NewGuid(),Name=Guid.NewGuid().ToString(),StartingUrl="https://app.example.com/start",AllowedHost="example.com",Environment=TargetEnvironment.Staging,IsEnabled=enabled,CreatedAtUtc=DateTimeOffset.UtcNow,UpdatedAtUtc=DateTimeOffset.UtcNow};
    private static Scan QueuedScan(Guid targetId)=>new(){Id=Guid.NewGuid(),TargetId=targetId,Status=ScanStatus.Queued,Stage="Waiting for scanner",RequestedAtUtc=DateTimeOffset.UtcNow,StartingUrl="https://app.example.com/start",ViewportWidth=1440,ViewportHeight=900};
    private static Scan RunningScan(Guid targetId){var scan=QueuedScan(targetId);scan.Start(DateTimeOffset.UtcNow);return scan;}
}
