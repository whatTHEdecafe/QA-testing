namespace QaAutomation.Core.Scans;

public sealed class ScannerOptions
{
    public const string SectionName = "Scanner";
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
