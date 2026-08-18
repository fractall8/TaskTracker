namespace Contracts.DTOs;

// Served so the client resolves "today" the same way the server does, rather than from the browser's zone.
public record AppConfigDto(string TimeZoneId, int CurrentUtcOffsetMinutes);
