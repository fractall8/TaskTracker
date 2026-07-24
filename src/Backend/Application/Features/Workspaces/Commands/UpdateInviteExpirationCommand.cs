using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record UpdateInviteExpirationCommand(Guid WorkspaceId, Guid InviteId, DateTimeOffset NewExpirationDate)
    : IRequest<Unit>;

public class UpdateInviteExpirationCommandHandler(
    IWorkspaceInviteRepository inviteRepository,
    IWorkspaceAccessService workspaceAccessService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateInviteExpirationCommand, Unit>
{
    public async Task<Unit> Handle(UpdateInviteExpirationCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanManageInvitesAsync(request.WorkspaceId, ct);

        var invite = await inviteRepository.GetByIdAsync(request.InviteId, ct)
                     ?? throw new NotFoundException("Invite not found.");

        if (invite.WorkspaceId != request.WorkspaceId)
        {
            throw new ForbiddenException("Invite does not belong to this workspace.");
        }

        invite.ExpiresAt = request.NewExpirationDate.ToUniversalTime();

        inviteRepository.Update(invite);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

public class UpdateInviteExpirationCommandValidator : AbstractValidator<UpdateInviteExpirationCommand>
{
    public UpdateInviteExpirationCommandValidator()
    {
        RuleFor(x => x.NewExpirationDate)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Expiration date must be in the future.");
    }
}
