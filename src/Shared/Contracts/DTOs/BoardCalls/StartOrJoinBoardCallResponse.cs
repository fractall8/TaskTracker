namespace Contracts.DTOs;

public record StartOrJoinBoardCallResponse(BoardCallDto Call, AcsCallCredentialsDto Credentials);
