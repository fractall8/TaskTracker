using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Boards.Commands;

public record DeleteBoardCommand(Guid BoardId) : IRequest;

public class DeleteBoardCommandHandler(
    IBoardAccessService boardAccessService,
    IBoardRepository boardRepository,
    IAttachmentRepository attachmentRepository,
    IFileService fileService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteBoardCommandHandler> logger) : IRequestHandler<DeleteBoardCommand>
{
    public async Task Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
    {
        await boardAccessService.EnsureCanDeleteBoardAsync(request.BoardId, cancellationToken);

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Board with ID {request.BoardId} not found.");

        var fileUrlsToDelete = await attachmentRepository.GetUrlsByBoardIdAsync(request.BoardId, cancellationToken);

        boardRepository.Delete(board);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
