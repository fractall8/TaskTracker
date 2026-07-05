using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Workspaces.Commands;

public record CreateWorkspaceCommand(string Name, string? Description) : IRequest<WorkspaceDto>;

public class CreateWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    public async Task<WorkspaceDto> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId =
            await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u.Id,
                cancellationToken);

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Members = []
        };

        var ownerMember = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = currentUserId,
            Role = WorkspaceRole.Owner,
            JoinedAt = DateTimeOffset.UtcNow
        };

        workspace.Members.Add(ownerMember);

        await workspaceRepository.AddAsync(workspace, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkspaceDto(workspace.Id, workspace.Name, workspace.Description, Contracts.Enums.WorkspaceRoleDto.Owner);
    }
}

public class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(WorkspaceConstants.MaxNameLength);
        RuleFor(x => x.Description).MaximumLength(WorkspaceConstants.MaxDescriptionLength);
    }
}
