using Application.Interfaces.Services;
using Application.Settings;
using Domain.Constants;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Files.Commands;

public record UploadAvatarCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

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


public class UploadAvatarCommandHandler(IFileService fileService) 
    : IRequestHandler<UploadAvatarCommand, string>
{
    public async Task<string> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        return await fileService.UploadFileAsync(
            request.FileStream, 
            request.FileName, 
            request.ContentType, 
            BlobContainerNames.Avatars,
            cancellationToken);
    }
}