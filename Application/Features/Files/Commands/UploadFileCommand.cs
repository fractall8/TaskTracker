using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.Files.Commands;

public record UploadFileCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

public class UploadFileCommandHandler(IFileService fileService) 
    : IRequestHandler<UploadFileCommand, string>
{
    public async Task<string> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        return await fileService.UploadFileAsync(
            request.FileStream, 
            request.FileName, 
            request.ContentType, 
            cancellationToken);
    }
}