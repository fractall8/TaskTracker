using Contracts.Requests;
using Refit;

namespace Services.Api;

public record UploadAvatarResponse(string Url);

public interface IProfileApi
{
    [Put("/api/profile")]
    Task<IApiResponse> UpdateProfileAsync([Body] UpdateProfileRequest request, CancellationToken ct = default);

    [Multipart]
    [Post("/api/profile/avatar")]
    Task<IApiResponse<UploadAvatarResponse>> UploadAvatarAsync(StreamPart stream, CancellationToken ct = default);

    [Delete("/api/profile/avatar")]
    Task<IApiResponse> DeleteAvatarAsync(CancellationToken ct = default);
}
