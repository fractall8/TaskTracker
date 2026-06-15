namespace Contracts.DTOs;

public record CommentDto(
    Guid Id,
    string Text,
    Guid TaskId,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt);