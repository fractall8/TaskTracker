using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Features.Tags.Queries;

public record GetWorkspaceTagsQuery(Guid WorkspaceId) : IRequest<List<TagDto>>;

public class GetWorkspaceTagsQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    ITagRepository tagRepository)
    : IRequestHandler<GetWorkspaceTagsQuery, List<TagDto>>
{
    public async Task<List<TagDto>> Handle(GetWorkspaceTagsQuery request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        var tags = await tagRepository.GetByWorkspaceIdAsync(request.WorkspaceId, ct);

        return [.. tags.Select(tag => new TagDto(tag.Id, tag.Name, tag.Color))];
    }
}

public class GetWorkspaceTagsQueryValidator : AbstractValidator<GetWorkspaceTagsQuery>
{
    public GetWorkspaceTagsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
