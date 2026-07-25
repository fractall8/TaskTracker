using Contracts.DTOs;
using Microsoft.JSInterop;
using Services.Abstractions.Auth;
using Services.Abstractions.BoardCalls;

namespace Services.BoardCalls;

public class AcsCallInteropService(
    IJSRuntime jsRuntime,
    IBoardCallStore boardCallStore,
    IProfileStore profileStore) : IAcsCallInteropService
{
    private const string _modulePath = "./js/acsCallInterop.bundle.js";

    private IJSObjectReference? _module;
    private DotNetObjectReference<AcsCallInteropService>? _selfReference;

    public bool IsInCall { get; private set; }

    public bool IsAudioAvailable { get; private set; }

    public bool IsVideoAvailable { get; private set; }

    public event Action<IReadOnlyList<RemoteParticipantInfo>>? RemoteParticipantsChanged;

    public async Task StartCallAsync(CancellationToken ct = default)
    {
        var response = await boardCallStore.StartCallAsync(ct);
        await JoinAcsRoomAsync(response.Credentials);
    }

    public async Task JoinCallAsync(CancellationToken ct = default)
    {
        var response = await boardCallStore.JoinCallAsync(ct);
        await JoinAcsRoomAsync(response.Credentials);
    }

    private async Task JoinAcsRoomAsync(AcsCallCredentialsDto credentials)
    {
        var module = await GetModuleAsync();

        await module.InvokeVoidAsync("initCallAgent", credentials.Token, profileStore.Profile?.DisplayName ?? string.Empty);

        _selfReference = DotNetObjectReference.Create(this);

        var access = await module.InvokeAsync<DeviceAccessResult>("joinRoom", credentials.RoomId, _selfReference);

        IsAudioAvailable = access.AudioAvailable;
        IsVideoAvailable = access.VideoAvailable;
        IsInCall = true;
    }

    public async Task LeaveCallAsync(CancellationToken ct = default)
    {
        await EndLocalSessionAsync();
        await boardCallStore.LeaveCallAsync(ct);
    }

    public async Task EndCallAsync(CancellationToken ct = default)
    {
        await EndLocalSessionAsync();
        await boardCallStore.EndCallAsync(ct);
    }

    private async Task EndLocalSessionAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("leaveCall");
        }

        IsInCall = false;
        IsAudioAvailable = false;
        IsVideoAvailable = false;

        _selfReference?.Dispose();
        _selfReference = null;
    }

    public async Task ToggleMicAsync(bool enabled)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("toggleMic", enabled);
    }

    public async Task ToggleCameraAsync(bool enabled)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("toggleCamera", enabled);
    }

    public async Task StartScreenShareAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("startScreenShare");
    }

    public async Task StopScreenShareAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("stopScreenShare");
    }

    public async Task<bool> AttachRendererAsync(string streamId, string videoElementId)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("attachRenderer", streamId, videoElementId);
    }

    [JSInvokable]
    public void OnRemoteParticipantsChanged(RemoteParticipantInfo[] participants)
    {
        RemoteParticipantsChanged?.Invoke(participants);
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        // Stateful (holds the live CallAgent/Call) — imported once and cached, never re-imported per call.
        return _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", _modulePath);
    }

    public async ValueTask DisposeAsync()
    {
        _selfReference?.Dispose();

        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }

    private sealed record DeviceAccessResult(bool AudioAvailable, bool VideoAvailable);
}
