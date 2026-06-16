using Contracts.DTOs;

namespace Services.Abstractions.Auth;

public interface IProfileStore
{
    UserDto? Profile { get; }
    bool IsLoading { get; }
    bool IsLoaded { get; }

    /// <summary>
    /// True when the API rejected the token (401/403). The component should
    /// redirect to /login so MSAL can acquire a fresh token.
    /// </summary>
    bool RequiresLogin { get; }

    string? ErrorMessage { get; }

    event Action? StateChanged;

    /// <summary>
    /// Ensures the profile is loaded. No-ops if already loaded; attaches to
    /// the in-flight load if one is running; starts a new load otherwise.
    /// </summary>
    Task EnsureLoadedAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces a fresh profile load regardless of current state. Safe to call
    /// concurrently — subsequent callers attach to the same in-flight refresh.
    /// </summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears all cached profile state. Call on logout before MSAL redirect.
    /// </summary>
    void Reset();
}