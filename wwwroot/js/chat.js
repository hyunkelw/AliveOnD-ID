// Chat JavaScript Functions for AliveOnD-ID with D-ID WebRTC Integration

let mediaRecorder = null;
let audioChunks = [];
let recordingStartTime = null;
let recordingInterval = null;

// WebRTC variables for D-ID avatar streaming
let peerConnection = null;
let streamId = null;
let sessionId = null;

// Initialize audio recording functionality
window.initializeAudioRecording = () => {
    console.log('Audio recording initialized');
};

window.initializeAvatarStream = async (iceServers, streamOffer) => {
    try {
        console.log('Initializing avatar stream...');
        
        // Create peer connection
        peerConnection = new RTCPeerConnection({ iceServers });
        
        // Set up event listeners
        peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                console.log('ICE candidate generated:', event.candidate);
                // Send ICE candidate to server
                DotNet.invokeMethodAsync('AliveOnD-ID', 'HandleIceCandidate', {
                    candidate: event.candidate.candidate,
                    sdpMid: event.candidate.sdpMid,
                    sdpMLineIndex: event.candidate.sdpMLineIndex
                });
            }
        };

        peerConnection.ontrack = (event) => {
            console.log('Received remote stream track:', event);
            console.log('Track kind:', event.track.kind);
            console.log('Streams:', event.streams);
            
            if (event.streams && event.streams[0]) {
                const remoteStream = event.streams[0];
                const videoElement = document.getElementById('avatar-video');
                
                if (videoElement) {
                    console.log('Setting video source...');
                    videoElement.srcObject = remoteStream;
                    
                    // Force play
                    videoElement.play().then(() => {
                        console.log('Video playing successfully');
                    }).catch((e) => {
                        console.error('Error playing video:', e);
                    });
                } else {
                    console.error('Video element not found!');
                }
            }
        };

        peerConnection.onconnectionstatechange = () => {
            console.log('Connection state:', peerConnection.connectionState);
            
            // Notify Blazor component of connection state changes
            DotNet.invokeMethodAsync('AliveOnD-ID', 'HandleConnectionStateChange', peerConnection.connectionState);
        };

        // Set remote description
        await peerConnection.setRemoteDescription(streamOffer);
        
        // Create and set local description (answer)
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        
        console.log('Avatar stream initialized successfully');
        return answer;
        
    } catch (error) {
        console.error('Error initializing avatar stream:', error);
        throw error;
    }
};


// Get the SDP answer to send back to D-ID (called from C#)
window.getSdpAnswer = () => {
    if (peerConnection && peerConnection.localDescription) {
        return peerConnection.localDescription;
    }
    return null;
};

// Close WebRTC connection
window.closeAvatarStream = () => {
    try {
        if (peerConnection) {
            peerConnection.close();
            peerConnection = null;
        }
        
        const videoElement = document.getElementById('avatar-video');
        if (videoElement) {
            videoElement.srcObject = null;
        }
        
        console.log('Avatar stream closed');
    } catch (error) {
        console.error('Error closing avatar stream:', error);
    }
};

// Start audio recording
window.startAudioRecording = async () => {
    try {
        console.log('Starting audio recording...');
        
        // Request microphone permission
        const stream = await navigator.mediaDevices.getUserMedia({ 
            audio: {
                echoCancellation: true,
                noiseSuppression: true,
                sampleRate: 44100
            } 
        });

        // Create MediaRecorder instance
        mediaRecorder = new MediaRecorder(stream, {
            mimeType: 'audio/webm;codecs=opus'
        });

        audioChunks = [];
        recordingStartTime = Date.now();

        // Handle data available event
        mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                audioChunks.push(event.data);
            }
        };

        // Handle recording stop
        mediaRecorder.onstop = async () => {
            console.log('Recording stopped');
            
            // Stop all tracks to release microphone
            stream.getTracks().forEach(track => track.stop());
            
            // Create audio blob
            const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
            
            // For now, just log the audio data
            // TODO: Implement audio processing
            console.log('Audio recorded:', audioBlob.size, 'bytes');
        };

        // Start recording
        mediaRecorder.start(1000); // Collect data every second
        
        // Start recording timer
        startRecordingTimer();
        
        console.log('Recording started successfully');
        return true;

    } catch (error) {
        console.error('Error starting audio recording:', error);
        
        if (error.name === 'NotAllowedError') {
            alert('Microphone access was denied. Please allow microphone access and try again.');
        } else if (error.name === 'NotFoundError') {
            alert('No microphone found. Please connect a microphone and try again.');
        } else {
            alert('Error accessing microphone: ' + error.message);
        }
        
        return false;
    }
};

// Stop audio recording
window.stopAudioRecording = () => {
    try {
        console.log('Stopping audio recording...');
        
        if (mediaRecorder && mediaRecorder.state === 'recording') {
            mediaRecorder.stop();
            stopRecordingTimer();
            return true;
        } else {
            console.warn('MediaRecorder is not recording');
            return false;
        }
    } catch (error) {
        console.error('Error stopping audio recording:', error);
        return false;
    }
};

// Recording timer functions
function startRecordingTimer() {
    recordingInterval = setInterval(() => {
        if (recordingStartTime) {
            const elapsed = Date.now() - recordingStartTime;
            const minutes = Math.floor(elapsed / 60000);
            const seconds = Math.floor((elapsed % 60000) / 1000);
            const timeString = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            
            // Update timer display
            const timerElement = document.querySelector('.recording-timer');
            if (timerElement) {
                timerElement.textContent = timeString;
            }
        }
    }, 1000);
}

function stopRecordingTimer() {
    if (recordingInterval) {
        clearInterval(recordingInterval);
        recordingInterval = null;
    }
    recordingStartTime = null;
}

// Scroll to bottom of messages
window.scrollToBottom = (elementId) => {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    } catch (error) {
        console.error('Error scrolling to bottom:', error);
    }
};

// Smooth scroll to bottom with animation
window.smoothScrollToBottom = (elementId) => {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTo({
                top: element.scrollHeight,
                behavior: 'smooth'
            });
        }
    } catch (error) {
        console.error('Error smooth scrolling to bottom:', error);
    }
};

// Check if browser supports audio recording
window.supportsAudioRecording = () => {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia && window.MediaRecorder);
};

// Get audio recording support details
window.getAudioRecordingSupport = () => {
    const support = {
        mediaDevices: !!navigator.mediaDevices,
        getUserMedia: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia),
        mediaRecorder: !!window.MediaRecorder,
        supportedMimeTypes: []
    };

    if (window.MediaRecorder) {
        const mimeTypes = [
            'audio/webm;codecs=opus',
            'audio/webm',
            'audio/mp4',
            'audio/wav'
        ];

        mimeTypes.forEach(mimeType => {
            if (MediaRecorder.isTypeSupported(mimeType)) {
                support.supportedMimeTypes.push(mimeType);
            }
        });
    }

    return support;
};

// Utility function to convert blob to base64
window.blobToBase64 = (blob) => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
};

// Debug function to log browser capabilities
window.logBrowserCapabilities = () => {
    console.log('Browser Capabilities:');
    console.log('- Audio Recording:', window.supportsAudioRecording());
    console.log('- Audio Support Details:', window.getAudioRecordingSupport());
    console.log('- WebRTC Support:', !!window.RTCPeerConnection);
    console.log('- User Agent:', navigator.userAgent);
};

// Initialize everything when the page loads
document.addEventListener('DOMContentLoaded', () => {
    console.log('AliveOnD-ID Chat JavaScript loaded');
    window.logBrowserCapabilities();
});