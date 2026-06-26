using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Domain.Constants;
using FluentValidation;
using MediatR;

namespace Application.Features.Profile.Commands;

public record UpdateProfileCommand(string? DisplayName) : IRequest<Unit>;


public class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProfileCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetUserByAzureAdIdAsync(
            currentUserAccessor.AzureAdObjectId,
            u => u,
            ct);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        user.DisplayName = request.DisplayName;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}


public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(ProfileConstants.MaxDisplayNameLength).WithMessage($"Display name must not exceed {ProfileConstants.MaxDisplayNameLength} characters.");
    }
}
