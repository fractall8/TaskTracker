namespace Contracts.DTOs;

public record BoardExportCommentDto(
    Guid Id,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    BoardExportUserDto Author);
