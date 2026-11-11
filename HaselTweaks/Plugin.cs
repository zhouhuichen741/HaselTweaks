using System.Threading;
using System.Threading.Tasks;
using HaselCommon;

namespace HaselTweaks;

[AutoConstruct]
public sealed partial class Plugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private Host _host;
    private bool _isDev;

    [AutoPostConstruct]
    private void Initialize()
    {

#if !DEBUG
        if (_pluginInterface.IsDev || !_pluginInterface.SourceRepository.Contains("zhouhuichen741"))
        {
            _isDev = true;
            return;
        }
#endif

        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddFallbackProvider(_pluginInterface);
                services.AddSingleton(_pluginInterface);
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();
    }

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.InitializeCustomClientStructs();
        return _host.StartAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDev)
            return;

        try
        {
            await _host.StopAsync().ConfigureAwait(false);
        }
        finally
        {
            await _host.DisposeAsync().ConfigureAwait(false);
        }
    }
}