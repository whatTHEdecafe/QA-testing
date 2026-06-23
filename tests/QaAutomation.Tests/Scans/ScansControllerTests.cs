using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QaAutomation.Api.Controllers;
using QaAutomation.Core.Scans;
using QaAutomation.Infrastructure.Persistence;
using QaAutomation.Infrastructure.Scans;

namespace QaAutomation.Tests.Scans;

public sealed class ScansControllerTests
{
    [Fact]
    public async Task Start_ReturnsAcceptedWithPollableId()
    {
        var service=new StubScanService();await using var db=new QaAutomationDbContext(new DbContextOptionsBuilder<QaAutomationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var controller=new ScansController(service,db,new ManagedArtifactStorage(Options.Create(new ScannerOptions{ScreenshotDirectory=Path.GetTempPath()})));
        var result=await controller.Start(Guid.NewGuid(),CancellationToken.None);var accepted=Assert.IsType<AcceptedAtActionResult>(result.Result);Assert.Equal(nameof(ScansController.Get),accepted.ActionName);Assert.IsType<StartScanResponse>(accepted.Value);
    }

    [Fact]
    public async Task Thumbnail_ReturnsManagedFileByDatabaseIdentifier()
    {
        var root=Path.Combine(Path.GetTempPath(),"qa-api-artifact-"+Guid.NewGuid().ToString("N"));var storage=new ManagedArtifactStorage(Options.Create(new ScannerOptions{ScreenshotDirectory=root}));
        try{await using var db=new QaAutomationDbContext(new DbContextOptionsBuilder<QaAutomationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);var scanId=Guid.NewGuid();var pageId=Guid.NewGuid();var relative=storage.GetRelativePath(scanId,"thumbnail.png");await File.WriteAllBytesAsync(storage.GetAbsoluteWritePath(relative),[1,2,3]);db.ScannedPages.Add(new ScannedPage{Id=pageId,ScanId=scanId,OriginalUrl="http://localhost",FinalUrl="http://localhost",Route="/",GeneratedDisplayName="Local",ThumbnailPath=relative,CreatedAtUtc=DateTimeOffset.UtcNow});await db.SaveChangesAsync();var controller=new ScansController(new StubScanService(),db,storage);var result=await controller.PageThumbnail(pageId,CancellationToken.None);var file=Assert.IsType<FileStreamResult>(result);Assert.Equal("image/png",file.ContentType);await file.FileStream.DisposeAsync();}
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }

    [Fact]
    public async Task Cancel_RunningScan_ReturnsAcceptedWithCurrentState()
    {
        await using var db=Database();var controller=Controller(new StubScanService(CancelResponse(ScanCancellationOutcome.CancellationRequested,ScanStatus.Running)),db);
        var result=await controller.Cancel(Guid.NewGuid(),CancellationToken.None);
        var accepted=Assert.IsType<AcceptedResult>(result);var scan=Assert.IsType<ScanSummaryResponse>(accepted.Value);Assert.Equal(ScanStatus.Running,scan.Status);Assert.True(scan.CancellationRequested);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledScan_ReturnsOkWithCancelledState()
    {
        await using var db=Database();var controller=Controller(new StubScanService(CancelResponse(ScanCancellationOutcome.AlreadyCancelled,ScanStatus.Cancelled)),db);
        var result=await controller.Cancel(Guid.NewGuid(),CancellationToken.None);
        var ok=Assert.IsType<OkObjectResult>(result);var scan=Assert.IsType<ScanSummaryResponse>(ok.Value);Assert.Equal(ScanStatus.Cancelled,scan.Status);
    }

    [Fact]
    public async Task Cancel_CompletedScan_ReturnsConflict()
    {
        await using var db=Database();var controller=Controller(new StubScanService(CancelResponse(ScanCancellationOutcome.NotCancellable,ScanStatus.Completed)),db);
        var result=await controller.Cancel(Guid.NewGuid(),CancellationToken.None);
        var conflict=Assert.IsType<ConflictObjectResult>(result);var problem=Assert.IsType<ProblemDetails>(conflict.Value);Assert.Equal(409,problem.Status);
    }

    [Fact]
    public async Task Cancel_MissingScan_ReturnsNotFound()
    {
        await using var db=Database();var controller=Controller(new StubScanService(new CancelScanResponse(ScanCancellationOutcome.NotFound,null,"Scan was not found.")),db);
        var result=await controller.Cancel(Guid.NewGuid(),CancellationToken.None);
        var notFound=Assert.IsType<NotFoundObjectResult>(result);var problem=Assert.IsType<ProblemDetails>(notFound.Value);Assert.Equal(404,problem.Status);
    }

    private static ScansController Controller(IScanService service,QaAutomationDbContext db)=>new(service,db,new ManagedArtifactStorage(Options.Create(new ScannerOptions{ScreenshotDirectory=Path.GetTempPath()})));
    private static QaAutomationDbContext Database()=>new(new DbContextOptionsBuilder<QaAutomationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static CancelScanResponse CancelResponse(ScanCancellationOutcome outcome,ScanStatus status)=>new(outcome,new ScanSummaryResponse(Guid.NewGuid(),Guid.NewGuid(),"Test target",status,"Stage",DateTimeOffset.UtcNow,status==ScanStatus.Queued?null:DateTimeOffset.UtcNow,status is ScanStatus.Completed or ScanStatus.Failed or ScanStatus.Cancelled?DateTimeOffset.UtcNow:null,"https://app.example.com/start",null,null,0,0,0,0,null,true),outcome.ToString());

    private sealed class StubScanService:IScanService
    {
        private readonly CancelScanResponse _cancelResponse;
        public StubScanService():this(new CancelScanResponse(ScanCancellationOutcome.NotCancellable,null,"Scan cannot be cancelled.")){}
        public StubScanService(CancelScanResponse cancelResponse)=>_cancelResponse=cancelResponse;
        public Task<StartScanResponse> StartAsync(Guid targetId,CancellationToken token)=>Task.FromResult(new StartScanResponse(Guid.NewGuid(),ScanStatus.Queued,"Waiting"));
        public Task<IReadOnlyList<ScanSummaryResponse>> ListAsync(int limit,CancellationToken token)=>Task.FromResult<IReadOnlyList<ScanSummaryResponse>>([]);
        public Task<ScanDetailsResponse?> GetAsync(Guid id,CancellationToken token)=>Task.FromResult<ScanDetailsResponse?>(null);
        public Task<CancelScanResponse> CancelAsync(Guid id,CancellationToken token)=>Task.FromResult(_cancelResponse);
        public Task<ScanDashboardSummary> GetDashboardSummaryAsync(CancellationToken token)=>Task.FromResult(new ScanDashboardSummary(0,null,null));
        public Task<int> RecoverInterruptedAsync(CancellationToken token)=>Task.FromResult(0);
    }
}
