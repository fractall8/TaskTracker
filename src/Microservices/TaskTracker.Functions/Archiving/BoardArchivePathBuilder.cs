using System.Text;

namespace TaskTracker.Functions.Archiving;

internal sealed class BoardArchivePathBuilder
{
    private readonly Dictionary<Guid, string> _taskFolderByTaskId = new();
    private readonly Dictionary<string, int> _taskTitleUsageCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _usedEntryPaths = new(StringComparer.OrdinalIgnoreCase);

    public string BuildArchiveFileName(string boardName) =>
        $"{SanitizeFileName(boardName)}.zip";

    public string BuildAttachmentEntryPath(Guid taskId, string taskTitle, string originalFileName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(taskId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);

        var taskFolder = AllocateTaskFolderName(taskId, taskTitle);

        var entryPath = $"attachments/{taskFolder}/{SanitizeFileName(originalFileName)}";

        return EnsureUniqueEntryPath(entryPath);
    }

    private string AllocateTaskFolderName(Guid taskId, string taskTitle)
    {
        if (_taskFolderByTaskId.TryGetValue(taskId, out var existingFolder))
        {
            return existingFolder;
        }

        var baseName = SanitizePathSegment(taskTitle);

        if (!_taskTitleUsageCounts.TryGetValue(baseName, out var count))
        {
            _taskTitleUsageCounts[baseName] = 1;
            _taskFolderByTaskId[taskId] = baseName;
            return baseName;
        }

        count++;
        _taskTitleUsageCounts[baseName] = count;
        var folder = $"{baseName}_{count}";
        _taskFolderByTaskId[taskId] = folder;

        return folder;
    }

    private string EnsureUniqueEntryPath(string entryPath)
    {
        if (_usedEntryPaths.Add(entryPath))
        {
            return entryPath;
        }

        var directory = Path.GetDirectoryName(entryPath)!.Replace('\\', '/');
        var fileName = Path.GetFileName(entryPath);

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{directory}/{AppendNumericSuffix(fileName, suffix)}";
            if (_usedEntryPaths.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static string AppendNumericSuffix(string fileName, int suffix)
    {
        var extension = Path.GetExtension(fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);

        return string.IsNullOrEmpty(extension)
            ? $"{stem}_{suffix}"
            : $"{stem}_{suffix}{extension}";
    }

    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);

        foreach (var ch in name)
        {
            sb.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        return sb.Length > 0 ? sb.ToString() : "attachment";
    }

    public static string SanitizePathSegment(string segment)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(segment.Length);

        foreach (var ch in segment.Trim())
        {
            sb.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        return sb.Length > 0 ? sb.ToString() : "task";
    }
}
