// UI Management - DOM manipulation and display logic
export class UIManager {
    constructor(app) {
        this.app = app;
    }

    setupEventListeners() {
        // Auto-play toggle - sync between mobile and desktop
        this.setupSyncedControl('auto-play-toggle', 'auto-play-toggle-desktop', (e) => {
            this.app.autoPlay = e.target.checked;
            this.app.settingsManager.saveSettings();
        });

        // Background playback toggle - sync between mobile and desktop
        this.setupSyncedControl('background-playback-toggle', 'background-playback-toggle-desktop', (e) => {
            if (this.app.mediaSessionManager) {
                this.app.mediaSessionManager.setSetting('background-playback', e.target.checked);
            }
            this.app.settingsManager.saveSettings();
        });

        // Wake lock toggle - sync between mobile and desktop
        this.setupSyncedControl('wake-lock-toggle', 'wake-lock-toggle-desktop', (e) => {
            if (this.app.mediaSessionManager) {
                this.app.mediaSessionManager.setSetting('wake-lock', e.target.checked);
                
                // Release wake lock if disabled while playing
                if (!e.target.checked && this.app.audioManager.currentlyPlaying) {
                    this.app.mediaSessionManager.releaseWakeLock();
                }
            }
            this.app.settingsManager.saveSettings();
        });

        // Volume control - sync between mobile and desktop
        this.setupSyncedControl('volume-control', 'volume-control-desktop', (e) => {
            this.app.audioManager.setVolume(e.target.value);
            this.app.settingsManager.saveSettings();
        });

        // Clear subscriptions buttons
        document.getElementById('clear-subscriptions')?.addEventListener('click', () => {
            this.app.clearAllSubscriptions();
        });
        document.getElementById('clear-subscriptions-desktop')?.addEventListener('click', () => {
            this.app.clearAllSubscriptions();
        });

        // Refresh calls
        document.getElementById('refresh-calls').addEventListener('click', () => {
            this.app.dataManager.loadRecentCalls();
        });

        // Refresh talk groups
        document.getElementById('refresh-talkgroups')?.addEventListener('click', () => {
            this.app.dataManager.loadTalkGroupData();
        });

        // Clear stream
        document.getElementById('clear-stream').addEventListener('click', () => {
            this.app.clearCallStream();
        });

        // Event delegation for dynamically created buttons
        document.addEventListener('click', (e) => {
            // Handle play button clicks
            if (e.target.closest('.btn-play')) {
                const button = e.target.closest('.btn-play');
                const callData = button.dataset.call;
                if (callData) {
                    try {
                        const call = JSON.parse(callData);
                        this.app.playCall(call);
                    } catch (error) {
                        console.error('Failed to parse call data:', error);
                        this.showToast('Failed to play recording', 'error');
                    }
                }
                e.preventDefault();
                return false;
            }
        });
    }

