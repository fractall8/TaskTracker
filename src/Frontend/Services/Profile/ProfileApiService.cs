using Contracts.Requests.Profile;
using Refit;
using Services.Abstractions.Profile;
using Services.Api;
using Services.Extensions;

namespace Services.Profile;

public class ProfileApiService(IProfileApi profileApi) : IProfileApiService
{
    public async Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
        await (await profileApi.UpdateProfileAsync(request, ct)).HandleResponseAsync();

    public async Task<string> UploadAvatarAsync(StreamPart stream, CancellationToken ct = default)
    {
        var response = await (await profileApi.UploadAvatarAsync(stream, ct)).HandleResponseAsync();
        return response.Url;
    }

    public async Task DeleteAvatarAsync(CancellationToken ct = default) =>
        await (await profileApi.DeleteAvatarAsync(ct)).HandleResponseAsync();
}
