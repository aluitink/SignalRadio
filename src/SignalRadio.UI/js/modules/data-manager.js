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
                return;
            }
            
            this.app.displayCalls(data.calls);
            console.log(`Successfully loaded ${data.calls.length} recent calls`);
            
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
            const payload = await response.json();

            // The API returns two shapes:
            // - GET /api/talkgroup -> [ { decimal, description, ... }, ... ]
            // - GET /api/talkgroup/{id} -> { TalkGroup: { decimal, description, ... }, RecentCalls: [...] }
            // Normalize to always cache the actual talk group object.
            let talkGroup = payload;
            if (payload && payload.TalkGroup) {
                talkGroup = payload.TalkGroup;
            } else if (payload && payload.talkGroup) {
                talkGroup = payload.talkGroup;
            }

            // Choose a cache key that matches how the rest of the UI looks up talk groups.
            // Prefer the decimal property (camelCase from the API), fall back to the provided id.
            const key = (talkGroup && (talkGroup.decimal || talkGroup.Decimal)) ? (talkGroup.decimal || talkGroup.Decimal) : talkgroupId;

            // Add to cache
            this.talkGroupCache.set(key, talkGroup);

            // Update any existing call cards that use this talk group
            this.app.uiManager.updateCallCardsForTalkGroup(key);
            
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
