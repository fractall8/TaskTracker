using System.Net;
using Contracts.DTOs;
using Services.Abstractions.Auth;
using Services.Api;

namespace Services.Auth;

internal class AuthApiService(IAuthApi api) : IAuthApiService
{
    public async Task<UserDto> LoginAsync(CancellationToken ct = default)
    {
        var response = await api.LoginAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw response.Error; 
        }

        return response.Content!;
    }

    public async Task<UserDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var response = await api.GetCurrentUserAsync(ct);

        if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw response.Error;
        }

        return response.Content;
    }
}