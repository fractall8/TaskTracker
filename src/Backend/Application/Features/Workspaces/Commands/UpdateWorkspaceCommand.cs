using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Constants;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record UpdateWorkspaceCommand(Guid WorkspaceId, string Name, string? Description) : IRequest;

public class UpdateWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceAccessService workspaceAccessService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWorkspaceCommand>
{
    public async Task Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await workspaceAccessService.EnsureCanManageWorkspaceAsync(request.WorkspaceId, cancellationToken);

        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken)
                        ?? throw new NotFoundException("Workspace not found.");

        workspace.Name = request.Name;
        workspace.Description = request.Description;

        workspaceRepository.Update(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(WorkspaceConstants.MaxNameLength);
        RuleFor(x => x.Description).MaximumLength(WorkspaceConstants.MaxDescriptionLength);
    }
}
