using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QaAutomation.Core.Scans;
using QaAutomation.Core.Targets;
using QaAutomation.Infrastructure.Persistence;
using QaAutomation.Infrastructure.Scans;

namespace QaAutomation.Tests.Scans;

public sealed class ScannerReviewToolsTests
{
    [Fact]
    public async Task PageManualName_UpdateAndClear_UsesEffectiveFallback()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        var updated=await service.UpdatePageReviewAsync(seed.ScanId,seed.PageId,new("Customer checkout"),CancellationToken.None);
        Assert.Equal("Customer checkout",updated!.DisplayName);Assert.Equal("Customer checkout",(await service.GetAsync(seed.ScanId,CancellationToken.None))!.Pages[0].DisplayName);
        var cleared=await service.UpdatePageReviewAsync(seed.ScanId,seed.PageId,new(null),CancellationToken.None);
        Assert.Null(cleared!.UserDisplayName);Assert.Equal("Generated page",cleared.DisplayName);
    }

    [Fact]
    public async Task ElementManualName_UpdateAndClear_UsesEffectiveFallback()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        var updated=await service.UpdateElementReviewAsync(seed.ScanId,seed.ElementId,new("Primary CTA",null),CancellationToken.None);
        Assert.Equal("Primary CTA",updated!.DisplayName);Assert.True(updated.HasManualReview);
        var cleared=await service.UpdateElementReviewAsync(seed.ScanId,seed.ElementId,new(null,null),CancellationToken.None);
        Assert.Null(cleared!.UserDisplayName);Assert.Equal("Book now",cleared.DisplayName);Assert.False(cleared.HasManualReview);
    }

    [Fact]
    public async Task ClassificationOverride_UpdateAndClear_UsesEffectiveFallback()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        var updated=await service.UpdateElementReviewAsync(seed.ScanId,seed.ElementId,new(null,ElementClassification.Submission),CancellationToken.None);
        Assert.Equal(ElementClassification.Action,updated!.Classification);Assert.Equal(ElementClassification.Submission,updated.EffectiveClassification);
        var cleared=await service.UpdateElementReviewAsync(seed.ScanId,seed.ElementId,new(null,null),CancellationToken.None);
        Assert.Equal(ElementClassification.Action,cleared!.EffectiveClassification);
    }

    [Fact]
    public async Task ManualSelector_SelectAndClear_ValidatesOwnershipAndUsesEffectiveFallback()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        var selected=await service.SelectManualSelectorAsync(seed.ScanId,seed.ElementId,new(seed.SecondSelectorId),CancellationToken.None);
        Assert.Equal(seed.SecondSelectorId,selected!.ManualPreferredSelectorCandidateId);Assert.Contains(selected.Selectors,x=>x.Id==seed.SecondSelectorId&&x.IsManualPreferred&&x.IsEffectivePreferred);
        var cleared=await service.SelectManualSelectorAsync(seed.ScanId,seed.ElementId,new(null),CancellationToken.None);
        Assert.Null(cleared!.ManualPreferredSelectorCandidateId);Assert.Contains(cleared.Selectors,x=>x.Id==seed.FirstSelectorId&&x.IsScannerPreferred&&x.IsEffectivePreferred);
        await Assert.ThrowsAsync<DomainValidationException>(()=>service.SelectManualSelectorAsync(seed.ScanId,seed.ElementId,new(seed.OtherElementSelectorId),CancellationToken.None));
    }

    [Fact]
    public async Task ScanHistory_FilteringAndPagination_Work()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        var byStatus=await service.ListAsync(new(null,ScanStatus.Completed,null,null,null,null,null,1,10),CancellationToken.None);
        Assert.Contains(byStatus.Items,x=>x.Id==seed.ScanId);
        var bySearch=await service.ListAsync(new(null,null,null,null,null,null,"booking",1,10),CancellationToken.None);
        Assert.Single(bySearch.Items);
        await Assert.ThrowsAsync<DomainValidationException>(()=>service.ListAsync(new(null,null,null,null,null,null,null,0,25),CancellationToken.None));
        await Assert.ThrowsAsync<DomainValidationException>(()=>service.ListAsync(new(null,null,null,null,null,null,null,1,101),CancellationToken.None));
    }

    [Fact]
    public async Task ElementFiltering_SearchClassificationDestructiveAndManual_Work()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        await service.UpdateElementReviewAsync(seed.ScanId,seed.ElementId,new("Reviewed CTA",ElementClassification.Submission),CancellationToken.None);
        await service.SelectManualSelectorAsync(seed.ScanId,seed.ElementId,new(seed.SecondSelectorId),CancellationToken.None);
        Assert.Single((await service.QueryElementsAsync(seed.ScanId,new("Reviewed",null,null,null,null,null,null,null,null,null,null,1,25),CancellationToken.None)).Items);
        Assert.Single((await service.QueryElementsAsync(seed.ScanId,new(null,null,ElementClassification.Submission,null,null,null,null,null,null,null,null,1,25),CancellationToken.None)).Items);
        Assert.Single((await service.QueryElementsAsync(seed.ScanId,new(null,null,null,null,null,true,null,null,null,null,null,1,25),CancellationToken.None)).Items);
        Assert.Single((await service.QueryElementsAsync(seed.ScanId,new(null,null,null,null,null,null,null,null,true,true,true,1,25),CancellationToken.None)).Items);
    }

    [Fact]
    public async Task DiagnosticFiltering_CategorySeverityStatusAndPagination_Work()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        var result=await service.QueryDiagnosticsAsync(seed.ScanId,new("HTTP",DiagnosticCategory.HttpResponseError,DiagnosticSeverity.Warning,404,true,1,10),CancellationToken.None);
        Assert.Single(result.Items);Assert.Equal(1,result.PageNumber);
        await Assert.ThrowsAsync<DomainValidationException>(()=>service.QueryDiagnosticsAsync(seed.ScanId,new(null,null,null,null,true,1,0),CancellationToken.None));
    }

    [Fact]
    public async Task ScanSettingsValidation_AndSnapshotPersistence_Work()
    {
        await using var db=Database();var target=Target(true);db.Targets.Add(target);await db.SaveChangesAsync();var service=Service(db);
        await Assert.ThrowsAsync<DomainValidationException>(()=>service.StartAsync(target.Id,new(new(OverallTimeoutSeconds:1)),CancellationToken.None));
        var start=await service.StartAsync(target.Id,new(new(OverallTimeoutSeconds:30,MaximumDetectedElements:12,ViewportWidth:1024,ViewportHeight:768)),CancellationToken.None);
        var scan=await db.Scans.FindAsync(start.Id);
        Assert.Equal(30,scan!.OverallTimeoutSeconds);Assert.Equal(12,scan.MaximumDetectedElements);Assert.Equal(1024,scan.ViewportWidth);Assert.Equal(768,scan.ViewportHeight);
    }

    [Fact]
    public async Task ExistingPhaseTwoScanWithoutNewReviewFields_RemainsReadable()
    {
        await using var db=Database();var seed=await Seed(db);var service=Service(db);
        var details=await service.GetAsync(seed.ScanId,CancellationToken.None);
        Assert.Equal("Generated page",details!.Pages[0].DisplayName);Assert.Equal(ElementClassification.Action,details.Pages[0].Elements[0].EffectiveClassification);Assert.Null(details.Pages[0].Elements[0].ManualPreferredSelectorCandidateId);
    }

    private static ScanService Service(QaAutomationDbContext db)=>new(db,new ScanJobQueue(),TimeProvider.System,Options.Create(new ScannerOptions()));
    private static QaAutomationDbContext Database()=>new(new DbContextOptionsBuilder<QaAutomationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Target Target(bool enabled)=>new(){Id=Guid.NewGuid(),Name="Booking target",StartingUrl="https://app.example.com/start",AllowedHost="example.com",Environment=TargetEnvironment.Staging,IsEnabled=enabled,CreatedAtUtc=DateTimeOffset.UtcNow,UpdatedAtUtc=DateTimeOffset.UtcNow};

    private static async Task<SeedIds> Seed(QaAutomationDbContext db)
    {
        var target=Target(true);var scanId=Guid.NewGuid();var pageId=Guid.NewGuid();var elementId=Guid.NewGuid();var otherElementId=Guid.NewGuid();
        var firstSelector=Guid.NewGuid();var secondSelector=Guid.NewGuid();var otherSelector=Guid.NewGuid();
        db.Targets.Add(target);
        db.Scans.Add(new Scan{Id=scanId,TargetId=target.Id,Target=target,Status=ScanStatus.Completed,Stage="Scan completed",RequestedAtUtc=DateTimeOffset.UtcNow.AddMinutes(-3),StartedAtUtc=DateTimeOffset.UtcNow.AddMinutes(-2),CompletedAtUtc=DateTimeOffset.UtcNow.AddMinutes(-1),StartingUrl=target.StartingUrl,FinalUrl="https://app.example.com/booking",PageTitle="booking",DetectedPageCount=1,DetectedElementCount=2,ViewportWidth=1440,ViewportHeight=900});
        db.ScannedPages.Add(new ScannedPage{Id=pageId,ScanId=scanId,OriginalUrl=target.StartingUrl,FinalUrl="https://app.example.com/booking",Route="/booking",OriginalPageTitle="booking",MainHeading="Book",GeneratedDisplayName="Generated page",DiscoveryOrder=1,CreatedAtUtc=DateTimeOffset.UtcNow});
        db.DetectedElements.AddRange(
            new DetectedElement{Id=elementId,PageId=pageId,DiscoveryOrder=1,TagName="button",AccessibleName="Book now",VisibleText="Book now",Classification=ElementClassification.Action,IsActionable=true,IsVisible=true,IsEnabled=true,IsPotentiallyDestructive=false,CropPath="scan/button.png",CreatedAtUtc=DateTimeOffset.UtcNow},
            new DetectedElement{Id=otherElementId,PageId=pageId,DiscoveryOrder=2,TagName="button",AccessibleName="Delete booking",VisibleText="Delete booking",Classification=ElementClassification.PotentiallyDestructive,IsActionable=true,IsVisible=true,IsEnabled=true,IsPotentiallyDestructive=true,CreatedAtUtc=DateTimeOffset.UtcNow});
        db.SelectorCandidates.AddRange(
            new SelectorCandidate{Id=firstSelector,ElementId=elementId,SelectorType="Role",SelectorValue="button Book now",Priority=2,WasUnique=true,Confidence=.92m,IsPreferred=true},
            new SelectorCandidate{Id=secondSelector,ElementId=elementId,SelectorType="Css",SelectorValue="button.primary",Priority=8,WasUnique=true,Confidence=.5m,IsPreferred=false},
            new SelectorCandidate{Id=otherSelector,ElementId=otherElementId,SelectorType="Text",SelectorValue="Delete booking",Priority=7,WasUnique=true,Confidence=.65m,IsPreferred=true});
        db.ScanDiagnostics.AddRange(
            new ScanDiagnostic{Id=Guid.NewGuid(),ScanId=scanId,Category=DiagnosticCategory.HttpResponseError,Severity=DiagnosticSeverity.Warning,Message="HTTP 404 response",Url="https://app.example.com/missing?redacted",Method="GET",StatusCode=404,CreatedAtUtc=DateTimeOffset.UtcNow},
            new ScanDiagnostic{Id=Guid.NewGuid(),ScanId=scanId,Category=DiagnosticCategory.BrowserConsoleError,Severity=DiagnosticSeverity.Error,Message="Console failure",CreatedAtUtc=DateTimeOffset.UtcNow});
        await db.SaveChangesAsync();
        return new(scanId,pageId,elementId,firstSelector,secondSelector,otherSelector);
    }

    private sealed record SeedIds(Guid ScanId,Guid PageId,Guid ElementId,Guid FirstSelectorId,Guid SecondSelectorId,Guid OtherElementSelectorId);
}
