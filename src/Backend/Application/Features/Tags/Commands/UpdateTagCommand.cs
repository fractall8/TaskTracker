using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Contracts.Tags;
using Domain.Constants;
using Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Features.Tags.Commands;

public record UpdateTagCommand(Guid WorkspaceId, Guid TagId, string Name, string Color) : IRequest<TagDto>;

public class UpdateTagCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTagCommand, TagDto>
{
    public async Task<TagDto> Handle(UpdateTagCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanCurateTagsAsync(request.WorkspaceId, ct);

        var name = request.Name.Trim();

        // Scoped by workspace, so a tag id from another tenant reads as missing rather than editable.
        var tag = await tagRepository.GetByIdInWorkspaceAsync(request.TagId, request.WorkspaceId, ct)
                  ?? throw new NotFoundException("Tag not found.");

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.AcquireDistributedLockAsync($"workspace:{request.WorkspaceId}:tags", token);

            if (await tagRepository.NameExistsAsync(request.WorkspaceId, name, request.TagId, token))
            {
                throw new ValidationException([
                    new ValidationFailure("Name", "A tag with this name already exists in the workspace.")
                ]);
            }

            tag.Name = name;
            tag.Color = request.Color;

            tagRepository.Update(tag);
            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        return new TagDto(tag.Id, tag.Name, tag.Color);
    }
}

public class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(TagConstants.MaxNameLength)
            .WithMessage($"Tag name must not exceed {TagConstants.MaxNameLength} characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Tag colour is required.")
            .Must(TagColors.IsKnown)
            .WithMessage($"Colour must be one of: {string.Join(", ", TagColors.All)}.");
    }
}
