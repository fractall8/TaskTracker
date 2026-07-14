using Domain.Constants;

namespace Application.Options;

public class BoardExportSchedulerOptions
{
    public required string CronExpression { get; set; }

    public required int DocumentBatchSize { get; set; }

    public void Validate()
    {
        var section = BoardExportStrings.BoardExportScheduler;

        if (string.IsNullOrWhiteSpace(CronExpression))
        {
            throw new InvalidOperationException($"{section}:{nameof(CronExpression)} is not configured.");
        }

        if (DocumentBatchSize <= 0)
        {
            throw new InvalidOperationException(
                $"{section}:{nameof(DocumentBatchSize)} must be greater than 0.");
        }
    }
}
