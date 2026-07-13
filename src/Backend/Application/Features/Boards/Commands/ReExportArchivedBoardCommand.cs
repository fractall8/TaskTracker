using Contracts.DTOs;
using MediatR;

namespace Application.Features.Boards.Commands;

public record ReExportArchivedBoardCommand(Guid BoardId, BoardExportOptionsDto ReExportOptions)
    : IRequest;

public class ReExportArchivedBoardCommandHandler()
    : IRequestHandler<ReExportArchivedBoardCommand>
{
    public async Task Handle(ReExportArchivedBoardCommand request, CancellationToken ct)
    {
        // TODO implement handler
    }
}
