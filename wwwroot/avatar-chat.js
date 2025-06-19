// Avatar Chat - Simple JavaScript Implementation
// Debug logging utility
const DEBUG = true;

function log(message, type = 'info') {
    if (!DEBUG) return;

    const timestamp = new Date().toLocaleTimeString();
    const logEntry = `[${timestamp}] ${message}`;

    // Console log
    console.log(logEntry);

    // Debug panel
    const debugPanel = document.getElementById('debug-panel');
    const logDiv = document.createElement('div');
    logDiv.className = `debug-log debug-${type}`;
    logDiv.textContent = logEntry;
    debugPanel.appendChild(logDiv);

    // Keep only last 10 logs
    while (debugPanel.children.length > 10) {
        debugPanel.removeChild(debugPanel.firstChild);
    }
}

// API Configuration
const API_BASE_URL = window.location.origin;
const API_ENDPOINTS = {
    createSession: '/api/session/create',
    getSession: '/api/session',
    addMessage: '/api/session',
    createStream: '/api/avatar/stream/create',
    startStream: '/api/avatar/stream',  // {streamId}/start
    sendIce: '/api/avatar/stream',      // {streamId}/ice
    sendText: '/api/avatar/stream',     // {streamId}/text
    uploadAudio: '/api/audio/upload',
    testLLM: '/api/llm/test'
};

// Application State
let state = {
    sessionId: null,
    userId: null,
    streamId: null,
    streamSessionId: null,
    peerConnection: null,
    dataChannel: null,      // ADD THIS
    isStreamReady: false,   // ADD THIS
    isConnected: false,
    isRecording: false,
    mediaRecorder: null,
    audioChunks: []
};

// DOM Elements
const elements = {
    status: document.getElementById('status'),
    video: document.getElementById('avatar-video'),
    chatMessages: document.getElementById('chat-messages'),
    messageInput: document.getElementById('message-input'),
    sendBtn: document.getElementById('send-btn'),
    connectBtn: document.getElementById('connect-btn'),
    recordBtn: document.getElementById('record-btn')
};

// Update video debug info
function updateVideoDebug() {
    const debugSpan = document.getElementById('video-debug');
    if (debugSpan && elements.video) {
        const info = `${elements.video.videoWidth}x${elements.video.videoHeight} | Ready: ${elements.video.readyState} | Src: ${elements.video.srcObject ? 'Yes' : 'No'}`;
        debugSpan.textContent = info;
    }
}

function debugMediaState() {
    log('=== MEDIA STATE DEBUG ===', 'info');
    
    if (elements.video.srcObject) {
        const stream = elements.video.srcObject;
        const audioTracks = stream.getAudioTracks();
        const videoTracks = stream.getVideoTracks();
        
        log(`Stream active: ${stream.active}`, 'info');
        log(`Video element muted: ${elements.video.muted}`, 'info');
        log(`Video element volume: ${elements.video.volume}`, 'info');
        log(`Video element paused: ${elements.video.paused}`, 'info');
        
        log(`Audio tracks: ${audioTracks.length}`, 'info');
        audioTracks.forEach((track, i) => {
            log(`  Audio ${i}: enabled=${track.enabled}, muted=${track.muted}, readyState=${track.readyState}`, 'info');
        });
        
        log(`Video tracks: ${videoTracks.length}`, 'info');
        videoTracks.forEach((track, i) => {
            log(`  Video ${i}: enabled=${track.enabled}, muted=${track.muted}, readyState=${track.readyState}`, 'info');
        });
    } else {
        log('No srcObject attached to video element', 'error');
    }
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    log('Application initialized');
    setupEventListeners();
    createChatSession();

    // Update video debug info periodically
    setInterval(updateVideoDebug, 500);
});

// Event Listeners
function setupEventListeners() {
    elements.connectBtn.addEventListener('click', handleConnect);
    elements.sendBtn.addEventListener('click', handleSendMessage);
    elements.recordBtn.addEventListener('click', handleRecord);
    elements.messageInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') handleSendMessage();
    });

    // Video element event listeners for debugging
    elements.video.addEventListener('loadstart', () => {
        log('Video load started', 'info');
    });

    elements.video.addEventListener('loadeddata', () => {
        log('Video data loaded', 'success');
    });

    elements.video.addEventListener('loadedmetadata', () => {
        log(`Video metadata loaded: ${elements.video.videoWidth}x${elements.video.videoHeight}`, 'success');
    });

    elements.video.addEventListener('canplay', () => {
        log('Video can start playing', 'success');
    });

    elements.video.addEventListener('playing', () => {
        log('Video is playing', 'success');
    });

    elements.video.addEventListener('waiting', () => {
        log('Video is waiting for data', 'info');
    });

    elements.video.addEventListener('stalled', () => {
        log('Video stalled', 'error');
    });

    elements.video.addEventListener('error', (e) => {
        const error = elements.video.error;
        if (error) {
            log(`Video error: ${error.message} (code: ${error.code})`, 'error');
        }
    });
}

