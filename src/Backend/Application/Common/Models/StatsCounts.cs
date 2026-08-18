namespace Application.Common.Models;

// One grouped round trip fills all six. Cohort figures answer "of the tasks created in this window, how
// many are now done", which is the only completion rate that cannot exceed 100% (EPIC 5 Decision 4).
public record StatsCounts(
    int CreatedInPeriod,
    int CompletedOfCreatedInPeriod,
    int CreatedInPreviousPeriod,
    int CompletedOfCreatedInPreviousPeriod,
    int OverdueNow,
    int NewlyOverdue);
