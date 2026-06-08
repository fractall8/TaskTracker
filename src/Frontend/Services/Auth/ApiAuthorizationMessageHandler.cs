using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using Services.Configuration;

namespace Services.Auth;

internal sealed class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IOptions<ApiClientOptions> options)
        : base(provider, navigation)
    {
        ConfigureHandler(
            authorizedUrls: [options.Value.BaseUrl],
            scopes: options.Value.Scopes);
    }
}

