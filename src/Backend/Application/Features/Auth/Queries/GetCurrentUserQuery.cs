using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Auth.Queries;

public record GetCurrentUserQuery : IRequest<UserWithRolesDto?>;

public class GetCurrentUserQueryHandler(
    ICurrentUserAccessor currentUser,
    IUserRepository userRepository) 
    : IRequestHandler<GetCurrentUserQuery, UserWithRolesDto?>
{
    public async Task<UserWithRolesDto?> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var userDto = await userRepository.GetUserDtoByAzureAdIdAsync(currentUser.AzureAdObjectId, ct);

        if (userDto == null)
        {
            return null;
        }
        
        var rolesFromToken = currentUser.AppRoles ?? [];

        var userWithRoles = new UserWithRolesDto(
            userDto.Id,
            userDto.Email,
            userDto.DisplayName,
            rolesFromToken
        );

        return userWithRoles;
    }
}