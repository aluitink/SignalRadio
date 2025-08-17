// Settings Management - Local storage and user preferences
export class SettingsManager {
    constructor(app) {
        this.app = app;
    }

    loadSettings() {
        const settings = JSON.parse(localStorage.getItem('signalradio-settings') || '{}');
        
        this.app.autoPlay = settings.autoPlay || false;
        document.getElementById('auto-play-toggle').checked = this.app.autoPlay;
        
        // Load background playback settings
        const backgroundPlayback = settings.backgroundPlayback !== false; // default true
        const wakeLock = settings.wakeLock !== false; // default true
        
        const backgroundToggle = document.getElementById('background-playback-toggle');
        const wakeLockToggle = document.getElementById('wake-lock-toggle');
        
        if (backgroundToggle) backgroundToggle.checked = backgroundPlayback;
        if (wakeLockToggle) wakeLockToggle.checked = wakeLock;
        
        // Sync desktop versions
        this.syncToggleStates();
        
        const volume = settings.volume || 50;
        document.getElementById('volume-control').value = volume;
        this.app.audioManager.setVolume(volume);

        this.app.subscriptions = new Set(settings.subscriptions || []);
        this.app.uiManager.updateSubscriptionsDisplay();
    }

    syncToggleStates() {
        // Sync toggle states between mobile and desktop versions
        const togglePairs = [
            ['auto-play-toggle', 'auto-play-toggle-desktop'],
            ['background-playback-toggle', 'background-playback-toggle-desktop'],
            ['wake-lock-toggle', 'wake-lock-toggle-desktop'],
            ['volume-control', 'volume-control-desktop']
        ];

        togglePairs.forEach(([mobileId, desktopId]) => {
            const mobile = document.getElementById(mobileId);
            const desktop = document.getElementById(desktopId);
            
            if (mobile && desktop) {
                if (mobile.type === 'checkbox') {
                    desktop.checked = mobile.checked;
                } else {
                    desktop.value = mobile.value;
                }
            }
        });
    }

    saveSettings() {
        const settings = {
            autoPlay: this.app.autoPlay,
            volume: document.getElementById('volume-control').value,
            subscriptions: Array.from(this.app.subscriptions),
            backgroundPlayback: document.getElementById('background-playback-toggle')?.checked !== false,
            wakeLock: document.getElementById('wake-lock-toggle')?.checked !== false
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
