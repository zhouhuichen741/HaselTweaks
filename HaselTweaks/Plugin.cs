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
    private readonly IPluginLog _pluginLog;
    private readonly IFramework _framework;
    private IHost? _host;
    private bool _isDev;

    public Task LoadAsync(CancellationToken cancellationToken)
    {
#if !DEBUG
        if (_pluginInterface.IsDev || !_pluginInterface.SourceRepository.Contains("zhouhuichen741"))
        {
            _isDev = true;
            return Task.CompletedTask;
        }
#endif

        _pluginInterface.InitializeCustomClientStructs();

        _host = new HostBuilder()
            .UseContentRoot(_pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(_pluginInterface);
                services.AddConfig(PluginConfig.Load(_pluginInterface, _pluginLog));
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();

        return _host.StartOnFrameworkThread(_framework, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (_isDev)
            return ValueTask.CompletedTask;

        return _host?.StopOnFrameworkThread(_framework) ?? ValueTask.CompletedTask;
    }
}