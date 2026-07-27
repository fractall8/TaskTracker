using Contracts.DTOs;

namespace Contracts.Notifications.BoardActions.Payloads;

public record CallParticipantsChangedPayload(Guid BoardCallId, IReadOnlyList<BoardCallParticipantDto> Participants) : BoardActionPayload;
