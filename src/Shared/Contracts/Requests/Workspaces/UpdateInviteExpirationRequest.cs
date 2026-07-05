namespace Contracts.Requests.Workspaces;

public record UpdateInviteExpirationRequest(DateTimeOffset NewExpirationDate);
