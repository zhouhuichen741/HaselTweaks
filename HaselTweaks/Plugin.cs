using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.Logging;
using Microsoft.Extensions.Hosting;
using System;

namespace HaselTweaks;

[AutoConstruct]
public sealed partial class Plugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private IHost _host;
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
            .UseContentRoot(_pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(_pluginInterface);
                services.AddConfig(PluginConfig.Load(_pluginInterface));
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();
    }

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.InitializeCustomClientStructs();
        return _host.StartOnFrameworkThread(_framework, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDev)
            return;

        try
        {
            await _host.StopOnFrameworkThread(_framework).ConfigureAwait(false);
        }
        finally
        {
            _host.Dispose();
        }
    }
}