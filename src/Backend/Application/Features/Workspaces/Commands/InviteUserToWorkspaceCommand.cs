using System.Security.Cryptography;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Settings;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Workspaces.Commands;

public record InviteUserToWorkspaceCommand(Guid WorkspaceId, string? Email) : IRequest<InviteResultDto>;

public class InviteUserToWorkspaceCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceInviteRepository workspaceInviteRepository,
    IUnitOfWork unitOfWork,
    IOptions<WorkspaceSettings> workspaceSettings)
    : IRequestHandler<InviteUserToWorkspaceCommand, InviteResultDto>
{
    public async Task<InviteResultDto> Handle(InviteUserToWorkspaceCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanInviteUsersAsync(request.WorkspaceId, ct);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var options = workspaceSettings.Value;

        var invite = new WorkspaceInvite
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Email = request.Email,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(options.InviteExpiryDays)
        };

        await workspaceInviteRepository.AddAsync(invite, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new InviteResultDto(false, token);
    }
}

public class InviteUserToWorkspaceCommandValidator : AbstractValidator<InviteUserToWorkspaceCommand>
{
    public InviteUserToWorkspaceCommandValidator()
    {
        RuleFor(v => v.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId is required.");

        When(v => v.Email is not null, () =>
        {
            RuleFor(v => v.Email!)
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(WorkspaceConstants.MaxEmailLength)
                .WithMessage($"Email must not exceed {WorkspaceConstants.MaxEmailLength} characters.");
        });
    }
}
