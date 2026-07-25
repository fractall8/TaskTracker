namespace Services.Abstractions.BoardCalls;

public interface IAcsCallInteropService : IAsyncDisposable
{
    bool IsInCall { get; }

    bool IsAudioAvailable { get; }

    bool IsVideoAvailable { get; }

    event Action<IReadOnlyList<RemoteParticipantInfo>>? RemoteParticipantsChanged;

    Task StartCallAsync(CancellationToken ct = default);

    Task JoinCallAsync(CancellationToken ct = default);

    Task LeaveCallAsync(CancellationToken ct = default);

    Task EndCallAsync(CancellationToken ct = default);

    Task ToggleMicAsync(bool enabled);

    Task ToggleCameraAsync(bool enabled);

    Task StartScreenShareAsync();

    Task StopScreenShareAsync();

    Task<bool> AttachRendererAsync(string streamId, string videoElementId);
}