// Create Chat Session
async function createChatSession() {
    try {
        state.userId = `user_${Date.now()}`;
        log(`Creating session for user: ${state.userId}`);

        const response = await fetch(`${API_BASE_URL}${API_ENDPOINTS.createSession}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: state.userId })
        });

        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

        const session = await response.json();
        state.sessionId = session.sessionId;
        log(`Session created: ${state.sessionId}`, 'success');

    } catch (error) {
        log(`Failed to create session: ${error.message}`, 'error');
        addMessage('Failed to create chat session', 'system');
    }
}

// Connect to Avatar
async function handleConnect() {
    if (state.isConnected) {
        await disconnectAvatar();
    } else {
        await connectAvatar();
    }
}

async function connectAvatar() {
    try {
        updateStatus('connecting');
        elements.connectBtn.disabled = true;
        log('Creating D-ID stream...');

        // Create D-ID stream with additional options based on SDK docs
        const response = await fetch(`${API_BASE_URL}${API_ENDPOINTS.createStream}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                // Add stream options that might help
                compatibilityMode: 'auto',
                streamWarmup: true
            })
        });

        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

        const streamData = await response.json();

        // Debug: log the entire response
        log(`D-ID response: ${JSON.stringify(streamData)}`, 'info');

        state.streamId = streamData.id;
        // Try both formats since D-ID might return either
        state.streamSessionId = streamData.session_id || streamData.sessionId;

        if (!state.streamSessionId) {
            log('WARNING: No session ID found in response!', 'error');
        }

        log(`Stream created: ${state.streamId}`, 'success');
        log(`Session ID: ${state.streamSessionId}`, 'info');

        // Setup WebRTC
        await setupWebRTC(streamData);

    } catch (error) {
        log(`Connection failed: ${error.message}`, 'error');
        updateStatus('disconnected');
        elements.connectBtn.disabled = false;
    }
}

