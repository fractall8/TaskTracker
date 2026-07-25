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
        if (IsInCall)
        {
            throw new InvalidOperationException("Already in a call.");
        }

        var response = await boardCallStore.StartCallAsync(ct);

        try
        {
            await JoinAcsRoomAsync(response.Credentials, ct);
        }
        catch
        {
            // The ACS session never actually connected — release the reservation the Start call
            // above already committed server-side, so the board doesn't stay stuck "in a call".
            await boardCallStore.EndCallAsync(ct);
            throw;
        }
    }

    public async Task JoinCallAsync(CancellationToken ct = default)
    {
        if (IsInCall)
        {
            throw new InvalidOperationException("Already in a call.");
        }

        var response = await boardCallStore.JoinCallAsync(ct);

        try
        {
            await JoinAcsRoomAsync(response.Credentials, ct);
        }
        catch
        {
            // Same reasoning as StartCallAsync — release the seat the Join call already reserved.
            await boardCallStore.LeaveCallAsync(ct);
            throw;
        }
    }

    private async Task JoinAcsRoomAsync(AcsCallCredentialsDto credentials, CancellationToken ct)
    {
        var module = await GetModuleAsync(ct);

        await module.InvokeVoidAsync("initCallAgent", ct, credentials.Token, profileStore.Profile?.DisplayName ?? string.Empty);

        _selfReference = DotNetObjectReference.Create(this);

        var access = await module.InvokeAsync<DeviceAccessResult>("joinRoom", ct, credentials.RoomId, _selfReference);

        IsAudioAvailable = access.AudioAvailable;
        IsVideoAvailable = access.VideoAvailable;
        IsInCall = true;
    }

    public async Task LeaveCallAsync(CancellationToken ct = default)
    {
        await EndLocalSessionAsync(ct);
        await boardCallStore.LeaveCallAsync(ct);
    }

    public async Task EndCallAsync(CancellationToken ct = default)
    {
        await EndLocalSessionAsync(ct);
        await boardCallStore.EndCallAsync(ct);
    }

    private async Task EndLocalSessionAsync(CancellationToken ct)
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("leaveCall", ct);
            }
            catch
            {
                // Best-effort local hangup — the backend Leave/EndCallAsync call that follows is the
                // source of truth for other participants and must still run even if this failed.
            }
        }

        IsInCall = false;
        IsAudioAvailable = false;
        IsVideoAvailable = false;

        _selfReference?.Dispose();
        _selfReference = null;
    }

    public async Task ToggleMicAsync(bool enabled, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await module.InvokeVoidAsync("toggleMic", ct, enabled);
    }

    public async Task ToggleCameraAsync(bool enabled, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await module.InvokeVoidAsync("toggleCamera", ct, enabled);
    }

    public async Task StartScreenShareAsync(CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await module.InvokeVoidAsync("startScreenShare", ct);
    }

    public async Task StopScreenShareAsync(CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await module.InvokeVoidAsync("stopScreenShare", ct);
    }

    public async Task<bool> AttachRendererAsync(string streamId, string videoElementId, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        return await module.InvokeAsync<bool>("attachRenderer", ct, streamId, videoElementId);
    }

    [JSInvokable]
    public void OnRemoteParticipantsChanged(RemoteParticipantInfo[] participants)
    {
        RemoteParticipantsChanged?.Invoke(participants);
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken ct)
    {
        // Stateful (holds the live CallAgent/Call) — imported once and cached, never re-imported per call.
        return _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ct, _modulePath);
    }

    public async ValueTask DisposeAsync()
    {
        if (IsInCall && _module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("leaveCall");
            }
            catch
            {
                // Best-effort — nothing left to react to a failure during disposal.
            }
        }

        _selfReference?.Dispose();

        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }

    private sealed record DeviceAccessResult(bool AudioAvailable, bool VideoAvailable);
}
