using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Application.Settings;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
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
    IUnitOfWork unitOfWork,
    ILogger<UploadAttachmentCommandHandler> logger)
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
            await boardAccessService.EnsureCanManageTasksAsync(request.BoardId, cancellationToken);

            var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, cancellationToken);
            if (task == null || task.Column?.BoardId != request.BoardId)
            {
                throw new KeyNotFoundException("Task not found on this board.");
            }

            // for default blob container is private
            var fileUrl = await fileService.UploadFileAsync(
                fileStream: request.FileStream,
                fileName: request.FileName,
                contentType: request.ContentType,
                containerName: BlobContainerNames.Attachments,
                cancellationToken: cancellationToken);

            var currentUser =
                await userRepository.GetUserByAzureAdIdAsync(currentUserAccessor.AzureAdObjectId, u => u,
                    cancellationToken);

            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                FileName = request.FileName,
                FileUrl = fileUrl,
                SizeInBytes = request.SizeInBytes,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedById = currentUser.Id,
                UploadedBy = currentUser,
                UploadedById = currentUser.Id,
                ContentType = request.ContentType
            };

            await attachmentRepository.AddAsync(attachment, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await fileService.DeleteFileAsync(fileUrl, cancellationToken);
                logger.LogError(ex,"Failed to upload attachment blob: {FileUrl}", fileUrl);
                throw;
            }

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

        RuleFor(x => x.FileStream.Length)
            .LessThanOrEqualTo(maxFileSizeBytes)
            .WithMessage($"File size must not exceed {settings.MaxSizeMb} MB.");
    }
}
