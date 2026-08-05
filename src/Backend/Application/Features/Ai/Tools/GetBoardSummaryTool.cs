using Application.Ai.Projections;
using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Ai.Tools;

public record GetBoardSummaryTool(Guid BoardId) : IRequest<AiBoardDetail>;

public class GetBoardSummaryToolHandler(
    IAiDataRepository aiDataRepository,
    IBoardAccessService boardAccessService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetBoardSummaryTool, AiBoardDetail>
{
    public async Task<AiBoardDetail> Handle(GetBoardSummaryTool request, CancellationToken ct)
    {
        var access = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        return await aiDataRepository.GetBoardDetailAsync(
                   request.BoardId,
                   access.UserId,
                   dateTimeProvider.UtcNow,
                   ct)
               ?? throw new NotFoundException("Board not found.");
    }
}

public class GetBoardSummaryToolValidator : AbstractValidator<GetBoardSummaryTool>
{
    public GetBoardSummaryToolValidator()
    {
        RuleFor(tool => tool.BoardId).NotEmpty();
    }
}
