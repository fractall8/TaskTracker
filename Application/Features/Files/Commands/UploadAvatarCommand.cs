using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Files.Commands;

public record UploadAvatarCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private const int MaxFileSize = 2 * 1024 * 1024; // 2 mb
    
    public UploadAvatarCommandValidator()
    {
        var allowedTypes = new[] { "image/jpeg", "image/png" };
        
        RuleFor(x => x.ContentType)
            .Must(type => allowedTypes.Contains(type))
            .WithMessage("Unsupported file type. Please upload an image (png/jpg).");

        RuleFor(x => x.FileStream.Length)
            .LessThanOrEqualTo(MaxFileSize) 
            .WithMessage($"File size must not exceed {MaxFileSize / (1024*1024)} MB.");
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
            "avatars",
            cancellationToken);
    }
}