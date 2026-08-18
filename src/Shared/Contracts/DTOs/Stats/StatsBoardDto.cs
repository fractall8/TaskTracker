namespace Contracts.DTOs;

public record StatsBoardDto(Guid BoardId, string BoardName, int CompletedInPeriod, int OpenNow);
