// SignalRadio Admin Panel
class AdminManager {
    constructor() {
        this.connection = null;
        this.logRefreshInterval = null;
        
        this.init();
    }

    async init() {
        console.log('Initializing Admin Manager...');
        
        // Setup event listeners
        this.setupEventListeners();
        
        // Connect to SignalR for real-time updates
        await this.initializeSignalR();
        
        // Load initial data
        await this.refreshStats();
        await this.refreshSystemInfo();
    }

    async initializeSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/talkgroup')
                .withAutomaticReconnect()
                .build();

            this.setupConnectionEvents();

            // Start connection
            this.onConnectionStateChanged('connecting', false);
            await this.connection.start();
            this.onConnectionStateChanged('connected', true);

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.onConnectionStateChanged('disconnected', false);
            this.showToast('Failed to connect to server', 'error');
        }
    }

    setupConnectionEvents() {
        this.connection.onclose(() => {
            this.onConnectionStateChanged('disconnected', false);
        });

        this.connection.onreconnecting(() => {
            this.onConnectionStateChanged('connecting', false);
            this.showToast('Reconnecting to server...', 'warning');
        });

        this.connection.onreconnected(() => {
            this.onConnectionStateChanged('connected', true);
            this.showToast('Reconnected to server', 'success');
        });
    }

    isConnected() {
        return this.connection && this.connection.state === signalR.HubConnectionState.Connected;
    }

    setupEventListeners() {
        // CSV Upload Form
        document.getElementById('csv-upload-form').addEventListener('submit', (e) => this.handleCsvUpload(e));
        
        // Stats refresh
        document.getElementById('refresh-stats').addEventListener('click', () => this.refreshStats());
        
        // System info refresh
        document.getElementById('refresh-system-info').addEventListener('click', () => this.refreshSystemInfo());
        
        // Danger zone - clear all talk groups
        document.getElementById('clear-all-talkgroups').addEventListener('click', () => this.confirmClearAllTalkGroups());
        
        // Sample CSV download
        document.getElementById('download-sample').addEventListener('click', () => this.downloadSampleCsv());
        
        // Log management
        document.getElementById('refresh-logs').addEventListener('click', () => this.refreshLogs());
        document.getElementById('clear-logs').addEventListener('click', () => this.clearLogs());
        
        // Auto-refresh logs toggle
        document.getElementById('auto-refresh-logs').addEventListener('change', (e) => {
            if (e.target.checked) {
                this.startLogAutoRefresh();
            } else {
                this.stopLogAutoRefresh();
            }
        });
        
        // Log level filter
        document.getElementById('log-level-filter').addEventListener('change', () => this.filterLogs());
        
        // Smooth scrolling for navigation links
        document.querySelectorAll('a[href^="#"]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const target = document.querySelector(link.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({ behavior: 'smooth' });
                }
            });
        });
    }

    async handleCsvUpload(event) {
        event.preventDefault();
        
        const fileInput = document.getElementById('csv-file');
        const clearExisting = document.getElementById('clear-existing').checked;
        
        if (!fileInput.files[0]) {
            this.showToast('Please select a CSV file', 'error');
            return;
        }

        const file = fileInput.files[0];
        if (!file.name.toLowerCase().endsWith('.csv')) {
            this.showToast('Please select a valid CSV file', 'error');
            return;
        }

        // Show progress
        this.showUploadProgress(true);
        this.updateUploadStatus('Preparing upload...');

        const formData = new FormData();
        formData.append('csvFile', file);

        try {
            // Clear existing data if requested
            if (clearExisting) {
                this.updateUploadStatus('Clearing existing talk groups...');
                await this.clearAllTalkGroups(false); // Don't show confirmation
            }

            this.updateUploadStatus('Uploading CSV file...');
            this.updateUploadProgress(25);

            const response = await fetch('/api/talkgroup/import', {
                method: 'POST',
                body: formData
            });

            this.updateUploadProgress(75);

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `Upload failed: ${response.status}`);
            }

            const result = await response.json();
            this.updateUploadProgress(100);
            
            // Show results
            this.showUploadResults({
                success: true,
                message: result.message,
                count: result.count,
                fileName: result.fileName
            });

            // Refresh stats
            await this.refreshStats();
            
            this.showToast(`Successfully imported ${result.count} talk groups`, 'success');

        } catch (error) {
            console.error('CSV upload failed:', error);
            this.showUploadResults({
                success: false,
                message: error.message,
                count: 0
            });
            this.showToast('Failed to upload CSV file', 'error');
        } finally {
            this.showUploadProgress(false);
            // Reset form
            fileInput.value = '';
        }
    }

    showUploadProgress(show) {
        const progressContainer = document.getElementById('upload-progress');
        if (show) {
            progressContainer.classList.remove('d-none');
            this.updateUploadProgress(0);
        } else {
            progressContainer.classList.add('d-none');
        }
    }

    updateUploadProgress(percent) {
        const progressBar = document.querySelector('#upload-progress .progress-bar');
        progressBar.style.width = `${percent}%`;
        progressBar.setAttribute('aria-valuenow', percent);
    }

    updateUploadStatus(status) {
        document.getElementById('upload-status').textContent = status;
    }

    showUploadResults(result) {
        const resultsContainer = document.getElementById('upload-results');
        
        resultsContainer.innerHTML = `
            <div class="alert ${result.success ? 'alert-success' : 'alert-danger'}" role="alert">
                <div class="d-flex align-items-center">
                    <i class="bi bi-${result.success ? 'check-circle-fill' : 'exclamation-triangle-fill'} me-2"></i>
                    <div>
                        <strong>${result.success ? 'Success!' : 'Error!'}</strong>
                        <div>${result.message}</div>
                        ${result.success ? `<div class="small text-muted mt-1">Imported ${result.count} talk groups from ${result.fileName}</div>` : ''}
                    </div>
                </div>
            </div>
        `;
        
        resultsContainer.classList.remove('d-none');
        
        // Auto-hide after 10 seconds
        setTimeout(() => {
            resultsContainer.classList.add('d-none');
        }, 10000);
    }

    async refreshStats() {
        try {
            // Get talk group count
            const talkGroupResponse = await fetch('/api/talkgroup');
            if (talkGroupResponse.ok) {
                const talkGroups = await talkGroupResponse.json();
                document.getElementById('total-talkgroups').textContent = talkGroups.length;
                
                // Count categories
                const categories = new Set(talkGroups.map(tg => tg.category).filter(c => c));
                document.getElementById('category-count').textContent = categories.size;
                
                // Update last updated (use current time as approximation)
                document.getElementById('last-updated').textContent = new Date().toLocaleString();
            }
            
        } catch (error) {
            console.error('Failed to refresh stats:', error);
            document.getElementById('total-talkgroups').textContent = 'Error';
            document.getElementById('category-count').textContent = 'Error';
        }
    }

    async refreshSystemInfo() {
        // Simulate system info gathering (extend with real API calls as needed)
        this.updateSystemStatus('api-status', 'Connected', 'success');
        this.updateSystemStatus('signalr-status', this.isConnected() ? 'Connected' : 'Disconnected', 
                                this.isConnected() ? 'success' : 'danger');
        this.updateSystemStatus('database-status', 'Connected', 'success');
        this.updateSystemStatus('storage-status', 'Connected', 'success');
        
        // Update statistics (these would come from real API endpoints)
        document.getElementById('total-recordings').textContent = '1,234';
        document.getElementById('recent-calls').textContent = '89';
        document.getElementById('connected-clients').textContent = '5';
        document.getElementById('system-uptime').textContent = '2d 14h 32m';
    }

    updateSystemStatus(elementId, text, type) {
        const element = document.getElementById(elementId);
        element.textContent = text;
        element.className = `badge bg-${type}`;
    }

    confirmClearAllTalkGroups() {
        this.showConfirmation(
            'Are you sure you want to delete ALL talk groups? This action cannot be undone.',
            () => this.clearAllTalkGroups(true)
        );
    }

    async clearAllTalkGroups(showToast = true) {
        try {
            const response = await fetch('/api/talkgroup', {
                method: 'DELETE'
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `Failed to clear talk groups: ${response.status}`);
            }

            const result = await response.json();
            
            if (showToast) {
                this.showToast('All talk groups have been cleared', 'success');
            }
            
            // Refresh stats
            await this.refreshStats();

        } catch (error) {
            console.error('Failed to clear talk groups:', error);
            if (showToast) {
                this.showToast('Failed to clear talk groups', 'error');
            }
            throw error; // Re-throw for upload process
        }
    }

    downloadSampleCsv() {
        const csvContent = `Decimal,Hex,Mode,Alpha Tag,Description,Tag,Category,Priority
1001,3E9,D,DISPATCH,County Dispatch Center,DISP,Emergency Services,1
1002,3EA,D,FIRE1,Fire Department Channel 1,FIRE,Emergency Services,2
1003,3EB,D,FIRE2,Fire Department Channel 2,FIRE,Emergency Services,2
2001,7D1,D,POLICE1,Police Patrol Channel 1,PD,Law Enforcement,1
2002,7D2,D,POLICE2,Police Patrol Channel 2,PD,Law Enforcement,1
3001,BB9,D,EMS1,Emergency Medical Services,EMS,Emergency Services,2
4001,FA1,D,PUBLIC1,Public Works Channel 1,PW,Public Services,3
4002,FA2,D,PUBLIC2,Public Works Channel 2,PW,Public Services,3`;

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'sample-talkgroups.csv';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        
        this.showToast('Sample CSV file downloaded', 'success');
    }

    async refreshLogs() {
        const container = document.getElementById('logs-container');
        container.innerHTML = '<div class="text-center py-3"><div class="spinner-border spinner-border-sm"></div> Loading logs...</div>';
        
        // Simulate log loading (replace with real API call)
        setTimeout(() => {
            const sampleLogs = [
                { timestamp: new Date().toISOString(), level: 'Information', message: 'New call uploaded: TG 1001, Duration: 15s' },
                { timestamp: new Date(Date.now() - 60000).toISOString(), level: 'Information', message: 'Client connected to TalkGroupHub' },
                { timestamp: new Date(Date.now() - 120000).toISOString(), level: 'Warning', message: 'High CPU usage detected: 85%' },
                { timestamp: new Date(Date.now() - 180000).toISOString(), level: 'Information', message: 'Talk groups imported from CSV: 150 records' },
                { timestamp: new Date(Date.now() - 240000).toISOString(), level: 'Error', message: 'Failed to connect to Azure Blob Storage' },
            ];
            
            this.displayLogs(sampleLogs);
        }, 1000);
    }

    displayLogs(logs) {
        const container = document.getElementById('logs-container');
        const levelFilter = document.getElementById('log-level-filter').value;
        
        const filteredLogs = levelFilter ? logs.filter(log => log.level === levelFilter) : logs;
        
        if (filteredLogs.length === 0) {
            container.innerHTML = '<div class="text-center py-3 text-muted">No logs found</div>';
            return;
        }
        
        container.innerHTML = filteredLogs.map(log => {
            const levelClass = this.getLogLevelClass(log.level);
            return `
                <div class="d-flex align-items-start mb-2 p-2 border rounded">
                    <span class="badge ${levelClass} me-2">${log.level}</span>
                    <div class="flex-grow-1">
                        <div class="small text-muted">${new Date(log.timestamp).toLocaleString()}</div>
                        <div>${log.message}</div>
                    </div>
                </div>
            `;
        }).join('');
        
        // Auto-scroll to bottom
        container.scrollTop = container.scrollHeight;
    }

    getLogLevelClass(level) {
        switch (level) {
            case 'Error': return 'bg-danger';
            case 'Warning': return 'bg-warning text-dark';
            case 'Information': return 'bg-info';
            case 'Debug': return 'bg-secondary';
            default: return 'bg-light text-dark';
        }
    }

    filterLogs() {
        // Re-display logs with current filter
        this.refreshLogs();
    }

    clearLogs() {
        this.showConfirmation(
            'Are you sure you want to clear the displayed logs?',
            () => {
                document.getElementById('logs-container').innerHTML = 
                    '<div class="text-center py-3 text-muted">Logs cleared</div>';
                this.showToast('Logs cleared', 'success');
            }
        );
    }

    startLogAutoRefresh() {
        if (this.logRefreshInterval) {
            clearInterval(this.logRefreshInterval);
        }
        
        this.logRefreshInterval = setInterval(() => {
            this.refreshLogs();
        }, 10000); // Refresh every 10 seconds
        
        this.showToast('Auto-refresh enabled (10s)', 'info');
    }

    stopLogAutoRefresh() {
        if (this.logRefreshInterval) {
            clearInterval(this.logRefreshInterval);
            this.logRefreshInterval = null;
        }
    }

    showConfirmation(message, onConfirm) {
        const modal = new bootstrap.Modal(document.getElementById('confirmationModal'));
        document.getElementById('confirmation-message').textContent = message;
        
        const confirmButton = document.getElementById('confirm-action');
        
        // Remove any existing event listeners
        const newConfirmButton = confirmButton.cloneNode(true);
        confirmButton.parentNode.replaceChild(newConfirmButton, confirmButton);
        
        // Add new event listener
        newConfirmButton.addEventListener('click', () => {
            modal.hide();
            onConfirm();
        });
        
        modal.show();
    }

    // SignalR event handlers
    onConnectionStateChanged(state, connected) {
        const statusElement = document.getElementById('connection-status');
        if (connected) {
            statusElement.innerHTML = '<i class="bi bi-circle-fill me-1"></i>Connected';
            statusElement.className = 'badge bg-success';
        } else {
            statusElement.innerHTML = '<i class="bi bi-circle-fill me-1"></i>Disconnected';
            statusElement.className = 'badge bg-danger';
        }
        
        // Update system info
        this.updateSystemStatus('signalr-status', connected ? 'Connected' : 'Disconnected', 
                                connected ? 'success' : 'danger');
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
}

// Initialize the admin manager
const adminManager = new AdminManager();

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    adminManager.stopLogAutoRefresh();
});
