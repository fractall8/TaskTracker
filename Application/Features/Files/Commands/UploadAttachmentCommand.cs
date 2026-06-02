using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Files.Commands;

public record UploadAttachmentCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    private const int MaxFileSize = 10 * 1024 * 1024; // 10 mb
    
    public UploadAttachmentCommandValidator()
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf", "application/msword" };
        
        RuleFor(x => x.ContentType)
            .Must(type => allowedTypes.Contains(type))
            .WithMessage("Unsupported file type. Please upload an image, PDF, or Word document.");

        RuleFor(x => x.FileStream.Length)
            .LessThanOrEqualTo(MaxFileSize) 
            .WithMessage($"File size must not exceed {MaxFileSize / (1024*1024)} MB.");
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
            "attachments",
            cancellationToken);
    }
}