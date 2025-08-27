// UI Management - DOM manipulation and display logic
export class UIManager {
    // Dynamically update transcription display for mobile
    updateTranscriptionDisplay(call) {
        // Check for transcribed recordings
        const transcribedRecordings = call.recordings?.filter(r => r.hasTranscription) || [];
        const transcriptionMobile = document.getElementById('transcription-mobile');
        if (!transcriptionMobile) return;
        if (transcribedRecordings.length === 0) {
            transcriptionMobile.textContent = 'No transcription available';
            return;
        }

        // Get best transcription
        const bestTranscription = transcribedRecordings.reduce((best, current) => {
            const currentConfidence = current.transcriptionConfidence || 0;
            const bestConfidence = best.transcriptionConfidence || 0;
            return currentConfidence > bestConfidence ? current : best;
        });

        const confidencePercent = Math.round((bestTranscription.transcriptionConfidence || 0) * 100);
        const transcriptionText = bestTranscription.transcriptionText || '';
        const lang = bestTranscription.transcriptionLanguage ? bestTranscription.transcriptionLanguage.toUpperCase() : '';

        // Use HTML-escaped transcription to avoid corrupting the DOM when special characters are present
        transcriptionMobile.innerHTML =
            `<div><strong>Transcription:</strong> ${this.escapeHtml(transcriptionText)}</div>` +
            `<div><span class='badge bg-info'>${this.escapeHtml(lang)}</span> <span class='badge bg-success'>${confidencePercent}%</span></div>`;
    }
    constructor(app) {
        this.app = app;
        // Inject responsive styles so the call card action column isn't a fixed 220px on small screens.
        // Mobile: main content 60% / actions 40% (3/5 - 2/5). Desktop: keep 220px action column.
        this.injectResponsiveStyles();
    }

