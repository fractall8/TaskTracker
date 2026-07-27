import { CallClient, LocalVideoStream, VideoStreamRenderer } from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";

const LOCAL_STREAM_ID = "local";
const LOCAL_SCREEN_STREAM_ID = "local-screen";
const SCREEN_SHARE_SUFFIX = "-screen";

let callClient = null;
let deviceManager = null;
let callAgent = null;
let call = null;
let localVideoStream = null;
let dotNetRef = null;
const videoRenderers = new Map();

const fallbackParticipantIds = new WeakMap();
let fallbackParticipantIdCounter = 0;

function getParticipantId(remoteParticipant) {
    const identifier = remoteParticipant.identifier;
    const id = identifier.communicationUserId ?? identifier.id ?? remoteParticipant.displayName;

    if (id) {
        return id;
    }

    // None of the identifier fields resolved (not realistically reachable — this app always provisions
    // ACS Communication User identities) — fall back to a stable, per-participant-object id that can
    // never collide with another participant or with the "local"/"local-screen" sentinels.
    if (!fallbackParticipantIds.has(remoteParticipant)) {
        fallbackParticipantIdCounter += 1;
        fallbackParticipantIds.set(remoteParticipant, `unknown-participant-${fallbackParticipantIdCounter}`);
    }

    return fallbackParticipantIds.get(remoteParticipant);
}

function findRemoteVideoStream(participantId, mediaStreamType) {
    if (!call) {
        return null;
    }

    const participant = call.remoteParticipants.find(p => getParticipantId(p) === participantId);
    return participant?.videoStreams.find(s => s.mediaStreamType === mediaStreamType && s.isAvailable) ?? null;
}

// Resolves a UI-facing streamId (as used by attachRenderer/detachRenderer) to the actual ACS stream
// object. Camera and screen-share are separate MediaStreamType streams per participant, so a bare
// participant id always means their camera; "{participantId}-screen" means their screen share.
function resolveStream(streamId) {
    if (streamId === LOCAL_STREAM_ID) {
        return localVideoStream;
    }

    if (streamId === LOCAL_SCREEN_STREAM_ID) {
        return call?.localVideoStreams.find(s => s.mediaStreamType === "ScreenSharing") ?? null;
    }

    if (streamId.endsWith(SCREEN_SHARE_SUFFIX)) {
        const participantId = streamId.slice(0, -SCREEN_SHARE_SUFFIX.length);
        return findRemoteVideoStream(participantId, "ScreenSharing");
    }

    return findRemoteVideoStream(streamId, "Video");
}

async function notifyParticipantsChanged() {
    if (!dotNetRef || !call) {
        return;
    }

    const participants = call.remoteParticipants.map(p => ({
        id: getParticipantId(p),
        displayName: p.displayName ?? null,
        hasVideo: p.videoStreams.some(s => s.mediaStreamType === "Video" && s.isAvailable),
        isScreenSharing: p.videoStreams.some(s => s.mediaStreamType === "ScreenSharing" && s.isAvailable)
    }));

    await dotNetRef.invokeMethodAsync("OnRemoteParticipantsChanged", participants);
}

function subscribeToVideoStream(stream) {
    stream.on("isAvailableChanged", () => notifyParticipantsChanged());
}

function subscribeToRemoteParticipant(remoteParticipant) {
    remoteParticipant.on("stateChanged", () => notifyParticipantsChanged());

    remoteParticipant.videoStreams.forEach(subscribeToVideoStream);

    // A stream added mid-call (e.g. screen sharing starting) needs its own isAvailableChanged
    // listener too — not just the ones present when this participant was first subscribed.
    remoteParticipant.on("videoStreamsUpdated", e => {
        e.added.forEach(subscribeToVideoStream);
        notifyParticipantsChanged();
    });
}

function disposeRenderer(streamId) {
    const existing = videoRenderers.get(streamId);
    if (existing) {
        try {
            existing.renderer.dispose();
        } catch {
            // best effort — a renderer failing to dispose cleanly shouldn't block anything else
        }

        videoRenderers.delete(streamId);
    }
}

