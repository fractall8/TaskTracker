namespace Application.Options;

public class AiToolOptions
{
    public const string SectionName = "AiTools";

    public bool Enabled { get; set; } = true;

    public int MaxToolCallsPerTurn { get; set; } = 5;

    public int MaxRowsPerTool { get; set; } = 25;

    public bool IncludeTaskDescriptions { get; set; }
}