    injectResponsiveStyles() {
        try {
            if (document.getElementById('sr-call-responsive-styles')) return;
            const style = document.createElement('style');
            style.id = 'sr-call-responsive-styles';
            style.textContent = `
                /* Call card responsive layout */
                .call-container { display: flex; align-items: flex-start; gap: .75rem; }

                /* Ensure the actions column has space on the right so a vertical scrollbar
                   doesn't overlap the action buttons. Use box-sizing so padding is included
                   in the fixed width on desktop. */
                .call-container .call-actions { box-sizing: border-box; padding-right: 0.75rem; }

                /* Mobile-first: use 60% / 40% split for main/actions */
                @media (max-width: 767.98px) {
                    .call-container .call-main-content { flex: 0 0 60%; }
                    .call-container .call-actions { flex: 0 0 40%; padding-right: 0.75rem; }
                }

                /* Desktop: allow main to grow and keep actions as 220px fixed */
                @media (min-width: 768px) {
                    .call-container .call-main-content { flex: 1 1 auto; }
                    .call-container .call-actions { flex: 0 0 220px; padding-right: 1rem; }
                }
            `;
            document.head.appendChild(style);
        } catch (e) {
            // If DOM isn't ready, try again later silently
            setTimeout(() => this.injectResponsiveStyles(), 200);
        }
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
                        const json = this.decodeCallData(callData);
                        let call = JSON.parse(json);

                        // Normalize server-side PascalCase properties if present
                        // Ensure we have recordings available; talkgroup list endpoints only include RecordingCount/Formats
                        const hasRecordingsArray = call.recordings && call.recordings.length > 0;
                        const hasRecordingCount = (call.recordingCount && call.recordingCount > 0) || (call.RecordingCount && call.RecordingCount > 0);

                        // If we're in a talkgroup-specific Recent Calls view, queue the clicked call
                        // and also queue all calls that appear above it so playback will continue
                        // until the view is caught up.
                        const talkgroupStream = button.closest('#talkgroup-call-stream');

                        // Helper to fetch/normalize full call details when only a recordingCount is present
                        const ensureHasRecordings = async (c) => {
                            const hasRecArray = c.recordings && c.recordings.length > 0;
                            const hasRecCount = (c.recordingCount && c.recordingCount > 0) || (c.RecordingCount && c.RecordingCount > 0);
                            if (hasRecArray) return c;
                            if (!hasRecCount) return c;

                            try {
                                const id = c.id || c.Id;
                                if (!id) throw new Error('Call id missing');
                                const resp = await fetch(`/api/calls/${id}`);
                                if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
                                const payload = await resp.json();

                                return {
                                    id: payload.id || payload.Id,
                                    talkgroupId: payload.talkgroupId || payload.TalkgroupId || payload.TalkgroupID || payload.TalkGroupId,
                                    systemName: payload.systemName || payload.SystemName,
                                    recordingTime: payload.recordingTime || payload.RecordingTime,
                                    frequency: payload.frequency || payload.Frequency,
                                    duration: payload.duration || payload.Duration,
                                    createdAt: payload.createdAt || payload.CreatedAt,
                                    updatedAt: payload.updatedAt || payload.UpdatedAt,
                                    recordings: (payload.recordings || payload.Recordings || []).map(r => ({
                                        id: r.id || r.Id,
                                        fileName: r.fileName || r.FileName || r.File,
                                        format: (r.format || r.Format || '').toUpperCase(),
                                        fileSize: r.fileSize || r.FileSize || 0,
                                        isUploaded: r.isUploaded || r.IsUploaded || false,
                                        blobName: r.blobName || r.BlobName || r.blobUri || r.BlobUri || null,
                                        uploadedAt: r.uploadedAt || r.UploadedAt || r.createdAt || r.CreatedAt || null,
                                        hasTranscription: r.hasTranscription || r.HasTranscription || false,
                                        transcriptionText: r.transcriptionText || r.TranscriptionText || null,
                                        transcriptionConfidence: r.transcriptionConfidence || r.TranscriptionConfidence || null,
                                        transcriptionLanguage: r.transcriptionLanguage || r.TranscriptionLanguage || null
                                    }))
                                };
                            } catch (err) {
                                console.error('Failed to fetch call details for playback:', err);
                                return null;
                            }
                        };

                        if (talkgroupStream && this.app.autoPlay) {
                            // We're in talkgroup view: queue clicked call first, then queue calls above it
                            (async () => {
                                try {
                                    // Queue the clicked call (ensure it has recordings)
                                    const mainCall = await ensureHasRecordings(call);
                                    if (mainCall) this.app.audioManager.queueCall(mainCall);

                                    // Find the call card and collect preceding call cards (nearest above first)
                                    const currentCard = button.closest('.call-item');
                                    let prev = currentCard ? currentCard.previousElementSibling : null;
                                    const aboveCards = [];
                                    while (prev) {
                                        if (prev.classList && prev.classList.contains('call-item')) {
                                            aboveCards.push(prev);
                                        }
                                        prev = prev.previousElementSibling;
                                    }

                                    // Queue each above card in the order collected (nearest above plays first)
                                    for (const card of aboveCards) {
                                        const playBtn = card.querySelector('.btn-play');
                                        if (!playBtn) continue;
                                        const callDataAttr = playBtn.dataset.call;
                                        if (!callDataAttr) continue;
                                        try {
                                            const decoded = JSON.parse(this.decodeCallData(callDataAttr));
                                            const resolved = await ensureHasRecordings(decoded);
                                            if (resolved) this.app.audioManager.queueCall(resolved);
                                        } catch (e) {
                                            console.warn('Failed to decode/queue call from talkgroup list', e);
                                        }
                                    }
                                } catch (e) {
                                    console.error('Failed to queue talkgroup calls for continuous play', e);
                                    this.showToast('Failed to queue talkgroup calls', 'error');
                                }
                            })();
                        } else {
                            // Not in talkgroup view: behave as before
                            if (!hasRecordingsArray && hasRecordingCount) {
                                // If we only have a recording count but not the recordings array, fetch full call details
                                (async () => {
                                    try {
                                        const id = call.id || call.Id;
                                        if (!id) throw new Error('Call id missing');
                                        const resp = await fetch(`/api/calls/${id}`);
                                        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
                                        const payload = await resp.json();

                                        // Normalize payload to expected shape (lowercase keys)
                                        const normalized = {
                                            id: payload.id || payload.Id,
                                            talkgroupId: payload.talkgroupId || payload.TalkgroupId || payload.TalkgroupID || payload.TalkGroupId,
                                            systemName: payload.systemName || payload.SystemName,
                                            recordingTime: payload.recordingTime || payload.RecordingTime,
                                            frequency: payload.frequency || payload.Frequency,
                                            duration: payload.duration || payload.Duration,
                                            createdAt: payload.createdAt || payload.CreatedAt,
                                            updatedAt: payload.updatedAt || payload.UpdatedAt,
                                            recordings: (payload.recordings || payload.Recordings || []).map(r => ({
                                                id: r.id || r.Id,
                                                fileName: r.fileName || r.FileName || r.File, 
                                                format: (r.format || r.Format || '').toUpperCase(),
                                                fileSize: r.fileSize || r.FileSize || 0,
                                                isUploaded: r.isUploaded || r.IsUploaded || false,
                                                blobName: r.blobName || r.BlobName || r.blobUri || r.BlobUri || null,
                                                uploadedAt: r.uploadedAt || r.UploadedAt || r.createdAt || r.CreatedAt || null,
                                                hasTranscription: r.hasTranscription || r.HasTranscription || false,
                                                transcriptionText: r.transcriptionText || r.TranscriptionText || null,
                                                transcriptionConfidence: r.transcriptionConfidence || r.TranscriptionConfidence || null,
                                                transcriptionLanguage: r.transcriptionLanguage || r.TranscriptionLanguage || null
                                            }))
                                        };

                                        this.app.playCall(normalized);
                                    } catch (fetchErr) {
                                        console.error('Failed to fetch call details for playback:', fetchErr);
                                        this.showToast('Failed to load recording details', 'error');
                                    }
                                })();
                            } else {
                                // We already have recordings embedded (or none at all), attempt to play directly
                                this.app.playCall(call);
                            }
                        }
                    } catch (error) {
                        console.error('Failed to parse call data:', error);
                        this.showToast('Failed to play recording', 'error');
                    }
                }
                e.preventDefault();
                return false;
            }

            // Handle share button clicks
            if (e.target.closest('.btn-share')) {
                const btn = e.target.closest('.btn-share');
                const callId = btn.dataset.callId;
                if (!callId) {
                    this.showToast('Unable to share: call id missing', 'error');
                    return;
                }

                try {
                    const url = new URL(window.location.href);
                    // remove talkgroup param if present to make a direct play link
                    url.searchParams.delete('talkgroup');
                    url.searchParams.set('call', String(callId));

                    const shareUrl = url.toString();

                    // Try navigator.share first for native share UIs
                    if (navigator.share) {
                        navigator.share({ title: 'SignalRadio - Play Call', url: shareUrl }).catch(() => {});
                        this.showToast('Share dialog opened', 'success');
                        return;
                    }

                    // Fallback: copy to clipboard
                    if (navigator.clipboard && navigator.clipboard.writeText) {
                        navigator.clipboard.writeText(shareUrl).then(() => {
                            this.showToast('Call link copied to clipboard', 'success');
                        }).catch((err) => {
                            console.warn('Clipboard write failed', err);
                            // Fall back to prompt
                            window.prompt('Copy this link to share the call:', shareUrl);
                        });
                    } else {
                        // Old browsers: prompt with the URL
                        window.prompt('Copy this link to share the call:', shareUrl);
                    }
                } catch (err) {
                    console.error('Failed to create share link', err);
                    this.showToast('Failed to create share link', 'error');
                }

                e.preventDefault();
                return false;
            }
        });

        // Handle transcription "show more" toggles via delegation
        document.addEventListener('click', (e) => {
            const toggle = e.target.closest('.transcription-toggle');
            if (!toggle) return;
            e.preventDefault();

            const container = toggle.closest('.transcription-text');
            if (!container) return;

            const full = container.querySelector('.transcription-full');
            const truncated = toggle.previousElementSibling && toggle.previousElementSibling.tagName === 'SMALL' ? toggle.previousElementSibling : null;

            const state = toggle.getAttribute('data-state');
            if (state === 'truncated') {
                // Show full, hide truncated and update button
                if (truncated) truncated.style.display = 'none';
                if (full) full.style.display = 'block';
                toggle.setAttribute('data-state', 'full');
                toggle.innerHTML = '<small>Show less</small>';
            } else {
                // Collapse back to truncated
                if (truncated) truncated.style.display = '';
                if (full) full.style.display = 'none';
                toggle.setAttribute('data-state', 'truncated');
                toggle.innerHTML = '<small>Show more</small>';
            }
        });

        // Persist mobile 'Controls' accordion state across page loads
        try {
            const STORAGE_KEY = 'sr.controlsCollapse.expanded';
            const controlsId = 'controlsCollapse';
            const controlsCollapseEl = document.getElementById(controlsId);
            const toggleButtons = Array.from(document.querySelectorAll(`[data-bs-target="#${controlsId}"], [data-target="#${controlsId}"]`));

            if (controlsCollapseEl) {
                // Initialize from storage (default: expanded)
                const stored = localStorage.getItem(STORAGE_KEY);
                const expanded = stored === null ? true : (stored === 'true');

                if (expanded) {
                    controlsCollapseEl.classList.add('show');
                } else {
                    controlsCollapseEl.classList.remove('show');
                }

                // Reflect state on any toggle buttons
                toggleButtons.forEach(btn => {
                    if (expanded) {
                        btn.classList.remove('collapsed');
                        btn.setAttribute('aria-expanded', 'true');
                    } else {
                        btn.classList.add('collapsed');
                        btn.setAttribute('aria-expanded', 'false');
                    }
                });

                // Persist changes when Bootstrap emits collapse events
                controlsCollapseEl.addEventListener('shown.bs.collapse', () => {
                    try { localStorage.setItem(STORAGE_KEY, 'true'); } catch (e) {}
                    toggleButtons.forEach(btn => { btn.classList.remove('collapsed'); btn.setAttribute('aria-expanded', 'true'); });
                });
                controlsCollapseEl.addEventListener('hidden.bs.collapse', () => {
                    try { localStorage.setItem(STORAGE_KEY, 'false'); } catch (e) {}
                    toggleButtons.forEach(btn => { btn.classList.add('collapsed'); btn.setAttribute('aria-expanded', 'false'); });
                });
            }
        } catch (e) {
            // ignore storage errors (e.g., private mode)
        }
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
        
        if (!streamContainer) return;

        if (isNew) {
            streamContainer.insertBefore(callElement, streamContainer.firstChild);
        } else {
            streamContainer.appendChild(callElement);
        }

        // Enforce maximum number of call cards in the live stream to avoid memory/DOM bloat
        const MAX_CALL_CARDS = 100;
        const callItems = streamContainer.querySelectorAll('.call-item');
        if (callItems.length > MAX_CALL_CARDS) {
            // remove oldest cards from the end until we are at the cap
            for (let i = callItems.length - 1; i >= MAX_CALL_CARDS; i--) {
                const el = callItems[i];
                if (el && el.parentNode) el.parentNode.removeChild(el);
            }
        }

        // Remove new-call class after animation
        if (isNew) {
            setTimeout(() => {
                callElement.classList.remove('new-call');
            }, 500);
        }
    }

    createCallElement(call, isNew = false, isSubscribedCall = null, minimal = false) {
        // Determine subscription status
        const isSubscribed = isSubscribedCall !== null ? isSubscribedCall : this.app.subscriptions.has(call.talkgroupId);
        
        // Handle both formats: main calls API has 'recordings' array, talkgroup API has 'recordingCount'
        const hasRecordings = (call.recordings && call.recordings.length > 0) || (call.recordingCount && call.recordingCount > 0);
        const recordingCount = call.recordings ? call.recordings.length : (call.recordingCount || 0);
        const duration = call.duration ? this.app.utils.formatDuration(call.duration) : 'Unknown';
        const relativeTime = this.app.utils.formatRelativeTime(call.createdAt);
        const formattedFrequency = this.app.utils.formatFrequency(call.frequency);
        const talkGroupInfo = this.app.dataManager.getTalkGroupInfo(call.talkgroupId);
        const priorityClass = this.app.utils.getPriorityClass(talkGroupInfo?.priority);
        const recordingQuality = this.app.utils.getRecordingQuality(call.recordings);
        const ageClass = this.app.utils.getAgeClass(call.createdAt);
        
    const callElement = document.createElement('div');
        callElement.className = `call-item${isNew ? ' new-call' : ''}${isSubscribed ? ' subscribed' : ''}${priorityClass ? ` ${priorityClass}` : ''} ${ageClass}`;
        callElement.dataset.callId = call.id;
        callElement.dataset.talkgroupId = call.talkgroupId;

        // Build a cleaner, focused call card layout. When `minimal` is true (talkgroup view)
        // render a slim card: no badges, truncated meta, and show a first-recording summary if available.
        const formattedDateTime = this.app.utils.formatDateTime(call.createdAt);

        // Prepare file/recording info for display in minimal mode
        let fileInfoHtml = '';
        let headerHtml = '';
        if (call.recordings && call.recordings.length > 0) {
            const r = call.recordings[0];
            const fileName = r.fileName || r.FileName || r.file || r.File || 'unknown';
            const formatUpper = (r.format || r.Format || '').toUpperCase() || (r.fileName && r.fileName.split('.').pop().toUpperCase()) || '';
            const fileSize = r.fileSize || r.FileSize || 0;
            const sizeDisplay = fileSize ? this.app.utils.formatFileSize ? this.app.utils.formatFileSize(fileSize) : `${Math.round(fileSize/1024)} KB` : '';

            fileInfoHtml = `
                <div class="call-file-info small text-muted">
                    <div><strong>File:</strong> ${this.escapeHtml(fileName)}${formatUpper ? ` <span class="text-uppercase">(${this.escapeHtml(formatUpper)})</span>` : ''}</div>
                    ${sizeDisplay ? `<div><strong>Size:</strong> ${this.escapeHtml(sizeDisplay)}</div>` : ''}
                </div>
            `;
            // If we're in minimal (talkgroup) mode, use the filename as the header to break up the stream
            headerHtml = this.escapeHtml(fileName);
        } else if (recordingCount > 0) {
            fileInfoHtml = `<div class="call-file-info small text-muted"><strong>Recordings:</strong> ${recordingCount}</div>`;
        }

        callElement.innerHTML = `
            <div class="call-container">
                <div class="call-main-content" style="padding-right: 0.75rem;">
                    <div>
                        ${minimal ? (headerHtml ? `<h6 class="call-title mb-0">${headerHtml}</h6>` : '') : `<h6 class="call-title mb-0">${talkGroupInfo?.description || `Talk Group ${call.talkgroupId}`}</h6>`}
                    </div>

                    ${minimal ? `
                        <div class="call-meta mb-2 text-muted small">
                            <div>${relativeTime}</div>
                            <div><strong>Dur:</strong> ${duration}</div>
                            ${fileInfoHtml}
                        </div>
                    ` : `
                        <div class="call-bottom-badges mt-2">
                            ${talkGroupInfo?.category ? `<span class="badge bg-secondary me-1">${talkGroupInfo.category}</span>` : ''}
                            ${talkGroupInfo?.tag ? `<span class="badge bg-info">${talkGroupInfo.tag}</span>` : ''}
                        </div>

                        <div class="call-meta mb-2 text-muted small">
                            <div><strong></strong> ${relativeTime}</div>
                            <div><strong>Dur:</strong> ${duration}</div>
                            <div><strong>TG:</strong> ${call.talkgroupId}</div>
                            ${formattedFrequency ? `<div><strong>Freq:</strong> ${formattedFrequency}</div>` : ''}
                        </div>
                    `}

                </div>

                <div class="call-actions d-flex flex-column gap-2 align-items-end">
                    <div class="w-100 d-flex flex-column">
                        ${hasRecordings ? `
                            <button type="button" class="btn btn-primary btn-play w-100 mb-2" 
                                    data-call='${this.encodeCallData(JSON.stringify(call))}' title="Play recording">
                                <i class="bi bi-play-fill"></i>
                                <span class="ms-1">Play</span>
                            </button>
                            <button type="button" class="btn btn-outline-primary btn-share btn-wide w-100 mb-2" 
                                    data-call-id='${this.escapeHtml(String(call.id))}' title="Share this call">
                                <i class="bi bi-share"></i>
                                <span class="ms-1">Share</span>
                            </button>
                        ` : ''}
                        ${minimal ? '' : `
                        <button type="button" class="btn btn-outline-secondary btn-wide w-100 mb-2" 
                                onclick="app.viewTalkgroupStream('${call.talkgroupId}')" title="Open talkgroup view">
                            <i class="bi bi-list-ul"></i>
                            <span class="ms-1">View</span>
                        </button>

                        <div class="d-flex w-100 gap-2">
                            <button type="button" class="btn btn-outline-success btn-subscribe btn-wide flex-fill ${isSubscribed ? 'd-none' : ''}" 
                                    onclick="app.toggleSubscription('${call.talkgroupId}', this)" title="Subscribe to this talk group">
                                <i class="bi bi-bookmark-plus"></i>
                                <span class="ms-1">Subscribe</span>
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-unsubscribe btn-wide flex-fill ${!isSubscribed ? 'd-none' : ''}" 
                                    onclick="app.toggleSubscription('${call.talkgroupId}', this)" title="Unsubscribe from this talk group">
                                <i class="bi bi-bookmark-dash"></i>
                                <span class="ms-1">Unsubscribe</span>
                            </button>
                        </div>
                        `}
                    </div>
                </div>
            </div>

            <!-- Transcription row spans both the main content and actions -->
            ${this.createTranscriptionSection(call, minimal) ? `
                <div class="call-transcription-row mt-2">
                    ${this.createTranscriptionSection(call, minimal)}
                </div>
            ` : ''}

            <div id="audio-controls-${call.id}" class="audio-controls d-none mt-2">
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

    createTranscriptionSection(call, minimal = false) {
        // Check if any recordings have transcriptions
        const transcribedRecordings = call.recordings?.filter(r => r.hasTranscription) || [];
        
        if (transcribedRecordings.length === 0) {
            return ''; // No transcriptions available
        }

        // Get the best transcription (highest confidence)
        const bestTranscription = transcribedRecordings.reduce((best, current) => {
            const currentConfidence = current.transcriptionConfidence || 0;
            const bestConfidence = best.transcriptionConfidence || 0;
            return currentConfidence > bestConfidence ? current : best;
        });

        if (!bestTranscription.transcriptionText) {
            return ''; // No transcription text available
        }

        const confidencePercent = Math.round((bestTranscription.transcriptionConfidence || 0) * 100);
        // Only show confidence/language badges when not in minimal mode
        const confidenceClass = confidencePercent >= 80 ? 'bg-success' : 
                               confidencePercent >= 60 ? 'bg-warning text-dark' : 'bg-danger';

        // Truncate long transcriptions for the card view
        const maxLength = 150;
        const transcriptionText = bestTranscription.transcriptionText;
        const truncatedText = transcriptionText.length > maxLength ? 
            transcriptionText.substring(0, maxLength) + '...' : transcriptionText;

        return `
            <div class="transcription-section mt-2">
                <div class="d-flex align-items-center mb-1">
                    <i class="bi bi-chat-quote text-primary me-1"></i>
                    <small class="text-muted me-2">Transcription</small>
                </div>

                ${minimal ? '' : `
                <!-- Badges row: placed under the label and above the transcription text -->
                <div class="transcription-badges mb-1">
                ${bestTranscription.transcriptionLanguage ? 
                        `<span class="badge bg-info badge-sm">${bestTranscription.transcriptionLanguage.toUpperCase()}</span>` : ''}    
                <span class="badge ${confidenceClass} badge-sm me-1">${confidencePercent}% confidence</span>
                </div>
                `}

                <div class="transcription-text border-start border-primary border-2 ps-2 py-1 bg-light">
                    <small class="text-dark">${this.escapeHtml(truncatedText)}</small>
                    ${transcriptionText.length > maxLength ? `
                        <button type="button" class="btn btn-link btn-sm p-0 ms-1 transcription-toggle" 
                                data-state="truncated" title="Show full transcription">
                            <small>Show more</small>
                        </button>
                        <div class="transcription-full" style="display: none;">
                            <small class="text-dark">${this.escapeHtml(transcriptionText)}</small>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Encode JSON string to base64 (Unicode-safe) for embedding in data attributes
    encodeCallData(jsonString) {
        try {
            // Use URL-safe base64 to avoid spaces/quotes issues
            const utf8Bytes = new TextEncoder().encode(jsonString);
            let base64String = btoa(String.fromCharCode(...utf8Bytes));
            return base64String.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
        } catch (e) {
            console.warn('Failed to encode call data, falling back to URI encoding', e);
            return encodeURIComponent(jsonString);
        }
    }

    // Decode call data produced by encodeCallData; accepts url-encoded fallback as well
    decodeCallData(data) {
        try {
            // Detect URL-encoded fallback
            if (data.indexOf('%') !== -1) {
                return decodeURIComponent(data);
            }

            // Recreate standard base64 from URL-safe base64
            let base64 = data.replace(/-/g, '+').replace(/_/g, '/');
            // Pad base64 string length
            while (base64.length % 4) base64 += '=';
            const binary = atob(base64);
            // Convert binary string to Uint8Array
            const bytes = new Uint8Array([...binary].map(ch => ch.charCodeAt(0)));
            return new TextDecoder().decode(bytes);
        } catch (e) {
            console.error('Failed to decode call data', e);
            throw e;
        }
    }

    updateCallInStream(call) {
        const existingElement = document.querySelector(`[data-call-id="${call.id}"]`);
        if (existingElement) {
            const inTalkgroupStream = existingElement.closest('#talkgroup-call-stream') !== null;
            const newElement = this.createCallElement(call, false, null, inTalkgroupStream);
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
                    const ageClass = this.app.utils.getAgeClass(call.createdAt);
                    element.classList.add(ageClass);
                }
            }
        });

        // Also update any talkgroup view header buttons if present
        try {
            const headerSubscribe = document.getElementById('talkgroup-subscribe-btn');
            const headerUnsubscribe = document.getElementById('talkgroup-unsubscribe-btn');
            if (headerSubscribe && headerUnsubscribe) {
                if (isSubscribed) {
                    headerSubscribe.classList.add('d-none');
                    headerUnsubscribe.classList.remove('d-none');
                } else {
                    headerSubscribe.classList.remove('d-none');
                    headerUnsubscribe.classList.add('d-none');
                }
            }
        } catch (e) {
            // ignore
        }
    }

    updateCallCardsForTalkGroup(talkgroupId) {
        // Find all call elements for this talk group and refresh them
        const callElements = document.querySelectorAll(`[data-talkgroup-id="${talkgroupId}"]`);
        
        callElements.forEach(element => {
            const callId = element.dataset.callId;
            const call = this.app.activeCalls.get(parseInt(callId));
            if (call) {
                const inTalkgroupStream = element.closest('#talkgroup-call-stream') !== null;
                const newElement = this.createCallElement(call, false, null, inTalkgroupStream);
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
                const ageClass = this.app.utils.getAgeClass(call.createdAt);
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
        // Update live stream title to the talkgroup description for this call (if available)
        try {
            let call = null;
            if (this.app && this.app.activeCalls) {
                // Try direct lookup (handles string IDs), then numeric lookup
                call = this.app.activeCalls.get(callId);
                if (!call) {
                    const num = parseInt(callId);
                    if (!isNaN(num)) call = this.app.activeCalls.get(num);
                }
            }

            // As a final fallback, try to find the DOM element and get its dataset
            if (!call) {
                const el = document.querySelector(`[data-call-id="${callId}"]`);
                if (el && this.app && this.app.activeCalls) {
                    const ds = el.dataset.callId;
                    call = this.app.activeCalls.get(ds) || this.app.activeCalls.get(parseInt(ds));
                }
            }

            if (call && call.talkgroupId) {
                this.updateLiveStreamTitle(call.talkgroupId);
            }
        } catch (e) {
            // ignore
        }
    }

    hideAudioControls(callId) {
        const controlsElement = document.getElementById(`audio-controls-${callId}`);
        if (controlsElement) {
            controlsElement.classList.add('d-none');
        }
        // If another call is currently playing, keep title in sync with it; otherwise reset
        try {
            const currentId = this.app.audioManager && this.app.audioManager.currentlyPlaying ? this.app.audioManager.currentlyPlaying : null;
            let currentCall = null;
            if (currentId && this.app && this.app.activeCalls) {
                currentCall = this.app.activeCalls.get(currentId) || this.app.activeCalls.get(parseInt(currentId));
            }

            if (currentCall && currentCall.talkgroupId) {
                this.updateLiveStreamTitle(currentCall.talkgroupId);
            } else {
                this.clearLiveStreamTitle();
            }
        } catch (e) {
            // ignore
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
            return '<div class="text-muted small">No talk groups subscribed</div>';
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
                    <span class="queue-time">${this.app.utils.formatDateTime(call.createdAt)}</span>
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
                        <div class="d-flex gap-2 align-items-center">
                            <button type="button" class="btn btn-outline-secondary" onclick="app.returnToMainStream()">
                                <i class="bi bi-arrow-left me-1"></i>Back to Live Stream
                            </button>
                            <button type="button" class="btn btn-outline-success btn-sm ${this.app.subscriptions.has(talkgroupId) ? 'd-none' : ''}" 
                                    id="talkgroup-subscribe-btn" onclick="app.toggleSubscription('${talkgroupId}', this)">
                                <i class="bi bi-bookmark-plus me-1"></i>Subscribe
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-sm ${!this.app.subscriptions.has(talkgroupId) ? 'd-none' : ''}" 
                                    id="talkgroup-unsubscribe-btn" onclick="app.toggleSubscription('${talkgroupId}', this)">
                                <i class="bi bi-bookmark-dash me-1"></i>Unsubscribe
                            </button>
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

            // Add calls to the talkgroup stream using minimal rendering (no per-call view/unsubscribe)
            const talkgroupStream = document.getElementById('talkgroup-call-stream');
            calls.forEach(call => {
                const callElement = this.createCallElement(call, false, null, true);
                talkgroupStream.appendChild(callElement);
            });
        }
    }

    // Call-specific view for deep links like ?call=ID
    showCallView(call) {
        const talkGroupInfo = this.app.dataManager.getTalkGroupInfo(call.talkgroupId);
        const talkGroupName = talkGroupInfo?.description || `Talk Group ${call.talkgroupId}`;

        const mainContent = document.querySelector('.row .col-md-9');

        const duration = call.duration ? this.app.utils.formatDuration(call.duration) : 'Unknown';
        const created = this.app.utils.formatDateTime(call.createdAt);

        // Prepare recording info (first recording if present)
        let recordingHtml = '<div class="text-muted small">No recordings available</div>';
        if (call.recordings && call.recordings.length > 0) {
            const r = call.recordings[0];
            const fileName = r.fileName || r.FileName || r.file || r.File || 'unknown';
            const formatUpper = (r.format || r.Format || '').toUpperCase() || (r.fileName && r.fileName.split('.').pop().toUpperCase()) || '';
            const sizeDisplay = r.fileSize ? (this.app.utils.formatFileSize ? this.app.utils.formatFileSize(r.fileSize) : `${Math.round(r.fileSize/1024)} KB`) : '';

            recordingHtml = `
                <div class="call-file-info small text-muted">
                    <div><strong>File:</strong> ${this.escapeHtml(fileName)} ${formatUpper ? `<span class="text-uppercase">(${this.escapeHtml(formatUpper)})</span>` : ''}</div>
                    ${sizeDisplay ? `<div><strong>Size:</strong> ${this.escapeHtml(sizeDisplay)}</div>` : ''}
                </div>
            `;
        }

        mainContent.innerHTML = `
            <div class="call-view">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <div>
                        <h4><i class="bi bi-broadcast-pin me-2"></i>${this.escapeHtml(talkGroupName)}</h4>
                        <p class="text-muted mb-0">Talk Group ID: ${this.escapeHtml(String(call.talkgroupId))}</p>
                        <p class="text-muted small mb-0">Recorded: ${this.escapeHtml(created)} &middot; Duration: ${this.escapeHtml(duration)}</p>
                    </div>
                    <div>
                        <button type="button" class="btn btn-outline-secondary" onclick="app.returnToMainStream()">
                            <i class="bi bi-arrow-left me-1"></i>Back
                        </button>
                    </div>
                </div>

                <div class="card">
                    <div class="card-body text-center">
                        <div class="mb-3">
                            ${recordingHtml}
                        </div>
                        <div class="mb-2">
                            <!-- Play button requires user interaction; do not autoplay -->
                            <button type="button" class="btn btn-primary btn-lg btn-play" 
                                    data-call='${this.encodeCallData(JSON.stringify(call))}'>
                                <i class="bi bi-play-fill"></i>
                                <span class="ms-1">Play</span>
                            </button>
                        </div>

                        ${this.createTranscriptionSection(call, false) ? `
                            <div class="mt-3 text-start">${this.createTranscriptionSection(call, false)}</div>
                        ` : ''}
                    </div>
                </div>
            </div>
        `;
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

    // Update the live stream header to show the talkgroup description for the given talkgroup
    updateLiveStreamTitle(talkgroupId) {
        // Try to find the primary title element by id
        let titleEl = document.getElementById('call-stream-title');

        // If not found, try to find the visible card header title
        if (!titleEl) {
            titleEl = document.querySelector('.col-md-9 .card-header .card-title');
        }

        if (!titleEl) return;

        const display = (() => {
            if (!talkgroupId) return 'Live Call Stream';
            const talkGroupInfo = this.app.dataManager.getTalkGroupInfo(talkgroupId);
            return talkGroupInfo?.description || `Talk Group ${talkgroupId}`;
        })();

        // Preserve any leading icon HTML if present
        const iconEl = titleEl.querySelector('i');
        const iconHtml = iconEl ? iconEl.outerHTML + ' ' : '';

        // Safe-escape display text
        const escapeHtml = (text) => {
            const d = document.createElement('div');
            d.textContent = text;
            return d.innerHTML;
        };

        titleEl.innerHTML = iconHtml + escapeHtml(display);
    }

    // Clear the live stream title back to the default when nothing is playing
    clearLiveStreamTitle() {
    let titleEl = document.getElementById('call-stream-title');
    if (!titleEl) titleEl = document.querySelector('.col-md-9 .card-header .card-title');
    if (!titleEl) return;

    const iconEl = titleEl.querySelector('i');
    const iconHtml = iconEl ? iconEl.outerHTML + ' ' : '';
    titleEl.innerHTML = iconHtml + 'Live Call Stream';
    }

    clearCallStream() {
        document.getElementById('call-stream').innerHTML = '';
    }
}
