namespace Application.Interfaces.Services;

public interface IWorkspaceEntitlementService
{
    Task<bool> HasFeatureAsync(Guid workspaceId, string feature, CancellationToken ct = default);
}
