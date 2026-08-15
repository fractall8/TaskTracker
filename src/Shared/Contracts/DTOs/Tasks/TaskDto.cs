namespace Contracts.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    int Position,
    DateTimeOffset? DueDate,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    Guid ColumnId,

    Guid? AssigneeId,
    string? AssigneeName,
    string? AssigneeAvatarUrl,

    Guid ReporterId,
    string? ReporterName,
    string? ReporterAvatarUrl,

    List<AttachmentDto> Attachments);
