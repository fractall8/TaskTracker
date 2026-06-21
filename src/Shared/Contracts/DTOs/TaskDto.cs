namespace Contracts.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    int Position,
    DateTimeOffset? DueDate,
    Guid ColumnId,

    Guid? AssigneeId,
    string? AssigneeName,

    Guid ReporterId,
    string? ReporterName,

    List<AttachmentDto> Attachments);