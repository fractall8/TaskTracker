using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Boards.Commands;

public record DeleteBoardCommand(Guid BoardId) : IRequest;

public class DeleteBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IBoardCallRepository boardCallRepository,
    IBoardCallLifecycleService boardCallLifecycleService,
    IAttachmentRepository attachmentRepository,
    IFileService fileService,
    ILogger<DeleteBoardCommandHandler> logger) : IRequestHandler<DeleteBoardCommand>
{
    public async Task Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
    {
        var boardAccessContext = await boardAccessService.EnsureCanDeleteBoardAsync(request.BoardId, cancellationToken);

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken)
                    ?? throw new NotFoundException($"Board with ID {request.BoardId} not found.");

        // The ACS room (and any still-connected participants) must be released before the board
        // disappears from under them — otherwise it's never cleaned up, since nothing else would ever
        // call EndCallAsync for a call whose board no longer exists.
        var activeCall = await boardCallRepository.GetActiveCallForBoardAsync(request.BoardId, cancellationToken);

        if (activeCall is not null)
        {
            await boardCallLifecycleService.EndCallAsync(activeCall.Id, boardAccessContext.UserId, cancellationToken);
        }

        var fileUrlsToDelete = await attachmentRepository.GetUrlsByBoardIdAsync(request.BoardId, cancellationToken);

        await boardRepository.SoftDeleteCascadeAsync(request.BoardId, cancellationToken);

        foreach (var fileUrl in fileUrlsToDelete)
        {
            try
            {
                await fileService.DeleteFileAsync(fileUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete orphaned blob for Board {BoardId}: {FileUrl}", request.BoardId, fileUrl);
            }
        }
    }
}

public class DeleteBoardCommandValidator : AbstractValidator<DeleteBoardCommand>
{
    public DeleteBoardCommandValidator()
    {
        RuleFor(v => v.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");
    }
}
