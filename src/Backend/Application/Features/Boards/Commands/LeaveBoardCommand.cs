using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using MediatR;

namespace Application.Features.Boards.Commands;

public record LeaveBoardCommand(Guid BoardId) : IRequest;

public class LeaveBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardMemberRepository boardMemberRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LeaveBoardCommand>
{
    public async Task Handle(LeaveBoardCommand request, CancellationToken ct)
    {
        var (currentUserId, _) = await boardAccessService.GetCurrentUserAsync(ct);

        var removed = await boardMemberRepository.RemoveUserFromBoardAsync(request.BoardId, currentUserId, ct);

        if (removed)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
