namespace Contracts.DTOs;

public record BoardExportOptionsDto(
    bool IncludeDescriptions,
    bool IncludeComments,
    bool IncludeAttachments,
    bool IncludeMembers);