async function setupWebRTC(streamData) {
    try {
        log('Setting up WebRTC connection...');

        // Create peer connection
        state.peerConnection = new RTCPeerConnection({
            iceServers: streamData.iceServers
        });

        // Add data channel for stream events
        state.dataChannel = state.peerConnection.createDataChannel('JanusDataChannel');
        state.isStreamReady = false; // Add this to state

        // Handle data channel events
        state.dataChannel.onopen = () => {
            log('Data channel opened', 'success');
        };

        state.dataChannel.onmessage = (event) => {
            const [eventType, _] = event.data.split(':');
            log(`Data channel event: ${event.data}`, 'info');

            if (eventType === 'stream/ready') {
                log('Stream is ready!', 'success');
                state.isStreamReady = true;
                // Now send the greeting
                sendInitialGreeting();
            }
        };

        // Store ICE candidates that arrive before we set remote description
        const pendingCandidates = [];

        // Setup event handlers
        state.peerConnection.onicecandidate = async (event) => {
            if (event.candidate) {
                try {
                    const { candidate, sdpMid, sdpMLineIndex } = event.candidate;
                    log(`ICE candidate: ${candidate}\n mid: ${sdpMid}\n lineIndex: ${sdpMLineIndex}`, 'info');
                    // Send ICE candidate to backend with explicit fields
                    const response = await fetch(`${API_BASE_URL}/api/avatar/stream/${state.streamId}/ice`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            sessionId: state.streamSessionId,
                            candidate: candidate,
                            mid: sdpMid,
                            lineIndex: sdpMLineIndex
                        })
                    });
                    if (!response.ok) {
                        log('Failed to send ICE candidate', 'error');
                    }
                } catch (error) {
                    log(`ICE candidate error: ${error.message}`, 'error');
                }
            }
        };

        state.peerConnection.ontrack = (event) => {
            log(`Received ${event.track.kind} track - ID: ${event.track.id}, Label: ${event.track.label}`, 'info');

            if (event.streams && event.streams[0]) {
                const stream = event.streams[0];
                log(`Stream ID: ${stream.id}, Active: ${stream.active}`, 'info');

                const audioTracks = stream.getAudioTracks();
                const videoTracks = stream.getVideoTracks();

                log(`Audio tracks: ${audioTracks.length}, Video tracks: ${videoTracks.length}`, 'info');

                audioTracks.forEach((track, index) => {
                    log(`Audio track ${index}: ${track.label}, enabled: ${track.enabled}, muted: ${track.muted}`, 'info');
                });
            }

            if (event.track.kind === 'video' && event.streams && event.streams[0]) {
                // Clear any existing source first
                elements.video.srcObject = null;

                // Set the new stream
                elements.video.srcObject = event.streams[0];
                log('Video stream attached to element');

                // Force load
                elements.video.load();

                // Try to play after a short delay
                setTimeout(() => {
                    elements.video.play().then(() => {
                        log('Video play started', 'success');
                    }).catch(e => {
                        log(`Video play failed: ${e.message}`, 'error');
                        // Try playing muted if autoplay fails
                        elements.video.muted = true;
                        elements.video.play().then(() => {
                            log('Video playing muted', 'info');
                        }).catch(e2 => {
                            log(`Muted play also failed: ${e2.message}`, 'error');
                        });
                    });
                }, 100);
            }
        };

        state.peerConnection.ontrack = (event) => {
    OnTrackReceived(event);
};

        state.peerConnection.onconnectionstatechange = () => {
            log(`Connection state: ${state.peerConnection.connectionState}`);
            if (state.peerConnection.connectionState === 'connected') {
                // onConnected();
                log('Peer connection established', 'success');
            } else if (state.peerConnection.connectionState === 'failed') {
                onDisconnected();
            }
        };

        state.peerConnection.oniceconnectionstatechange = () => {
            // log(`ICE connection state: ${state.peerConnection.iceConnectionState}`);
            if (state.peerConnection.iceConnectionState === 'disconnected' ||
                state.peerConnection.iceConnectionState === 'failed') {
                onDisconnected();
            }
            else if (state.peerConnection.iceConnectionState === 'connected') {
                log(`ICE connection state changed: ${state.peerConnection.iceConnectionState}`, 'info');
                onConnected();
            }
        }

        // Set remote description (offer from D-ID)
        await state.peerConnection.setRemoteDescription(streamData.offer);
        log('Remote description set');

        // Create answer
        const answer = await state.peerConnection.createAnswer();
        await state.peerConnection.setLocalDescription(answer);
        log('Local description set');

        // Send answer to D-ID
        log('Sending answer to D-ID...');
        const startResponse = await fetch(`${API_BASE_URL}/api/avatar/stream/${state.streamId}/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                sessionId: state.streamSessionId,  // camelCase for our backend
                sdpAnswer: answer
            })
        });

        if (!startResponse.ok) {
            throw new Error('Failed to start stream');
        }

        log('Answer sent to D-ID successfully', 'success');

        // The connection state will update automatically
        // No need for manual timeout

    } catch (error) {
        log(`WebRTC setup failed: ${error.message}`, 'error');
        throw error;
    }
}

function OnTrackReceived(event) {
    log(`=== TRACK RECEIVED ===`, 'info');
    log(`Track kind: ${event.track.kind}`, 'info');
    log(`Track ID: ${event.track.id}`, 'info');
    log(`Track enabled: ${event.track.enabled}`, 'info');
    log(`Track muted: ${event.track.muted}`, 'info');
    log(`Track readyState: ${event.track.readyState}`, 'info');

    if (event.streams && event.streams[0]) {
        const stream = event.streams[0];
        log(`Stream ID: ${stream.id}`, 'info');

        // Log ALL tracks in the stream
        const allTracks = stream.getTracks();
        log(`Total tracks in stream: ${allTracks.length}`, 'info');

        allTracks.forEach((track, index) => {
            log(`  Track ${index}: ${track.kind} - ${track.label} (enabled: ${track.enabled})`, 'info');
        });

        // Count audio and video tracks
        const audioTracks = stream.getAudioTracks();
        const videoTracks = stream.getVideoTracks();
        log(`Stream contains: ${audioTracks.length} audio tracks, ${videoTracks.length} video tracks`, 'info');

        // Log detailed audio track info
        audioTracks.forEach((track, i) => {
            log(`  Audio Track ${i}: Label="${track.label}", ID="${track.id}", enabled=${track.enabled}, muted=${track.muted}`, 'info');
        });

        // Attach the stream to video element only once
        if (!elements.video.srcObject || elements.video.srcObject.id !== stream.id) {
            // Clear any existing source first
            if (elements.video.srcObject) {
                log('Replacing existing stream', 'info');
            }

            elements.video.srcObject = stream;
            log('Stream attached to video element', 'success');

            // Force load
            elements.video.load();

            // Try to play after a short delay
            setTimeout(async () => {
                try {
                    // Check current state before playing
                    log(`Before play: muted=${elements.video.muted}, volume=${elements.video.volume}`, 'info');

                    // First try to play with audio
                    await elements.video.play();
                    log('Video playing with audio', 'success');

                    // Double-check audio state after play
                    const postPlayAudioTracks = elements.video.srcObject.getAudioTracks();
                    log(`After play: ${postPlayAudioTracks.length} audio tracks`, 'info');
                    postPlayAudioTracks.forEach((track, i) => {
                        log(`  Post-play Audio ${i}: enabled=${track.enabled}`, 'info');
                    });

                } catch (e) {
                    log(`Autoplay failed: ${e.name} - ${e.message}`, 'error');

                    // Handle autoplay policy restrictions
                    if (e.name === 'NotAllowedError') {
                        // Check if there's already an audio enable button
                        const existingBtn = document.getElementById('enable-audio-btn');
                        if (!existingBtn) {
                            // Create a button to manually enable audio
                            const enableAudioBtn = document.createElement('button');
                            enableAudioBtn.id = 'enable-audio-btn';
                            enableAudioBtn.textContent = '🔊 Click to Enable Audio';
                            enableAudioBtn.style.cssText = `
                                position: absolute;
                                top: 50%;
                                left: 50%;
                                transform: translate(-50%, -50%);
                                z-index: 1000;
                                padding: 15px 30px;
                                font-size: 18px;
                                background: #3b82f6;
                                color: white;
                                border: none;
                                border-radius: 25px;
                                cursor: pointer;
                                box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                            `;

                            elements.video.parentElement.appendChild(enableAudioBtn);

                            enableAudioBtn.onclick = async () => {
                                try {
                                    elements.video.muted = false;
                                    elements.video.volume = 1.0;
                                    await elements.video.play();
                                    enableAudioBtn.remove();
                                    log('Audio enabled by user interaction', 'success');

                                    // Log final audio state
                                    const audioTracks = elements.video.srcObject.getAudioTracks();
                                    log(`Audio enabled: ${audioTracks.length} tracks, muted=${elements.video.muted}, volume=${elements.video.volume}`, 'success');
                                } catch (err) {
                                    log(`Failed to enable audio: ${err.message}`, 'error');
                                }
                            };
                        }

                        // Try to play muted as fallback
                        try {
                            elements.video.muted = true;
                            await elements.video.play();
                            log('Playing muted due to autoplay policy - click button to enable audio', 'warning');
                        } catch (e2) {
                            log(`Even muted play failed: ${e2.message}`, 'error');
                        }
                    } else {
                        // Other errors
                        log(`Play error: ${e.toString()}`, 'error');
                    }
                }
            }, 100);

        } else {
            log('Stream already attached, skipping', 'info');
        }

    } else {
        log('No streams in track event!', 'error');
    }

    // Log specific track type
    if (event.track.kind === 'video') {
        log('Video track processing complete', 'success');
    } else if (event.track.kind === 'audio') {
        log('Audio track processing complete', 'success');

        // Additional audio-specific debugging
        if (event.streams && event.streams[0]) {
            const audioTracks = event.streams[0].getAudioTracks();
            if (audioTracks.length > 0) {
                log(`Audio track settings: ${JSON.stringify(audioTracks[0].getSettings())}`, 'info');
            }
        }
    }
}

async function sendInitialGreeting() {
    if (!state.isStreamReady) {
        log('Stream not ready yet, waiting...', 'info');
        return;
    }

    const greetingText = "Hello! I'm your AI assistant. How can I help you today?";

    // Your existing greeting code here...
    const requestBody = {
        session_id: state.streamSessionId,
        script: {
            type: "text",
            provider: {
                type: "microsoft",
                voice_id: "en-US-JennyNeural"
            },
            input: greetingText,
            ssml: false
        },
        config: {
            stitch: true
        }
    };

    log(`Sending request body: ${JSON.stringify(requestBody)}`, 'info');

    // D-ID expects the stream endpoint without '/text' suffix
    const response = await fetch(`${API_BASE_URL}/api/avatar/stream/${state.streamId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestBody)
    });

    if (response.ok) {
        log('Greeting sent to avatar', 'success');
        addMessage(greetingText, 'assistant');
    } else {
        const errorText = await response.text();
        log(`Failed to send greeting: ${response.status} - ${errorText}`, 'error');
    }
}

