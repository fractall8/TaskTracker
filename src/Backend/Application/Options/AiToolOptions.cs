namespace Application.Options;

public class AiToolOptions
{
    public const string SectionName = "AiTools";

    public bool Enabled { get; set; } = true;

    public int MaxToolCallsPerTurn { get; set; } = 5;

    public int MaxRowsPerTool { get; set; } = 25;

    // Must stay false on this branch: descriptions are the largest free-text and injection surface.
    public bool IncludeTaskDescriptions { get; set; }

    public void Validate()
    {
        if (MaxToolCallsPerTurn is < 1 or > 20)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(MaxToolCallsPerTurn)} must be between 1 and 20.");
        }

        if (MaxRowsPerTool is < 1 or > 100)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(MaxRowsPerTool)} must be between 1 and 100.");
        }
    }
}
