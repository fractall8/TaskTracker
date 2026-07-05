using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Profile.Queries;

public record GetProfileQuery : IRequest<UserDto>;

public class GetProfileQueryHandler(
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor)
    : IRequestHandler<GetProfileQuery, UserDto>
{
    public async Task<UserDto> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new UserDto(u.Id, u.Email, u.DisplayName, u.AvatarUrl),
            ct);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        return user;
    }
}
