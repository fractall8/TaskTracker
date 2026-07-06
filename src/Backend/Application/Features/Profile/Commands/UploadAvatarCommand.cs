using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Application.Settings;
using Domain.Constants;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Features.Profile.Commands;

public record UploadAvatarCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

public class UploadAvatarCommandHandler(
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IFileService fileService,
    IUnitOfWork unitOfWork,
    ILogger<UploadAvatarCommandHandler> logger)
    : IRequestHandler<UploadAvatarCommand, string>
{
    public async Task<string> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => u,
            ct) ?? throw new UnauthorizedAccessException("User not found.");

        var oldAvatarUrl = user.AvatarUrl;

        var newFileUrl = await fileService.UploadFileAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            BlobContainerNames.Avatars,
            true, // make container public for avatars
            ct);

        user.AvatarUrl = newFileUrl;
        userRepository.Update(user);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            await fileService.DeleteFileAsync(newFileUrl, ct);
            logger.LogError(ex, "Failed to save new avatar to DB, rolling back file: {FileUrl}", newFileUrl);
            throw;
        }

        if (!string.IsNullOrEmpty(oldAvatarUrl))
        {
            try
            {
                await fileService.DeleteFileAsync(oldAvatarUrl, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old avatar blob: {Url}", oldAvatarUrl);
            }
        }

        return newFileUrl;
    }
}

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    public UploadAvatarCommandValidator(IOptions<FileSettings> options)
    {
        var settings = options.Value.Avatars;
        var maxFileSizeBytes = settings.MaxSizeMb * 1024 * 1024;

        RuleFor(x => x.ContentType)
            .Must(type => settings.AllowedTypes.Contains(type))
            .WithMessage("Unsupported file type. Please upload an image (png/jpg).");

        RuleFor(x => x.FileStream.Length)
            .LessThanOrEqualTo(maxFileSizeBytes)
            .WithMessage($"File size must not exceed {settings.MaxSizeMb} MB.");
    }
}
