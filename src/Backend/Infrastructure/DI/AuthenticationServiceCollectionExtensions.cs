using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces.Services;
using Infrastructure.Auth;
using Infrastructure.Auth.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Infrastructure.DI;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddEntraIdAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentuserAccessor>();

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));


        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure(options =>
            {
                var clientId = configuration["AzureAd:ClientId"];

                options.TokenValidationParameters.ValidAudiences =
                [
                    clientId,
                    $"api://{clientId}"
                ];

                options.TokenValidationParameters.RoleClaimType = EntraClaimTypes.Roles;
                options.TokenValidationParameters.NameClaimType = EntraClaimTypes.ObjectId;
            });

        return services;
    }
}