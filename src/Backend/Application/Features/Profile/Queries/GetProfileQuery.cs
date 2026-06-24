using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using MediatR;

namespace Application.Features.Profile.Queries;

public record GetProfileQuery : IRequest<UserProfileDto>;

public class GetProfileQueryHandler(
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor)
    : IRequestHandler<GetProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => new UserProfileDto(u.Id, u.Email, u.DisplayName, u.AvatarUrl),
            ct);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        return user;
    }
}