function disposeAllRenderers() {
    for (const streamId of Array.from(videoRenderers.keys())) {
        disposeRenderer(streamId);
    }
}

export async function initPreview(videoElementId) {
    // Reused by initCallAgent below if the user goes on to actually join — avoids re-requesting device
    // permission and re-creating the camera stream a second time right after the preview already did.
    if (!callClient) {
        callClient = new CallClient();
    }

    if (!deviceManager) {
        deviceManager = await callClient.getDeviceManager();
    }

    let access;
    try {
        access = await deviceManager.askDevicePermission({ audio: true, video: true });
    } catch {
        access = { audio: false, video: false };
    }

    let videoAvailable = false;

    if (access.video) {
        const cameras = await deviceManager.getCameras();
        if (cameras.length > 0) {
            localVideoStream = new LocalVideoStream(cameras[0]);
            videoAvailable = await attachRenderer(LOCAL_STREAM_ID, videoElementId);
        }
    }

    return { audioAvailable: access.audio, videoAvailable };
}

export async function setPreviewCamera(enabled, videoElementId) {
    if (!enabled) {
        disposeRenderer(LOCAL_STREAM_ID);
        localVideoStream = null;
        return true;
    }

    if (!deviceManager) {
        return false;
    }

    if (!localVideoStream) {
        const cameras = await deviceManager.getCameras();
        if (cameras.length === 0) {
            return false;
        }

        localVideoStream = new LocalVideoStream(cameras[0]);
    }

    return await attachRenderer(LOCAL_STREAM_ID, videoElementId);
}

export async function disposePreview() {
    // If a real call has already started, its localVideoStream/renderers are live session state —
    // only tear down when the user backed out of the preview without ever joining.
    if (call) {
        return;
    }

    disposeAllRenderers();
    localVideoStream = null;
}

export async function initCallAgent(userToken, displayName) {
    // Defensive cleanup — the C# side guards against calling this while already in a call, but a
    // stale call/agent left over from an unclean previous session should never linger into a new one.
    if (call) {
        try {
            await call.hangUp();
        } catch {
            // best effort
        }

        call = null;
    }

    if (callAgent) {
        try {
            await callAgent.dispose();
        } catch {
            // best effort — CallAgent.dispose() is known to occasionally throw, safe to ignore here
        }
    }

    if (!callClient) {
        callClient = new CallClient();
    }

    const tokenCredential = new AzureCommunicationTokenCredential(userToken);
    callAgent = await callClient.createCallAgent(tokenCredential, { displayName });

    if (!deviceManager) {
        deviceManager = await callClient.getDeviceManager();
    }
}

export async function joinRoom(roomId, dotNetObjectRef, micEnabled, cameraEnabled) {
    dotNetRef = dotNetObjectRef;

    let access;
    try {
        access = await deviceManager.askDevicePermission({ audio: true, video: true });
    } catch {
        // Some environments (unsupported/insecure context, unusual browser configurations) can
        // throw instead of resolving false — degrade to audio/video-less rather than fail the join.
        access = { audio: false, video: false };
    }

    let videoOptions;
    if (access.video && cameraEnabled) {
        if (!localVideoStream) {
            const cameras = await deviceManager.getCameras();
            if (cameras.length > 0) {
                localVideoStream = new LocalVideoStream(cameras[0]);
            }
        }

        if (localVideoStream) {
            videoOptions = { localVideoStreams: [localVideoStream] };
        }
    } else if (localVideoStream) {
        // Carried over from the preview, but the user chose to join with the camera off — release it
        // so the camera hardware actually turns off instead of just being omitted from the join.
        disposeRenderer(LOCAL_STREAM_ID);
        localVideoStream = null;
    }

    call = callAgent.join({ roomId }, { videoOptions });

    // Detects the call ending from the other side too (e.g. a ScrumMaster/Admin ending it for
    // everyone, which revokes ACS Room access and disconnects this client) — not just our own
    // explicit leaveCall() hangup.
    call.on("stateChanged", () => {
        if (call && call.state === "Disconnected" && dotNetRef) {
            dotNetRef.invokeMethodAsync("OnCallDisconnected").catch(() => {});
        }
    });

    // Screen sharing can stop from outside our own toggle button (e.g. the browser's native "Stop
    // sharing" bar), so the local IsScreenSharing state must be driven by this event — not just set
    // optimistically when our own startScreenShare/stopScreenShare calls resolve — otherwise the two
    // can desync and leave the toggle stuck, unable to start a new share for the rest of the call.
    call.on("isScreenSharingOnChanged", () => {
        if (!call.isScreenSharingOn) {
            disposeRenderer(LOCAL_SCREEN_STREAM_ID);
        }

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync("OnScreenSharingChanged", call.isScreenSharingOn).catch(() => {});
        }
    });

    call.remoteParticipants.forEach(subscribeToRemoteParticipant);
    call.on("remoteParticipantsUpdated", e => {
        e.added.forEach(subscribeToRemoteParticipant);
        e.removed.forEach(p => {
            const participantId = getParticipantId(p);
            disposeRenderer(participantId);
            disposeRenderer(`${participantId}${SCREEN_SHARE_SUFFIX}`);
        });
        notifyParticipantsChanged();
    });

    await notifyParticipantsChanged();

    const videoOn = !!videoOptions;

    if (access.audio && !micEnabled) {
        try {
            await call.mute();
        } catch {
            // best effort — if this fails the mic stays live; the in-call toggle still lets the user retry
        }
    }

    return {
        audioAvailable: access.audio,
        videoAvailable: access.video,
        audioOn: access.audio && micEnabled,
        videoOn
    };
}

