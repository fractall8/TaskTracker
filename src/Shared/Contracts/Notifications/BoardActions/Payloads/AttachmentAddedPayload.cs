using Contracts.DTOs;

namespace Contracts.Notifications.BoardActions.Payloads;

public record AttachmentAddedPayload(Guid TaskId, AttachmentDto Attachment) : BoardActionPayload;
