namespace Application.Interfaces.Services;

public interface ICurrentUserAccessor
{
    Guid AzureAdObjectId { get; }
    string Email { get; }
    string? DisplayName { get; }
    IReadOnlyList<string> AppRoles { get; }

}