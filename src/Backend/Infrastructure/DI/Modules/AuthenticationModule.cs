using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces.Services;
using Infrastructure.Auth;
using Infrastructure.Auth.Constants;
using Infrastructure.Auth.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Infrastructure.DI.Modules;

internal static class AuthenticationModule
{
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentuserAccessor>();
        
        var azureAdSection = configuration.GetSection("AzureAd");
        var azureAdOptions = azureAdSection.Get<AzureAdOptions>() 
                             ?? throw new InvalidOperationException("AzureAd configuration is missing.");
        
        services.Configure<AzureAdOptions>(azureAdSection);

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(azureAdSection);
        
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure(options =>
            {
                options.TokenValidationParameters.ValidAudiences =
                [
                    azureAdOptions.ClientId,
                    $"api://{azureAdOptions.ClientId}"
                ];

                options.TokenValidationParameters.RoleClaimType = EntraClaimTypes.Roles;
                options.TokenValidationParameters.NameClaimType = EntraClaimTypes.ObjectId;
            });

        return services;
    }
}