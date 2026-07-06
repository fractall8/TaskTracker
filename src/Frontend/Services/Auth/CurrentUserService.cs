using Contracts.DTOs;
using Services.Abstractions.Auth;

namespace Services.Auth;

public class CurrentUserService(IProfileStore profileStore, IAuthApiService authApiService) : ICurrentUserService
{
    public UserDto? User { get; private set; }
    private bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            await profileStore.EnsureLoadedAsync();
            User = profileStore.Profile;
            _isInitialized = true;
        }
        catch
        {
            User = null;
        }
    }
}
