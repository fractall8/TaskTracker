using Contracts.Requests.Profile;
using Refit;

namespace Services.Abstractions.Profile;

public interface IProfileApiService
{
    Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);

    Task<string> UploadAvatarAsync(StreamPart stream, CancellationToken ct = default);

    Task DeleteAvatarAsync(CancellationToken ct = default);
}
