namespace Contracts.Requests.Boards;

public record UpdateBoardRequest(
    string Name,
    string? Description
);
