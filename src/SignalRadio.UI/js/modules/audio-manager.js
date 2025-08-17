// Audio Management - Playback and Queue
export class AudioManager {
    constructor(app) {
        this.app = app;
        this.audioPlayer = document.getElementById('audio-player');
        this.audioQueue = [];
        this.isProcessingQueue = false;
        this.currentlyPlaying = null;
        this.userHasInteracted = false;
        this.audioContextReady = false;
        
        this.setupAudioEvents();
        this.setupUserInteractionDetection();
    }

    setupUserInteractionDetection() {
        const markUserInteraction = () => {
            if (!this.userHasInteracted) {
                console.log('User interaction detected - audio playback enabled');
                this.userHasInteracted = true;
                this.audioContextReady = true;
                
                // Remove listeners after first interaction
                document.removeEventListener('click', markUserInteraction);
                document.removeEventListener('keydown', markUserInteraction);
                document.removeEventListener('touchstart', markUserInteraction);
                
                this.app.uiManager.showToast('Audio playback ready', 'success');
            }
        };

        // Listen for any user interaction
        document.addEventListener('click', markUserInteraction);
        document.addEventListener('keydown', markUserInteraction);
        document.addEventListener('touchstart', markUserInteraction);
    }

    setupAudioEvents() {
        this.audioPlayer.addEventListener('ended', () => {
            this.onAudioEnded();
        });

        this.audioPlayer.addEventListener('timeupdate', () => {
            this.updateAudioProgress();
        });

        this.audioPlayer.addEventListener('loadedmetadata', () => {
            this.updateAudioDuration();
        });

        this.audioPlayer.addEventListener('error', (e) => {
            console.error('Audio playback error:', e);
            this.app.uiManager.showToast('Audio playback failed', 'error');
            this.processNextInQueue();
        });
    }

    async playCall(call) {
        this.queueCall(call);
    }

    queueCall(call) {
        if (!call.recordings || call.recordings.length === 0) {
            this.app.uiManager.showToast('No recordings available for this call', 'warning');
            return;
        }

        // Check if call is already in queue
        const alreadyQueued = this.audioQueue.some(queuedCall => queuedCall.id === call.id);
        if (alreadyQueued) {
            this.app.uiManager.showToast('Call is already queued for playback', 'info');
            return;
        }

        this.audioQueue.push(call);
        this.app.uiManager.updateQueueDisplay();
        
        // If not currently processing, start processing the queue
        if (!this.isProcessingQueue) {
            this.processQueue();
        } else {
            this.app.uiManager.showToast(`Call queued for playback (${this.audioQueue.length} in queue)`, 'info');
        }
    }

    async processQueue() {
        if (this.isProcessingQueue || this.audioQueue.length === 0) {
            return;
        }

        this.isProcessingQueue = true;

        while (this.audioQueue.length > 0) {
            const call = this.audioQueue.shift();
            this.app.uiManager.updateQueueDisplay();
            
            try {
                await this.playCallDirectly(call);
                
                // Wait for the audio to finish playing
                await new Promise((resolve) => {
                    const checkAudioEnded = () => {
                        if (this.audioPlayer.ended || this.audioPlayer.paused || this.currentlyPlaying !== call.id) {
                            resolve();
                        } else {
                            setTimeout(checkAudioEnded, 100);
                        }
                    };
                    checkAudioEnded();
                });
                
            } catch (error) {
                console.error('Error playing queued call:', error);
                this.app.uiManager.showToast(`Failed to play call from talk group ${call.talkgroupId}`, 'error');
            }
        }

        this.isProcessingQueue = false;
    }

    async playCallDirectly(call) {
        if (!call.recordings || call.recordings.length === 0) {
            this.app.uiManager.showToast('No recordings available for this call', 'warning');
            return;
        }

        // Find the best recording (prefer M4A, then WAV)
        let recording = call.recordings.find(r => r.format === 'M4A' && r.isUploaded);
        if (!recording) {
            recording = call.recordings.find(r => r.format === 'WAV' && r.isUploaded);
        }
        if (!recording) {
            this.app.uiManager.showToast('No playable recordings available', 'warning');
            return;
        }

        try {
            // Stop current playback
            this.stopCurrentPlayback();

            // Set up new playback
            this.currentlyPlaying = call.id;
            const audioUrl = `/api/recording/${recording.id}/download`;
            
            console.log('Setting audio source to:', audioUrl);
            
            this.setupAudioDebugEvents();
            
            this.audioPlayer.src = audioUrl;
            this.audioPlayer.load();
            
            // Show audio controls
            this.app.uiManager.showAudioControls(call.id);
            
            // Mark call as playing
            const callElement = document.querySelector(`[data-call-id="${call.id}"]`);
            if (callElement) {
                callElement.classList.add('playing');
            }

            // Wait for the audio to be ready before playing
            await new Promise((resolve, reject) => {
                const handleCanPlay = () => {
                    this.audioPlayer.removeEventListener('canplay', handleCanPlay);
                    this.audioPlayer.removeEventListener('error', handleError);
                    resolve();
                };
                
                const handleError = (e) => {
                    this.audioPlayer.removeEventListener('canplay', handleCanPlay);
                    this.audioPlayer.removeEventListener('error', handleError);
                    reject(e);
                };
                
                this.audioPlayer.addEventListener('canplay', handleCanPlay);
                this.audioPlayer.addEventListener('error', handleError);
                
                // If already ready, resolve immediately
                if (this.audioPlayer.readyState >= 3) {
                    handleCanPlay();
                }
            });

            await this.audioPlayer.play();
            this.app.uiManager.showToast(`Playing call from talk group ${call.talkgroupId}`, 'info');

        } catch (error) {
            console.error('Failed to play audio:', error);
            
            // Check if it's an auto-play policy error
            if (error.name === 'NotAllowedError') {
                this.app.uiManager.showToast('Auto-play blocked - click to enable audio', 'warning');
                // Don't mark as failed, just pause auto-play until user interacts
                this.userHasInteracted = false;
            } else {
                this.app.uiManager.showToast('Failed to play audio', 'error');
            }
            
            this.stopCurrentPlayback();
        }
    }

