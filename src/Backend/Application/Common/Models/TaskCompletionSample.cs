namespace Application.Common.Models;

// CompletedAt is carried so one fetch spanning both windows can be split in memory rather than queried twice.
public record TaskCompletionSample(DateTimeOffset CompletedAt, double DaysToComplete);
