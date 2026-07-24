using Application.Common.Models;
using Contracts.DTOs;

namespace Application.Common.Mappings;

public static class SubscriptionLimitsMappings
{
    public static SubscriptionLimitsDto ToDto(this WorkspaceLimits limits) =>
        new(
            limits.MaxMembersPerWorkspace,
            limits.MaxBoardsPerWorkspace,
            limits.MaxColumnsPerBoard,
            limits.MaxTasksPerBoard,
            limits.MaxAttachmentSizeMb,
            limits.CanExportBoard);
}
