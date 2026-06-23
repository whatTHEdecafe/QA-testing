namespace QaAutomation.Core.Scans;

public sealed class ScannerOptions
{
    public const string SectionName = "Scanner";
    public const int MinOverallTimeoutSeconds = 10;
    public const int MaxOverallTimeoutSeconds = 600;
    public const int MinNavigationTimeoutMilliseconds = 1_000;
    public const int MaxNavigationTimeoutMilliseconds = 120_000;
    public const int MinActionTimeoutMilliseconds = 500;
    public const int MaxActionTimeoutMilliseconds = 60_000;
    public const int MinMaximumDetectedElements = 1;
    public const int MaxMaximumDetectedElements = 500;
    public const int MinMaximumDiagnosticRecords = 0;
    public const int MaxMaximumDiagnosticRecords = 1000;
    public const int MinElementScreenshotPadding = 0;
    public const int MaxElementScreenshotPadding = 40;
    public const int MinViewportWidth = 320;
    public const int MaxViewportWidth = 3840;
    public const int MinViewportHeight = 320;
    public const int MaxViewportHeight = 2160;
    public int OverallTimeoutSeconds { get; set; } = 120;
    public int NavigationTimeoutMilliseconds { get; set; } = 30_000;
    public int ActionTimeoutMilliseconds { get; set; } = 10_000;
    public int MaximumDetectedElements { get; set; } = 150;
    public string ScreenshotDirectory { get; set; } = "app-data/scans";
    public int ElementScreenshotPadding { get; set; } = 8;
    public bool Headless { get; set; } = true;
    public int ViewportWidth { get; set; } = 1440;
    public int ViewportHeight { get; set; } = 900;
    public int MaximumDiagnosticRecords { get; set; } = 250;
}
