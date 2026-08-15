using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Features.Tags.Commands;

public record CreateTagCommand(Guid WorkspaceId, string Name, string? Color) : IRequest<TagDto>;

public class CreateTagCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTagCommand, TagDto>
{
    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken ct)
    {
        // Any member may add to the vocabulary; only curators may rename or delete.
        await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        var name = request.Name.Trim();

        Tag tag = null!;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await unitOfWork.AcquireDistributedLockAsync($"workspace:{request.WorkspaceId}:tags", token);

            if (await tagRepository.NameExistsAsync(request.WorkspaceId, name, null, token))
            {
                throw new ValidationException([
                    new ValidationFailure("Name", "A tag with this name already exists in the workspace.")
                ]);
            }

            tag = new Tag
            {
                Id = Guid.NewGuid(),
                WorkspaceId = request.WorkspaceId,
                Name = name,
                Color = request.Color ?? TagConstants.DefaultColor
            };

            await tagRepository.AddAsync(tag, token);
            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        return new TagDto(tag.Id, tag.Name, tag.Color);
    }
}

public class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(TagConstants.MaxNameLength)
            .WithMessage($"Tag name must not exceed {TagConstants.MaxNameLength} characters.");

        RuleFor(x => x.Color)
            .Matches(TagConstants.ColorPattern)
            .When(x => x.Color is not null)
            .WithMessage("Colour must be a hex value such as #4F46E5.");
    }
}
