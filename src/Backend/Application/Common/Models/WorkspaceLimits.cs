namespace Application.Common.Models;

public record WorkspaceLimits(
    int? MaxMembersPerWorkspace,
    int? MaxBoardsPerWorkspace,
    int? MaxColumnsPerBoard,
    int? MaxTasksPerBoard,
    int? MaxAttachmentSizeMb,
    bool CanExportBoard);