function onConnected() {
    state.isConnected = true;
    updateStatus('connected');
    elements.connectBtn.textContent = 'Disconnect';
    elements.connectBtn.disabled = false;
    elements.messageInput.disabled = false;
    elements.sendBtn.disabled = false;
    elements.recordBtn.disabled = false;

    addMessage('Avatar connected! You can now start chatting.', 'system');
    log('Avatar connected successfully', 'success');

    const sendGreetingWhenReady = () => {
        if (!state.isStreamReady) {
            log('Waiting for stream to be ready...', 'info');
            setTimeout(sendGreetingWhenReady, 500); // Check every 500ms
            return;
        }

        // Now send the greeting
        log('Sending greeting to activate avatar...');
        const greetingText = "Hello! I'm your AI assistant. How can I help you today?";

        // Your existing greeting sending code here
        fetch(`${API_BASE_URL}/api/avatar/stream/${state.streamId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                session_id: state.streamSessionId,
                script: {
                    type: "text",
                    provider: {
                        type: "microsoft",
                        voice_id: "en-US-JennyNeural"
                    },
                    input: greetingText,
                    ssml: false
                },
                config: {
                    stitch: true
                }
            })
        }).then(response => {
            if (response.ok) {
                log('Greeting sent to avatar', 'success');
                addMessage(greetingText, 'assistant');
            } else {
                response.text().then(errorText => {
                    log(`Failed to send greeting: ${response.status} - ${errorText}`, 'error');
                });
            }
        }).catch(error => {
            log(`Greeting error: ${error.message}`, 'error');
        });
    };

    // Start checking for readiness
    sendGreetingWhenReady();

    // Fallback: force ready after 5 seconds if no stream/ready event
    setTimeout(() => {
        if (!state.isStreamReady) {
            log('Forcing stream ready state after timeout', 'warning');
            state.isStreamReady = true;
        }
    }, 5000);

    // Debug video element state after a delay
    setTimeout(() => {
        if (elements.video.srcObject) {
            const stream = elements.video.srcObject;
            log(`Video element state: readyState=${elements.video.readyState}, videoWidth=${elements.video.videoWidth}, videoHeight=${elements.video.videoHeight}`);
            log(`Stream active: ${stream.active}, tracks: ${stream.getTracks().length}`);
            stream.getTracks().forEach(track => {
                log(`Track: ${track.kind}, enabled=${track.enabled}, readyState=${track.readyState}`);
            });
        } else {
            log('No srcObject on video element!', 'error');
        }
    }, 3000);
}

function onDisconnected() {
    state.isConnected = false;
    updateStatus('disconnected');
    elements.connectBtn.textContent = 'Connect Avatar';
    elements.connectBtn.disabled = false;
    elements.messageInput.disabled = true;
    elements.sendBtn.disabled = true;
    elements.recordBtn.disabled = true;

    if (state.peerConnection) {
        state.peerConnection.close();
        state.peerConnection = null;
    }

    // Properly clean up video element
    if (elements.video.srcObject) {
        const stream = elements.video.srcObject;
        stream.getTracks().forEach(track => track.stop());
        elements.video.srcObject = null;
    }

    addMessage('Avatar disconnected', 'system');
    log('Avatar disconnected');
}

async function disconnectAvatar() {
    log('Disconnecting avatar...');

    // Close the D-ID stream properly
    if (state.streamId && state.streamSessionId) {
        try {
            const response = await fetch(`${API_BASE_URL}/api/avatar/stream/${state.streamId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    session_id: state.streamSessionId  // Note: underscore format
                })
            });

            if (response.ok) {
                log('Stream closed successfully', 'success');
            } else {
                log('Failed to close stream properly', 'error');
            }
        } catch (error) {
            log(`Error closing stream: ${error.message}`, 'error');
        }
    }

    onDisconnected();
}

