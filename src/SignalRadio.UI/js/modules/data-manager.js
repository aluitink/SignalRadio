// Data Management - API calls and caching
export class DataManager {
    constructor(app) {
        this.app = app;
        this.talkGroupCache = new Map();
    }

    async loadTalkGroupData() {
        try {
            console.log('Loading talk group data...');
            const response = await fetch('/api/talkgroup');
            if (!response.ok) {
                console.error('Failed to load talk groups:', response.status, response.statusText);
                return;
            }
            
            const talkGroups = await response.json();
            console.log(`Loaded ${talkGroups.length} talk groups`);
            
            // Clear existing cache
            this.talkGroupCache.clear();
            
            // Populate cache using decimal ID as key
            talkGroups.forEach(tg => {
                this.talkGroupCache.set(tg.decimal, tg);
            });
            
            console.log('Talk group cache populated');
            
        } catch (error) {
            console.error('Failed to load talk group data:', error);
            this.app.uiManager.showToast('Failed to load talk group information', 'warning');
        }
    }

    async loadRecentCalls() {
        try {
            console.log('Loading recent calls...');
            const response = await fetch('/api/calls?limit=50');
            
            if (!response.ok) {
                console.error('API response not ok:', response.status, response.statusText);
                throw new Error(`Failed to load calls: ${response.status} ${response.statusText}`);
            }
            
            const data = await response.json();
            console.log('Received calls data:', data);
            
            if (!data.calls) {
                console.warn('No calls array in response:', data);
                this.app.uiManager.showToast('No calls data received', 'warning');
                return;
            }
            
            this.app.displayCalls(data.calls);
            console.log(`Successfully loaded ${data.calls.length} recent calls`);
            this.app.uiManager.showToast(`Loaded ${data.calls.length} recent calls`, 'success');
            
        } catch (error) {
            console.error('Failed to load recent calls:', error);
            this.app.uiManager.showToast(`Failed to load recent calls: ${error.message}`, 'error');
        }
    }

    getTalkGroupInfo(talkgroupId) {
        // First check the cache
        const cachedInfo = this.talkGroupCache.get(talkgroupId);
        if (cachedInfo) {
            return cachedInfo;
        }

        // If not in cache, try to fetch it
        this.fetchTalkGroupInfo(talkgroupId);
        
        // Return null for now, but the call card will be updated when data loads
        return null;
    }

    async fetchTalkGroupInfo(talkgroupId) {
        try {
            const response = await fetch(`/api/talkgroup/${talkgroupId}`);
            if (!response.ok) {
                if (response.status !== 404) {
                    console.error(`Failed to fetch talk group ${talkgroupId}:`, response.status, response.statusText);
                }
                return;
            }
            
            const talkGroup = await response.json();
            
            // Add to cache
            this.talkGroupCache.set(talkgroupId, talkGroup);
            
            // Update any existing call cards that use this talk group
            this.app.uiManager.updateCallCardsForTalkGroup(talkgroupId);
            
        } catch (error) {
            console.error(`Failed to fetch talk group ${talkgroupId}:`, error);
        }
    }

    clearTalkGroupCache() {
        this.talkGroupCache.clear();
    }

    getTalkGroupCacheSize() {
        return this.talkGroupCache.size;
    }
}
