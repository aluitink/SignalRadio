// Import modules
import { ConnectionManager } from './modules/connection-manager.js';
import { AudioManager } from './modules/audio-manager.js';
import { DataManager } from './modules/data-manager.js';
import { SettingsManager } from './modules/settings-manager.js';
import { UIManager } from './modules/ui-manager.js';
import { MediaSessionManager } from './modules/media-session-manager.js';
import { Utils } from './modules/utils.js';

// SignalRadio UI Application - Refactored
class SignalRadioApp {
    constructor() {
        // Initialize core properties
        this.subscriptions = new Set();
        this.activeCalls = new Map();
        this.totalCallsReceived = 0;
        this.autoPlay = false;
        
        // Initialize managers
        this.utils = new Utils();
        this.settingsManager = new SettingsManager(this);
        this.dataManager = new DataManager(this);
        this.audioManager = new AudioManager(this);
        this.mediaSessionManager = new MediaSessionManager(this);
        this.uiManager = new UIManager(this);
        this.connectionManager = new ConnectionManager(this);
        
        this.initializeApp();
    }

    async initializeApp() {
        try {
            console.log('Initializing SignalRadio App...');
            
            // Register service worker for background playbook
            await this.registerServiceWorker();
            
            this.uiManager.setupEventListeners();
            this.settingsManager.loadSettings();
            
            console.log('Loading talk group data...');
            await this.dataManager.loadTalkGroupData();
            
            console.log('Initializing SignalR connection...');
            await this.connectionManager.initializeSignalR();
            
            // Handle browser navigation
            this.setupBrowserNavigation();
            
            // Check if we should load a specific talkgroup view
            const urlParams = new URLSearchParams(window.location.search);
            const talkgroupId = urlParams.get('talkgroup');
            const callIdParam = urlParams.get('call');
            
            if (talkgroupId) {
                console.log(`Loading talkgroup view for ${talkgroupId}...`);
                this.loadTalkgroupView(talkgroupId);
            } else if (callIdParam) {
                console.log(`Show call view for URL call: ${callIdParam}`);
                // Fetch call details and render a focused call page that requires the user to click Play
                (async () => {
                    try {
                        const resp = await fetch(`/api/calls/${encodeURIComponent(callIdParam)}`);
                        if (!resp.ok) {
                            console.warn('Failed to fetch call for view:', resp.status);
                            this.dataManager.loadRecentCalls();
                            return;
                        }
                        const payload = await resp.json();

                        const normalized = {
                            id: payload.id || payload.Id,
                            talkgroupId: payload.talkgroupId || payload.TalkgroupId || payload.TalkgroupID || payload.TalkGroupId,
                            systemName: payload.systemName || payload.SystemName,
                            recordingTime: payload.recordingTime || payload.RecordingTime,
                            frequency: payload.frequency || payload.Frequency,
                            duration: payload.duration || payload.Duration,
                            createdAt: payload.createdAt || payload.CreatedAt,
                            updatedAt: payload.updatedAt || payload.UpdatedAt,
                            recordings: (payload.recordings || payload.Recordings || []).map(r => ({
                                id: r.id || r.Id,
                                fileName: r.fileName || r.FileName || r.File,
                                format: (r.format || r.Format || '').toUpperCase(),
                                fileSize: r.fileSize || r.FileSize || 0,
                                isUploaded: r.isUploaded || r.IsUploaded || false,
                                blobName: r.blobName || r.BlobName || r.blobUri || r.BlobUri || null,
                                uploadedAt: r.uploadedAt || r.UploadedAt || r.createdAt || r.CreatedAt || null,
                                hasTranscription: r.hasTranscription || r.HasTranscription || false,
                                transcriptionText: r.transcriptionText || r.TranscriptionText || null,
                                transcriptionConfidence: r.transcriptionConfidence || r.TranscriptionConfidence || null,
                                transcriptionLanguage: r.transcriptionLanguage || r.TranscriptionLanguage || null
                            }))
                        };

                        // Render the single-call view; playback will start only when user clicks Play
                        this.uiManager.showCallView(normalized);
                    } catch (err) {
                        console.error('Failed to load call for view', err);
                        this.dataManager.loadRecentCalls();
                    }
                })();
            } else {
                console.log('Loading recent calls...');
                this.dataManager.loadRecentCalls();
            }
            
            console.log('Starting age update timer...');
            this.uiManager.startAgeUpdateTimer();
            
            console.log('App initialization complete');
        } catch (error) {
            console.error('Failed to initialize app:', error);
            this.uiManager.showToast(`App initialization failed: ${error.message}`, 'error');
        }
    }

