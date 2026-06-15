using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Settings;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Files.Commands;

public record UploadAttachmentCommand(
    Guid BoardId,
    Guid TaskId,
    Stream FileStream, 
    string FileName, 
    string ContentType,
    long SizeInBytes) : IRequest<AttachmentDto>;

public class UploadAttachmentCommandHandler(
    IBoardAccessService boardAccessService,
    ITaskRepository taskRepository,
    IRepository<Attachment, Guid> attachmentRepository,
    IFileService fileService,
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUnitOfWork unitOfWork) 
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken ct)
    {
        await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, ct);

        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, ct);
        if (task == null || task.Column?.BoardId != request.BoardId)
        {
            throw new KeyNotFoundException("Task not found on this board.");
        }

        var fileUrl = await fileService.UploadFileAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            BlobContainerNames.Attachments,
            ct);

        var currentUserId = await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u.Id, ct);

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            FileName = request.FileName,
            FileUrl = fileUrl,
            SizeInBytes = request.SizeInBytes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = currentUserId,
            ContentType = request.ContentType
        };

        await attachmentRepository.AddAsync(attachment);
        await unitOfWork.SaveChangesAsync(ct);

        return new AttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.FileUrl,
            attachment.SizeInBytes,
            attachment.CreatedAt,
            attachment.CreatedById);
    }
}

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator(IOptions<FileSettings> options)
    {
        var settings = options.Value.Attachments;
        var maxFileSizeBytes = settings.MaxSizeMb * 1024 * 1024;
        
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        
        RuleFor(x => x.ContentType)
            .Must(type => settings.AllowedTypes.Contains(type))
            .WithMessage("Unsupported file type. Please upload an image, PDF, or Word document.");

        RuleFor(x => x.SizeInBytes)
            .LessThanOrEqualTo(maxFileSizeBytes) 
            .WithMessage($"File size must not exceed {settings.MaxSizeMb} MB.");
    }
}