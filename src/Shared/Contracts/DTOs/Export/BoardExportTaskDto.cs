namespace Contracts.DTOs;

public record BoardExportTaskDto(
    Guid Id,
    string Title,
    int Position,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    BoardExportUserDto Reporter,
    BoardExportUserDto? Assignee,
    string? Description,
    DateTimeOffset? DueDate,
    IReadOnlyList<BoardExportCommentDto>? Comments,
    IReadOnlyList<BoardExportAttachmentDto>? Attachments);
