// Search Application for SignalRadio
import { Utils } from './modules/utils.js';

class SearchApp {
    constructor() {
        this.utils = new Utils();
        this.currentPage = 1;
        this.pageSize = 20;
        this.currentQuery = '';
        this.currentFilters = {};
        this.talkGroups = new Map();
        
        this.initializeApp();
    }

    async initializeApp() {
        try {
            console.log('Initializing Search App...');
            
            // Load talk group data for filters
            await this.loadTalkGroupData();
            
            // Setup event listeners
            this.setupEventListeners();
            
            // Check for URL parameters
            this.loadFromUrlParams();
            
            console.log('Search App initialization complete');
        } catch (error) {
            console.error('Failed to initialize search app:', error);
            this.showToast('App initialization failed', 'error');
        }
    }

    async loadTalkGroupData() {
        try {
            const response = await fetch('/api/talkgroup');
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            
            const data = await response.json();

            // API may return an array or an object. Normalize to an array of talk groups.
            let talkGroupsData = [];
            if (Array.isArray(data)) {
                talkGroupsData = data;
            } else if (data && data.talkGroups) {
                talkGroupsData = data.talkGroups;
            } else if (data && data.TalkGroups) {
                talkGroupsData = data.TalkGroups;
            }

            const talkGroupSelect = document.getElementById('talkgroup-filter');
            if (!talkGroupSelect) {
                console.debug('Talkgroup select element not found; skipping talk group population');
                return;
            }

            // Store talk group data (normalize PascalCase and camelCase)
            talkGroupsData.forEach(tg => {
                const id = tg.id || tg.Id || tg.Decimal || tg.decimal;
                const description = tg.description || tg.Description || tg.AlphaTag || tg.alphaTag || 'Unknown';

                if (!id) return;

                this.talkGroups.set(String(id), tg);

                const option = document.createElement('option');
                option.value = id;
                option.textContent = `${id} - ${description || 'Unknown'}`;
                talkGroupSelect.appendChild(option);
            });

            console.log(`Loaded ${talkGroupsData.length} talk groups for filtering`);
        } catch (error) {
            console.error('Failed to load talk group data:', error);
        }
    }

