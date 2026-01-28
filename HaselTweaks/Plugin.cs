namespace HaselTweaks;

using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.Logging;
using Microsoft.Extensions.Hosting;
using System;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;
    private bool isDev;

    public Plugin(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, IFramework framework)
    {
    #if !DEBUG 
        if (pluginInterface.IsDev || !pluginInterface.SourceRepository.Contains("zhouhuichen741")) 
        { 
            isDev = true;
            return;
        }
    #endif
        pluginInterface.InitializeCustomClientStructs();

        _host = new HostBuilder()
            .UseContentRoot(pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(pluginInterface);
                services.AddConfig(PluginConfig.Load(pluginInterface, pluginLog));
                services.AddHaselCommon();
                services.AddHaselTweaks();
            })
            .Build();

        framework.RunOnFrameworkThread(_host.Start);
    }

    void IDisposable.Dispose()
    {
        if (isDev) 
            return;
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}
