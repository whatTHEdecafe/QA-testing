using Microsoft.Extensions.Options;
using QaAutomation.Core.Scans;

namespace QaAutomation.Infrastructure.Scans;

public sealed class ManagedArtifactStorage(IOptions<ScannerOptions> options) : IManagedArtifactStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.ScreenshotDirectory, Directory.GetCurrentDirectory());
    public string GetRelativePath(Guid scanId, string fileName) { if (Path.GetFileName(fileName) != fileName) throw new ArgumentException("Invalid artifact file name."); return Path.Combine(scanId.ToString("N"), fileName).Replace('\\', '/'); }
    public string GetAbsoluteWritePath(string relativePath) { var full = Resolve(relativePath); Directory.CreateDirectory(Path.GetDirectoryName(full)!); return full; }
    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken token) { var full = Resolve(relativePath); return Task.FromResult<Stream?>(File.Exists(full) ? new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true) : null); }
    public Task DeleteScanArtifactsAsync(Guid scanId, CancellationToken token) { var dir = Resolve(scanId.ToString("N")); if (Directory.Exists(dir)) Directory.Delete(dir, true); return Task.CompletedTask; }
    private string Resolve(string relativePath) { if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath)) throw new InvalidOperationException("Managed path is invalid."); var full = Path.GetFullPath(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar))); if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Managed path escapes storage root."); return full; }
}
