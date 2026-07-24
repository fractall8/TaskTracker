using Contracts.Constants;
using Contracts.DTOs;

namespace Domain.Subscriptions;

public static class PlanCopyCatalog
{
    private static readonly Dictionary<string, string> _featureCopy = new(StringComparer.OrdinalIgnoreCase)
    {
        [FeatureConstants.BoardExport] = "Archive and export boards",
        [FeatureConstants.BoardReExport] = "Re-export updated archives",
        [FeatureConstants.BoardArchiveDownload] = "Download board archives",
    };

    public static IReadOnlyList<string> BuildSellingPoints(SubscriptionLimitsDto limits, IReadOnlyList<string> features)
    {
        var points = new List<string>
        {
            FormatCount(limits.MaxMembersPerWorkspace, "team member", "team members"),
            FormatCount(limits.MaxBoardsPerWorkspace, "active board", "active boards"),
            FormatCount(limits.MaxColumnsPerBoard, "column per board", "columns per board"),
            FormatCount(limits.MaxTasksPerBoard, "task per board", "tasks per board"),
            FormatAttachmentSize(limits.MaxAttachmentSizeMb),
        };

        points.AddRange(features.Select(DescribeFeature));

        return points;
    }

    public static string DescribeFeature(string featureKey) =>
        _featureCopy.TryGetValue(featureKey, out var copy) ? copy : Humanize(featureKey);

    private static string FormatCount(int? limit, string singular, string plural) =>
        limit switch
        {
            null => $"Unlimited {plural}",
            1 => $"Up to 1 {singular}",
            _ => $"Up to {limit} {plural}",
        };

    private static string FormatAttachmentSize(int? maxMb) =>
        maxMb is { } mb ? $"Up to {mb} MB per attachment" : "Unlimited attachment size";

    private static string Humanize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        var words = key.Replace('.', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}
