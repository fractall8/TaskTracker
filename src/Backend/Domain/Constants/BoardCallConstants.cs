namespace Domain.Constants;

public static class BoardCallConstants
{
    public const int MaxAcsRoomIdLength = 128;

    public const int MaxAcsCommunicationUserIdLength = 255;

    // Global for now; the intended path to a per-plan limit is Subscription.Plans.*.Limits.MaxCallParticipants
    // (mirroring MaxBoardsPerWorkspace etc.) — this is the only place this number should ever appear.
    public const int MaxParticipants = 4;
}
