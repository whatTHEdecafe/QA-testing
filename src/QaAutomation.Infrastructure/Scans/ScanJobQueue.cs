using System.Collections.Concurrent;
using System.Threading.Channels;
using QaAutomation.Core.Scans;

namespace QaAutomation.Infrastructure.Scans;

public sealed class ScanJobQueue : IScanJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(100) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _tokens = new();
    public async ValueTask QueueAsync(Guid id, CancellationToken token) { _tokens.TryAdd(id, new()); await _channel.Writer.WriteAsync(id, token); }
    public ValueTask<Guid> DequeueAsync(CancellationToken token) => _channel.Reader.ReadAsync(token);
    public CancellationToken GetCancellationToken(Guid id) => _tokens.GetOrAdd(id, _ => new()).Token;
    public bool RequestCancellation(Guid id) { if (!_tokens.TryGetValue(id, out var source)) return false; source.Cancel(); return true; }
    public void Complete(Guid id) { if (_tokens.TryRemove(id, out var source)) source.Dispose(); }
}
