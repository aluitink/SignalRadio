// Settings Management - Local storage and user preferences
export class SettingsManager {
    constructor(app) {
        this.app = app;
    }

    loadSettings() {
        const settings = JSON.parse(localStorage.getItem('signalradio-settings') || '{}');
        
        this.app.autoPlay = settings.autoPlay || false;
        document.getElementById('auto-play-toggle').checked = this.app.autoPlay;
        
        const volume = settings.volume || 50;
        document.getElementById('volume-control').value = volume;
        this.app.audioManager.setVolume(volume);

        this.app.subscriptions = new Set(settings.subscriptions || []);
        this.app.uiManager.updateSubscriptionsDisplay();
    }

    saveSettings() {
        const settings = {
            autoPlay: this.app.autoPlay,
            volume: document.getElementById('volume-control').value,
            subscriptions: Array.from(this.app.subscriptions)
        };
        localStorage.setItem('signalradio-settings', JSON.stringify(settings));
    }

    clearSettings() {
        localStorage.removeItem('signalradio-settings');
        this.loadSettings();
    }

    exportSettings() {
        const settings = localStorage.getItem('signalradio-settings');
        return settings ? JSON.parse(settings) : {};
    }

    importSettings(settings) {
        localStorage.setItem('signalradio-settings', JSON.stringify(settings));
        this.loadSettings();
    }
}
