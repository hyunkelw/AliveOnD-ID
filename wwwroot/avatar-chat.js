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

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    log('Application initialized');
    setupEventListeners();
    createChatSession();
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
    elements.video.addEventListener('loadedmetadata', () => {
        log('Video metadata loaded', 'success');
    });
    
    elements.video.addEventListener('playing', () => {
        log('Video is playing', 'success');
    });
    
    elements.video.addEventListener('error', (e) => {
        log(`Video error: ${e.message || 'Unknown error'}`, 'error');
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
        
        // Create D-ID stream
        const response = await fetch(`${API_BASE_URL}${API_ENDPOINTS.createStream}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });

        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        
        const streamData = await response.json();
        state.streamId = streamData.id;
        state.streamSessionId = streamData.sessionId;
        
        log(`Stream created: ${state.streamId}`, 'success');
        
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
        
        // Store ICE candidates that arrive before we set remote description
        const pendingCandidates = [];
        
        // Setup event handlers
        state.peerConnection.onicecandidate = async (event) => {
            if (event.candidate) {
                log('ICE candidate generated');
                try {
                    // Send ICE candidate to D-ID
                    const response = await fetch(`${API_BASE_URL}/api/avatar/stream/${state.streamId}/ice`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            sessionId: state.streamSessionId,
                            candidate: event.candidate
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
            log(`Received ${event.track.kind} track`);
            
            // Only set the video source once, when we get the first stream
            if (event.streams && event.streams[0] && !elements.video.srcObject) {
                elements.video.srcObject = event.streams[0];
                log('Video stream attached to element');
            }
        };
        
        state.peerConnection.onconnectionstatechange = () => {
            log(`Connection state: ${state.peerConnection.connectionState}`);
            if (state.peerConnection.connectionState === 'connected') {
                onConnected();
            } else if (state.peerConnection.connectionState === 'failed') {
                onDisconnected();
            }
        };
        
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
                sessionId: state.streamSessionId,
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
    
    // Debug video element state
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
    }, 1000);
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
    onDisconnected();
}

// Send Message
async function handleSendMessage() {
    const message = elements.messageInput.value.trim();
    if (!message || !state.isConnected) return;
    
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
        
        // Send text to avatar
        log('Sending text to avatar...');
        const avatarResponse = await fetch(`${API_BASE_URL}${API_ENDPOINTS.sendText}/${state.streamId}/text`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                sessionId: state.streamSessionId,
                text: responseText,
                emotion: null
            })
        });
        
        if (!avatarResponse.ok) {
            throw new Error('Failed to send text to avatar');
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