export async function toggleMic(enabled) {
    if (!call) {
        return;
    }

    if (enabled) {
        await call.unmute();
    } else {
        await call.mute();
    }
}

export async function toggleCamera(enabled) {
    if (!call || !deviceManager) {
        return;
    }

    if (enabled) {
        if (!localVideoStream) {
            const cameras = await deviceManager.getCameras();
            if (cameras.length === 0) {
                return;
            }

            localVideoStream = new LocalVideoStream(cameras[0]);
        }

        await call.startVideo(localVideoStream);
    } else if (localVideoStream) {
        await call.stopVideo(localVideoStream);
    }
}

export async function startScreenShare() {
    if (!call) {
        return;
    }

    // No explicit LocalVideoStream is passed — the SDK creates and owns one internally per share
    // session (via getDisplayMedia) and disposes it on stopScreenSharing, so this is safely repeatable
    // any number of times within the same call.
    await call.startScreenSharing();
}

export async function stopScreenShare() {
    if (!call) {
        return;
    }

    await call.stopScreenSharing();
}

export async function attachRenderer(streamId, videoElementId) {
    const container = document.getElementById(videoElementId);
    if (!container) {
        return false;
    }

    // Note: LocalVideoStream (used for the local camera/screen-share tiles) has no isAvailable property
    // at all — that's a RemoteVideoStream-only concept, already filtered for by findRemoteVideoStream's
    // own `s.isAvailable` predicate before a remote stream ever reaches here. Checking it again here
    // (as a previous version of this function did) would make every local stream evaluate as
    // unavailable (undefined is falsy) and silently refuse to render the local user's own video/share.
    const stream = resolveStream(streamId);
    if (!stream) {
        return false;
    }

    disposeRenderer(streamId);
    container.replaceChildren();

    const renderer = new VideoStreamRenderer(stream);

    let view;
    try {
        view = await renderer.createView();
    } catch {
        // The stream can legitimately go unavailable in the moment between the availability check
        // above and this call resolving (e.g. the participant just turned their camera/share off) —
        // an expected race, not a real failure. Degrade to "no renderer" instead of throwing, since an
        // uncaught rejection here becomes an uncaught exception in the caller's render cycle.
        renderer.dispose();
        return false;
    }

    container.appendChild(view.target);
    videoRenderers.set(streamId, { renderer, view });

    return true;
}

export async function detachRenderer(streamId) {
    disposeRenderer(streamId);
}

export async function leaveCall() {
    if (call) {
        await call.hangUp();
    }

    disposeAllRenderers();

    call = null;
    localVideoStream = null;
    dotNetRef = null;
}
