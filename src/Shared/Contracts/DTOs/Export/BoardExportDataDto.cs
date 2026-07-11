namespace Contracts.DTOs;

public record BoardExportDataDto(
    BoardExportBoardDto Board,
    BoardExportOptionsDto AppliedOptions,
    DateTimeOffset ExportedAt,
    IReadOnlyList<BoardExportColumnDto> Columns,
    IReadOnlyList<BoardExportMemberDto>? Members);
