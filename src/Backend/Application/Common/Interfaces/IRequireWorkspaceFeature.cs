namespace Application.Common.Interfaces;

public interface IRequireWorkspaceFeature
{
    Guid WorkspaceId { get; }
    string Feature { get; }
}
