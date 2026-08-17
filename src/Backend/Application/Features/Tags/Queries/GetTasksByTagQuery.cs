using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Tags.Queries;

public record GetTasksByTagQuery(Guid WorkspaceId, Guid TagId) : IRequest<List<TaggedTaskDto>>;

public class GetTasksByTagQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    ITagRepository tagRepository)
    : IRequestHandler<GetTasksByTagQuery, List<TaggedTaskDto>>
{
    public async Task<List<TaggedTaskDto>> Handle(GetTasksByTagQuery request, CancellationToken ct)
    {
        var member = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        // Scoped by workspace, so a tag id from another tenant reads as missing.
        _ = await tagRepository.GetByIdInWorkspaceAsync(request.TagId, request.WorkspaceId, ct)
            ?? throw new NotFoundException("Tag not found.");

        var tasks = await tagRepository.GetTasksByTagAsync(
            request.TagId, request.WorkspaceId, member.UserId, ct);

        return
        [
            .. tasks.Select(task => new TaggedTaskDto(
                task.Id,
                task.Title,
                task.IsCompleted,
                task.DueDate,
                task.Column!.BoardId,
                task.Column.Board!.Name,
                task.Column.Board.IsArchived,
                task.Column.Name))
        ];
    }
}

public class GetTasksByTagQueryValidator : AbstractValidator<GetTasksByTagQuery>
{
    public GetTasksByTagQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();
    }
}
