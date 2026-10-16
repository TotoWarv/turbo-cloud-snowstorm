namespace Turbo.Games.SnowWar.Simulation;

internal sealed record SnowWarResolvedEvent(
    SnowWarScheduledEvent Event,
    SnowWarHuman? Human,
    SnowWarGameObject? Other = null,
    SnowWarRayGun? RayGun = null
);
