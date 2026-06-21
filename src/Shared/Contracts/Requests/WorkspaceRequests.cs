namespace Contracts.Requests;

public record InviteUserRequest(string Email);

public record AcceptInviteRequest(string Token);
