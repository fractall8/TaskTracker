namespace Contracts.DTOs;

public record UpdateBoardRequest(
    string Name, 
    string? Description
);