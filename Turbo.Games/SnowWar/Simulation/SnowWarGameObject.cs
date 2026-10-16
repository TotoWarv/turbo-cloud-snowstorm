using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal abstract class SnowWarGameObject(int id)
{
    public int Id { get; } = id;

    public abstract SnowWarGameObjectType Type { get; }

    public bool IsActive { get; private set; } = true;

    public virtual IReadOnlyList<string> Strings => [];

    public void MarkRemoved() => IsActive = false;

    public abstract void AppendVariables(List<int> variables);

    public virtual void Update(SnowWarWorld world) { }

    public virtual bool StopsSnowball(SnowWarSnowball snowball) => false;

    public virtual void OnSnowballImpact(SnowWarWorld world, SnowWarSnowball snowball) { }
}
