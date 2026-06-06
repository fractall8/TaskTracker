using System.Net;
using Contracts.DTOs;
using Refit;
using Services.Abstractions.Auth;

namespace Services.Auth.Stores;

internal sealed class ProfileStore(IAuthApiService authApiService) : IProfileStore, IDisposable
{
    private readonly SemaphoreSlim _sync = new(1, 1);

    private Task? _inFlightLoad;

    public UserWithRolesDto? Profile { get; private set; }
    public bool IsLoading { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool RequiresLogin { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public Task EnsureLoadedAsync(CancellationToken ct = default) =>
        EnsureLoadedInternalAsync(forceRefresh: false, ct);

    public Task RefreshAsync(CancellationToken ct = default) =>
        EnsureLoadedInternalAsync(forceRefresh: true, ct);

    public void Reset()
    {
        Profile = null;
        IsLoading = false;
        IsLoaded = false;
        RequiresLogin = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    public void Dispose() => _sync.Dispose();

    private async Task EnsureLoadedInternalAsync(bool forceRefresh, CancellationToken ct)
    {
        if (!forceRefresh && IsLoaded)
            return;

        Task loadTask;
        
        await _sync.WaitAsync(ct);
        try
        {
            if (!forceRefresh && IsLoaded)
                return;

            _inFlightLoad ??= LoadProfileAsync();
            loadTask = _inFlightLoad;
        }
        finally
        {
            _sync.Release();
        }

        await loadTask.WaitAsync(ct);
    }

    private async Task LoadProfileAsync()
    {
        await Task.Yield();
        
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var profile = await authApiService.GetCurrentUserAsync(CancellationToken.None);

            profile ??= await authApiService.LoginAsync(CancellationToken.None);

            Profile = profile;
            IsLoaded = true;
            RequiresLogin = false;
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            Profile = null;
            IsLoaded = false;
            RequiresLogin = true;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            Profile = null;
            IsLoaded = false;
            RequiresLogin = false;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;

            await _sync.WaitAsync(CancellationToken.None);
            try
            {
                _inFlightLoad = null;
            }
            finally
            {
                _sync.Release();
            }

            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}