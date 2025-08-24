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
            const talkGroupSelect = document.getElementById('talkgroup-filter');
            
            // Store talk group data
            data.talkGroups.forEach(tg => {
                this.talkGroups.set(tg.id, tg);
                
                const option = document.createElement('option');
                option.value = tg.id;
                option.textContent = `${tg.id} - ${tg.description || 'Unknown'}`;
                talkGroupSelect.appendChild(option);
            });
            
            console.log(`Loaded ${data.talkGroups.length} talk groups for filtering`);
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
        card.className = 'card mb-3';

        const talkGroupInfo = this.talkGroups.get(result.call.talkgroupId);
        const recordingTime = new Date(result.call.recordingTime);
        const relativeTime = this.utils.formatRelativeTime(recordingTime);
        const formattedFrequency = this.utils.formatFrequency(result.call.frequency);

        // Highlight search terms in transcription
        const highlightedText = this.highlightSearchTerms(result.transcriptionText, this.currentQuery);

        const confidencePercent = Math.round((result.transcriptionConfidence || 0) * 100);
        const confidenceClass = result.transcriptionConfidence >= 0.8 ? 'bg-success' : 
                               result.transcriptionConfidence >= 0.6 ? 'bg-warning' : 'bg-danger';

        card.innerHTML = `
            <div class="call-container p-3">
                <div class="call-main-content">
                    <h6 class="call-title mb-1">${talkGroupInfo?.description || `Talk Group ${result.call.talkgroupId}`}</h6>
                    <div class="call-meta small text-muted">
                        <div>${relativeTime}</div>
                        <div><strong>TG:</strong> ${result.call.talkgroupId}</div>
                    </div>
                </div>

                <div class="call-actions d-flex flex-column gap-2 align-items-stretch" style="flex: 0 0 220px; max-width: 220px;">
                    <div class="w-100 d-flex flex-column">
                        ${result.blobUri ? `
                            <button type="button" class="btn btn-primary btn-play w-100 mb-2" onclick="searchApp.playRecording('${result.blobUri}', '${result.fileName}')" title="Play recording">
                                <i class="bi bi-play-fill"></i>
                                <span class="ms-1">Play</span>
                            </button>
                        ` : ''}

                        <button type="button" class="btn btn-outline-secondary btn-wide w-100 mb-2" 
                                onclick="window.open('index.html?talkgroup=${result.call.talkgroupId}', '_blank')" title="View talkgroup view">
                            <i class="bi bi-list-ul"></i>
                            <span class="ms-1">View</span>
                        </button>
                    </div>
                </div>
            </div>

            <!-- Transcription row spans both main content and actions -->
            <div class="call-transcription-row mt-2 p-3">
                <div class="transcription-section">
                    <div class="d-flex align-items-center mb-1">
                        <i class="bi bi-chat-quote text-primary me-1"></i>
                        <small class="text-muted me-2">Transcription</small>
                        <span class="badge ${confidenceClass} badge-sm">${confidencePercent}% confidence</span>
                        ${result.transcriptionLanguage ? `<span class="badge bg-info badge-sm ms-1">${result.transcriptionLanguage.toUpperCase()}</span>` : ''}
                    </div>
                    <div class="transcription-text border-start border-primary border-2 ps-2 py-1 bg-light">
                        <p class="mb-0">${highlightedText}</p>
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

    playRecording(blobUri, fileName) {
        // Create audio element and play
        const audio = new Audio(blobUri);
        audio.play().catch(error => {
            console.error('Failed to play audio:', error);
            this.showToast('Failed to play recording', 'error');
        });
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
