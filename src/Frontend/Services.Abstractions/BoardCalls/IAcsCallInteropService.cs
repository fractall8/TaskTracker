namespace Services.Abstractions.BoardCalls;

public interface IAcsCallInteropService : IAsyncDisposable
{
    bool IsInCall { get; }

    bool IsAudioAvailable { get; }

    bool IsVideoAvailable { get; }

    bool IsMicOn { get; }

    bool IsCameraOn { get; }

    bool IsScreenSharing { get; }

    event Action<IReadOnlyList<RemoteParticipantInfo>>? RemoteParticipantsChanged;

    event Action? StateChanged;

    event Action? CallEndedByOthers;

    Task<PreviewDeviceAccess> StartPreviewAsync(string videoElementId, CancellationToken ct = default);

    Task<bool> SetPreviewCameraAsync(bool enabled, string videoElementId, CancellationToken ct = default);

    Task StopPreviewAsync(CancellationToken ct = default);

    Task StartCallAsync(Guid boardId, bool micEnabled, bool cameraEnabled, CancellationToken ct = default);

    Task JoinCallAsync(Guid boardId, bool micEnabled, bool cameraEnabled, CancellationToken ct = default);

    Task LeaveCallAsync(Guid boardId, CancellationToken ct = default);

    Task EndCallAsync(Guid boardId, CancellationToken ct = default);

    Task ToggleMicAsync(bool enabled, CancellationToken ct = default);

    Task ToggleCameraAsync(bool enabled, CancellationToken ct = default);

    Task StartScreenShareAsync(CancellationToken ct = default);

    Task StopScreenShareAsync(CancellationToken ct = default);

    Task<bool> AttachRendererAsync(string streamId, string videoElementId, CancellationToken ct = default);

    Task DetachRendererAsync(string streamId, CancellationToken ct = default);
}
