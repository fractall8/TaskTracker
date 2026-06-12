using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Boards.Commands;

public record CreateBoardCommand(string Name, string? Description) : IRequest<BoardDto>;

public class CreateBoardCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBoardCommand, BoardDto>
{
    public async Task<BoardDto> Handle(CreateBoardCommand request, CancellationToken ct)
    {
        var userInfo = await userRepository.GetUserByAzureAdIdAsync(
                           currentUserAccessor.AzureAdObjectId, 
                           u => new { Id = (Guid?)u.Id, u.Email }, 
                           ct);

        if (userInfo == null || userInfo.Id == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };

        var admin = new BoardMember
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            UserId = userInfo.Id.Value,
            Role = BoardRole.Admin
        };

        board.Members.Add(admin);

        await boardRepository.AddAsync(board, ct);
        
        await unitOfWork.SaveChangesAsync(ct);
        
        return new BoardDto(
            Id: board.Id,
            Name: board.Name,
            Description: board.Description,
            CreatedAt: board.CreatedAt, 
            UserRole: (Contracts.Enums.BoardRoleDto)admin.Role,
            Members: [new UserWithRoleDto(userInfo.Id.Value, userInfo.Email, null, (Contracts.Enums.BoardRoleDto)admin.Role)],
            Columns: [] 
        );
    }
}

public class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Board name is required.")
            .MaximumLength(BoardConstants.MaxNameLength).WithMessage("Board name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(BoardConstants.MaxDescriptionLength).WithMessage("Description must not exceed 500 characters.");
    }
}