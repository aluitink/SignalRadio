// SignalRadio UI Application
class SignalRadioApp {
    constructor() {
        this.connection = null;
        this.subscriptions = new Set();
        this.activeCalls = new Map();
        this.totalCallsReceived = 0;
        this.audioPlayer = document.getElementById('audio-player');
        this.autoPlay = false;
        this.currentlyPlaying = null;
        this.audioQueue = [];
        this.isProcessingQueue = false;
        this.userHasInteracted = false;
        this.audioContextReady = false;
        
        this.initializeApp();
    }

    async initializeApp() {
        this.setupEventListeners();
        this.setupUserInteractionDetection();
        this.loadSettings();
        await this.initializeSignalR();
        this.loadRecentCalls();
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
                
                this.showToast('Audio playback ready', 'success');
            }
        };

        // Listen for any user interaction
        document.addEventListener('click', markUserInteraction);
        document.addEventListener('keydown', markUserInteraction);
        document.addEventListener('touchstart', markUserInteraction);
    }

    setupEventListeners() {
        // Auto-play toggle
        document.getElementById('auto-play-toggle').addEventListener('change', (e) => {
            this.autoPlay = e.target.checked;
            this.saveSettings();
            
            if (this.autoPlay) {
                if (this.userHasInteracted) {
                    this.showToast('Auto-play enabled', 'success');
                } else {
                    this.showToast('Auto-play enabled - click anywhere to activate', 'info');
                }
            } else {
                this.showToast('Auto-play disabled', 'info');
            }
        });

        // Volume control
        document.getElementById('volume-control').addEventListener('input', (e) => {
            this.audioPlayer.volume = e.target.value / 100;
            this.saveSettings();
        });

        // Clear subscriptions button
        document.getElementById('clear-subscriptions').addEventListener('click', () => {
            this.clearAllSubscriptions();
        });

        // Refresh calls
        document.getElementById('refresh-calls').addEventListener('click', () => {
            this.loadRecentCalls();
        });

        // Clear stream
        document.getElementById('clear-stream').addEventListener('click', () => {
            this.clearCallStream();
        });

        // Audio player events
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
            this.showToast('Audio playback failed', 'error');
            this.processNextInQueue();
        });
    }

    loadSettings() {
        const settings = JSON.parse(localStorage.getItem('signalradio-settings') || '{}');
        
        this.autoPlay = settings.autoPlay || false;
        document.getElementById('auto-play-toggle').checked = this.autoPlay;
        
        const volume = settings.volume || 50;
        document.getElementById('volume-control').value = volume;
        this.audioPlayer.volume = volume / 100;

        this.subscriptions = new Set(settings.subscriptions || []);
        this.updateSubscriptionsDisplay();
    }

    saveSettings() {
        const settings = {
            autoPlay: this.autoPlay,
            volume: document.getElementById('volume-control').value,
            subscriptions: Array.from(this.subscriptions)
        };
        localStorage.setItem('signalradio-settings', JSON.stringify(settings));
    }

    async initializeSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/talkgroup')
                .withAutomaticReconnect()
                .build();

            // Handle connection events
            this.connection.onclose(() => {
                this.updateConnectionStatus('disconnected');
            });

            this.connection.onreconnecting(() => {
                this.updateConnectionStatus('connecting');
                this.showToast('Reconnecting to server...', 'warning');
            });

            this.connection.onreconnected(() => {
                this.updateConnectionStatus('connected');
                this.showToast('Reconnected to server', 'success');
                this.resubscribeToTalkGroups();
            });

            // Handle SignalR events
            this.connection.on('NewCall', (callData) => {
                this.handleSubscribedCall(callData);
            });

            this.connection.on('AllCallsStreamUpdate', (callData) => {
                this.handleAllCallsStreamUpdate(callData);
            });

            this.connection.on('CallUpdated', (callData) => {
                this.handleCallUpdate(callData);
            });

            this.connection.on('SubscriptionConfirmed', (talkGroupId) => {
                this.subscriptions.add(talkGroupId);
                this.updateSubscriptionsDisplay();
                this.saveSettings();
                this.showToast(`Subscribed to talk group ${talkGroupId}`, 'success');
            });

            this.connection.on('UnsubscriptionConfirmed', (talkGroupId) => {
                this.subscriptions.delete(talkGroupId);
                this.updateSubscriptionsDisplay();
                this.saveSettings();
                this.showToast(`Unsubscribed from talk group ${talkGroupId}`, 'info');
            });

            this.connection.on('AllCallsStreamSubscribed', () => {
                this.showToast('Subscribed to all calls stream', 'info');
            });

            this.connection.on('AllCallsStreamUnsubscribed', () => {
                this.showToast('Unsubscribed from all calls stream', 'info');
            });

            // Start connection
            this.updateConnectionStatus('connecting');
            await this.connection.start();
            this.updateConnectionStatus('connected');
            this.showToast('Connected to SignalRadio', 'success');

            // Resubscribe to saved talk groups
            await this.resubscribeToTalkGroups();

            // Subscribe to all calls stream for general monitoring
            await this.connection.invoke('SubscribeToAllCallsStream');

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.updateConnectionStatus('disconnected');
            this.showToast('Failed to connect to server', 'error');
        }
    }

    async resubscribeToTalkGroups() {
        // Resubscribe to specific talk groups
        for (const talkGroupId of this.subscriptions) {
            try {
                await this.connection.invoke('SubscribeToTalkGroup', talkGroupId);
            } catch (error) {
                console.error(`Failed to resubscribe to ${talkGroupId}:`, error);
            }
        }

        // Resubscribe to all calls stream
        try {
            await this.connection.invoke('SubscribeToAllCallsStream');
        } catch (error) {
            console.error('Failed to resubscribe to all calls stream:', error);
        }
    }

    updateConnectionStatus(status) {
        const statusElement = document.getElementById('connection-status');
        const statusMap = {
            'connected': { class: 'bg-success', text: 'Connected', icon: 'circle-fill' },
            'connecting': { class: 'bg-warning', text: 'Connecting...', icon: 'circle-fill' },
            'disconnected': { class: 'bg-danger', text: 'Disconnected', icon: 'circle' }
        };

        const config = statusMap[status];
        statusElement.className = `badge ${config.class}`;
        statusElement.innerHTML = `<i class="bi bi-${config.icon} me-1"></i>${config.text}`;
    }

    async loadRecentCalls() {
        try {
            const response = await fetch('/api/calls?limit=50');
            if (!response.ok) throw new Error('Failed to load calls');
            
            const data = await response.json();
            this.displayCalls(data.calls || []);
            
        } catch (error) {
            console.error('Failed to load recent calls:', error);
            this.showToast('Failed to load recent calls', 'error');
        }
    }

    handleNewCall(callData) {
        this.totalCallsReceived++;
        this.activeCalls.set(callData.id, callData);
        this.addCallToStream(callData, true);
        this.updateStatistics();

        // Auto-play if subscribed and auto-play is enabled
        if (this.autoPlay && this.subscriptions.has(callData.talkgroupId)) {
            this.queueCall(callData);
        }
    }

    handleSubscribedCall(callData) {
        // This is for calls from talk groups we're specifically subscribed to
        this.totalCallsReceived++;
        this.activeCalls.set(callData.id, callData);
        this.addCallToStream(callData, true, true); // Mark as subscribed call
        this.updateStatistics();

        // Auto-play since this is a subscribed call (only if user has interacted and auto-play is enabled)
        if (this.autoPlay && this.userHasInteracted) {
            this.queueCall(callData);
        } else if (this.autoPlay && !this.userHasInteracted) {
            this.showToast('Click anywhere to enable auto-play', 'info');
        }
    }

    handleAllCallsStreamUpdate(callData) {
        // This is for general monitoring of all calls (not subscribed)
        // Only add to stream if we're not already subscribed to this talk group
        if (!this.subscriptions.has(callData.talkgroupId)) {
            this.totalCallsReceived++;
            this.activeCalls.set(callData.id, callData);
            this.addCallToStream(callData, true, false); // Mark as general stream call
            this.updateStatistics();
        }
        // If we are subscribed, we'll get it via handleSubscribedCall instead
    }

    handleCallUpdate(callData) {
        this.activeCalls.set(callData.id, callData);
        this.updateCallInStream(callData);
    }

    displayCalls(calls) {
        const streamContainer = document.getElementById('call-stream');
        streamContainer.innerHTML = '';

        calls.forEach(call => {
            this.activeCalls.set(call.id, call);
            this.addCallToStream(call, false);
        });

        this.updateStatistics();
    }

    addCallToStream(call, isNew = false, isSubscribedCall = null) {
        const streamContainer = document.getElementById('call-stream');
        const callElement = this.createCallElement(call, isNew, isSubscribedCall);
        
        if (isNew) {
            streamContainer.insertBefore(callElement, streamContainer.firstChild);
        } else {
            streamContainer.appendChild(callElement);
        }

        // Remove new-call class after animation
        if (isNew) {
            setTimeout(() => {
                callElement.classList.remove('new-call');
            }, 500);
        }
    }

    createCallElement(call, isNew = false, isSubscribedCall = null) {
        // Determine subscription status
        const isSubscribed = isSubscribedCall !== null ? isSubscribedCall : this.subscriptions.has(call.talkgroupId);
        const hasRecordings = call.recordings && call.recordings.length > 0;
        const duration = call.duration ? this.formatDuration(call.duration) : 'Unknown';
        
        const callElement = document.createElement('div');
        callElement.className = `call-item${isNew ? ' new-call' : ''}${isSubscribed ? ' subscribed' : ''}`;
        callElement.dataset.callId = call.id;
        callElement.dataset.talkgroupId = call.talkgroupId;

        callElement.innerHTML = `
            <div class="call-header">
                <div class="call-talkgroup">
                    <i class="bi bi-broadcast-pin me-2"></i>
                    Talk Group ${call.talkgroupId}
                </div>
                <div class="call-time">
                    ${this.formatDateTime(call.recordingTime)}
                </div>
            </div>
            
            <div class="call-details">
                <div class="call-detail-item">
                    <i class="bi bi-pc-display me-1"></i>
                    ${call.systemName}
                </div>
                <div class="call-detail-item">
                    <span class="frequency-display">${call.frequency}</span>
                </div>
                <div class="call-detail-item">
                    <i class="bi bi-clock me-1"></i>
                    <span class="duration-display">${duration}</span>
                </div>
                <div class="call-detail-item">
                    <span class="recording-indicator ${hasRecordings ? 'has-recordings' : ''}">
                        <i class="bi bi-file-earmark-music"></i>
                        ${call.recordingCount || 0} recordings
                    </span>
                </div>
            </div>

            <div class="call-actions">
                <button type="button" class="btn btn-outline-primary btn-subscribe btn-sm ${isSubscribed ? 'd-none' : ''}" 
                        onclick="app.toggleSubscription('${call.talkgroupId}', this)">
                    <i class="bi bi-bookmark-plus me-1"></i>
                    Subscribe
                </button>
                <button type="button" class="btn btn-outline-warning btn-unsubscribe btn-sm ${!isSubscribed ? 'd-none' : ''}" 
                        onclick="app.toggleSubscription('${call.talkgroupId}', this)">
                    <i class="bi bi-bookmark-dash me-1"></i>
                    Unsubscribe
                </button>
                ${hasRecordings ? `
                    <button type="button" class="btn btn-outline-success btn-play btn-sm" 
                            onclick="app.playCall(${JSON.stringify(call).replace(/"/g, '&quot;')})">
                        <i class="bi bi-play-fill me-1"></i>
                        Play
                    </button>
                ` : ''}
            </div>

            <div id="audio-controls-${call.id}" class="audio-controls d-none">
                <button type="button" class="btn btn-sm btn-outline-secondary" onclick="app.toggleAudioPlayback()">
                    <i class="bi bi-pause-fill"></i>
                </button>
                <div class="audio-progress">
                    <div class="audio-progress-bar" style="width: 0%"></div>
                </div>
                <div class="audio-time">0:00</div>
            </div>
        `;

        return callElement;
    }

    updateCallInStream(call) {
        const existingElement = document.querySelector(`[data-call-id="${call.id}"]`);
        if (existingElement) {
            const newElement = this.createCallElement(call);
            existingElement.replaceWith(newElement);
        }
    }

    async toggleSubscription(talkGroupId, buttonElement) {
        if (!this.connection) {
            this.showToast('Not connected to server', 'error');
            return;
        }

        try {
            const isCurrentlySubscribed = this.subscriptions.has(talkGroupId);
            
            if (isCurrentlySubscribed) {
                await this.connection.invoke('UnsubscribeFromTalkGroup', talkGroupId);
            } else {
                await this.connection.invoke('SubscribeToTalkGroup', talkGroupId);
            }

            // Update UI immediately (will be confirmed by SignalR event)
            this.updateCallSubscriptionUI(talkGroupId, !isCurrentlySubscribed);

        } catch (error) {
            console.error('Failed to toggle subscription:', error);
            this.showToast('Failed to update subscription', 'error');
        }
    }

    updateCallSubscriptionUI(talkGroupId, isSubscribed) {
        const callElements = document.querySelectorAll(`[data-talkgroup-id="${talkGroupId}"]`);
        
        callElements.forEach(element => {
            if (isSubscribed) {
                element.classList.add('subscribed');
                element.querySelector('.btn-subscribe')?.classList.add('d-none');
                element.querySelector('.btn-unsubscribe')?.classList.remove('d-none');
            } else {
                element.classList.remove('subscribed');
                element.querySelector('.btn-subscribe')?.classList.remove('d-none');
                element.querySelector('.btn-unsubscribe')?.classList.add('d-none');
            }
        });
    }

    async playCall(call) {
        // Add to queue and process
        this.queueCall(call);
    }

    queueCall(call) {
        if (!call.recordings || call.recordings.length === 0) {
            this.showToast('No recordings available for this call', 'warning');
            return;
        }

        // Check if call is already in queue
        const alreadyQueued = this.audioQueue.some(queuedCall => queuedCall.id === call.id);
        if (alreadyQueued) {
            this.showToast('Call is already queued for playback', 'info');
            return;
        }

        this.audioQueue.push(call);
        this.updateQueueDisplay();
        
        // If not currently processing, start processing the queue
        if (!this.isProcessingQueue) {
            this.processQueue();
        } else {
            this.showToast(`Call queued for playback (${this.audioQueue.length} in queue)`, 'info');
        }
    }

    async processQueue() {
        if (this.isProcessingQueue || this.audioQueue.length === 0) {
            return;
        }

        this.isProcessingQueue = true;

        while (this.audioQueue.length > 0) {
            const call = this.audioQueue.shift();
            this.updateQueueDisplay();
            
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
                this.showToast(`Failed to play call from talk group ${call.talkgroupId}`, 'error');
            }
        }

        this.isProcessingQueue = false;
    }

    async playCallDirectly(call) {
        if (!call.recordings || call.recordings.length === 0) {
            this.showToast('No recordings available for this call', 'warning');
            return;
        }

        // Find the best recording (prefer M4A, then WAV)
        let recording = call.recordings.find(r => r.format === 'M4A' && r.isUploaded);
        if (!recording) {
            recording = call.recordings.find(r => r.format === 'WAV' && r.isUploaded);
        }
        if (!recording) {
            this.showToast('No playable recordings available', 'warning');
            return;
        }

        try {
            // Stop current playback
            this.stopCurrentPlayback();

            // Set up new playback
            this.currentlyPlaying = call.id;
            const audioUrl = `/api/recording/${recording.id}/download`;
            
            console.log('Setting audio source to:', audioUrl);
            
            // Add audio event listeners for debugging
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
            
            this.audioPlayer.src = audioUrl;
            this.audioPlayer.load();
            
            // Show audio controls
            this.showAudioControls(call.id);
            
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
            this.showToast(`Playing call from talk group ${call.talkgroupId}`, 'info');

        } catch (error) {
            console.error('Failed to play audio:', error);
            
            // Check if it's an auto-play policy error
            if (error.name === 'NotAllowedError') {
                this.showToast('Auto-play blocked - click to enable audio', 'warning');
                // Don't mark as failed, just pause auto-play until user interacts
                this.userHasInteracted = false;
            } else {
                this.showToast('Failed to play audio', 'error');
            }
            
            this.stopCurrentPlayback();
        }
    }

    stopCurrentPlayback() {
        if (this.currentlyPlaying) {
            this.audioPlayer.pause();
            this.audioPlayer.currentTime = 0;
            
            // Hide audio controls
            this.hideAudioControls(this.currentlyPlaying);
            
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

    showAudioControls(callId) {
        const controlsElement = document.getElementById(`audio-controls-${callId}`);
        if (controlsElement) {
            controlsElement.classList.remove('d-none');
        }
    }

    hideAudioControls(callId) {
        const controlsElement = document.getElementById(`audio-controls-${callId}`);
        if (controlsElement) {
            controlsElement.classList.add('d-none');
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
                timeDisplay.textContent = this.formatAudioTime(this.audioPlayer.currentTime);
            }
        }
    }

    updateAudioDuration() {
        // This will be called when metadata loads
    }

    clearAllSubscriptions() {
        if (confirm('Are you sure you want to clear all subscriptions?')) {
            this.subscriptions.forEach(talkGroupId => {
                if (this.connection) {
                    this.connection.invoke('UnsubscribeFromTalkGroup', talkGroupId).catch(console.error);
                }
            });
            
            this.subscriptions.clear();
            this.updateSubscriptionsDisplay();
            this.saveSettings();
            
            // Update all call UIs
            document.querySelectorAll('.call-item').forEach(element => {
                element.classList.remove('subscribed');
                element.querySelector('.btn-subscribe')?.classList.remove('d-none');
                element.querySelector('.btn-unsubscribe')?.classList.add('d-none');
            });
            
            this.showToast('All subscriptions cleared', 'info');
        }
    }

    clearCallStream() {
        if (confirm('Clear the call stream?')) {
            document.getElementById('call-stream').innerHTML = '';
            this.activeCalls.clear();
            this.updateStatistics();
            this.showToast('Call stream cleared', 'info');
        }
    }

    updateSubscriptionsDisplay() {
        const container = document.getElementById('subscribed-list');
        
        if (this.subscriptions.size === 0) {
            container.innerHTML = '<div class="text-muted small">No subscriptions</div>';
        } else {
            container.innerHTML = Array.from(this.subscriptions).map(talkGroupId => `
                <div class="subscribed-item">
                    <span class="subscribed-talkgroup">Talk Group ${talkGroupId}</span>
                    <button type="button" class="btn btn-outline-danger btn-unsubscribe btn-sm" 
                            onclick="app.toggleSubscription('${talkGroupId}', this)">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            `).join('');
        }

        document.getElementById('subscriptions-count').textContent = this.subscriptions.size;
    }

    updateStatistics() {
        document.getElementById('active-calls-count').textContent = this.activeCalls.size;
        document.getElementById('total-calls-count').textContent = this.totalCallsReceived;
        this.updateQueueDisplay();
    }

    updateQueueDisplay() {
        const queueCountElement = document.getElementById('queue-count');
        if (queueCountElement) {
            queueCountElement.textContent = this.audioQueue.length;
        }
        
        const queueListElement = document.getElementById('queue-list');
        if (queueListElement) {
            if (this.audioQueue.length === 0) {
                queueListElement.innerHTML = '<div class="text-muted small">No calls queued</div>';
            } else {
                queueListElement.innerHTML = this.audioQueue.map((call, index) => `
                    <div class="queue-item">
                        <span class="queue-position">${index + 1}.</span>
                        <span class="queue-talkgroup">TG ${call.talkgroupId}</span>
                        <span class="queue-time">${this.formatDateTime(call.recordingTime)}</span>
                        <button type="button" class="btn btn-outline-danger btn-sm" 
                                onclick="app.removeFromQueue(${index})">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                `).join('');
            }
        }
    }

    removeFromQueue(index) {
        if (index >= 0 && index < this.audioQueue.length) {
            const removedCall = this.audioQueue.splice(index, 1)[0];
            this.updateQueueDisplay();
            this.showToast(`Removed call from talk group ${removedCall.talkgroupId} from queue`, 'info');
        }
    }

    clearQueue() {
        if (this.audioQueue.length === 0) {
            this.showToast('Queue is already empty', 'info');
            return;
        }
        
        if (confirm(`Clear all ${this.audioQueue.length} calls from the playback queue?`)) {
            this.audioQueue = [];
            this.updateQueueDisplay();
            this.showToast('Playback queue cleared', 'info');
        }
    }

    formatDateTime(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString();
    }

    formatDuration(duration) {
        // Duration comes as "HH:MM:SS" or TimeSpan format
        if (typeof duration === 'string') {
            return duration.split('.')[0]; // Remove milliseconds if present
        }
        return duration;
    }

    formatAudioTime(seconds) {
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = Math.floor(seconds % 60);
        return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
    }

    showToast(message, type = 'info') {
        const toastContainer = document.getElementById('toast-container');
        const toastId = 'toast-' + Date.now();
        
        const typeMap = {
            'success': { class: 'text-bg-success', icon: 'check-circle-fill' },
            'error': { class: 'text-bg-danger', icon: 'exclamation-triangle-fill' },
            'warning': { class: 'text-bg-warning', icon: 'exclamation-triangle' },
            'info': { class: 'text-bg-info', icon: 'info-circle-fill' }
        };

        const config = typeMap[type] || typeMap.info;

        const toastHtml = `
            <div id="${toastId}" class="toast ${config.class}" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-body">
                    <i class="bi bi-${config.icon} me-2"></i>
                    ${message}
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: type === 'error' ? 5000 : 3000
        });
        
        toast.show();
        
        // Remove toast element after it's hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }
}

// Initialize the application when the page loads
let app;
document.addEventListener('DOMContentLoaded', () => {
    app = new SignalRadioApp();
    // Make sure the app is available globally for onclick handlers
    window.app = app;
});
