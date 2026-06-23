using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QaAutomation.Core.Scans;
using QaAutomation.Core.Targets;
using QaAutomation.Infrastructure.Persistence;
using QaAutomation.Infrastructure.Scans;

namespace QaAutomation.Tests.Scans;

public sealed class PlaywrightScannerIntegrationTests
{
    [Fact]
    public async Task Scanner_InspectsOneLocalPageAndCreatesArtifacts()
    {
        await using var server = await LocalPageServer.StartAsync(false);
        var root=Path.Combine(Path.GetTempPath(),"qa-scan-"+Guid.NewGuid().ToString("N"));
        try
        {
            await using var db=Database();var scan=Seed(db,server.Url);var options=Options.Create(new ScannerOptions{ScreenshotDirectory=root,OverallTimeoutSeconds=30,NavigationTimeoutMilliseconds=10000,ActionTimeoutMilliseconds=5000,MaximumDetectedElements=20});
            await new PlaywrightScanExecutor(db,new ManagedArtifactStorage(options),options,TimeProvider.System,new ConsoleTestLogger<PlaywrightScanExecutor>()).ExecuteAsync(scan.Id,CancellationToken.None);
            var saved=await db.Scans.Include(x=>x.Pages).ThenInclude(x=>x.Elements).ThenInclude(x=>x.SelectorCandidates).SingleAsync();
            Assert.True(saved.Status==ScanStatus.Completed,saved.FailureSummary);Assert.Single(saved.Pages);Assert.NotEmpty(saved.Pages[0].Elements);Assert.Contains(saved.Pages[0].Elements,x=>x.AccessibleName=="Book now");
            Assert.True(File.Exists(Path.Combine(root,saved.Pages[0].ScreenshotPath!.Replace('/',Path.DirectorySeparatorChar))));Assert.True(File.Exists(Path.Combine(root,saved.Pages[0].ThumbnailPath!.Replace('/',Path.DirectorySeparatorChar))));Assert.Contains(saved.Pages[0].Elements,x=>x.CropPath is not null);
        }
        finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }

    [Fact]
    public async Task Scanner_CancellationStopsARealNavigation()
    {
        await using var server = await LocalPageServer.StartAsync(true);var root=Path.Combine(Path.GetTempPath(),"qa-cancel-"+Guid.NewGuid().ToString("N"));
        try
        {
            await using var db=Database();var scan=Seed(db,server.Url);var options=Options.Create(new ScannerOptions{ScreenshotDirectory=root,OverallTimeoutSeconds=30,NavigationTimeoutMilliseconds=20000});using var cancel=new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
            await new PlaywrightScanExecutor(db,new ManagedArtifactStorage(options),options,TimeProvider.System,NullLogger<PlaywrightScanExecutor>.Instance).ExecuteAsync(scan.Id,cancel.Token);
            Assert.Equal(ScanStatus.Cancelled,(await db.Scans.FindAsync(scan.Id))!.Status);
        }
        finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }

    [Fact]
    public async Task Scanner_ClosesBrowserAndPersistsFailureForBlockedRedirect()
    {
        await using var server=await LocalPageServer.StartAsync(false,true);var root=Path.Combine(Path.GetTempPath(),"qa-failure-"+Guid.NewGuid().ToString("N"));
        try{await using var db=Database();var scan=Seed(db,server.Url);var options=Options.Create(new ScannerOptions{ScreenshotDirectory=root,OverallTimeoutSeconds=20,NavigationTimeoutMilliseconds=5000});await new PlaywrightScanExecutor(db,new ManagedArtifactStorage(options),options,TimeProvider.System,NullLogger<PlaywrightScanExecutor>.Instance).ExecuteAsync(scan.Id,CancellationToken.None);var saved=await db.Scans.Include(x=>x.Diagnostics).SingleAsync();Assert.Equal(ScanStatus.Failed,saved.Status);Assert.Contains(saved.Diagnostics,x=>x.Category==DiagnosticCategory.NavigationError);}
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }

    private static QaAutomationDbContext Database()=>new(new DbContextOptionsBuilder<QaAutomationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Scan Seed(QaAutomationDbContext db,string url){var uri=new Uri(url);var target=new Target{Id=Guid.NewGuid(),Name="Controlled local page",StartingUrl=url,AllowedHost=uri.Host,Environment=TargetEnvironment.Development,IsEnabled=true,CreatedAtUtc=DateTimeOffset.UtcNow,UpdatedAtUtc=DateTimeOffset.UtcNow};var scan=new Scan{Id=Guid.NewGuid(),Target=target,TargetId=target.Id,Status=ScanStatus.Queued,Stage="Waiting",StartingUrl=url,RequestedAtUtc=DateTimeOffset.UtcNow,BrowserName="Chromium",ViewportWidth=1000,ViewportHeight=700};db.AddRange(target,scan);db.SaveChanges();return scan;}
    private sealed class ConsoleTestLogger<T>:ILogger<T>{public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;public bool IsEnabled(LogLevel level)=>true;public void Log<TState>(LogLevel level,EventId eventId,TState state,Exception? exception,Func<TState,Exception?,string> formatter)=>Console.WriteLine($"{level}: {formatter(state,exception)} {exception}");}

    private sealed class LocalPageServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;private readonly CancellationTokenSource _stop=new();private readonly bool _delay;private readonly bool _redirect;private readonly Task _loop;
        public string Url { get; }
        private LocalPageServer(TcpListener listener,bool delay,bool redirect){_listener=listener;_delay=delay;_redirect=redirect;Url=$"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";_loop=Run();}
        public static Task<LocalPageServer> StartAsync(bool delay,bool redirect=false){var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();return Task.FromResult(new LocalPageServer(listener,delay,redirect));}
        private async Task Run(){while(!_stop.IsCancellationRequested){try{var client=await _listener.AcceptTcpClientAsync(_stop.Token);_ = Respond(client);}catch(OperationCanceledException){break;}}}
        private async Task Respond(TcpClient client){using var owned=client;try{using var reader=new StreamReader(client.GetStream(),Encoding.ASCII,leaveOpen:true);while(!string.IsNullOrEmpty(await reader.ReadLineAsync(_stop.Token))){}if(_delay)await Task.Delay(TimeSpan.FromSeconds(10),_stop.Token);if(_redirect){var redirect=Encoding.ASCII.GetBytes("HTTP/1.1 302 Found\r\nLocation: http://example.com/blocked\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");await client.GetStream().WriteAsync(redirect,_stop.Token);return;}const string html="<!doctype html><title>Local booking</title><h1>Book a move</h1><label>Phone <input type='tel' name='phone' placeholder='555-0100'></label><button data-testid='book-now'>Book now</button><a href='/next'>Next</a>";var body=Encoding.UTF8.GetBytes(html);var header=Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");await client.GetStream().WriteAsync(header,_stop.Token);await client.GetStream().WriteAsync(body,_stop.Token);}catch(OperationCanceledException){}}
        public async ValueTask DisposeAsync(){_stop.Cancel();_listener.Stop();try{await _loop;}catch{} _stop.Dispose();}
    }
}