    setupAudioDebugEvents() {
        this.audioPlayer.onerror = (e) => {
            console.error('Audio error event:', e);
            console.error('Audio error details:', {
                error: this.audioPlayer.error,
                networkState: this.audioPlayer.networkState,
                readyState: this.audioPlayer.readyState,
                src: this.audioPlayer.src
            });
        };
        
        this.audioPlayer.oncanplaythrough = () => {
            console.log('Audio can play through');
        };
        
        this.audioPlayer.onloadstart = () => {
            console.log('Audio load start');
        };
        
        this.audioPlayer.onloadeddata = () => {
            console.log('Audio loaded data');
        };
    }

    stopCurrentPlayback() {
        if (this.currentlyPlaying) {
            this.audioPlayer.pause();
            this.audioPlayer.currentTime = 0;
            
            // Hide audio controls
            this.app.uiManager.hideAudioControls(this.currentlyPlaying);
            
            // Remove playing class
            const callElement = document.querySelector(`[data-call-id="${this.currentlyPlaying}"]`);
            if (callElement) {
                callElement.classList.remove('playing');
            }
            
            this.currentlyPlaying = null;
        }
    }

    toggleAudioPlayback() {
        if (this.audioPlayer.paused) {
            this.audioPlayer.play();
        } else {
            this.audioPlayer.pause();
        }
    }

    onAudioEnded() {
        this.stopCurrentPlayback();
        this.processNextInQueue();
    }

    processNextInQueue() {
        // This will be called when audio ends or encounters an error
        // The processQueue method will handle the next item automatically
        if (this.isProcessingQueue && this.audioQueue.length > 0) {
            // Queue processing will continue automatically
            return;
        }
        
        // If we're not processing queue but have items, restart processing
        if (!this.isProcessingQueue && this.audioQueue.length > 0) {
            this.processQueue();
        }
    }

    updateAudioProgress() {
        if (!this.currentlyPlaying) return;

        const progress = (this.audioPlayer.currentTime / this.audioPlayer.duration) * 100;
        const controlsElement = document.getElementById(`audio-controls-${this.currentlyPlaying}`);
        
        if (controlsElement) {
            const progressBar = controlsElement.querySelector('.audio-progress-bar');
            const timeDisplay = controlsElement.querySelector('.audio-time');
            
            if (progressBar) {
                progressBar.style.width = `${progress}%`;
            }
            
            if (timeDisplay) {
                timeDisplay.textContent = this.app.utils.formatAudioTime(this.audioPlayer.currentTime);
            }
        }
    }

    updateAudioDuration() {
        // This will be called when metadata loads
    }

    removeFromQueue(index) {
        if (index >= 0 && index < this.audioQueue.length) {
            const removedCall = this.audioQueue.splice(index, 1)[0];
            this.app.uiManager.updateQueueDisplay();
            this.app.uiManager.showToast(`Removed call from talk group ${removedCall.talkgroupId} from queue`, 'info');
        }
    }

    clearQueue() {
        if (this.audioQueue.length === 0) {
            this.app.uiManager.showToast('Queue is already empty', 'info');
            return;
        }
        
        if (confirm(`Clear all ${this.audioQueue.length} calls from the playback queue?`)) {
            this.audioQueue = [];
            this.app.uiManager.updateQueueDisplay();
            this.app.uiManager.showToast('Playback queue cleared', 'info');
        }
    }

    getQueueLength() {
        return this.audioQueue.length;
    }

    getQueue() {
        return [...this.audioQueue];
    }

    setVolume(volume) {
        this.audioPlayer.volume = volume / 100;
    }
}
