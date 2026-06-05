using System.Security.Claims;
using Application.Interfaces.Services;
using Infrastructure.Auth.Constants;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public class CurrentuserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User
                                    ?? throw new InvalidOperationException("No HttpContext.");

    public Guid AzureAdObjectId =>
        Guid.Parse(
            GetClaim(User, EntraClaimTypes.ObjectId)
            ?? GetClaim(User, ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing oid claim."));

    public string Email =>
        GetClaim(User, EntraClaimTypes.PreferredUsername)
        ?? GetClaim(User, ClaimTypes.Email)
        ?? throw new InvalidOperationException("Missing email claim.");

    public string? DisplayName =>
        GetClaim(User, EntraClaimTypes.DisplayName);

    public IReadOnlyList<string> AppRoles =>
        User.FindAll(EntraClaimTypes.Roles).Select(c => c.Value).ToList();

    private static string? GetClaim(ClaimsPrincipal user, string type) =>
        user.FindFirst(type)?.Value;
}