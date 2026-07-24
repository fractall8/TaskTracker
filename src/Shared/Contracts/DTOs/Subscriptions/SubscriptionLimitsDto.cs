namespace Contracts.DTOs;

public record SubscriptionLimitsDto(
    int? MaxMembersPerWorkspace,
    int? MaxBoardsPerWorkspace,
    int? MaxColumnsPerBoard,
    int? MaxTasksPerBoard,
    int? MaxAttachmentSizeMb,
    bool CanExportBoard);
