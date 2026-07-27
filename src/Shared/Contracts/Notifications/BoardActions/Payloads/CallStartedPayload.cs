using Contracts.DTOs;

namespace Contracts.Notifications.BoardActions.Payloads;

public record CallStartedPayload(BoardCallDto Call) : BoardActionPayload;
