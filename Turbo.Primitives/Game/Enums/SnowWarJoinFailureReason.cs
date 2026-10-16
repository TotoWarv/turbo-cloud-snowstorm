namespace Turbo.Primitives.Game.Enums;

public enum SnowWarJoinFailureReason
{
    Kicked = 1,
    DuplicateMachineId = 2,
    InvitationRequired = 3,
    NoSpaceInTeam = 4,
    TeamNotFound = 5,
    UserHasActiveInstance = 6,
    UserHasPendingInstanceRequest = 7,
    UserHasNoFreeGamesLeft = 8,
}
