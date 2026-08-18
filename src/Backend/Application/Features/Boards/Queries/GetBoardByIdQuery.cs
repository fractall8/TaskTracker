using Application.Common.Mappings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using Contracts.Enums;
using Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Boards.Queries;

public record GetBoardByIdQuery(Guid BoardId, string? SearchTerm = null) : IRequest<BoardWithColumnsDto>;

public class GetBoardByIdQueryHandler(
    IBoardAccessService boardAccessService,
    IBoardExportService boardExportService,
    IBoardRepository boardRepository,
    ITaskRepository taskRepository)
    : IRequestHandler<GetBoardByIdQuery, BoardWithColumnsDto>
{
    public async Task<BoardWithColumnsDto> Handle(GetBoardByIdQuery request, CancellationToken ct)
    {
        var accessContext = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);

        var board = await boardRepository.GetBoardWithHierarchyAsync(request.BoardId, request.SearchTerm, ct);

        if (board is null)
        {
            throw new NotFoundException($"Board {request.BoardId} does not exist");
        }

        var columnDtos = board.Columns
            .OrderBy(c => c.Position)
            .Select(c => new ColumnDto(
                Id: c.Id,
                Name: c.Name,
                Position: c.Position,
                Tasks: c.Tasks.OrderBy(t => t.Position).Select(t => new TaskDto(
                    Id: t.Id,
                    Title: t.Title,
                    Description: t.Description,
                    Position: t.Position,
                    DueDate: t.DueDate,
                    IsCompleted: t.IsCompleted,
                    CompletedAt: t.CompletedAt,
                    ColumnId: t.ColumnId,
                    AssigneeId: t.AssigneeId,
                    AssigneeName: t.Assignee?.DisplayName,
                    AssigneeAvatarUrl: t.Assignee?.AvatarUrl,
                    ReporterId: t.ReporterId,
                    ReporterName: t.Reporter?.DisplayName,
                    ReporterAvatarUrl: t.Reporter?.AvatarUrl,
                    Attachments: new List<AttachmentDto>(),
                    Tags: t.ToTagDtos()
                )).ToList()
            ))
            .ToList();

        var boardExportInfo = await boardExportService.GetBoardExportInfoAsync(request.BoardId, ct);

        var totalTaskCount = await taskRepository.CountByBoardIdAsync(request.BoardId, ct);

        return new BoardWithColumnsDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            WorkspaceId: board.WorkspaceId,
            BoardRole: (BoardRoleDto)accessContext.Role,
            Columns: columnDtos,
            TotalTaskCount: totalTaskCount,
            IsArchived: board.IsArchived,
            ExportStatus: boardExportInfo?.ExportStatus,
            ReExportStatus: boardExportInfo?.ReExportStatus
        );
    }
}

public class GetBoardByIdQueryValidator : AbstractValidator<GetBoardByIdQuery>
{
    public GetBoardByIdQueryValidator(IOptions<PaginationOptions> options)
    {
        var paginationOptions = options.Value;

        RuleFor(x => x.BoardId)
            .NotEmpty().WithMessage("Board ID is required.");

        RuleFor(v => v.SearchTerm)
            .MaximumLength(paginationOptions.MaxSearchTermLength).WithMessage($"Search term must not exceed {paginationOptions.MaxSearchTermLength} characters.");
    }
}
