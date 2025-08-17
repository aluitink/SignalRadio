// Import modules
import { ConnectionManager } from './modules/connection-manager.js';
import { AudioManager } from './modules/audio-manager.js';
import { DataManager } from './modules/data-manager.js';
import { SettingsManager } from './modules/settings-manager.js';
import { UIManager } from './modules/ui-manager.js';
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
        this.uiManager = new UIManager(this);
        this.connectionManager = new ConnectionManager(this);
        
        this.initializeApp();
    }

    async initializeApp() {
        try {
            console.log('Initializing SignalRadio App...');
            this.uiManager.setupEventListeners();
            this.settingsManager.loadSettings();
            
            console.log('Loading talk group data...');
            await this.dataManager.loadTalkGroupData();
            
            console.log('Initializing SignalR connection...');
            await this.connectionManager.initializeSignalR();
            
            console.log('Loading recent calls...');
            this.dataManager.loadRecentCalls();
            
            console.log('Starting age update timer...');
            this.uiManager.startAgeUpdateTimer();
            
            console.log('App initialization complete');
        } catch (error) {
            console.error('Failed to initialize app:', error);
            this.uiManager.showToast(`App initialization failed: ${error.message}`, 'error');
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
    }

    handleSubscribedCall(callData) {
        // This is for calls from talk groups we're specifically subscribed to
        this.totalCallsReceived++;
        this.activeCalls.set(callData.id, callData);
        this.uiManager.addCallToStream(callData, true, true); // Mark as subscribed call
        this.uiManager.updateStatistics();

        // Auto-play since this is a subscribed call (only if user has interacted and auto-play is enabled)
        if (this.autoPlay && this.audioManager.userHasInteracted) {
            this.audioManager.queueCall(callData);
        } else if (this.autoPlay && !this.audioManager.userHasInteracted) {
            this.uiManager.showToast('Click anywhere to enable auto-play', 'info');
        }
    }

    handleAllCallsStreamUpdate(callData) {
        // This is for general monitoring of all calls (not subscribed)
        // Only add to stream if we're not already subscribed to this talk group
        if (!this.subscriptions.has(callData.talkgroupId)) {
            this.totalCallsReceived++;
            this.activeCalls.set(callData.id, callData);
            this.uiManager.addCallToStream(callData, true, false); // Mark as general stream call
            this.uiManager.updateStatistics();
        }
        // If we are subscribed, we'll get it via handleSubscribedCall instead
    }

    handleCallUpdate(callData) {
        this.activeCalls.set(callData.id, callData);
        this.uiManager.updateCallInStream(callData);
    }

    displayCalls(calls) {
        const streamContainer = document.getElementById('call-stream');
        streamContainer.innerHTML = '';

        calls.forEach(call => {
            this.activeCalls.set(call.id, call);
            this.uiManager.addCallToStream(call, false);
        });

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
            
            this.uiManager.showToast('All subscriptions cleared', 'info');
        }
    }

    clearCallStream() {
        if (confirm('Clear the call stream?')) {
            this.uiManager.clearCallStream();
            this.activeCalls.clear();
            this.uiManager.updateStatistics();
            this.uiManager.showToast('Call stream cleared', 'info');
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
