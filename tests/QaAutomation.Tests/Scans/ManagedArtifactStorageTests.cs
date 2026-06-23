using Microsoft.Extensions.Options;
using QaAutomation.Core.Scans;
using QaAutomation.Infrastructure.Scans;

namespace QaAutomation.Tests.Scans;

public sealed class ManagedArtifactStorageTests
{
    [Fact]
    public void ManagedPaths_StayInsideConfiguredRoot()
    {
        var root=Path.Combine(Path.GetTempPath(),"qa-storage-"+Guid.NewGuid().ToString("N"));var storage=new ManagedArtifactStorage(Options.Create(new ScannerOptions{ScreenshotDirectory=root}));
        var relative=storage.GetRelativePath(Guid.NewGuid(),"page.png");Assert.False(Path.IsPathRooted(relative));Assert.StartsWith(Path.GetFullPath(root),storage.GetAbsoluteWritePath(relative),StringComparison.OrdinalIgnoreCase);Assert.Throws<InvalidOperationException>(()=>storage.GetAbsoluteWritePath("../escape.png"));Assert.Throws<ArgumentException>(()=>storage.GetRelativePath(Guid.NewGuid(),"../bad.png"));
    }
}
