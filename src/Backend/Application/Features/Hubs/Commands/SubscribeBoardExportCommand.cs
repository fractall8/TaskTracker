using Application.Interfaces.Repositories;
using Domain.Authorization;
using FluentValidation;
using MediatR;

namespace Application.Features.Hubs.Commands;

public record SubscribeBoardExportStatusCommand(IReadOnlyList<Guid> BoardIds, Guid AzureAdObjectId)
    : IRequest<IReadOnlyList<Guid>>;

public class SubscribeBoardExportStatusCommandHandler(
    IUserRepository userRepository,
    IBoardMemberRepository boardMemberRepository)
    : IRequestHandler<SubscribeBoardExportStatusCommand, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(
        SubscribeBoardExportStatusCommand request,
        CancellationToken ct)
    {
        var distinctBoardIds = SubscribeBoardExportStatusBoardIds.Normalize(request.BoardIds);
        var userId = await userRepository.GetUserByAzureAdIdAsync(request.AzureAdObjectId, u => u.Id, ct);

        var rolesByBoardId = await boardMemberRepository.GetUserRolesForArchivedBoardsAsync(
            distinctBoardIds,
            userId,
            ct);

        if (rolesByBoardId.Count != distinctBoardIds.Count)
        {
            throw new UnauthorizedAccessException("Some boards were not found or access is denied.");
        }

        if (rolesByBoardId.Values.Any(role => !BoardRolePermissions.CanExportBoard(role)))
        {
            throw new UnauthorizedAccessException("You do not have permission to export one or more of these boards.");
        }

        return distinctBoardIds;
    }
}

public class SubscribeBoardExportStatusCommandValidator : AbstractValidator<SubscribeBoardExportStatusCommand>
{
    public SubscribeBoardExportStatusCommandValidator()
    {
        int maxBoardCount = 50; // TODO: move to smth like settings

        RuleFor(x => x.BoardIds)
            .NotNull();

        RuleFor(x => x.BoardIds!)
            .Must(ids => SubscribeBoardExportStatusBoardIds.Normalize(ids).Count > 0)
            .WithMessage("At least one valid board id is required.");

        RuleFor(x => x.BoardIds)
            .Must(ids => SubscribeBoardExportStatusBoardIds.Normalize(ids).Count <= maxBoardCount)
            .WithMessage($"Cannot subscribe to more than {maxBoardCount} boards at once.");
    }
}

internal static class SubscribeBoardExportStatusBoardIds
{
    internal static List<Guid> Normalize(IReadOnlyList<Guid> boardIds) =>
        boardIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
}
