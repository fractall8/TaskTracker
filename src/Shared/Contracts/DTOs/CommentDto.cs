namespace Contracts.DTOs;

public record CommentDto(
    Guid Id,
    string Text,
    Guid TaskId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid AuthorId,
    string AuthorName);