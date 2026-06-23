using Microsoft.EntityFrameworkCore;
using QaAutomation.Core.Targets;
using QaAutomation.Core.Scans;

namespace QaAutomation.Infrastructure.Persistence;

public sealed class QaAutomationDbContext(DbContextOptions<QaAutomationDbContext> options) : DbContext(options)
{
    public DbSet<Target> Targets => Set<Target>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<ScannedPage> ScannedPages => Set<ScannedPage>();
    public DbSet<DetectedElement> DetectedElements => Set<DetectedElement>();
    public DbSet<SelectorCandidate> SelectorCandidates => Set<SelectorCandidate>();
    public DbSet<ScanDiagnostic> ScanDiagnostics => Set<ScanDiagnostic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var target = modelBuilder.Entity<Target>();
        target.ToTable("Targets");
        target.HasKey(x => x.Id);
        target.Property(x => x.Name).HasMaxLength(120).IsRequired();
        target.Property(x => x.StartingUrl).HasMaxLength(2048).IsRequired();
        target.Property(x => x.AllowedHost).HasMaxLength(253).IsRequired();
        target.Property(x => x.Environment).HasConversion<string>().HasMaxLength(32).IsRequired();
        target.Property(x => x.Description).HasMaxLength(1000);
        target.HasIndex(x => x.Name);
        target.HasIndex(x => new { x.IsEnabled, x.Environment });

        var scan = modelBuilder.Entity<Scan>();
        scan.ToTable("Scans"); scan.HasKey(x => x.Id);
        scan.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        scan.Property(x => x.Stage).HasMaxLength(250).IsRequired(); scan.Property(x => x.StartingUrl).HasMaxLength(2048).IsRequired();
        scan.Property(x => x.FinalUrl).HasMaxLength(2048); scan.Property(x => x.PageTitle).HasMaxLength(500);
        scan.Property(x => x.BrowserName).HasMaxLength(50); scan.Property(x => x.FailureSummary).HasMaxLength(2000); scan.Property(x => x.CancellationReason).HasMaxLength(500);
        scan.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
        scan.HasIndex(x => x.TargetId); scan.HasIndex(x => x.Status); scan.HasIndex(x => x.RequestedAtUtc); scan.HasIndex(x => new { x.TargetId, x.Status });

        var page = modelBuilder.Entity<ScannedPage>();
        page.ToTable("ScannedPages"); page.HasKey(x => x.Id);
        page.Property(x => x.OriginalUrl).HasMaxLength(2048); page.Property(x => x.FinalUrl).HasMaxLength(2048); page.Property(x => x.Route).HasMaxLength(2048);
        page.Property(x => x.OriginalPageTitle).HasMaxLength(500); page.Property(x => x.MainHeading).HasMaxLength(500); page.Property(x => x.GeneratedDisplayName).HasMaxLength(500);
        page.Property(x => x.ScreenshotPath).HasMaxLength(500); page.Property(x => x.ThumbnailPath).HasMaxLength(500);
        page.HasOne(x => x.Scan).WithMany(x => x.Pages).HasForeignKey(x => x.ScanId).OnDelete(DeleteBehavior.Cascade); page.HasIndex(x => new { x.ScanId, x.DiscoveryOrder });

        var element = modelBuilder.Entity<DetectedElement>();
        element.ToTable("DetectedElements"); element.HasKey(x => x.Id);
        element.Property(x => x.TagName).HasMaxLength(80); element.Property(x => x.InputType).HasMaxLength(80); element.Property(x => x.AccessibleRole).HasMaxLength(100);
        element.Property(x => x.AccessibleName).HasMaxLength(1000); element.Property(x => x.VisibleText).HasMaxLength(2000); element.Property(x => x.AssociatedLabel).HasMaxLength(1000);
        element.Property(x => x.Placeholder).HasMaxLength(1000); element.Property(x => x.NameAttribute).HasMaxLength(500); element.Property(x => x.HtmlId).HasMaxLength(500); element.Property(x => x.TestId).HasMaxLength(500);
        element.Property(x => x.Classification).HasConversion<string>().HasMaxLength(64); element.Property(x => x.CropPath).HasMaxLength(500); element.Property(x => x.ScreenshotError).HasMaxLength(2000);
        element.HasOne(x => x.Page).WithMany(x => x.Elements).HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.Cascade); element.HasIndex(x => new { x.PageId, x.DiscoveryOrder });

        var selector = modelBuilder.Entity<SelectorCandidate>();
        selector.ToTable("SelectorCandidates"); selector.HasKey(x => x.Id); selector.Property(x => x.SelectorType).HasMaxLength(50); selector.Property(x => x.SelectorValue).HasMaxLength(2000); selector.Property(x => x.Confidence).HasPrecision(5, 4);
        selector.HasOne(x => x.Element).WithMany(x => x.SelectorCandidates).HasForeignKey(x => x.ElementId).OnDelete(DeleteBehavior.Cascade); selector.HasIndex(x => new { x.ElementId, x.Priority });

        var diagnostic = modelBuilder.Entity<ScanDiagnostic>();
        diagnostic.ToTable("ScanDiagnostics"); diagnostic.HasKey(x => x.Id); diagnostic.Property(x => x.Category).HasConversion<string>().HasMaxLength(64); diagnostic.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32);
        diagnostic.Property(x => x.Message).HasMaxLength(4000); diagnostic.Property(x => x.Url).HasMaxLength(2048); diagnostic.Property(x => x.Method).HasMaxLength(20);
        diagnostic.HasOne(x => x.Scan).WithMany(x => x.Diagnostics).HasForeignKey(x => x.ScanId).OnDelete(DeleteBehavior.Cascade); diagnostic.HasIndex(x => new { x.ScanId, x.CreatedAtUtc });
    }
}
