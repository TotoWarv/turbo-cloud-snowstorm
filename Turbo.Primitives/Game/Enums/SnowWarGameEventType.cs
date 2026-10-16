namespace Turbo.Primitives.Game.Enums;

public enum SnowWarGameEventType
{
    HumanLeftGame = 1,
    NewMoveTarget = 2,
    HumanThrowsSnowballAtHuman = 3,
    HumanThrowsSnowballAtPosition = 4,
    HumanStartsToMakeASnowball = 7,
    CreateSnowball = 8,
    MachineCreatesSnowball = 11,
    HumanGetsSnowballsFromMachine = 12,
    RayGunBurst = 100,
}
