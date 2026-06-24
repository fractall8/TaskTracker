using Contracts.Requests;
using Refit;

namespace Services.Abstractions.Auth;

public interface IProfileApiService
{
    Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<string> UploadAvatarAsync(StreamPart stream, CancellationToken ct = default);
    Task DeleteAvatarAsync(CancellationToken ct = default);
}