    setupEventListeners() {
        // Search form submission
        document.getElementById('search-form').addEventListener('submit', (e) => {
            e.preventDefault();
            this.performSearch();
        });

        // Clear search
        document.getElementById('clear-search').addEventListener('click', () => {
            this.clearSearch();
        });

        // Pagination
        document.getElementById('prev-page').addEventListener('click', () => {
            if (this.currentPage > 1) {
                this.currentPage--;
                this.performSearch();
            }
        });

        document.getElementById('next-page').addEventListener('click', () => {
            this.currentPage++;
            this.performSearch();
        });

        // Real-time search on input (debounced)
        let searchTimeout;
        document.getElementById('search-query').addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();
            
            if (query.length >= 3) {
                searchTimeout = setTimeout(() => {
                    this.performSearch();
                }, 500); // 500ms debounce
            } else if (query.length === 0) {
                this.clearResults();
            }
        });

        // Filter changes
        document.getElementById('talkgroup-filter').addEventListener('change', () => {
            if (this.currentQuery) {
                this.currentPage = 1;
                this.performSearch();
            }
        });

        document.getElementById('date-range').addEventListener('change', () => {
            if (this.currentQuery) {
                this.currentPage = 1;
                this.performSearch();
            }
        });

        // Delegate play button clicks in search results: support data-call payloads like UIManager
        document.addEventListener('click', (e) => {
            const btn = e.target.closest && e.target.closest('.btn-play');
            if (!btn) return;

            const dataCall = btn.getAttribute('data-call');
            if (!dataCall) return; // if no data-call, other handlers may handle it

            e.preventDefault();
            try {
                const json = this.decodeCallData(dataCall);
                const call = JSON.parse(json);
                // prefer the first recording
                const rec = (call.recordings && call.recordings[0]) || null;
                if (!rec) {
                    this.showToast('No recording available to play', 'warning');
                    return;
                }

                const id = rec.id || rec.Id || null;
                const blobUri = rec.blobUri || rec.BlobUri || rec.blobName || rec.BlobName || '';
                const fileName = rec.fileName || rec.FileName || '';

                this.playRecording(id, blobUri, fileName);
            } catch (err) {
                console.error('Failed to decode/play call from search result', err);
                this.showToast('Failed to play recording', 'error');
            }
        });
    }

    // Decode call data produced by UIManager.encodeCallData; accepts URL-encoded fallback as well
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

    loadFromUrlParams() {
        const urlParams = new URLSearchParams(window.location.search);
        const query = urlParams.get('q');
        const talkGroup = urlParams.get('talkgroup');
        const dateRange = urlParams.get('dateRange');

        if (query) {
            document.getElementById('search-query').value = query;
        }
        if (talkGroup) {
            document.getElementById('talkgroup-filter').value = talkGroup;
        }
        if (dateRange) {
            document.getElementById('date-range').value = dateRange;
        }

        if (query && query.length >= 3) {
            this.performSearch();
        }
    }

    async performSearch() {
        const query = document.getElementById('search-query').value.trim();
        
        if (query.length < 3) {
            this.showToast('Search query must be at least 3 characters long', 'warning');
            return;
        }

        this.currentQuery = query;
        this.updateUrlParams();
        
        const talkGroupId = document.getElementById('talkgroup-filter').value;
        const dateRange = document.getElementById('date-range').value;
        
        // Calculate date filters
        let startDate = null;
        if (dateRange) {
            const days = parseInt(dateRange);
            startDate = new Date();
            startDate.setDate(startDate.getDate() - days);
        }

        this.showSearchStatus(true);
        this.hideAllStates();

        try {
            const params = new URLSearchParams({
                q: query,
                page: this.currentPage,
                pageSize: this.pageSize
            });

            if (talkGroupId) {
                params.append('talkGroupId', talkGroupId);
            }
            if (startDate) {
                params.append('startDate', startDate.toISOString());
            }

            const response = await fetch(`/api/search/transcriptions?${params}`);
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `HTTP ${response.status}`);
            }

            const data = await response.json();
            this.displayResults(data);
            
        } catch (error) {
            console.error('Search failed:', error);
            this.showToast(`Search failed: ${error.message}`, 'error');
            this.showNoResults();
        } finally {
            this.showSearchStatus(false);
        }
    }

    displayResults(data) {
        const resultsContainer = document.getElementById('search-results');
        const summaryElement = document.getElementById('results-summary');
        const searchSummary = document.getElementById('search-summary');
        
        // Update summary
        const totalResults = data.pagination.totalCount;
        const startResult = ((data.pagination.page - 1) * data.pagination.pageSize) + 1;
        const endResult = Math.min(data.pagination.page * data.pagination.pageSize, totalResults);
        
        summaryElement.textContent = `Showing ${startResult}-${endResult} of ${totalResults} results for "${data.query}"`;
        searchSummary.classList.remove('d-none');

        // Update pagination
        this.updatePagination(data.pagination);

        if (data.results.length === 0) {
            this.showNoResults();
            return;
        }

        // Create result cards
        resultsContainer.innerHTML = '';
        data.results.forEach(result => {
            const resultCard = this.createResultCard(result);
            resultsContainer.appendChild(resultCard);
        });

        document.getElementById('search-results').classList.remove('d-none');
    }

    createResultCard(result) {
    const card = document.createElement('div');
    // Use the same classes as live call stream cards so styles/behavior match
    card.className = 'call-item';
    const callId = result.call?.id || result.id;
    // Talkgroup id may be in different shapes: talkgroupId, talkGroupId, tg, talkgroup
    const talkgroupId = result.call?.talkgroupId || result.call?.talkGroupId || result.call?.tg || result.call?.talkgroup || result.talkgroupId || result.talkGroupId || result.tg || result.talkgroup;
        if (callId) card.dataset.callId = callId;
        if (talkgroupId) card.dataset.talkgroupId = talkgroupId;

        // Prefer the app-wide DataManager cache (same approach used by ui-manager)
        let talkGroupInfo = null;
        try {
            if (window.app && window.app.dataManager && typeof window.app.dataManager.getTalkGroupInfo === 'function') {
                talkGroupInfo = window.app.dataManager.getTalkGroupInfo(talkgroupId);
            }
        } catch (e) {
            // ignore
        }

        // Fallback to the local talkGroups map populated by this class (keys stored as strings)
        if (!talkGroupInfo) {
            talkGroupInfo = this.talkGroups.get(String(talkgroupId || '')) || this.talkGroups.get(Number(talkgroupId)) || null;
        }
    const recordingTime = new Date(result.call?.recordingTime || result.recordingTime || result.createdAt || Date.now());
        const relativeTime = this.utils.formatRelativeTime(recordingTime);
    const formattedFrequency = this.utils.formatFrequency(result.call && result.call.frequency ? result.call.frequency : result.frequency);

        // Highlight search terms in transcription (already escapes HTML)
        const highlightedText = this.highlightSearchTerms(result.transcriptionText, this.currentQuery);

    const confidencePercent = Math.round((result.transcriptionConfidence || 0) * 100);
    const confidenceClass = (result.transcriptionConfidence || 0) >= 0.8 ? 'bg-success' : 
                   (result.transcriptionConfidence || 0) >= 0.6 ? 'bg-warning' : 'bg-danger';

        // Build minimal call object compatible with UIManager's data-call payload
        const normalizedCall = {
            id: callId,
            talkgroupId: talkgroupId,
            recordingTime: result.call?.recordingTime || result.recordingTime || result.createdAt,
            frequency: result.call?.frequency || result.frequency,
            duration: this.utils.formatDuration(result.duration || (result.call && result.call.duration) || null),
            createdAt: result.createdAt || result.call?.createdAt || result.call?.recordingTime,
            recordings: [
                {
                    id: result.id,
                    fileName: result.fileName || result.file || null,
                    format: result.format || (result.fileName ? (result.fileName.split('.').pop() || '').toUpperCase() : ''),
                    fileSize: result.fileSize || 0,
                    isUploaded: !!result.blobUri,
                    blobName: result.blobName || null,
                    blobUri: result.blobUri || null,
                    hasTranscription: !!result.transcriptionText,
                    transcriptionText: result.transcriptionText,
                    transcriptionConfidence: result.transcriptionConfidence,
                    transcriptionLanguage: result.transcriptionLanguage
                }
            ]
        };

        // Encode call data using URL-safe base64 (same algorithm as UIManager.encodeCallData)
        const encodeCallData = (jsonString) => {
            try {
                const utf8Bytes = new TextEncoder().encode(jsonString);
                let base64String = btoa(String.fromCharCode(...utf8Bytes));
                return base64String.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
            } catch (e) {
                return encodeURIComponent(jsonString);
            }
        };

        const encodedCall = encodeCallData(JSON.stringify(normalizedCall));

        // Build markup closely matching UIManager.createCallElement for consistent appearance
        // Determine subscription state if the main app is available
        const isSubscribed = (window.app && window.app.subscriptions && window.app.subscriptions.has && window.app.subscriptions.has(String(talkgroupId))) || false;

        // Truncate long transcriptions similar to UIManager
        const fullTranscription = result.transcriptionText || '';
        const maxLength = 150;
        const truncated = fullTranscription.length > maxLength ? fullTranscription.substring(0, maxLength) + '...' : fullTranscription;
        const showToggle = fullTranscription.length > maxLength;

        card.innerHTML = `
            <div class="call-container">
                <div class="call-main-content" style="flex: 0 0 80%; padding-right: 0.75rem;">
                    <div>
                        <h6 class="call-title mb-0"><a href="#" onclick="app.viewTalkgroupStream('${talkgroupId}'); return false;" class="text-decoration-none">${talkGroupInfo?.description || `Talk Group ${talkgroupId}`}</a></h6>
                    </div>

                        <div class="call-meta mb-2 text-muted small">
                        <div>${relativeTime}</div>
                        <div><strong>Dur:</strong> ${this.utils.formatDuration(result.duration || (result.call && result.call.duration) || null)}</div>
                        <div><strong>TG:</strong> ${talkgroupId}</div>
                        ${formattedFrequency ? `<div><strong>Freq:</strong> ${formattedFrequency}</div>` : ''}
                    </div>
                </div>

                <div class="call-actions d-flex flex-column gap-2 align-items-end" style="flex: 0 0 20%; padding-right: 0.75rem;">
                    <div class="w-100 d-flex flex-column">
                        ${result.blobUri || result.id ? `
                            <button type="button" class="btn btn-primary btn-play w-100 mb-2" 
                                    data-call='${encodedCall}' title="Play recording">
                                <i class="bi bi-play-fill"></i>
                                <span class="ms-1">Play</span>
                            </button>
                        ` : ''}

                        ${result.blobUri || result.id ? `
                            <button type="button" class="btn btn-outline-primary btn-share btn-wide w-100 mb-2" 
                                    data-call-id='${this.utils ? (this.utils.escapeHtml ? this.utils.escapeHtml(String(result.id)) : String(result.id)) : String(result.id)}' title="Share this call">
                                <i class="bi bi-share"></i>
                                <span class="ms-1">Share</span>
                            </button>
                        ` : ''}

                        <div class="d-flex w-100 gap-3">
                            <button type="button" class="btn btn-outline-success btn-subscribe btn-wide flex-fill ${isSubscribed ? 'd-none' : ''}" 
                                    onclick="app.toggleSubscription('${talkgroupId}', this)" title="Subscribe to this talk group">
                                <i class="bi bi-bookmark-plus"></i>
                                <span class="ms-1">Subscribe</span>
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-unsubscribe btn-wide flex-fill ${!isSubscribed ? 'd-none' : ''}" 
                                    onclick="app.toggleSubscription('${talkgroupId}', this)" title="Unsubscribe from this talk group">
                                <i class="bi bi-bookmark-dash"></i>
                                <span class="ms-1">Unsubscribe</span>
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Transcription row spans both main content and actions -->
            <div class="call-transcription-row mt-2">
                <div class="transcription-section mt-2">
                    <div class="d-flex align-items-center mb-1">
                        <i class="bi bi-chat-quote text-primary me-1"></i>
                        <small class="text-muted me-2">Transcription</small>
                    </div>
                    <div class="transcription-badges mb-1">
                        ${result.transcriptionLanguage ? `<span class="badge bg-info badge-sm">${(result.transcriptionLanguage || '').toUpperCase()}</span>` : ''}
                        <span class="badge ${confidenceClass} badge-sm me-1">${confidencePercent}% confidence</span>
                    </div>
                    <div class="transcription-text border-start border-primary border-2 ps-2 py-1 bg-light">
                        <small class="text-dark">${this.highlightSearchTerms(truncated, this.currentQuery)}</small>
                        ${showToggle ? `
                            <button type="button" class="btn btn-link btn-sm p-0 ms-1 transcription-toggle" data-state="truncated" title="Show full transcription">
                                <small>Show more</small>
                            </button>
                            <div class="transcription-full" style="display: none;">
                                <small class="text-dark">${this.highlightSearchTerms(fullTranscription, this.currentQuery)}</small>
                            </div>
                        ` : ''}
                    </div>
                </div>
            </div>
        `;

        return card;
    }

    highlightSearchTerms(text, query) {
        if (!text || !query) return text || '';
        
        // Simple highlighting - escape HTML and highlight terms
        const escaped = text.replace(/[&<>"']/g, (m) => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        })[m]);
        
        // Highlight search terms (case insensitive)
        const terms = query.split(/\s+/).filter(term => term.length > 2);
        let highlighted = escaped;
        
        terms.forEach(term => {
            const regex = new RegExp(`(${term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
            highlighted = highlighted.replace(regex, '<mark>$1</mark>');
        });
        
        return highlighted;
    }

    updatePagination(pagination) {
        const prevBtn = document.getElementById('prev-page');
        const nextBtn = document.getElementById('next-page');
        
        prevBtn.disabled = !pagination.hasPreviousPage;
        nextBtn.disabled = !pagination.hasNextPage;
    }

    playRecording(recordingId, blobUri, fileName) {
        // recordingId may be null if not provided by the API
        // blobUri may be an https URL or a file:// path created by LocalDiskStorageService
        (async () => {
            try {
                let sourceUrl = null;

                // Prefer HTTPS/HTTP blob URIs served from object storage
                if (blobUri && /^https?:\/\//i.test(blobUri)) {
                    sourceUrl = blobUri;
                    console.debug('Using direct blob URL for playback', sourceUrl);
                }

                // If we don't have an http(s) URL but we have a recording id, use the server stream endpoint
                if (!sourceUrl && recordingId) {
                    sourceUrl = `/api/recording/stream/${recordingId}`;
                    console.debug('Falling back to server stream endpoint for playback', sourceUrl);
                }

                // Last resort: if blobUri looks like a file:// or absolute path, try to request the download endpoint with the encoded path
                if (!sourceUrl && blobUri) {
                    // Remove file:// prefix if present and try to use download route
                    const cleaned = blobUri.replace(/^file:\/\//i, '');
                    // Ensure we URL-encode the path segment
                    const encoded = encodeURIComponent(cleaned);
                    sourceUrl = `/api/recording/download/${encoded}`;
                    console.debug('Attempting download endpoint for playback', sourceUrl);
                }

                if (!sourceUrl) {
                    throw new Error('No playable URL available for this recording');
                }

                // Create audio element and play with proper error handling
                const audio = new Audio(sourceUrl);
                audio.crossOrigin = 'anonymous';
                await audio.play();
            } catch (error) {
                console.error('Failed to play audio:', error);
                this.showToast(`Failed to play recording: ${error.message || error}`, 'error');
            }
        })();
    }

    clearSearch() {
        document.getElementById('search-query').value = '';
        document.getElementById('talkgroup-filter').value = '';
        document.getElementById('date-range').value = '';
        this.clearResults();
        this.currentQuery = '';
        this.currentPage = 1;
        this.updateUrlParams();
    }

    clearResults() {
        this.hideAllStates();
        document.getElementById('initial-state').classList.remove('d-none');
    }

    showNoResults() {
        this.hideAllStates();
        document.getElementById('no-results').classList.remove('d-none');
    }

    showSearchStatus(show) {
        const statusElement = document.getElementById('search-status');
        if (show) {
            statusElement.classList.remove('d-none');
        } else {
            statusElement.classList.add('d-none');
        }
    }

    hideAllStates() {
        document.getElementById('search-results').classList.add('d-none');
        document.getElementById('search-summary').classList.add('d-none');
        document.getElementById('no-results').classList.add('d-none');
        document.getElementById('initial-state').classList.add('d-none');
    }

    updateUrlParams() {
        const params = new URLSearchParams();
        
        if (this.currentQuery) {
            params.set('q', this.currentQuery);
        }
        
        const talkGroup = document.getElementById('talkgroup-filter').value;
        if (talkGroup) {
            params.set('talkgroup', talkGroup);
        }
        
        const dateRange = document.getElementById('date-range').value;
        if (dateRange) {
            params.set('dateRange', dateRange);
        }

        const newUrl = params.toString() ? 
            `${window.location.pathname}?${params.toString()}` : 
            window.location.pathname;
            
        window.history.replaceState({}, '', newUrl);
    }

    showToast(message, type = 'info') {
        // Simple toast implementation
        const toastContainer = document.getElementById('toast-container') || this.createToastContainer();
        
        const toast = document.createElement('div');
        toast.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show`;
        toast.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        toastContainer.appendChild(toast);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 5000);
    }

    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1055';
        document.body.appendChild(container);
        return container;
    }
}

// Initialize the search app
const searchApp = new SearchApp();

// Make it globally available for inline event handlers
window.searchApp = searchApp;
