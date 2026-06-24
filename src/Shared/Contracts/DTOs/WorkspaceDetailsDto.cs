namespace Contracts.DTOs;

public record WorkspaceDetailsDto(Guid Id, string Name, string? Description, List<WorkspaceMemberDto> Members);
