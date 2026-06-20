using Domain.Enums;

namespace Application.Common.Models;

public record BoardAccessContext(Guid UserId, BoardRole Role);
