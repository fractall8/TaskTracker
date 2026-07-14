using Contracts.DTOs;
using Contracts.Export;
using Contracts.Messaging;

namespace TaskTracker.Functions.Processing.Export;

public class ExportContextResolver
{
    public BoardExportContext Resolve(BoardExportMessage message, BoardExportStatusInfoDto info)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(info);

        if (message.BoardId != info.BoardId)
        {
            throw new InvalidOperationException(
                $"Board export document mismatch. Message board id: {message.BoardId}, document board id: {info.BoardId}.");
        }

        return message.ExportType switch
        {
            BoardExportType.InitialExport => ResolveInitialExport(message, info),
            BoardExportType.ReExport => ResolveReExport(message, info),
            _ => throw new ArgumentOutOfRangeException(nameof(message), message.ExportType, "Unsupported export type."),
        };
    }

    private static BoardExportContext ResolveInitialExport(BoardExportMessage message, BoardExportStatusInfoDto info)
    {
        if (!IsProcessable(info.ExportStatus))
        {
            return Skip(
                message.BoardId,
                message.ExportType,
                $"Export is not pending or requested (current status: {info.ExportStatus}).");
        }

        if (info.ExportOptions is null)
        {
            throw new InvalidOperationException(
                $"Export options are missing for board {message.BoardId}.");
        }

        return new BoardExportContext(message.BoardId, message.ExportType, info.ExportOptions);
    }

    private static BoardExportContext ResolveReExport(BoardExportMessage message, BoardExportStatusInfoDto info)
    {
        if (!IsProcessable(info.ReExportStatus))
        {
            return Skip(
                message.BoardId,
                message.ExportType,
                $"Re-export is not pending or requested (current status: {info.ReExportStatus}).");
        }

        if (info.ReExportOptions is null)
        {
            throw new InvalidOperationException(
                $"Re-export options are missing for board {message.BoardId}.");
        }

        return new BoardExportContext(message.BoardId, message.ExportType, info.ReExportOptions);
    }

    private static bool IsProcessable(BoardExportStatusDto status) =>
        status is BoardExportStatusDto.Pending or BoardExportStatusDto.Requested;

    private static BoardExportContext Skip(Guid boardId, BoardExportType type, string reason) =>
        new(boardId, type, Options: null, ShouldSkip: true, SkipReason: reason);
}