    // Helper method to setup synced controls between mobile and desktop versions
    setupSyncedControl(mobileId, desktopId, handler) {
        const mobileElement = document.getElementById(mobileId);
        const desktopElement = document.getElementById(desktopId);
        
        if (mobileElement) {
            mobileElement.addEventListener(mobileElement.type === 'range' ? 'input' : 'change', (e) => {
                handler(e);
                // Sync to desktop version
                if (desktopElement) {
                    if (desktopElement.type === 'checkbox') {
                        desktopElement.checked = e.target.checked;
                    } else {
                        desktopElement.value = e.target.value;
                    }
                }
            });
        }
        
        if (desktopElement) {
            desktopElement.addEventListener(desktopElement.type === 'range' ? 'input' : 'change', (e) => {
                handler(e);
                // Sync to mobile version
                if (mobileElement) {
                    if (mobileElement.type === 'checkbox') {
                        mobileElement.checked = e.target.checked;
                    } else {
                        mobileElement.value = e.target.value;
                    }
                }
            });
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

    addCallToStream(call, isNew = false, isSubscribedCall = null) {
        const streamContainer = document.getElementById('call-stream');
        const emptyState = document.getElementById('empty-state');
        const callElement = this.createCallElement(call, isNew, isSubscribedCall);
        
        // Hide empty state when adding calls
        if (emptyState) {
            emptyState.style.display = 'none';
        }
        
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
        const isSubscribed = isSubscribedCall !== null ? isSubscribedCall : this.app.subscriptions.has(call.talkgroupId);
        
        // Handle both formats: main calls API has 'recordings' array, talkgroup API has 'recordingCount'
        const hasRecordings = (call.recordings && call.recordings.length > 0) || (call.recordingCount && call.recordingCount > 0);
        const recordingCount = call.recordings ? call.recordings.length : (call.recordingCount || 0);
        
        const duration = call.duration ? this.app.utils.formatDuration(call.duration) : 'Unknown';
        const relativeTime = this.app.utils.formatRelativeTime(call.recordingTime);
        const formattedFrequency = this.app.utils.formatFrequency(call.frequency);
        const talkGroupInfo = this.app.dataManager.getTalkGroupInfo(call.talkgroupId);
        const priorityClass = this.app.utils.getPriorityClass(talkGroupInfo?.priority);
        const recordingQuality = this.app.utils.getRecordingQuality(call.recordings);
        const ageClass = this.app.utils.getAgeClass(call.recordingTime);
        
        const callElement = document.createElement('div');
        callElement.className = `call-item${isNew ? ' new-call' : ''}${isSubscribed ? ' subscribed' : ''}${priorityClass ? ` ${priorityClass}` : ''} ${ageClass}`;
        callElement.dataset.callId = call.id;
        callElement.dataset.talkgroupId = call.talkgroupId;

        callElement.innerHTML = `
            <div class="call-container">
                <div class="call-main-content">
                    <h6 class="call-title">
                        ${talkGroupInfo?.description || `Talk Group ${call.talkgroupId}`}
                    </h6>
                    ${talkGroupInfo?.category || talkGroupInfo?.tag ? `
                    <div class="call-tags mb-2">
                        ${talkGroupInfo?.category ? `<span class="badge bg-secondary me-1">${talkGroupInfo.category}</span>` : ''}
                        ${talkGroupInfo?.tag ? `<span class="badge bg-info">${talkGroupInfo.tag}</span>` : ''}
                    </div>
                    ` : ''}
                    
                    <div class="call-details">
                        <div class="call-detail-item">
                            <i class="bi bi-broadcast-pin"></i>
                            <strong>${call.talkgroupId}</strong>
                        </div>
                        
                        <div class="call-detail-item">
                            <i class="bi bi-broadcast"></i>
                            ${formattedFrequency}
                        </div>
                        
                        <div class="call-detail-item">
                            <i class="bi bi-clock"></i>
                            ${duration}
                        </div>
                        
                        ${hasRecordings ? `
                        <div class="call-detail-item">
                            <i class="bi bi-file-earmark-music"></i>
                            ${recordingCount} files
                            ${recordingQuality ? `<span class="badge bg-success ms-1">${recordingQuality}</span>` : ''}
                        </div>
                        ` : ''}
                    </div>
                </div>
                
                <div class="call-meta-info">
                    <div class="call-time">
                        ${relativeTime}
                    </div>
                </div>
                
                <div class="call-actions">
                    <button type="button" class="btn btn-outline-info btn-talkgroup btn-sm" 
                            onclick="app.viewTalkgroupStream('${call.talkgroupId}')"
                            title="View all calls from this talk group">
                        <i class="bi bi-list-ul"></i>
                    </button>
                    <button type="button" class="btn btn-outline-success btn-subscribe btn-sm ${isSubscribed ? 'd-none' : ''}" 
                            onclick="app.toggleSubscription('${call.talkgroupId}', this)"
                            title="Subscribe to this talk group">
                        <i class="bi bi-bookmark-plus"></i>
                    </button>
                    <button type="button" class="btn btn-outline-danger btn-unsubscribe btn-sm ${!isSubscribed ? 'd-none' : ''}" 
                            onclick="app.toggleSubscription('${call.talkgroupId}', this)"
                            title="Unsubscribe from this talk group">
                        <i class="bi bi-bookmark-dash"></i>
                    </button>
                    ${hasRecordings ? `
                        <button type="button" class="btn btn-outline-success btn-play btn-sm" 
                                data-call='${JSON.stringify(call)}'
                                title="Play recording">
                            <i class="bi bi-play-fill"></i>
                        </button>
                    ` : ''}
                </div>
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
            
            // Preserve age class - remove any existing age classes and reapply current age
            const callId = element.dataset.callId;
            if (callId) {
                // Find the call data to recalculate age
                const call = this.app.activeCalls.get(parseInt(callId));
                if (call) {
                    // Remove existing age classes
                    element.classList.remove('age-fresh', 'age-recent', 'age-medium', 'age-old', 'age-very-old');
                    // Add current age class
                    const ageClass = this.app.utils.getAgeClass(call.recordingTime);
                    element.classList.add(ageClass);
                }
            }
        });
    }

    updateCallCardsForTalkGroup(talkgroupId) {
        // Find all call elements for this talk group and refresh them
        const callElements = document.querySelectorAll(`[data-talkgroup-id="${talkgroupId}"]`);
        
        callElements.forEach(element => {
            const callId = element.dataset.callId;
            const call = this.app.activeCalls.get(parseInt(callId));
            if (call) {
                const newElement = this.createCallElement(call);
                element.replaceWith(newElement);
            }
        });
    }

    updateCallAges() {
        // Update age classes for all call cards
        const callElements = document.querySelectorAll('.call-item[data-call-id]');
        
        callElements.forEach(element => {
            const callId = element.dataset.callId;
            const call = this.app.activeCalls.get(parseInt(callId));
            if (call) {
                // Remove existing age classes
                element.classList.remove('age-fresh', 'age-recent', 'age-medium', 'age-old', 'age-very-old');
                // Add current age class
                const ageClass = this.app.utils.getAgeClass(call.recordingTime);
                element.classList.add(ageClass);
            }
        });
    }

    startAgeUpdateTimer() {
        // Update ages every 5 minutes
        setInterval(() => {
            this.updateCallAges();
        }, 5 * 60 * 1000);
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

    updateSubscriptionsDisplay() {
        // Update both mobile and desktop versions
        const containers = [
            document.getElementById('subscribed-list'),
            document.getElementById('subscribed-list-mobile'),
            document.getElementById('subscribed-list-desktop')
        ].filter(el => el); // Remove null elements
        
        const content = this.generateSubscriptionsContent();
        
        containers.forEach(container => {
            container.innerHTML = content;
        });

        // Update subscription count - mobile accordion badge only
        const count = this.app.subscriptions.size;
        const mobileSubscriptionsElement = document.getElementById('mobile-subscriptions-count');
        if (mobileSubscriptionsElement) mobileSubscriptionsElement.textContent = count;
    }

    generateSubscriptionsContent() {
        if (this.app.subscriptions.size === 0) {
            return '<div class="text-muted small">No subscriptions</div>';
        } else {
            return Array.from(this.app.subscriptions).map(talkGroupId => {
                const talkGroupInfo = this.app.dataManager.getTalkGroupInfo(talkGroupId);
                const displayText = talkGroupInfo 
                    ? `${talkGroupId} - ${talkGroupInfo.description || talkGroupInfo.alphaTag || 'Unknown'}`
                    : `Talk Group ${talkGroupId}`;
                
                return `
                <div class="subscribed-item">
                    <span class="subscribed-talkgroup" title="${displayText}">${displayText}</span>
                    <button type="button" class="btn btn-outline-danger btn-unsubscribe btn-sm" 
                            onclick="app.toggleSubscription('${talkGroupId}', this)">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            `;
            }).join('');
        }
    }

    updateStatistics() {
        // Update subscriptions count - mobile accordion badge only
        const subscriptions = this.app.subscriptions.size;
        const mobileSubscriptionsElement = document.getElementById('mobile-subscriptions-count');
        if (mobileSubscriptionsElement) mobileSubscriptionsElement.textContent = subscriptions;

        this.updateQueueDisplay();
    }

    updateQueueDisplay() {
        const queueLength = this.app.audioManager.getQueueLength();
        
        // Update queue count - mobile accordion badge only
        const mobileQueueElement = document.getElementById('mobile-queue-count');
        if (mobileQueueElement) mobileQueueElement.textContent = queueLength;
        
        // Update queue lists
        const queueListElements = [
            document.getElementById('queue-list'),
            document.getElementById('queue-list-mobile'),
            document.getElementById('queue-list-desktop')
        ].filter(el => el); // Remove null elements
        
        const content = this.generateQueueContent();
        
        queueListElements.forEach(element => {
            element.innerHTML = content;
        });
    }

    generateQueueContent() {
        const queue = this.app.audioManager.getQueue();
        if (queue.length === 0) {
            return '<div class="text-muted small">No calls queued</div>';
        } else {
            return queue.map((call, index) => {
                const talkGroupInfo = this.app.dataManager.getTalkGroupInfo(call.talkgroupId);
                const displayText = talkGroupInfo 
                    ? `${call.talkgroupId} - ${talkGroupInfo.description || talkGroupInfo.alphaTag || 'Unknown'}`
                    : `TG ${call.talkgroupId}`;
                
                return `
                <div class="queue-item">
                    <span class="queue-position">${index + 1}.</span>
                    <span class="queue-talkgroup" title="${displayText}">${displayText}</span>
                    <span class="queue-time">${this.app.utils.formatDateTime(call.recordingTime)}</span>
                    <button type="button" class="btn btn-outline-danger btn-sm" 
                            onclick="app.removeFromQueue(${index})">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            `;
            }).join('');
        }
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

    // Talkgroup-specific view methods
    showTalkgroupView(talkgroupId, isLoading = false, calls = null) {
        const talkGroupInfo = this.app.dataManager.getTalkGroupInfo(talkgroupId);
        const talkGroupName = talkGroupInfo?.description || `Talk Group ${talkgroupId}`;
        
        // Hide main stream controls and show talkgroup view
        const mainContent = document.querySelector('.row .col-md-9');
        
        if (isLoading) {
            mainContent.innerHTML = `
                <div class="talkgroup-view">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <div>
                            <h4><i class="bi bi-broadcast-pin me-2"></i>${talkGroupName}</h4>
                            <p class="text-muted mb-0">Talk Group ID: ${talkgroupId}</p>
                        </div>
                        <button type="button" class="btn btn-outline-secondary" onclick="app.returnToMainStream()">
                            <i class="bi bi-arrow-left me-1"></i>Back to Live Stream
                        </button>
                    </div>
                    <div class="text-center py-5">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        <p class="mt-3">Loading calls for ${talkGroupName}...</p>
                    </div>
                </div>
            `;
            return;
        }

        if (calls) {
            mainContent.innerHTML = `
                <div class="talkgroup-view">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <div>
                            <h4><i class="bi bi-broadcast-pin me-2"></i>${talkGroupName}</h4>
                            <div class="d-flex flex-wrap gap-2 align-items-center">
                                <span class="text-muted">Talk Group ID: ${talkgroupId}</span>
                                ${talkGroupInfo?.category ? `<span class="badge bg-secondary">${talkGroupInfo.category}</span>` : ''}
                                ${talkGroupInfo?.tag ? `<span class="badge bg-info">${talkGroupInfo.tag}</span>` : ''}
                                <span class="badge bg-primary">${calls.length} calls</span>
                            </div>
                        </div>
                        <button type="button" class="btn btn-outline-secondary" onclick="app.returnToMainStream()">
                            <i class="bi bi-arrow-left me-1"></i>Back to Live Stream
                        </button>
                    </div>
                    
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <div class="card">
                                <div class="card-body">
                                    <h6 class="card-title">Statistics</h6>
                                    <div class="row">
                                        <div class="col-6">
                                            <div class="text-center">
                                                <h5 class="mb-1">${calls.length}</h5>
                                                <small class="text-muted">Total Calls</small>
                                            </div>
                                        </div>
                                        <div class="col-6">
                                            <div class="text-center">
                                                <h5 class="mb-1">${calls.reduce((sum, call) => sum + call.recordingCount, 0)}</h5>
                                                <small class="text-muted">Recordings</small>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="card">
                                <div class="card-body">
                                    <h6 class="card-title">Actions</h6>
                                    <div class="d-flex gap-2">
                                        <button type="button" class="btn btn-outline-primary btn-sm" 
                                                onclick="app.loadTalkgroupView('${talkgroupId}')">
                                            <i class="bi bi-arrow-clockwise me-1"></i>Refresh
                                        </button>
                                        <button type="button" class="btn btn-outline-success btn-sm ${this.app.subscriptions.has(talkgroupId) ? 'd-none' : ''}" 
                                                onclick="app.toggleSubscription('${talkgroupId}', this)">
                                            <i class="bi bi-bookmark-plus me-1"></i>Subscribe
                                        </button>
                                        <button type="button" class="btn btn-outline-danger btn-sm ${!this.app.subscriptions.has(talkgroupId) ? 'd-none' : ''}" 
                                                onclick="app.toggleSubscription('${talkgroupId}', this)">
                                            <i class="bi bi-bookmark-dash me-1"></i>Unsubscribe
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="card">
                        <div class="card-header">
                            <h6 class="mb-0">Recent Calls</h6>
                        </div>
                        <div class="card-body p-0">
                            <div id="talkgroup-call-stream" class="call-stream">
                                ${calls.length === 0 ? `
                                    <div class="text-center py-5">
                                        <i class="bi bi-inbox display-4 text-muted"></i>
                                        <h5 class="mt-3">No calls found</h5>
                                        <p class="text-muted mb-3">This talk group hasn't had any recent activity.</p>
                                        <button type="button" class="btn btn-outline-primary btn-sm" 
                                                onclick="app.loadTalkgroupView('${talkgroupId}')">
                                            <i class="bi bi-arrow-clockwise me-1"></i>Refresh
                                        </button>
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    </div>
                </div>
            `;

            // Add calls to the talkgroup stream
            const talkgroupStream = document.getElementById('talkgroup-call-stream');
            calls.forEach(call => {
                const callElement = this.createCallElement(call, false);
                talkgroupStream.appendChild(callElement);
            });
        }
    }

    showMainView() {
        // Restore the original main view content
        const mainContent = document.querySelector('.row .col-md-9');
        
        mainContent.innerHTML = `
            <!-- Call Stream -->
            <div class="d-flex justify-content-between align-items-center mb-3">
                <h4>Live Call Stream</h4>
                <div class="btn-group" role="group">
                    <button type="button" class="btn btn-outline-primary btn-sm" id="refresh-calls">
                        <i class="bi bi-arrow-clockwise"></i>
                        Refresh
                    </button>
                    <button type="button" class="btn btn-outline-warning btn-sm" id="clear-stream">
                        <i class="bi bi-trash"></i>
                        Clear
                    </button>
                </div>
            </div>

            <div class="card">
                <div class="card-header">
                    <div class="d-flex justify-content-between align-items-center">
                        <span>Recent Calls</span>
                        <div class="d-flex align-items-center">
                            <span class="badge bg-primary me-2">
                                <span id="calls-count">0</span> calls
                            </span>
                            <span class="badge bg-secondary">
                                <span id="recordings-count">0</span> recordings
                            </span>
                        </div>
                    </div>
                </div>
                <div class="card-body p-0">
                    <div id="call-stream" class="call-stream">
                        <!-- Calls will be populated here -->
                    </div>
                </div>
            </div>
        `;
        
        // Re-setup event listeners for the restored elements
        document.getElementById('refresh-calls').addEventListener('click', () => {
            this.app.dataManager.loadRecentCalls();
        });
        
        document.getElementById('clear-stream').addEventListener('click', () => {
            this.app.clearCallStream();
        });
        
        // Reload recent calls
        this.app.dataManager.loadRecentCalls();
    }

    clearCallStream() {
        document.getElementById('call-stream').innerHTML = '';
    }
}
