using Application.Interfaces.Services;
using Application.Settings;
using Domain.Constants;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace Application.Features.Files.Commands;

public record UploadAttachmentCommand(IFormFile FormFile) : IRequest<string>;

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator(IOptions<FileSettings> options)
    {
        var settings = options.Value.Attachments;
        var maxFileSizeBytes = settings.MaxSizeMb * 1024 * 1024;
        
        RuleFor(x => x.FormFile.ContentType)
            .Must(type => settings.AllowedTypes.Contains(type))
            .WithMessage("Unsupported file type. Please upload an image, PDF, or Word document.");

        RuleFor(x => x.FormFile.Length)
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
            request.FormFile,
            BlobContainerNames.Attachments,
            cancellationToken);
    }
}