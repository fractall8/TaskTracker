namespace Contracts.DTOs;

public record BoardExportMemberDto(
    BoardExportUserDto User,
    string Role,
    DateTimeOffset JoinedAt);
