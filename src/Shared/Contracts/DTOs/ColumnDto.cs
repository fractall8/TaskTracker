namespace Contracts.DTOs;

public record ColumnDto(Guid Id, string Name, int Position, List<TaskDto>? Tasks = null);