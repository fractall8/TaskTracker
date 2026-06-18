using Contracts.DTOs;

namespace Services.Abstractions.Auth;

public interface ICurrentUserService
{
    UserDto? User { get; }
    
    Task InitializeAsync();
}