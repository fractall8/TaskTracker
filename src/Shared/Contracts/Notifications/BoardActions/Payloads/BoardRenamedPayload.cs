namespace Contracts.Notifications.BoardActions.Payloads;

public record BoardRenamedPayload(string Name) : BoardActionPayload;
