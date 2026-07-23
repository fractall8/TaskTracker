namespace Contracts.Constants;

public static class FeatureConstants
{
    public const string BoardExport = "board.export";
    public const string BoardReExport = "board.reexport";
    public const string BoardArchiveDownload = "board.archive.download";

    private static readonly HashSet<string> _all =
    [
        BoardExport,
        BoardReExport,
        BoardArchiveDownload,
    ];

    public static bool IsValid(string? feature) =>
        !string.IsNullOrEmpty(feature) && _all.Contains(feature);

    public static IReadOnlyCollection<string> GetAll() => _all;
}
