using Contracts.DTOs;

namespace Contracts.Notifications.BoardActions.Payloads;

public record CommentAddedPayload(Guid TaskId, CommentDto Comment) : BoardActionPayload;
