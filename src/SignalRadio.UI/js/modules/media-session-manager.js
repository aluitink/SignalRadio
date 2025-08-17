// Media Session Manager - Handles background playback and media controls
export class MediaSessionManager {
    constructor(app) {
        this.app = app;
        this.wakeLock = null;
        this.isInitialized = false;
        
        this.init();
    }

    async init() {
        if (!this.isInitialized) {
            this.setupMediaSession();
            this.setupWakeLock();
            this.isInitialized = true;
            console.log('Media Session Manager initialized');
        }
    }

    setupMediaSession() {
        if ('mediaSession' in navigator) {
            console.log('Media Session API available');
            
            // Set up action handlers
            navigator.mediaSession.setActionHandler('play', () => {
                this.handlePlay();
            });
            
            navigator.mediaSession.setActionHandler('pause', () => {
                this.handlePause();
            });
            
            navigator.mediaSession.setActionHandler('stop', () => {
                this.handleStop();
            });
            
            navigator.mediaSession.setActionHandler('previoustrack', () => {
                this.handlePrevious();
            });
            
            navigator.mediaSession.setActionHandler('nexttrack', () => {
                this.handleNext();
            });
            
            navigator.mediaSession.setActionHandler('seekto', (details) => {
                this.handleSeekTo(details);
            });
            
            // Set initial playback state
            navigator.mediaSession.playbackState = 'none';
        } else {
            console.log('Media Session API not available');
        }
    }

    updateMediaMetadata(call) {
        if ('mediaSession' in navigator && call) {
            const talkGroup = this.app.dataManager?.talkGroups?.find(tg => tg.dec === call.talkgroupId);
            const talkGroupName = talkGroup ? talkGroup.description || `Talk Group ${call.talkgroupId}` : `Talk Group ${call.talkgroupId}`;
            
            navigator.mediaSession.metadata = new MediaMetadata({
                title: talkGroupName,
                artist: 'SignalRadio',
                album: `Call from ${this.formatTimestamp(call.startTime)}`,
                artwork: [
                    {
                        src: '/favicon.ico',
                        sizes: '256x256',
                        type: 'image/x-icon'
                    }
                ]
            });
            
            console.log('Updated media metadata for:', talkGroupName);
        }
    }

    updatePlaybackState(state) {
        if ('mediaSession' in navigator) {
            navigator.mediaSession.playbackState = state;
            console.log('Media session playback state:', state);
        }
    }

    updatePositionInfo(duration, currentTime, playbackRate = 1.0) {
        if ('mediaSession' in navigator && 'setPositionState' in navigator.mediaSession) {
            try {
                navigator.mediaSession.setPositionState({
                    duration: duration || 0,
                    playbackRate: playbackRate,
                    position: currentTime || 0
                });
            } catch (error) {
                console.warn('Failed to update position state:', error);
            }
        }
    }

    // Action handlers
    handlePlay() {
        console.log('Media session: Play requested');
        if (this.app.audioManager.audioPlayer.paused) {
            this.app.audioManager.audioPlayer.play().catch(error => {
                console.error('Failed to play from media session:', error);
            });
        }
    }

    handlePause() {
        console.log('Media session: Pause requested');
        if (!this.app.audioManager.audioPlayer.paused) {
            this.app.audioManager.audioPlayer.pause();
        }
    }

    handleStop() {
        console.log('Media session: Stop requested');
        this.app.audioManager.stopCurrentPlayback();
        this.app.audioManager.clearQueue();
    }

    handlePrevious() {
        console.log('Media session: Previous track requested');
        // Could implement previous call functionality if needed
        this.app.uiManager.showToast('Previous call not available', 'info');
    }

    handleNext() {
        console.log('Media session: Next track requested');
        if (this.app.audioManager.audioQueue.length > 0) {
            this.app.audioManager.stopCurrentPlayback();
            this.app.audioManager.processNextInQueue();
        } else {
            this.app.uiManager.showToast('No calls in queue', 'info');
        }
    }

    handleSeekTo(details) {
        if (details.seekTime && this.app.audioManager.audioPlayer) {
            console.log('Media session: Seek to', details.seekTime);
            this.app.audioManager.audioPlayer.currentTime = details.seekTime;
        }
    }

    // Wake Lock functionality
    async setupWakeLock() {
        if ('wakeLock' in navigator) {
            console.log('Wake Lock API available');
        } else {
            console.log('Wake Lock API not available');
        }
    }

    async requestWakeLock() {
        if ('wakeLock' in navigator && !this.wakeLock) {
            try {
                this.wakeLock = await navigator.wakeLock.request('screen');
                console.log('Screen wake lock acquired');
                
                this.wakeLock.addEventListener('release', () => {
                    console.log('Screen wake lock released');
                    this.wakeLock = null;
                });
                
                return true;
            } catch (error) {
                console.error('Failed to acquire wake lock:', error);
                return false;
            }
        }
        return false;
    }

    async releaseWakeLock() {
        if (this.wakeLock) {
            try {
                await this.wakeLock.release();
                this.wakeLock = null;
                console.log('Wake lock released manually');
            } catch (error) {
                console.error('Failed to release wake lock:', error);
            }
        }
    }

    // Helper methods
    formatTimestamp(timestamp) {
        try {
            const date = new Date(timestamp);
            return date.toLocaleTimeString();
        } catch (error) {
            return 'Unknown time';
        }
    }

    // Lifecycle methods
    onCallStarted(call) {
        this.updateMediaMetadata(call);
        this.updatePlaybackState('playing');
        this.requestWakeLock();
    }

    onCallPaused() {
        this.updatePlaybackState('paused');
    }

    onCallStopped() {
        this.updatePlaybackState('none');
        this.releaseWakeLock();
    }

    onCallEnded() {
        this.updatePlaybackState('none');
        // Don't release wake lock if there are more calls in queue
        if (this.app.audioManager.audioQueue.length === 0) {
            this.releaseWakeLock();
        }
    }

    onTimeUpdate(currentTime, duration) {
        this.updatePositionInfo(duration, currentTime);
    }

    // Settings management
    getSettings() {
        return {
            wakeLockEnabled: localStorage.getItem('signalradio-wake-lock') !== 'false',
            backgroundPlayback: localStorage.getItem('signalradio-background-playback') !== 'false'
        };
    }

    setSetting(key, value) {
        localStorage.setItem(`signalradio-${key}`, value.toString());
    }
}
