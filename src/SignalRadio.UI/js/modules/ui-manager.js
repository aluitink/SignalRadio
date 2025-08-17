// UI Management - DOM manipulation and display logic
export class UIManager {
    constructor(app) {
        this.app = app;
    }

    setupEventListeners() {
        // Auto-play toggle
        document.getElementById('auto-play-toggle').addEventListener('change', (e) => {
            this.app.autoPlay = e.target.checked;
            this.app.settingsManager.saveSettings();
            
            if (this.app.autoPlay) {
                if (this.app.audioManager.userHasInteracted) {
                    this.showToast('Auto-play enabled', 'success');
                } else {
                    this.showToast('Auto-play enabled - click anywhere to activate', 'info');
                }
            } else {
                this.showToast('Auto-play disabled', 'info');
            }
        });

        // Volume control
        document.getElementById('volume-control').addEventListener('input', (e) => {
            this.app.audioManager.setVolume(e.target.value);
            this.app.settingsManager.saveSettings();
        });

        // Clear subscriptions button
        document.getElementById('clear-subscriptions').addEventListener('click', () => {
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
        const callElement = this.createCallElement(call, isNew, isSubscribedCall);
        
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
        const hasRecordings = call.recordings && call.recordings.length > 0;
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
                            ${call.recordingCount || 0} files
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
                                onclick="app.playCall(${JSON.stringify(call).replace(/"/g, '&quot;')})"
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
        const container = document.getElementById('subscribed-list');
        
        if (this.app.subscriptions.size === 0) {
            container.innerHTML = '<div class="text-muted small">No subscriptions</div>';
        } else {
            container.innerHTML = Array.from(this.app.subscriptions).map(talkGroupId => `
                <div class="subscribed-item">
                    <span class="subscribed-talkgroup">Talk Group ${talkGroupId}</span>
                    <button type="button" class="btn btn-outline-danger btn-unsubscribe btn-sm" 
                            onclick="app.toggleSubscription('${talkGroupId}', this)">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            `).join('');
        }

        document.getElementById('subscriptions-count').textContent = this.app.subscriptions.size;
    }

    updateStatistics() {
        document.getElementById('active-calls-count').textContent = this.app.activeCalls.size;
        document.getElementById('total-calls-count').textContent = this.app.totalCallsReceived;
        this.updateQueueDisplay();
    }

    updateQueueDisplay() {
        const queueCountElement = document.getElementById('queue-count');
        if (queueCountElement) {
            queueCountElement.textContent = this.app.audioManager.getQueueLength();
        }
        
        const queueListElement = document.getElementById('queue-list');
        if (queueListElement) {
            const queue = this.app.audioManager.getQueue();
            if (queue.length === 0) {
                queueListElement.innerHTML = '<div class="text-muted small">No calls queued</div>';
            } else {
                queueListElement.innerHTML = queue.map((call, index) => `
                    <div class="queue-item">
                        <span class="queue-position">${index + 1}.</span>
                        <span class="queue-talkgroup">TG ${call.talkgroupId}</span>
                        <span class="queue-time">${this.app.utils.formatDateTime(call.recordingTime)}</span>
                        <button type="button" class="btn btn-outline-danger btn-sm" 
                                onclick="app.removeFromQueue(${index})">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                `).join('');
            }
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

    clearCallStream() {
        document.getElementById('call-stream').innerHTML = '';
    }
}
