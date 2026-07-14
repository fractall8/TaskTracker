using Domain.Constants;

namespace Application.Options;

public class BoardExportRecoverySchedulerOptions
{
    public required string CronExpression { get; set; }

    public required int DocumentBatchSize { get; set; }

    public required int FailedRetryCooldownMinutes { get; set; }

    public required int StaleCooldownMinutes { get; set; }

    public void Validate()
    {
        var section = BoardExportStrings.BoardExportRecoveryScheduler;

        if (string.IsNullOrWhiteSpace(CronExpression))
        {
            throw new InvalidOperationException($"{section}:{nameof(CronExpression)} is not configured.");
        }

        if (DocumentBatchSize <= 0)
        {
            throw new InvalidOperationException(
                $"{section}:{nameof(DocumentBatchSize)} must be greater than 0.");
        }

        if (FailedRetryCooldownMinutes < 0)
        {
            throw new InvalidOperationException(
                $"{section}:{nameof(FailedRetryCooldownMinutes)} must be greater than or equal to 0.");
        }

        if (StaleCooldownMinutes <= 0)
        {
            throw new InvalidOperationException(
                $"{section}:{nameof(StaleCooldownMinutes)} must be greater than 0.");
        }
    }
}
