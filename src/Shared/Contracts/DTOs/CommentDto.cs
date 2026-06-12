namespace Contracts.DTOs;

public record CommentDto(
    Guid Id,
    string Text,
    DateTimeOffset CreatedAt,
    Guid UserId,
    string UserFullName);