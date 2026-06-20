namespace Contracts.Requests;

public record UpdateBoardRequest(
    string Name,
    string? Description
);
