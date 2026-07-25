import { CallClient, LocalVideoStream, VideoStreamRenderer } from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";

const LOCAL_STREAM_ID = "local";

let callClient = null;
let deviceManager = null;
let callAgent = null;
let call = null;
let localVideoStream = null;
let dotNetRef = null;
const videoRenderers = new Map();

function getParticipantId(remoteParticipant) {
    const identifier = remoteParticipant.identifier;
    return identifier.communicationUserId ?? identifier.id ?? remoteParticipant.displayName ?? "";
}

function findRemoteVideoStream(participantId) {
    if (!call) {
        return null;
    }

    const participant = call.remoteParticipants.find(p => getParticipantId(p) === participantId);
    return participant?.videoStreams.find(s => s.isAvailable) ?? null;
}

async function notifyParticipantsChanged() {
    if (!dotNetRef || !call) {
        return;
    }

    const participants = call.remoteParticipants.map(p => ({
        id: getParticipantId(p),
        displayName: p.displayName ?? null,
        hasVideo: p.videoStreams.some(s => s.isAvailable)
    }));

    await dotNetRef.invokeMethodAsync("OnRemoteParticipantsChanged", participants);
}

function subscribeToRemoteParticipant(remoteParticipant) {
    remoteParticipant.on("stateChanged", () => notifyParticipantsChanged());
    remoteParticipant.on("videoStreamsUpdated", () => notifyParticipantsChanged());

    remoteParticipant.videoStreams.forEach(stream => {
        stream.on("isAvailableChanged", () => notifyParticipantsChanged());
    });
}

function disposeRenderer(streamId) {
    const existing = videoRenderers.get(streamId);
    if (existing) {
        existing.renderer.dispose();
        videoRenderers.delete(streamId);
    }
}

function disposeAllRenderers() {
    for (const streamId of Array.from(videoRenderers.keys())) {
        disposeRenderer(streamId);
    }
}

export async function initCallAgent(userToken, displayName) {
    callClient = new CallClient();
    const tokenCredential = new AzureCommunicationTokenCredential(userToken);
    callAgent = await callClient.createCallAgent(tokenCredential, { displayName });
    deviceManager = await callClient.getDeviceManager();
}

export async function joinRoom(roomId, dotNetObjectRef) {
    dotNetRef = dotNetObjectRef;

    const access = await deviceManager.askDevicePermission({ audio: true, video: true });

    let videoOptions;
    if (access.video) {
        const cameras = await deviceManager.getCameras();
        if (cameras.length > 0) {
            localVideoStream = new LocalVideoStream(cameras[0]);
            videoOptions = { localVideoStreams: [localVideoStream] };
        }
    }

    call = callAgent.join({ roomId }, { videoOptions });

    call.remoteParticipants.forEach(subscribeToRemoteParticipant);
    call.on("remoteParticipantsUpdated", e => {
        e.added.forEach(subscribeToRemoteParticipant);
        e.removed.forEach(p => disposeRenderer(getParticipantId(p)));
        notifyParticipantsChanged();
    });

    await notifyParticipantsChanged();

    return { audioAvailable: access.audio, videoAvailable: access.video };
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

    const stream = streamId === LOCAL_STREAM_ID ? localVideoStream : findRemoteVideoStream(streamId);
    if (!stream) {
        return false;
    }

    disposeRenderer(streamId);

    const renderer = new VideoStreamRenderer(stream);
    const view = await renderer.createView();
    container.appendChild(view.target);
    videoRenderers.set(streamId, { renderer, view });

    return true;
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
