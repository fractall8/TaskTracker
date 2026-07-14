using Contracts.Export;
using TaskTracker.Functions.Interfaces;

namespace TaskTracker.Functions.Processing.Export;


public sealed class BoardExportCompletionHandlerRegistry
{
    private readonly IReadOnlyDictionary<BoardExportType, IBoardExportCompletionHandler> _handlers;

    public BoardExportCompletionHandlerRegistry(IEnumerable<IBoardExportCompletionHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _handlers = handlers.ToDictionary(handler => handler.Type);

        var missingTypes = Enum.GetValues<BoardExportType>()
            .Where(type => !_handlers.ContainsKey(type))
            .ToArray();

        if (missingTypes.Length > 0)
        {
            throw new InvalidOperationException(
                $"Missing board export completion handlers for: {string.Join(", ", missingTypes)}.");
        }
    }

    public IBoardExportCompletionHandler Get(BoardExportType type) =>
        _handlers.TryGetValue(type, out var handler)
            ? handler
            : throw new InvalidOperationException($"No completion handler registered for {type}.");
}
