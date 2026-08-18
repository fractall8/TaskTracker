namespace Contracts.DTOs;

// Each card carries its current value and the same measure over the preceding equal-length window, so the
// client formats the delta (percent, count, days) without the server guessing at presentation.
// Previous values are null for the all-time period, which has no comparison window (EPIC 5 Decision 4b).
public record StatsSummaryDto(
    int TotalCreated,
    int? PreviousTotalCreated,

    // 0..1 over the tasks created in the period. Null when nothing was created, since there is no
    // denominator — the card shows a dash rather than 0%.
    double? CompletionRate,
    double? PreviousCompletionRate,

    int OverdueNow,
    int NewlyOverdue,

    // Created to completed, in days. Null when nothing completed in the window.
    double? MedianDaysToComplete,
    double? PreviousMedianDaysToComplete);
