namespace Contracts.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    int Position,
    DateTimeOffset? DueDate,
    Guid ColumnId,
    Guid? AssigneeId,
    Guid ReporterId,
    List<AttachmentDto> Attachments);