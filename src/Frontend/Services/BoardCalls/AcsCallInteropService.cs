using Contracts.DTOs;
using Microsoft.JSInterop;
using Services.Abstractions.Auth;
using Services.Abstractions.BoardCalls;

namespace Services.BoardCalls;

public class AcsCallInteropService : IAcsCallInteropService
{
    private const string _modulePath = "./js/acsCallInterop.bundle.js";

    private readonly IJSRuntime _jsRuntime;
    private readonly IBoardCallStore _boardCallStore;
    private readonly IProfileStore _profileStore;

    private IJSObjectReference? _module;
    private Task<IJSObjectReference>? _moduleTask;
    private DotNetObjectReference<AcsCallInteropService>? _selfReference;

    public AcsCallInteropService(IJSRuntime jsRuntime, IBoardCallStore boardCallStore, IProfileStore profileStore)
    {
        _jsRuntime = jsRuntime;
        _boardCallStore = boardCallStore;
        _profileStore = profileStore;

        _boardCallStore.CallEndedRemotely += OnCallEndedRemotely;
    }

    public bool IsInCall { get; private set; }

    public bool IsAudioAvailable { get; private set; }

    public bool IsVideoAvailable { get; private set; }

    public bool IsMicOn { get; private set; }

    public bool IsCameraOn { get; private set; }

    public bool IsScreenSharing { get; private set; }

    private bool _previewStarted;

    public event Action<IReadOnlyList<RemoteParticipantInfo>>? RemoteParticipantsChanged;

    public event Action? StateChanged;

    public async Task<PreviewDeviceAccess> StartPreviewAsync(string videoElementId, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        var access = await InvokeSafeAsync<PreviewDeviceAccess>(module, "initPreview", ct, videoElementId);
        _previewStarted = true;
        return access;
    }

    public async Task<bool> SetPreviewCameraAsync(bool enabled, string videoElementId, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        return await InvokeSafeAsync<bool>(module, "setPreviewCamera", ct, enabled, videoElementId);
    }

    public async Task StopPreviewAsync(CancellationToken ct = default)
    {
        if (!_previewStarted || IsInCall || _module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("disposePreview", ct);
        }
        catch
        {
            // Best-effort cleanup only — nothing left to react to if this fails while navigating away.
        }

        _previewStarted = false;
    }

    public async Task StartCallAsync(Guid boardId, bool micEnabled, bool cameraEnabled, CancellationToken ct = default)
    {
        if (IsInCall)
        {
            throw new InvalidOperationException("Already in a call.");
        }

        var response = await _boardCallStore.StartCallAsync(boardId, ct);

        try
        {
            await JoinAcsRoomAsync(response.Credentials, micEnabled, cameraEnabled, ct);
        }
        catch
        {
            await _boardCallStore.EndCallAsync(boardId, ct);
            throw;
        }
    }

    public async Task JoinCallAsync(Guid boardId, bool micEnabled, bool cameraEnabled, CancellationToken ct = default)
    {
        if (IsInCall)
        {
            throw new InvalidOperationException("Already in a call.");
        }

        var response = await _boardCallStore.JoinCallAsync(boardId, ct);

        try
        {
            await JoinAcsRoomAsync(response.Credentials, micEnabled, cameraEnabled, ct);
        }
        catch
        {
            await _boardCallStore.LeaveCallAsync(boardId, ct);
            throw;
        }
    }

    private async Task JoinAcsRoomAsync(AcsCallCredentialsDto credentials, bool micEnabled, bool cameraEnabled, CancellationToken ct)
    {
        var module = await GetModuleAsync(ct);

        await InvokeVoidSafeAsync(module, "initCallAgent", ct, credentials.Token, _profileStore.Profile?.DisplayName ?? string.Empty);

        _selfReference = DotNetObjectReference.Create(this);

        var access = await InvokeSafeAsync<DeviceAccessResult>(module, "joinRoom", ct, credentials.RoomId, _selfReference, micEnabled, cameraEnabled);

        IsAudioAvailable = access.AudioAvailable;
        IsVideoAvailable = access.VideoAvailable;
        IsMicOn = access.AudioOn;
        IsCameraOn = access.VideoOn;
        IsInCall = true;
        _previewStarted = false;

        StateChanged?.Invoke();
    }

