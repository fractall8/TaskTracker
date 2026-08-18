namespace Contracts.DTOs;

// TagId and Color are null for the computed "untagged" bucket, which is not a tag and must not be styled
// as one (EPIC 5 Decision 7).
//
// A task with several tags is counted once per tag, so these counts sum to more than the number of open
// tasks. The breakdown answers "how much open work carries each label", not "what share of tasks is this".
public record StatsTagDto(Guid? TagId, string Name, string? Color, int OpenTasks);
