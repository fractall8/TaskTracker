using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Auth.Queries;

public record GetCurrentUserQuery : IRequest<UserDto?>;

public class GetCurrentUserQueryHandler(
    ICurrentUserAccessor currentUser,
    IUserRepository userRepository)
    : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userDto = await userRepository.GetUserByAzureAdIdAsync(
            currentUser.AzureAdObjectId,
            u => new UserDto(u.Id, u.Email, u.DisplayName, u.AvatarUrl),
            cancellationToken);

        if (userDto == null)
        {
            return null;
        }

        var userWithRoles = new UserDto(
            userDto.Id,
            userDto.Email,
            userDto.DisplayName,
            userDto.AvatarUrl
        );

        return userWithRoles;
    }
}
