using Contracts.Enums;

namespace Contracts.DTOs;

// BucketStart carries the caller's UTC offset, so the axis label is the caller's own day.
public record StatsTrendPointDto(DateTimeOffset BucketStart, int Created, int Completed);

// Points are contiguous and zero-filled across the whole window: a quiet day is a zero, not a gap, so the
// chart cannot imply activity it does not have. Empty only when the workspace has no tasks at all.
public record StatsTrendDto(StatsTrendBucketDto Bucket, List<StatsTrendPointDto> Points);
