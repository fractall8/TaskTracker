using Application.Interfaces.Services;
using Application.Settings;
using Domain.Constants;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Files.Commands;

public record UploadAttachmentCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator(IOptions<FileSettings> options)
    {
        var settings = options.Value.Attachments;
        var maxFileSizeBytes = settings.MaxSizeMb * 1024 * 1024;
        
        RuleFor(x => x.ContentType)
            .Must(type => settings.AllowedTypes.Contains(type))
            .WithMessage("Unsupported file type. Please upload an image, PDF, or Word document.");

        RuleFor(x => x.FileStream.Length)
            .LessThanOrEqualTo(maxFileSizeBytes) 
            .WithMessage($"File size must not exceed {settings.MaxSizeMb} MB.");
    }
}


public class UploadAttachmentCommandHandler(IFileService fileService) 
    : IRequestHandler<UploadAttachmentCommand, string>
{
    public async Task<string> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        return await fileService.UploadFileAsync(
            request.FileStream, 
            request.FileName, 
            request.ContentType,
            BlobContainerNames.Attachments,
            cancellationToken);
    }
}