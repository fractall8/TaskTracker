namespace Application.Settings;

public class FileSettings
{
    public FileTypeSettings Avatars { get; set; } = new();
    public FileTypeSettings Attachments { get; set; } = new();
}

public class FileTypeSettings
{
    public int MaxSizeMb { get; set; }
    public string[] AllowedTypes { get; set; } = Array.Empty<string>();
}
