namespace TaskTracker.Functions.Models;

public sealed class BoardExportArchive(Stream content, string fileName) : IAsyncDisposable
{
    public Stream Content { get; } = content;
    public string FileName { get; } = fileName;

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync();
    }
}
