namespace Infrastructure.Subscriptions.Options;

public class SubscriptionLimitsOptions
{
    public int? MaxMembersPerWorkspace { get; set; }
    public int? MaxBoardsPerWorkspace { get; set; }
    public int? MaxColumnsPerBoard { get; set; }
    public int? MaxTasksPerBoard { get; set; }
    public int? MaxAttachmentSizeMb { get; set; }
    public bool CanExportBoard { get; set; }

    internal void Validate(string sectionPath)
    {
        foreach (var (name, value) in new[]
                 {
                     (nameof(MaxMembersPerWorkspace), MaxMembersPerWorkspace),
                     (nameof(MaxBoardsPerWorkspace), MaxBoardsPerWorkspace),
                     (nameof(MaxColumnsPerBoard), MaxColumnsPerBoard),
                     (nameof(MaxTasksPerBoard), MaxTasksPerBoard),
                     (nameof(MaxAttachmentSizeMb), MaxAttachmentSizeMb),
                 })
        {
            if (value is < 0)
            {
                throw new InvalidOperationException($"{sectionPath}:{name} must not be negative.");
            }
        }
    }
}
