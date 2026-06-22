using Microsoft.EntityFrameworkCore;
using QaAutomation.Core.Targets;

namespace QaAutomation.Infrastructure.Persistence;

public sealed class QaAutomationDbContext(DbContextOptions<QaAutomationDbContext> options) : DbContext(options)
{
    public DbSet<Target> Targets => Set<Target>();

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
    }
}
