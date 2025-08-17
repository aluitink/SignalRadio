// SignalRadio Subscriptions Management
class SubscriptionsManager {
    constructor() {
        this.talkGroups = [];
        this.filteredTalkGroups = [];
        this.subscriptions = new Set();
        this.connection = null;
        this.currentView = 'grid';
        
        this.init();
    }

    async init() {
        console.log('Initializing Subscriptions Manager...');
        
        // Load settings from localStorage
        this.loadSettings();
        
        // Setup event listeners
        this.setupEventListeners();
        
        // Connect to SignalR
        await this.initializeSignalR();
        
        // Load data
        await this.loadTalkGroups();
        await this.loadCategories();
        
        // Initial render
        this.applyFilters();
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
            this.onConnectionStateChanged('connecting', false);
            await this.connection.start();
            this.onConnectionStateChanged('connected', true);
            this.showToast('Connected to server', 'success');

            // Resubscribe to saved talk groups
            await this.resubscribeToTalkGroups();

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.onConnectionStateChanged('disconnected', false);
            this.showToast('Failed to connect to server', 'error');
        }
    }

    setupConnectionEvents() {
        this.connection.onclose(() => {
            this.onConnectionStateChanged('disconnected', false);
            this.showToast('Disconnected from server', 'error');
        });

        this.connection.onreconnecting(() => {
            this.onConnectionStateChanged('connecting', false);
        });

        this.connection.onreconnected(() => {
            this.onConnectionStateChanged('connected', true);
            this.showToast('Connected to server', 'success');
            this.resubscribeToTalkGroups();
        });
    }

    setupSignalREvents() {
        this.connection.on('SubscriptionConfirmed', (talkGroupId) => {
            this.onSubscriptionConfirmed(talkGroupId);
        });

        this.connection.on('UnsubscriptionConfirmed', (talkGroupId) => {
            this.onUnsubscriptionConfirmed(talkGroupId);
        });
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

    loadSettings() {
        const settings = JSON.parse(localStorage.getItem('signalradio-settings') || '{}');
        this.subscriptions = new Set(settings.subscriptions || []);
        this.updateStats();
    }

    saveSettings() {
        const settings = JSON.parse(localStorage.getItem('signalradio-settings') || '{}');
        settings.subscriptions = Array.from(this.subscriptions);
        localStorage.setItem('signalradio-settings', JSON.stringify(settings));
    }

    setupEventListeners() {
        // Filter controls
        document.getElementById('search-input').addEventListener('input', () => this.applyFilters());
        document.getElementById('category-filter').addEventListener('change', () => this.applyFilters());
        document.getElementById('subscription-filter').addEventListener('change', () => this.applyFilters());
        
        // View mode toggles
        document.getElementById('grid-view').addEventListener('change', () => this.setViewMode('grid'));
        document.getElementById('list-view').addEventListener('change', () => this.setViewMode('list'));
        
        // Quick actions
        document.getElementById('subscribe-all-filtered').addEventListener('click', () => this.subscribeAllFiltered());
        document.getElementById('unsubscribe-all-filtered').addEventListener('click', () => this.unsubscribeAllFiltered());
        document.getElementById('clear-all-subscriptions').addEventListener('click', () => this.clearAllSubscriptions());
        
        // Refresh button
        document.getElementById('refresh-data').addEventListener('click', () => this.refreshData());
    }

    async loadTalkGroups() {
        try {
            console.log('Loading talk groups...');
            const response = await fetch('/api/talkgroup');
            if (!response.ok) {
                throw new Error(`Failed to load talk groups: ${response.status}`);
            }
            
            this.talkGroups = await response.json();
            console.log(`Loaded ${this.talkGroups.length} talk groups`);
            
        } catch (error) {
            console.error('Failed to load talk groups:', error);
            this.showToast('Failed to load talk group data', 'error');
            this.talkGroups = [];
        }
    }

    async loadCategories() {
        try {
            const response = await fetch('/api/talkgroup/categories');
            if (!response.ok) {
                throw new Error(`Failed to load categories: ${response.status}`);
            }
            
            const categories = await response.json();
            this.populateCategoryFilter(categories);
            
        } catch (error) {
            console.error('Failed to load categories:', error);
        }
    }

    populateCategoryFilter(categories) {
        const select = document.getElementById('category-filter');
        const currentValue = select.value;
        
        // Clear existing options (except "All Categories")
        while (select.children.length > 1) {
            select.removeChild(select.lastChild);
        }
        
        // Add category options
        categories.forEach(category => {
            if (category) {
                const option = document.createElement('option');
                option.value = category;
                option.textContent = category;
                select.appendChild(option);
            }
        });
        
        // Restore selection
        select.value = currentValue;
    }

    applyFilters() {
        const searchTerm = document.getElementById('search-input').value.toLowerCase();
        const categoryFilter = document.getElementById('category-filter').value;
        const subscriptionFilter = document.getElementById('subscription-filter').value;

        this.filteredTalkGroups = this.talkGroups.filter(tg => {
            // Search filter
            if (searchTerm) {
                const searchMatch = 
                    tg.decimal.toLowerCase().includes(searchTerm) ||
                    tg.alphaTag.toLowerCase().includes(searchTerm) ||
                    (tg.description && tg.description.toLowerCase().includes(searchTerm)) ||
                    (tg.category && tg.category.toLowerCase().includes(searchTerm)) ||
                    (tg.tag && tg.tag.toLowerCase().includes(searchTerm));
                
                if (!searchMatch) return false;
            }

            // Category filter
            if (categoryFilter && tg.category !== categoryFilter) {
                return false;
            }

            // Subscription status filter
            if (subscriptionFilter === 'subscribed' && !this.subscriptions.has(tg.decimal)) {
                return false;
            }
            if (subscriptionFilter === 'unsubscribed' && this.subscriptions.has(tg.decimal)) {
                return false;
            }

            return true;
        });

        this.updateStats();
        this.renderTalkGroups();
    }

    updateStats() {
        document.getElementById('total-count').textContent = this.talkGroups.length;
        document.getElementById('subscribed-count').textContent = this.subscriptions.size;
        document.getElementById('filtered-count').textContent = this.filteredTalkGroups.length;
    }

    setViewMode(mode) {
        this.currentView = mode;
        
        const gridContainer = document.getElementById('talkgroups-grid');
        const listContainer = document.getElementById('talkgroups-list');
        
        if (mode === 'grid') {
            gridContainer.classList.remove('d-none');
            listContainer.classList.add('d-none');
        } else {
            gridContainer.classList.add('d-none');
            listContainer.classList.remove('d-none');
        }
        
        this.renderTalkGroups();
    }

    renderTalkGroups() {
        // Hide loading state
        document.getElementById('loading-state').style.display = 'none';
        
        if (this.filteredTalkGroups.length === 0) {
            document.getElementById('empty-state').classList.remove('d-none');
            document.getElementById('talkgroups-grid').innerHTML = '';
            document.getElementById('talkgroups-table-body').innerHTML = '';
            return;
        }
        
        document.getElementById('empty-state').classList.add('d-none');
        
        if (this.currentView === 'grid') {
            this.renderGridView();
        } else {
            this.renderListView();
        }
    }

    renderGridView() {
        const container = document.getElementById('talkgroups-grid');
        container.innerHTML = '';

        this.filteredTalkGroups.forEach(tg => {
            const isSubscribed = this.subscriptions.has(tg.decimal);
            const priorityClass = this.getPriorityClass(tg.priority);
            
            const card = document.createElement('div');
            card.className = 'col-md-6 col-lg-4';
            card.innerHTML = `
                <div class="card h-100 ${isSubscribed ? 'border-success' : ''}">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-2">
                            <h6 class="card-title mb-0">${this.escapeHtml(tg.alphaTag)}</h6>
                            <div class="d-flex gap-1">
                                ${tg.priority ? `<span class="badge ${priorityClass}">P${tg.priority}</span>` : ''}
                                <span class="badge ${isSubscribed ? 'bg-success' : 'bg-secondary'}">
                                    <i class="bi bi-${isSubscribed ? 'bookmark-fill' : 'bookmark'}"></i>
                                </span>
                            </div>
                        </div>
                        
                        <div class="text-muted small mb-2">
                            <div><strong>ID:</strong> ${tg.decimal}</div>
                            ${tg.category ? `<div><strong>Category:</strong> ${this.escapeHtml(tg.category)}</div>` : ''}
                        </div>
                        
                        ${tg.description ? `<p class="card-text small">${this.escapeHtml(tg.description)}</p>` : ''}
                        
                        <div class="d-flex gap-2">
                            <button class="btn btn-outline-info btn-sm" 
                                    onclick="subscriptionsManager.viewTalkgroupStream('${tg.decimal}')">
                                <i class="bi bi-list-ul me-1"></i>
                                View Stream
                            </button>
                            <button class="btn ${isSubscribed ? 'btn-outline-danger' : 'btn-outline-success'} btn-sm flex-fill" 
                                    onclick="subscriptionsManager.toggleSubscription('${tg.decimal}', this)">
                                <i class="bi bi-${isSubscribed ? 'bookmark-dash' : 'bookmark-plus'} me-1"></i>
                                ${isSubscribed ? 'Unsubscribe' : 'Subscribe'}
                            </button>
                        </div>
                    </div>
                </div>
            `;
            container.appendChild(card);
        });
    }

    renderListView() {
        const tbody = document.getElementById('talkgroups-table-body');
        tbody.innerHTML = '';

        this.filteredTalkGroups.forEach(tg => {
            const isSubscribed = this.subscriptions.has(tg.decimal);
            const priorityClass = this.getPriorityClass(tg.priority);
            
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>
                    <span class="badge ${isSubscribed ? 'bg-success' : 'bg-secondary'}">
                        <i class="bi bi-${isSubscribed ? 'bookmark-fill' : 'bookmark'}"></i>
                        ${isSubscribed ? 'Subscribed' : 'Not Subscribed'}
                    </span>
                </td>
                <td>
                    <div>${tg.decimal}</div>
                    ${tg.hex ? `<small class="text-muted">${tg.hex}</small>` : ''}
                </td>
                <td>${this.escapeHtml(tg.alphaTag)}</td>
                <td>${tg.description ? this.escapeHtml(tg.description) : '-'}</td>
                <td>
                    ${tg.category ? this.escapeHtml(tg.category) : '-'}
                    ${tg.priority ? `<span class="badge ${priorityClass} ms-1">P${tg.priority}</span>` : ''}
                </td>
                <td>
                    <div class="btn-group" role="group">
                        <button class="btn btn-outline-info btn-sm" 
                                onclick="subscriptionsManager.viewTalkgroupStream('${tg.decimal}')">
                            <i class="bi bi-list-ul"></i>
                        </button>
                        <button class="btn ${isSubscribed ? 'btn-outline-danger' : 'btn-outline-success'} btn-sm" 
                                onclick="subscriptionsManager.toggleSubscription('${tg.decimal}', this)">
                            <i class="bi bi-${isSubscribed ? 'bookmark-dash' : 'bookmark-plus'} me-1"></i>
                            ${isSubscribed ? 'Unsubscribe' : 'Subscribe'}
                        </button>
                    </div>
                </td>
            `;
            tbody.appendChild(row);
        });
    }

    getPriorityClass(priority) {
        if (!priority) return 'bg-secondary';
        if (priority <= 1) return 'bg-danger';
        if (priority <= 3) return 'bg-warning';
        return 'bg-info';
    }

    async toggleSubscription(talkGroupId, buttonElement) {
        if (!this.isConnected()) {
            this.showToast('Not connected to server', 'error');
            return;
        }

        try {
            const isCurrentlySubscribed = this.subscriptions.has(talkGroupId);
            
            if (isCurrentlySubscribed) {
                await this.unsubscribeFromTalkGroup(talkGroupId);
                this.subscriptions.delete(talkGroupId);
            } else {
                await this.subscribeToTalkGroup(talkGroupId);
                this.subscriptions.add(talkGroupId);
            }

            this.saveSettings();
            this.applyFilters(); // Re-render to update UI
            
            // Removed individual subscription toast notifications for less clutter

        } catch (error) {
            console.error('Failed to toggle subscription:', error);
            this.showToast('Failed to update subscription', 'error');
        }
    }

    async subscribeAllFiltered() {
        if (!this.isConnected()) {
            this.showToast('Not connected to server', 'error');
            return;
        }

        if (this.filteredTalkGroups.length === 0) {
            return;
        }

        const unsubscribed = this.filteredTalkGroups.filter(tg => !this.subscriptions.has(tg.decimal));
        
        if (unsubscribed.length === 0) {
            return;
        }

        try {
            for (const tg of unsubscribed) {
                await this.subscribeToTalkGroup(tg.decimal);
                this.subscriptions.add(tg.decimal);
            }

            this.saveSettings();
            this.applyFilters();
            
            this.showToast(`Subscribed to ${unsubscribed.length} talk groups`, 'success');

        } catch (error) {
            console.error('Failed to subscribe to all filtered talk groups:', error);
            this.showToast('Failed to subscribe to all talk groups', 'error');
        }
    }

    async unsubscribeAllFiltered() {
        if (!this.isConnected()) {
            this.showToast('Not connected to server', 'error');
            return;
        }

        const subscribed = this.filteredTalkGroups.filter(tg => this.subscriptions.has(tg.decimal));
        
        if (subscribed.length === 0) {
            return;
        }

        if (!confirm(`Are you sure you want to unsubscribe from ${subscribed.length} talk groups?`)) {
            return;
        }

        try {
            for (const tg of subscribed) {
                await this.unsubscribeFromTalkGroup(tg.decimal);
                this.subscriptions.delete(tg.decimal);
            }

            this.saveSettings();
            this.applyFilters();
            
            this.showToast(`Unsubscribed from ${subscribed.length} talk groups`, 'success');

        } catch (error) {
            console.error('Failed to unsubscribe from all filtered talk groups:', error);
            this.showToast('Failed to unsubscribe from all talk groups', 'error');
        }
    }

    async clearAllSubscriptions() {
        if (this.subscriptions.size === 0) {
            return;
        }

        if (!confirm(`Are you sure you want to clear all ${this.subscriptions.size} subscriptions?`)) {
            return;
        }

        try {
            for (const talkGroupId of this.subscriptions) {
                if (this.isConnected()) {
                    await this.unsubscribeFromTalkGroup(talkGroupId);
                }
            }

            this.subscriptions.clear();
            this.saveSettings();
            this.applyFilters();
            
            this.showToast('All subscriptions cleared', 'success');

        } catch (error) {
            console.error('Failed to clear all subscriptions:', error);
            this.showToast('Failed to clear all subscriptions', 'error');
        }
    }

    async refreshData() {
        await this.loadTalkGroups();
        await this.loadCategories();
        this.applyFilters();
    }

    // SignalR event handlers
    onSubscriptionConfirmed(talkGroupId) {
        this.subscriptions.add(talkGroupId);
        this.saveSettings();
        this.applyFilters();
    }

    onUnsubscriptionConfirmed(talkGroupId) {
        this.subscriptions.delete(talkGroupId);
        this.saveSettings();
        this.applyFilters();
    }

    onConnectionStateChanged(state, connected) {
        const statusElement = document.getElementById('connection-status');
        if (connected) {
            statusElement.innerHTML = '<i class="bi bi-circle-fill me-1"></i>Connected';
            statusElement.className = 'badge bg-success';
        } else {
            statusElement.innerHTML = '<i class="bi bi-circle-fill me-1"></i>Disconnected';
            statusElement.className = 'badge bg-danger';
        }
    }

    showToast(message, type = 'info') {
        const toastElement = document.getElementById('toast');
        const toastBody = toastElement.querySelector('.toast-body');
        
        toastBody.textContent = message;
        
        // Update toast styling based on type
        toastElement.className = `toast ${type === 'error' ? 'bg-danger text-white' : 
                                          type === 'success' ? 'bg-success text-white' :
                                          type === 'warning' ? 'bg-warning text-dark' : 'bg-info text-white'}`;
        
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
    }

    // Navigation method
    viewTalkgroupStream(talkgroupId) {
        // Navigate to the main page with talkgroup parameter
        const url = new URL(window.location.origin + '/index.html');
        url.searchParams.set('talkgroup', talkgroupId);
        
        // Navigate to the main page
        window.location.href = url.toString();
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize the application
const subscriptionsManager = new SubscriptionsManager();

// Make it globally available for inline event handlers
window.subscriptionsManager = subscriptionsManager;
