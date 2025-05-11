using CounterStrikeSharp.API.Core;
using CS2KnifeArena.managers;
using Microsoft.Extensions.DependencyInjection;

namespace CS2KnifeArena.misc;

public class KnifeArenaServiceCollection : IPluginServiceCollection<CS2KnifeArena>
{
    public void ConfigureServices(IServiceCollection service)
    {
        service.AddScoped<IKnifeArena, KnifeArenaManager>();
    }
}