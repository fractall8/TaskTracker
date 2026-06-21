using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Enums;
using MediatR;

namespace Application.Features.Workspaces.Queries;

public record GetWorkspaceByIdQuery(Guid WorkspaceId) : IRequest<WorkspaceDetailsDto>;

public class GetWorkspaceByIdQueryHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<GetWorkspaceByIdQuery, WorkspaceDetailsDto>
{
    public async Task<WorkspaceDetailsDto> Handle(GetWorkspaceByIdQuery request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, ct)
                        ?? throw new KeyNotFoundException("Workspace not found.");

        var members = workspace.Members
            .Select(m => new WorkspaceMemberDto(
                m.UserId,
                m.User!.Email,
                (WorkspaceRoleDto)m.Role,
                m.JoinedAt))
            .ToList();

        return new WorkspaceDetailsDto(workspace.Id, workspace.Name, workspace.Description, members);
    }
}
