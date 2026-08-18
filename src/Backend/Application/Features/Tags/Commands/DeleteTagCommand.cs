using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Tags.Commands;

public record DeleteTagCommand(Guid WorkspaceId, Guid TagId) : IRequest;

public class DeleteTagCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanCurateTagsAsync(request.WorkspaceId, ct);

        var tag = await tagRepository.GetByIdInWorkspaceAsync(request.TagId, request.WorkspaceId, ct)
                  ?? throw new NotFoundException("Tag not found.");

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            // Detaching first keeps a deleted tag from lingering on tasks across every board.
            await tagRepository.DetachFromAllTasksAsync(tag.Id, token);

            tagRepository.Delete(tag);
            await unitOfWork.SaveChangesAsync(token);
        }, ct);
    }
}

public class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
{
    public DeleteTagCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();
    }
}
