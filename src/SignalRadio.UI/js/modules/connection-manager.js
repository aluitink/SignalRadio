// SignalR Connection Management
export class ConnectionManager {
    constructor(app) {
        this.app = app;
        this.connection = null;
    }

    async initializeSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/talkgroup')
                .withAutomaticReconnect()
                .build();

            this.setupConnectionEvents();
            this.setupSignalREvents();

            // Start connection
            this.app.uiManager.updateConnectionStatus('connecting');
            await this.connection.start();
            this.app.uiManager.updateConnectionStatus('connected');
            this.app.uiManager.showToast('Connected to SignalRadio', 'success');

            // Resubscribe to saved talk groups
            await this.resubscribeToTalkGroups();

            // Subscribe to all calls stream for general monitoring
            await this.connection.invoke('SubscribeToAllCallsStream');

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.app.uiManager.updateConnectionStatus('disconnected');
            this.app.uiManager.showToast('Failed to connect to server', 'error');
        }
    }

    setupConnectionEvents() {
        this.connection.onclose(() => {
            this.app.uiManager.updateConnectionStatus('disconnected');
            this.app.uiManager.showToast('Disconnected from server', 'error');
        });

        this.connection.onreconnecting(() => {
            this.app.uiManager.updateConnectionStatus('connecting');
        });

        this.connection.onreconnected(() => {
            this.app.uiManager.updateConnectionStatus('connected');
            this.app.uiManager.showToast('Reconnected to server', 'success');
            this.resubscribeToTalkGroups();
        });
    }

    setupSignalREvents() {
        this.connection.on('NewCall', (callData) => {
            this.app.handleSubscribedCall(callData);
        });

        this.connection.on('AllCallsStreamUpdate', (callData) => {
            this.app.handleAllCallsStreamUpdate(callData);
        });

        this.connection.on('CallUpdated', (callData) => {
            this.app.handleCallUpdate(callData);
        });

        this.connection.on('SubscriptionConfirmed', (talkGroupId) => {
            this.app.subscriptions.add(talkGroupId);
            this.app.uiManager.updateSubscriptionsDisplay();
            this.app.settingsManager.saveSettings();
            // Removed toast notification for less clutter
        });

        this.connection.on('UnsubscriptionConfirmed', (talkGroupId) => {
            this.app.subscriptions.delete(talkGroupId);
            this.app.uiManager.updateSubscriptionsDisplay();
            this.app.settingsManager.saveSettings();
            // Removed toast notification for less clutter
        });

        this.connection.on('AllCallsStreamSubscribed', () => {
            // Removed notification for less clutter
        });

        this.connection.on('AllCallsStreamUnsubscribed', () => {
            // Removed notification for less clutter
        });
    }

    async resubscribeToTalkGroups() {
        // Resubscribe to specific talk groups
        for (const talkGroupId of this.app.subscriptions) {
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

    async subscribeToTalkGroup(talkGroupId) {
        if (!this.connection) {
            throw new Error('Not connected to server');
        }
        await this.connection.invoke('SubscribeToTalkGroup', talkGroupId);
    }

    async unsubscribeFromTalkGroup(talkGroupId) {
        if (!this.connection) {
            throw new Error('Not connected to server');
        }
        await this.connection.invoke('UnsubscribeFromTalkGroup', talkGroupId);
    }

    isConnected() {
        return this.connection && this.connection.state === signalR.HubConnectionState.Connected;
    }
}
