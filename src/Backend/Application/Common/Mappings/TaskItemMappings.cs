using Contracts.DTOs;
using Domain.Entities;

namespace Application.Common.Mappings;

public static class TaskItemMappings
{
    public static TaskDto ToDto(this TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Position,
            task.DueDate,
            task.IsCompleted,
            task.CompletedAt,
            task.ColumnId,
            task.AssigneeId,
            task.Assignee?.DisplayName,
            task.Assignee?.AvatarUrl,
            task.ReporterId,
            task.Reporter?.DisplayName,
            task.Reporter?.AvatarUrl,
            task.Attachments?.Select(attachment => new AttachmentDto(
                attachment.Id,
                attachment.FileName,
                attachment.FileUrl,
                attachment.SizeInBytes,
                attachment.CreatedAt,
                attachment.CreatedById)).ToList() ?? []);
}
