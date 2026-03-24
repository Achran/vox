// voiceInterop.js – LiveKit client integration for Blazor WASM via JS interop.
// Requires the livekit-client UMD bundle to be loaded globally (window.LivekitClient).

let _room = null;
let _dotNetRef = null;

/**
 * Connect to a LiveKit room, publish the microphone, and wire up events.
 * @param {string} url   LiveKit server WebSocket URL
 * @param {string} token LiveKit access JWT
 * @param {object} dotNetRef DotNet object reference for callbacks
 * @param {boolean} micEnabled Initial microphone state (true = unmuted)
 */
export async function connect(url, token, dotNetRef, micEnabled) {
    _dotNetRef = dotNetRef;

    if (typeof LivekitClient === "undefined") {
        console.warn("[voiceInterop] LivekitClient global not found – audio will be unavailable.");
        return false;
    }

    // Disconnect any existing room before creating a new one (e.g. on reconnect)
    if (_room) {
        try { _room.disconnect(); } catch (_) { /* ignore */ }
        _room = null;
    }

    try {
        _room = new LivekitClient.Room({
            adaptiveStream: true,
            dynacast: true
        });

        // Active-speaker changes
        _room.on(LivekitClient.RoomEvent.ActiveSpeakersChanged, (speakers) => {
            const ids = speakers.map(p => p.identity);
            _dotNetRef?.invokeMethodAsync("OnActiveSpeakersChanged", ids)
                .catch(e => console.warn("[voiceInterop] OnActiveSpeakersChanged callback failed:", e));
        });

        // Remote participant mute / unmute
        _room.on(LivekitClient.RoomEvent.TrackMuted, (publication, participant) => {
            if (publication.kind === "audio") {
                _dotNetRef?.invokeMethodAsync("OnParticipantMuteChanged", participant.identity, true)
                    .catch(e => console.warn("[voiceInterop] OnParticipantMuteChanged callback failed:", e));
            }
        });

        _room.on(LivekitClient.RoomEvent.TrackUnmuted, (publication, participant) => {
            if (publication.kind === "audio") {
                _dotNetRef?.invokeMethodAsync("OnParticipantMuteChanged", participant.identity, false)
                    .catch(e => console.warn("[voiceInterop] OnParticipantMuteChanged callback failed:", e));
            }
        });

        // Auto-subscribe to remote audio tracks so they play automatically
        _room.on(LivekitClient.RoomEvent.TrackSubscribed, (track, _publication, _participant) => {
            if (track.kind === "audio") {
                const el = track.attach();
                el.id = `lk-audio-${track.sid}`;
                document.body.appendChild(el);
            }
        });

        _room.on(LivekitClient.RoomEvent.TrackUnsubscribed, (track) => {
            track.detach().forEach(el => el.remove());
        });

        _room.on(LivekitClient.RoomEvent.Disconnected, () => {
            _dotNetRef?.invokeMethodAsync("OnLiveKitDisconnected")
                .catch(e => console.warn("[voiceInterop] OnLiveKitDisconnected callback failed:", e));
        });

        await _room.connect(url, token);
        await _room.localParticipant.setMicrophoneEnabled(!!micEnabled);

        return true;
    } catch (err) {
        console.error("[voiceInterop] Failed to connect to LiveKit:", err);
        _room = null;
        return false;
    }
}

/**
 * Enable or disable the local microphone track.
 * @param {boolean} enabled
 */
export async function setMicrophoneEnabled(enabled) {
    if (_room?.localParticipant) {
        await _room.localParticipant.setMicrophoneEnabled(enabled);
    }
}

/**
 * Disconnect from the LiveKit room and clean up.
 */
export async function disconnect() {
    if (_room) {
        _room.disconnect();
        _room = null;
    }
    _dotNetRef = null;
}
