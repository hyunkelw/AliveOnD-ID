// D-ID Stream Manager Class
class DIDStreamManager {
    constructor(videoElementId, options = {}) {
        console.log('Initializing DIDStreamManager...', { videoElementId, options });
        
        const videoElement = document.getElementById(videoElementId);
        if (!videoElement) {
            throw new Error(`Video element with id '${videoElementId}' not found`);
        }
        
        this.videoElement = videoElement;
        this.peerConnection = null;
        this.streamId = null;
        this.sessionId = null;
        this.isConnected = false;
        
        // Stream options
        this.options = {
            compatibilityMode: options.compatibilityMode || "auto",
            streamWarmup: options.streamWarmup ?? true,
            sessionTimeout: Math.min(options.sessionTimeout || 300, 300),
            outputResolution: Math.min(Math.max(options.outputResolution || 720, 150), 1080)
        };
        
        // Set up callback functions by evaluating the string expressions
        this.callbacks = {
            onConnectionStateChange: (state) => {
                try {
                    if (typeof options.onConnectionStateChange === 'string') {
                        (new Function('state', options.onConnectionStateChange))(state);
                    }
                } catch (error) {
                    console.error('Error in onConnectionStateChange callback:', error);
                }
            },
            onVideoStateChange: (state) => {
                try {
                    if (typeof options.onVideoStateChange === 'string') {
                        (new Function('state', options.onVideoStateChange))(state);
                    }
                } catch (error) {
                    console.error('Error in onVideoStateChange callback:', error);
                }
            },
            onError: (error, details) => {
                try {
                    if (typeof options.onError === 'string') {
                        (new Function('error, details', options.onError))(error, details);
                    }
                } catch (err) {
                    console.error('Error in onError callback:', err);
                }
            }
        };
    }

    async connect(streamData) {
        try {
            console.log('Connecting to D-ID stream...', streamData);

            if (this.peerConnection) {
                this.disconnect();
            }

            this.streamId = streamData.id;
            this.sessionId = streamData.sessionId;            // Create and configure peer connection with transceivers
            this.peerConnection = new RTCPeerConnection({
                iceServers: streamData.iceServers,
                iceTransportPolicy: 'all',
                bundlePolicy: 'max-bundle',
                rtcpMuxPolicy: 'require',
                sdpSemantics: 'unified-plan'
            });

            // Add transceivers to ensure we receive audio and video
            this.peerConnection.addTransceiver('video', {direction: 'recvonly'});
            this.peerConnection.addTransceiver('audio', {direction: 'recvonly'});

            // Handle tracks
            this.peerConnection.ontrack = (event) => {
                console.log('Received track:', event.track.kind);
                if (event.streams && event.streams[0]) {
                    this.onSrcObjectReady(event.streams[0]);
                }
            };

            // Handle ICE connection state changes
            this.peerConnection.oniceconnectionstatechange = () => {
                console.log('ICE connection state:', this.peerConnection.iceConnectionState);
            };

            // Handle ICE gathering state changes
            this.peerConnection.onicegatheringstatechange = () => {
                console.log('ICE gathering state:', this.peerConnection.iceGatheringState);
            };

            // Handle ICE candidate events
            this.peerConnection.onicecandidate = (event) => {
                if (event.candidate) {
                    console.log('New ICE candidate:', event.candidate);
                }
            };

            // Handle connection state changes
            this.peerConnection.onconnectionstatechange = () => {
                const state = this.peerConnection.connectionState;
                console.log('Connection state changed:', state);
                this.callbacks.onConnectionStateChange(state);
                
                this.isConnected = state === 'connected';
            };            // Set remote description (D-ID's offer)
            console.log('Setting remote description:', streamData.offer);
            await this.peerConnection.setRemoteDescription(new RTCSessionDescription({
                type: 'offer',
                sdp: typeof streamData.offer === 'string' ? streamData.offer : streamData.offer.sdp
            }));

            // Create answer with specific constraints
            const answer = await this.peerConnection.createAnswer({
                offerToReceiveAudio: true,
                offerToReceiveVideo: true
            });

            console.log('Created answer:', answer);

            // Set local description
            await this.peerConnection.setLocalDescription(answer);
            console.log('Local description set successfully');

            // Wait for ICE gathering to complete
            await new Promise(resolve => {
                if (this.peerConnection.iceGatheringState === 'complete') {
                    resolve();
                } else {
                    this.peerConnection.onicegatheringstatechange = () => {
                        if (this.peerConnection.iceGatheringState === 'complete') {
                            resolve();
                        }
                    };
                }
            });

            // Return the final answer with gathered candidates
            const finalAnswer = this.peerConnection.localDescription;
            console.log('Successfully created WebRTC answer:', finalAnswer);
            return finalAnswer;

        } catch (error) {
            console.error('Connection failed:', error);
            this.callbacks.onError(error);
            throw error;
        }
    }    onSrcObjectReady(stream) {
        console.log('Setting video source...', stream);
        if (this.videoElement && stream) {
            try {
                // Stop any existing tracks
                if (this.videoElement.srcObject) {
                    const oldStream = this.videoElement.srcObject;
                    oldStream.getTracks().forEach(track => track.stop());
                }

                // Configure video element
                this.videoElement.autoplay = true;
                this.videoElement.playsInline = true;
                this.videoElement.muted = true; // Start muted to ensure autoplay works
                this.videoElement.controls = false;

                // Set stream and add event listeners
                this.videoElement.srcObject = stream;

                // Handle loadedmetadata event
                this.videoElement.onloadedmetadata = async () => {
                    console.log('Video metadata loaded');
                    try {
                        await this.videoElement.play();
                        console.log('Initial muted playback successful');
                        
                        // Try to unmute after a short delay
                        setTimeout(async () => {
                            try {
                                this.videoElement.muted = false;
                                await this.videoElement.play();
                                console.log('Successfully unmuted video');
                            } catch (e) {
                                console.warn('Could not unmute video:', e);
                                // Keep it muted if we can't unmute
                                this.videoElement.muted = true;
                            }
                        }, 500);
                    } catch (error) {
                        console.error('Error during initial play:', error);
                        // If initial play fails, keep trying with muted audio
                        this.videoElement.muted = true;
                        this.retryPlay(3);
                    }
                };

                // Handle play errors
                this.videoElement.onplay = () => console.log('Video play event fired');
                this.videoElement.onplaying = () => console.log('Video playing event fired');
                this.videoElement.onwaiting = () => console.log('Video waiting for data');
                this.videoElement.onerror = (e) => {
                    console.error('Video element error:', e);
                    this.callbacks.onError('Video playback error', e);
                };
            } catch (error) {
                console.error('Error in onSrcObjectReady:', error);
                this.callbacks.onError('Video setup failed', error);
            }
        } else {
            console.error('Missing video element or stream');
            this.callbacks.onError('Invalid video setup', { videoElement: !!this.videoElement, stream: !!stream });
        }
    }

