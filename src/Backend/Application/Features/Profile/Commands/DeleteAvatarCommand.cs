using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Profile.Commands;

public record DeleteAvatarCommand : IRequest<Unit>;

public class DeleteAvatarCommandHandler(
    IFileService fileService,
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUnitOfWork unitOfWork,
    ILogger<DeleteAvatarCommandHandler> logger)
    : IRequestHandler<DeleteAvatarCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAvatarCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => u,
            ct) ?? throw new UnauthorizedAccessException("User not found.");

        if (string.IsNullOrEmpty(user.AvatarUrl))
        {
            return Unit.Value;
        }

        var avatarUrl = user.AvatarUrl;

        user.AvatarUrl = null;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            await fileService.DeleteFileAsync(avatarUrl, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete avatar blob during removal: {Url}", avatarUrl);
        }

        return Unit.Value;
    }
}