    setupBrowserNavigation() {
        // Handle browser back/forward buttons
        window.addEventListener('popstate', (event) => {
            const urlParams = new URLSearchParams(window.location.search);
            const talkgroupId = urlParams.get('talkgroup');
            
            if (talkgroupId) {
                this.loadTalkgroupView(talkgroupId);
            } else {
                this.uiManager.showMainView();
            }
        });
    }

    async registerServiceWorker() {
        console.log('🔧 Starting service worker registration process...');
        console.log('🌐 Browser:', navigator.userAgent);
        console.log('🔍 Checking service worker support...');
        console.log('🔍 navigator.serviceWorker exists:', 'serviceWorker' in navigator);
        console.log('🔍 window.location.protocol:', window.location.protocol);
        console.log('🔍 window.location.hostname:', window.location.hostname);
        console.log('🔍 Is secure context:', window.isSecureContext);
        
        if ('serviceWorker' in navigator) {
            try {
                console.log('✅ Service worker API is supported');
                console.log('🔄 Registering service worker at /service-worker.js...');
                
                const registration = await navigator.serviceWorker.register('/service-worker.js', {
                    scope: '/'
                });
                
                console.log('✅ Service worker registered successfully:', registration);
                console.log('📍 Registration scope:', registration.scope);
                console.log('🔧 Registration state:', registration.installing ? 'installing' : 
                           registration.waiting ? 'waiting' : 
                           registration.active ? 'active' : 'unknown');
                
                // Check if service worker is already active
                if (registration.active) {
                    console.log('✅ Service worker is already active');
                } else if (registration.installing) {
                    console.log('⏳ Service worker is installing...');
                } else if (registration.waiting) {
                    console.log('⏸️ Service worker is waiting...');
                }
                
                // Handle service worker updates
                // If there's already a waiting worker (from a previous navigation), prompt immediately
                if (registration.waiting) {
                    console.log('🔔 Service worker update already waiting');
                    if (confirm('An update is available. Refresh now to apply the update?')) {
                        registration.waiting.postMessage({ type: 'SKIP_WAITING' });
                        // Listen for controllerchange to reload when the new SW takes control
                        navigator.serviceWorker.addEventListener('controllerchange', () => {
                            window.location.reload();
                        });
                    }
                }

                // Handle future updates
                registration.addEventListener('updatefound', () => {
                    console.log('🔄 Service worker update found');
                    const newWorker = registration.installing;

                    if (newWorker) {
                        newWorker.addEventListener('statechange', () => {
                            console.log('🔄 Service worker state changed to:', newWorker.state);
                            if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                                // New service worker is available
                                if (confirm('App update available. Refresh to apply?')) {
                                    newWorker.postMessage({ type: 'SKIP_WAITING' });
                                    navigator.serviceWorker.addEventListener('controllerchange', () => {
                                        window.location.reload();
                                    });
                                }
                            }
                        });
                    }
                });
                
                // Listen for messages from service worker
                navigator.serviceWorker.addEventListener('message', (event) => {
                    const { type, data } = event.data;
                    console.log('📨 Message from service worker:', type, data);
                    
                    switch (type) {
                        case 'BACKGROUND_SYNC_COMPLETE':
                            console.log('🔄 Background sync completed:', data);
                            break;
                    }
                });
                
                // Additional debugging
                navigator.serviceWorker.addEventListener('controllerchange', () => {
                    console.log('🔄 Service worker controller changed');
                });
                
            } catch (error) {
                console.error('❌ Service worker registration failed:', error);
                console.error('Error details:', error.message, error.stack);
            }
        } else {
            console.log('❌ Service workers not supported in this browser');
        }
    }

    // Call handling methods
    handleNewCall(callData) {
        this.totalCallsReceived++;
        this.activeCalls.set(callData.id, callData);
        this.uiManager.addCallToStream(callData, true);
        this.uiManager.updateStatistics();

        // Auto-play if subscribed and auto-play is enabled
        if (this.autoPlay && this.subscriptions.has(callData.talkgroupId)) {
            this.audioManager.queueCall(callData);
        }

        // Update transcription display if available
        this.uiManager.updateTranscriptionDisplay(callData);

        // Update transcription display if available
        this.uiManager.updateTranscriptionDisplay(callData);
    }

    handleSubscribedCall(callData) {
        // This is for calls from talk groups we're specifically subscribed to
        console.log('[SignalRadio UI] Received subscribed call:', {
            callId: callData.id,
            talkgroupId: callData.talkgroupId,
            recordingTime: callData.recordingTime,
            recordingCount: callData.recordingCount || 0
        });
        
        this.totalCallsReceived++;
        this.activeCalls.set(callData.id, callData);
        this.uiManager.addCallToStream(callData, true, true); // Mark as subscribed call
        this.uiManager.updateStatistics();

        // Auto-play since this is a subscribed call (only if user has interacted and auto-play is enabled)
        if (this.autoPlay && this.audioManager.userHasInteracted) {
            console.log(`[SignalRadio UI] Auto-playing subscribed call ${callData.id}`);
            this.audioManager.queueCall(callData);
        } else if (this.autoPlay && !this.audioManager.userHasInteracted) {
            // Don't show notification for this case
        }

        // Update transcription display if available
        this.uiManager.updateTranscriptionDisplay(callData);

        // Update transcription display if available
        this.uiManager.updateTranscriptionDisplay(callData);
    }

    handleAllCallsStreamUpdate(callData) {
        // This is for general monitoring of all calls (not subscribed)
        console.log('[SignalRadio UI] Received call stream update:', {
            callId: callData.id,
            talkgroupId: callData.talkgroupId,
            recordingTime: callData.recordingTime,
            recordingCount: callData.recordingCount || 0,
            isSubscribed: this.subscriptions.has(callData.talkgroupId)
        });
        
        // Only add to stream if we're not already subscribed to this talk group
        if (!this.subscriptions.has(callData.talkgroupId)) {
            console.log(`[SignalRadio UI] Adding call ${callData.id} to general stream (not subscribed to TalkGroup ${callData.talkgroupId})`);
            this.totalCallsReceived++;
            this.activeCalls.set(callData.id, callData);
            this.uiManager.addCallToStream(callData, true, false); // Mark as general stream call
            this.uiManager.updateStatistics();
        } else {
            console.log(`[SignalRadio UI] Skipping call ${callData.id} - already subscribed to TalkGroup ${callData.talkgroupId}`);
        }
        // If we are subscribed, we'll get it via handleSubscribedCall instead
    }

    handleCallUpdate(callData) {
        console.log('[SignalRadio UI] Received call update:', {
            callId: callData.id,
            talkgroupId: callData.talkgroupId,
            recordingTime: callData.recordingTime,
            recordingCount: callData.recordingCount || 0
        });
        
        this.activeCalls.set(callData.id, callData);
        this.uiManager.updateCallInStream(callData);
    }

    displayCalls(calls) {
        const streamContainer = document.getElementById('call-stream');
        const emptyState = document.getElementById('empty-state');
        
        streamContainer.innerHTML = '';

        if (calls && calls.length > 0) {
            calls.forEach(call => {
                this.activeCalls.set(call.id, call);
                this.uiManager.addCallToStream(call, false);
            });
            
            // Hide empty state when we have calls
            if (emptyState) {
                emptyState.style.display = 'none';
            }
        } else {
            // Show empty state when no calls
            if (emptyState) {
                emptyState.style.display = 'block';
            }
        }

        this.uiManager.updateStatistics();
    }

    // Subscription management
    async toggleSubscription(talkGroupId, buttonElement) {
        if (!this.connectionManager.isConnected()) {
            this.uiManager.showToast('Not connected to server', 'error');
            return;
        }

        try {
            const isCurrentlySubscribed = this.subscriptions.has(talkGroupId);
            
            if (isCurrentlySubscribed) {
                await this.connectionManager.unsubscribeFromTalkGroup(talkGroupId);
            } else {
                await this.connectionManager.subscribeToTalkGroup(talkGroupId);
            }

            // Update UI immediately (will be confirmed by SignalR event)
            this.uiManager.updateCallSubscriptionUI(talkGroupId, !isCurrentlySubscribed);

        } catch (error) {
            console.error('Failed to toggle subscription:', error);
            this.uiManager.showToast('Failed to update subscription', 'error');
        }
    }

    clearAllSubscriptions() {
        if (confirm('Are you sure you want to clear all subscriptions?')) {
            this.subscriptions.forEach(talkGroupId => {
                if (this.connectionManager.connection) {
                    this.connectionManager.unsubscribeFromTalkGroup(talkGroupId).catch(console.error);
                }
            });
            
            this.subscriptions.clear();
            this.uiManager.updateSubscriptionsDisplay();
            this.settingsManager.saveSettings();
            
            // Update all call UIs
            document.querySelectorAll('.call-item').forEach(element => {
                element.classList.remove('subscribed');
                element.querySelector('.btn-subscribe')?.classList.remove('d-none');
                element.querySelector('.btn-unsubscribe')?.classList.add('d-none');
            });
            
            this.uiManager.showToast('All subscriptions cleared', 'success');
        }
    }

    clearCallStream() {
        if (confirm('Clear the call stream?')) {
            this.uiManager.clearCallStream();
            this.activeCalls.clear();
            this.uiManager.updateStatistics();
        }
    }

    // Audio playback methods (delegated to AudioManager)
    async playCall(call) {
        await this.audioManager.playCall(call);
    }

    toggleAudioPlayback() {
        this.audioManager.toggleAudioPlayback();
    }

    removeFromQueue(index) {
        this.audioManager.removeFromQueue(index);
    }

    clearQueue() {
        this.audioManager.clearQueue();
    }

    // Navigation methods
    viewTalkgroupStream(talkgroupId) {
        // Navigate to talkgroup-specific view
        const talkGroupInfo = this.dataManager.getTalkGroupInfo(talkgroupId);
        const talkGroupName = talkGroupInfo?.description || `Talk Group ${talkgroupId}`;
        
        // Create URL with talkgroup parameter
        const url = new URL(window.location);
        url.searchParams.set('talkgroup', talkgroupId);
        
        // Update browser history
        window.history.pushState({ talkgroupId }, `${talkGroupName} - SignalRadio`, url);
        
        // Load talkgroup-specific calls
        this.loadTalkgroupView(talkgroupId);
    }

    async loadTalkgroupView(talkgroupId) {
        try {
            // Update UI to show we're loading
            this.uiManager.showTalkgroupView(talkgroupId, true);

            // Fetch calls for this talkgroup (limit to last 150)
            const limit = 150;
            const response = await fetch(`/api/calls/talkgroup/${talkgroupId}?limit=${limit}`);
            if (!response.ok) {
                if (response.status === 404) {
                    // No calls found for this talkgroup
                    this.uiManager.showTalkgroupView(talkgroupId, false, []);
                    return;
                }
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const data = await response.json();
            const calls = data.calls || [];
            console.log(`Loaded ${calls.length} calls (summary) for talkgroup ${talkgroupId}`);

            // Fetch full call details (including recordings/transcriptions) for each call.
            // Do this in parallel batches to avoid overwhelming the server.
            const fetchCallDetail = async (summaryCall) => {
                try {
                    const id = summaryCall.id || summaryCall.Id;
                    if (!id) return summaryCall;
                    const resp = await fetch(`/api/calls/${id}`);
                    if (!resp.ok) return summaryCall; // fallback to summary
                    const payload = await resp.json();

                    return {
                        id: payload.id || payload.Id,
                        talkgroupId: payload.talkgroupId || payload.TalkgroupId || payload.TalkgroupID || payload.TalkGroupId,
                        systemName: payload.systemName || payload.SystemName,
                        recordingTime: payload.recordingTime || payload.RecordingTime,
                        frequency: payload.frequency || payload.Frequency,
                        duration: payload.duration || payload.Duration,
                        createdAt: payload.createdAt || payload.CreatedAt,
                        updatedAt: payload.updatedAt || payload.UpdatedAt,
                        recordings: (payload.recordings || payload.Recordings || []).map(r => ({
                            id: r.id || r.Id,
                            fileName: r.fileName || r.FileName || r.File,
                            format: (r.format || r.Format || '').toUpperCase(),
                            fileSize: r.fileSize || r.FileSize || 0,
                            isUploaded: r.isUploaded || r.IsUploaded || false,
                            blobName: r.blobName || r.BlobName || r.blobUri || r.BlobUri || null,
                            uploadedAt: r.uploadedAt || r.UploadedAt || r.createdAt || r.CreatedAt || null,
                            hasTranscription: r.hasTranscription || r.HasTranscription || false,
                            transcriptionText: r.transcriptionText || r.TranscriptionText || null,
                            transcriptionConfidence: r.transcriptionConfidence || r.TranscriptionConfidence || null,
                            transcriptionLanguage: r.transcriptionLanguage || r.TranscriptionLanguage || null
                        }))
                    };
                } catch (e) {
                    console.warn('Failed to fetch call details for', summaryCall, e);
                    return summaryCall;
                }
            };

            const concurrency = 10;
            const detailedCalls = [];
            for (let i = 0; i < calls.length; i += concurrency) {
                const chunk = calls.slice(i, i + concurrency);
                const results = await Promise.all(chunk.map(fetchCallDetail));
                detailedCalls.push(...results);
            }

            // Update UI with full call objects so transcriptions (if present) render in the talkgroup view
            this.uiManager.showTalkgroupView(talkgroupId, false, detailedCalls);

        } catch (error) {
            console.error('Failed to load talkgroup calls:', error);
            this.uiManager.showTalkgroupView(talkgroupId, false, []); // Show empty state
            this.uiManager.showToast(`Failed to load calls for talk group ${talkgroupId}: ${error.message}`, 'error');
        }
    }

    returnToMainStream() {
        // Clear talkgroup parameter
        const url = new URL(window.location);
        url.searchParams.delete('talkgroup');
        
        // Update browser history
        window.history.pushState({}, 'SignalRadio - Live Call Stream', url);
        
        // Return to main view
        this.uiManager.showMainView();
    }
}

// Initialize the application when the page loads
let app;
document.addEventListener('DOMContentLoaded', () => {
    app = new SignalRadioApp();
    // Make sure the app is available globally for onclick handlers
    window.app = app;
});

// Export for potential external use
export { SignalRadioApp };
