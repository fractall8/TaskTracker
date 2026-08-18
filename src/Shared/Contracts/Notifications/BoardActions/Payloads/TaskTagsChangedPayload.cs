using Contracts.DTOs;

namespace Contracts.Notifications.BoardActions.Payloads;

// Carries the whole set rather than a delta, so a client that missed an earlier change still converges.
public record TaskTagsChangedPayload(Guid TaskId, List<TagDto> Tags) : BoardActionPayload;