// Send Message
async function handleSendMessage() {
    const message = elements.messageInput.value.trim();
    if (!message || !state.isConnected) return;

    if (!state.isStreamReady) {
        log('Stream not ready yet, cannot send message', 'warning');
        addMessage('Please wait, avatar is still initializing...', 'system');
        return;
    }

    elements.messageInput.value = '';
    elements.sendBtn.disabled = true;

    // Add user message
    addMessage(message, 'user');

    try {
        // Get LLM response (skip for now due to missing LLM endpoint)
        log('Skipping LLM call (no endpoint configured)');
        const responseText = "I understand. This is a test response from the avatar. Your backend is working correctly!";

        // Add assistant message
        addMessage(responseText, 'assistant');

        // Send text to avatar using correct D-ID format
        log('Sending text to avatar...');
        const avatarResponse = await fetch(`${API_BASE_URL}/api/avatar/stream/${state.streamId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                session_id: state.streamSessionId,
                script: {
                    type: "text",
                    provider: {
                        type: "microsoft",
                        voice_id: "en-US-JennyNeural"  // Default Microsoft voice
                    },
                    input: responseText,
                    ssml: false
                },
                config: {
                    stitch: true
                }
            })
        });

        if (!avatarResponse.ok) {
            const errorText = await avatarResponse.text();
            throw new Error(`${avatarResponse.status}: ${errorText}`);
        }

        log('Text sent to avatar successfully', 'success');

    } catch (error) {
        log(`Message send failed: ${error.message}`, 'error');
        addMessage('Failed to send message', 'system');
    } finally {
        elements.sendBtn.disabled = false;
    }
}

// Audio Recording
async function handleRecord() {
    if (state.isRecording) {
        stopRecording();
    } else {
        startRecording();
    }
}

async function startRecording() {
    try {
        log('Starting audio recording...');

        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        state.mediaRecorder = new MediaRecorder(stream);
        state.audioChunks = [];

        state.mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                state.audioChunks.push(event.data);
            }
        };

        state.mediaRecorder.onstop = async () => {
            const audioBlob = new Blob(state.audioChunks, { type: 'audio/webm' });
            await processAudioRecording(audioBlob);

            // Stop all tracks
            stream.getTracks().forEach(track => track.stop());
        };

        state.mediaRecorder.start();
        state.isRecording = true;
        elements.recordBtn.classList.add('recording');
        elements.recordBtn.textContent = '⏹️ Stop';

        log('Recording started', 'success');

    } catch (error) {
        log(`Recording failed: ${error.message}`, 'error');
        alert('Microphone access denied');
    }
}

function stopRecording() {
    if (state.mediaRecorder && state.isRecording) {
        state.mediaRecorder.stop();
        state.isRecording = false;
        elements.recordBtn.classList.remove('recording');
        elements.recordBtn.textContent = '🎤 Record';
        log('Recording stopped');
    }
}

async function processAudioRecording(audioBlob) {
    try {
        log('Processing audio recording...');

        // For demo purposes, we'll just add a placeholder message
        // In a real implementation, you'd upload the audio and get transcription
        addMessage('[Audio message recorded]', 'user');

        // Simulate assistant response
        setTimeout(() => {
            addMessage('I heard your audio message. This is a demo response.', 'assistant');
        }, 1000);

    } catch (error) {
        log(`Audio processing failed: ${error.message}`, 'error');
    }
}

// UI Helper Functions
function updateStatus(status) {
    elements.status.className = `status-badge status-${status}`;
    elements.status.textContent = status.charAt(0).toUpperCase() + status.slice(1);
}

function addMessage(text, type) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${type}`;
    messageDiv.textContent = text;

    elements.chatMessages.appendChild(messageDiv);
    elements.chatMessages.scrollTop = elements.chatMessages.scrollHeight;

    // Store message in session (simplified)
    if (state.sessionId && type !== 'system') {
        // In real implementation, call API to store message
    }
}

// Utility function to check WebRTC support
function checkWebRTCSupport() {
    const supported = !!(
        window.RTCPeerConnection &&
        navigator.mediaDevices &&
        navigator.mediaDevices.getUserMedia
    );

    log(`WebRTC support: ${supported}`, supported ? 'success' : 'error');

    if (!supported) {
        addMessage('Your browser does not support WebRTC. Please use a modern browser.', 'system');
        elements.connectBtn.disabled = true;
    }
}

// Check support on load
checkWebRTCSupport();

window.debugMediaState = debugMediaState;