    public async Task LeaveCallAsync(Guid boardId, CancellationToken ct = default)
    {
        await _boardCallStore.LeaveCallAsync(boardId, ct);
        await EndLocalSessionAsync(ct);
    }

    public async Task EndCallAsync(Guid boardId, CancellationToken ct = default)
    {
        await _boardCallStore.EndCallAsync(boardId, ct);
        await EndLocalSessionAsync(ct);
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
        IsMicOn = false;
        IsCameraOn = false;
        IsScreenSharing = false;

        _selfReference?.Dispose();
        _selfReference = null;

        StateChanged?.Invoke();
    }

    public async Task ToggleMicAsync(bool enabled, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await InvokeVoidSafeAsync(module, "toggleMic", ct, enabled);
        IsMicOn = enabled;
    }

    public async Task ToggleCameraAsync(bool enabled, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await InvokeVoidSafeAsync(module, "toggleCamera", ct, enabled);
        IsCameraOn = enabled;
    }

    public async Task StartScreenShareAsync(CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await InvokeVoidSafeAsync(module, "startScreenShare", ct);
    }

    public async Task StopScreenShareAsync(CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await InvokeVoidSafeAsync(module, "stopScreenShare", ct);
    }

    public async Task<bool> AttachRendererAsync(string streamId, string videoElementId, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        return await InvokeSafeAsync<bool>(module, "attachRenderer", ct, streamId, videoElementId);
    }

    public async Task DetachRendererAsync(string streamId, CancellationToken ct = default)
    {
        var module = await GetModuleAsync(ct);
        await InvokeVoidSafeAsync(module, "detachRenderer", ct, streamId);
    }

    [JSInvokable]
    public void OnRemoteParticipantsChanged(RemoteParticipantInfo[] participants)
    {
        RemoteParticipantsChanged?.Invoke(participants);
    }

    [JSInvokable]
    public void OnCallDisconnected()
    {
        IsInCall = false;
        IsAudioAvailable = false;
        IsVideoAvailable = false;
        IsMicOn = false;
        IsCameraOn = false;
        IsScreenSharing = false;

        _selfReference?.Dispose();
        _selfReference = null;

        StateChanged?.Invoke();
    }

    [JSInvokable]
    public void OnScreenSharingChanged(bool isScreenSharing)
    {
        IsScreenSharing = isScreenSharing;
        StateChanged?.Invoke();
    }

    private void OnCallEndedRemotely(Guid boardCallId)
    {
        if (!IsInCall)
        {
            return;
        }

        _ = EndLocalSessionAsync(CancellationToken.None);
    }

    private static async Task InvokeVoidSafeAsync(IJSObjectReference module, string identifier, CancellationToken ct, params object?[] args)
    {
        try
        {
            await module.InvokeVoidAsync(identifier, ct, args);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException(CleanErrorMessage(ex.Message), ex);
        }
    }

    private static async Task<T> InvokeSafeAsync<T>(IJSObjectReference module, string identifier, CancellationToken ct, params object?[] args)
    {
        try
        {
            return await module.InvokeAsync<T>(identifier, ct, args);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException(CleanErrorMessage(ex.Message), ex);
        }
    }

    private static string CleanErrorMessage(string message)
    {
        var firstLine = message.Split('\n')[0].Trim();
        return string.IsNullOrWhiteSpace(firstLine) ? "Something went wrong with the call. Please try again." : firstLine;
    }

    private Task<IJSObjectReference> GetModuleAsync(CancellationToken ct)
    {
        return _moduleTask ??= ImportModuleAsync(ct);
    }

    private async Task<IJSObjectReference> ImportModuleAsync(CancellationToken ct)
    {
        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ct, _modulePath);
        return _module;
    }

    public async ValueTask DisposeAsync()
    {
        _boardCallStore.CallEndedRemotely -= OnCallEndedRemotely;

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

    private sealed record DeviceAccessResult(bool AudioAvailable, bool VideoAvailable, bool AudioOn, bool VideoOn);
}
