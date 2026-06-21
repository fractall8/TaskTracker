using System.Security.Cryptography;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Settings;
using Contracts.DTOs;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Workspaces.Commands;

public record InviteUserToWorkspaceCommand(Guid WorkspaceId, string Email) : IRequest<InviteResultDto>;

public class InviteUserToWorkspaceCommandHandler(
    IWorkspaceAccessService workspaceAccessService,
    IUserRepository userRepository,
    IRepository<WorkspaceMember, Guid> workspaceMemberRepository,
    IWorkspaceInviteRepository workspaceInviteRepository,
    IUnitOfWork unitOfWork,
    IOptions<WorkspaceSettings> workspaceSettings)
    : IRequestHandler<InviteUserToWorkspaceCommand, InviteResultDto>
{
    public async Task<InviteResultDto> Handle(InviteUserToWorkspaceCommand request, CancellationToken ct)
    {
        await workspaceAccessService.EnsureCanInviteUsersAsync(request.WorkspaceId, ct);

        var existingUser = await userRepository.GetByEmailAsync(request.Email, ct);

        if (existingUser != null)
        {
            var member = new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = request.WorkspaceId,
                UserId = existingUser.Id,
                Role = WorkspaceRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            };

            await workspaceMemberRepository.AddAsync(member, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return new InviteResultDto(true, null);
        }

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

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(WorkspaceConstants.MaxEmailLength)
            .WithMessage($"Email must not exceed {WorkspaceConstants.MaxEmailLength} characters.");
    }
}