    async retryPlay(attempts) {
        for (let i = 0; i < attempts; i++) {
            try {
                await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)));
                console.log(`Retry attempt ${i + 1} of ${attempts}`);
                await this.videoElement.play();
                console.log('Retry successful');
                return;
            } catch (error) {
                console.warn(`Retry ${i + 1} failed:`, error);
            }
        }
        this.callbacks.onError('All retry attempts failed');
    }

    async sendIceCandidate(candidate) {
        try {
            if (this.peerConnection && candidate) {
                await this.peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
            }
        } catch (error) {
            console.error('ICE candidate error:', error);
            this.callbacks.onError(error);
            throw error;
        }
    }

    disconnect() {
        console.log('Disconnecting D-ID stream...');
        
        if (this.videoElement?.srcObject) {
            const stream = this.videoElement.srcObject;
            stream.getTracks().forEach(track => {
                track.stop();
                console.log('Stopped track:', track.kind);
            });
            this.videoElement.srcObject = null;
        }

        if (this.peerConnection) {
            this.peerConnection.close();
            this.peerConnection = null;
        }

        this.isConnected = false;
        this.callbacks.onConnectionStateChange('disconnected');
    }
}

// Module-level instance
let instance = null;

// Exported functions for Blazor interop
export function create(videoElementId, options) {
    try {
        console.log('Creating DIDStreamManager...', { videoElementId, options });
        instance = new DIDStreamManager(videoElementId, options);
        return true;
    } catch (error) {
        console.error('Error creating DIDStreamManager:', error);
        return false;
    }
}

export function connect(streamData) {
    if (!instance) {
        throw new Error('DIDStreamManager not initialized. Call create() first.');
    }
    return instance.connect(streamData);
}

export function disconnect() {
    if (instance) {
        instance.disconnect();
        instance = null;
    }
    return true;
}

export function sendIceCandidate(candidate) {
    if (!instance) {
        throw new Error('DIDStreamManager not initialized. Call create() first.');
    }
    return instance.sendIceCandidate(candidate);
}
