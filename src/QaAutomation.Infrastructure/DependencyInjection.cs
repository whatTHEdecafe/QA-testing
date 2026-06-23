using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QaAutomation.Core.Targets;
using QaAutomation.Core.Scans;
using QaAutomation.Infrastructure.Persistence;
using QaAutomation.Infrastructure.Scans;
using QaAutomation.Infrastructure.Targets;

namespace QaAutomation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QaAutomation")
            ?? throw new InvalidOperationException("Connection string 'QaAutomation' is not configured.");
        services.AddDbContext<QaAutomationDbContext>(options => options.UseSqlServer(connectionString));
        var section = configuration.GetSection(ScannerOptions.SectionName);
        var scanner = new ScannerOptions
        {
            OverallTimeoutSeconds = ReadInt(section, nameof(ScannerOptions.OverallTimeoutSeconds), 120),
            NavigationTimeoutMilliseconds = ReadInt(section, nameof(ScannerOptions.NavigationTimeoutMilliseconds), 30000),
            ActionTimeoutMilliseconds = ReadInt(section, nameof(ScannerOptions.ActionTimeoutMilliseconds), 10000),
            MaximumDetectedElements = ReadInt(section, nameof(ScannerOptions.MaximumDetectedElements), 150),
            ScreenshotDirectory = section[nameof(ScannerOptions.ScreenshotDirectory)] ?? "app-data/scans",
            ElementScreenshotPadding = ReadInt(section, nameof(ScannerOptions.ElementScreenshotPadding), 8),
            Headless = !bool.TryParse(section[nameof(ScannerOptions.Headless)], out var headless) || headless,
            ViewportWidth = ReadInt(section, nameof(ScannerOptions.ViewportWidth), 1440),
            ViewportHeight = ReadInt(section, nameof(ScannerOptions.ViewportHeight), 900),
            MaximumDiagnosticRecords = ReadInt(section, nameof(ScannerOptions.MaximumDiagnosticRecords), 250)
        };
        if (scanner.OverallTimeoutSeconds is < 10 or > 1800 || scanner.MaximumDetectedElements is < 1 or > 1000)
            throw new InvalidOperationException("Scanner configuration is outside safe limits.");
        services.AddSingleton<IOptions<ScannerOptions>>(Options.Create(scanner));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITargetService, TargetService>();
        services.AddSingleton<IScanJobQueue, ScanJobQueue>();
        services.AddSingleton<IManagedArtifactStorage, ManagedArtifactStorage>();
        services.AddScoped<IScanService, ScanService>();
        services.AddScoped<IScanExecutor, PlaywrightScanExecutor>();
        return services;
    }

    private static int ReadInt(IConfiguration section, string key, int fallback) =>
        int.TryParse(section[key], out var value) ? value : fallback;
}
