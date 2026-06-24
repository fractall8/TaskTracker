using Application.Interfaces;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Domain.Entities;
using MediatR;

namespace Application.Features.Auth.Commands;

public record LoginCommand : IRequest<UserDto>;

public class LoginCommandHandler(
    ICurrentUserAccessor currentUser,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, UserDto>
{
    public async Task<UserDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetUserByAzureAdIdAsync(
            currentUser.AzureAdObjectId,
            u => u,
            ct);

        user ??= await CreateUser();

        SyncProfile(user);
        await unitOfWork.SaveChangesAsync(ct);

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl
        );
    }

    private async Task<User> CreateUser()
    {
        var user = new User
        {
            AzureAdObjectId = currentUser.AzureAdObjectId,
            Email = currentUser.Email,
            DisplayName = currentUser.DisplayName,
        };

        await userRepository.AddAsync(user);
        return user;
    }

    private void SyncProfile(User user)
    {
        user.Email = currentUser.Email;
        user.DisplayName = currentUser.DisplayName;
    }
}
