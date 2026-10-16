using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Turbo.Contracts.Plugins;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar;

namespace Turbo.Games;

public sealed class GamesModule : IHostPluginModule
{
    public string Key => "turbo-games";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<SnowWarConfig>(
            builder.Configuration.GetSection(SnowWarConfig.SECTION_NAME)
        );
        services.AddSingleton<SnowWarArenaCatalog>();
        services.TryAddSingleton(TimeProvider.System);
    }
}
