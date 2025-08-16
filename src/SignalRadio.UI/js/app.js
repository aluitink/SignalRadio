// Configuration
// Use relative URLs when served through nginx proxy, absolute for development
const API_BASE_URL = window.location.origin.includes('localhost:3000') 
    ? 'http://localhost:5000'   // Development: direct API access
    : '';                       // Production: nginx proxy

const SIGNALR_HUB_URL = `${API_BASE_URL}/hubs/talkgroups`;

// Global state
let connection = null;
let subscribedGroups = new Set();
let audioPlayer = null;
let isAutoPlayEnabled = true;

// Initialize the application
document.addEventListener('DOMContentLoaded', async function() {
    initializeUI();
    await initializeSignalR();
    loadRecentCalls();
});

// Initialize UI components
function initializeUI() {
    audioPlayer = document.getElementById('audioPlayer');
    
    // Setup volume control
    const volumeSlider = document.getElementById('volumeSlider');
    const volumeDisplay = document.getElementById('volumeDisplay');
    
    volumeSlider.addEventListener('input', function() {
        const volume = this.value;
        volumeDisplay.textContent = `${volume}%`;
        if (audioPlayer) {
            audioPlayer.volume = volume / 100;
        }
    });
    
    // Setup auto-play checkbox
    const autoPlayCheckbox = document.getElementById('autoPlay');
    autoPlayCheckbox.addEventListener('change', function() {
        isAutoPlayEnabled = this.checked;
    });
    
    // Setup enter key for talk group input
    const talkgroupInput = document.getElementById('talkgroupInput');
    talkgroupInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            subscribeTalkGroup();
        }
    });
    
    // Initialize audio player volume
    audioPlayer.volume = 0.5;
}

// Initialize SignalR connection
async function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl(SIGNALR_HUB_URL)
        .withAutomaticReconnect()
        .build();

    // Setup event handlers
    connection.on('SubscriptionConfirmed', function(talkGroupId) {
        console.log(`Subscription confirmed for talk group: ${talkGroupId}`);
        updateGroupDisplay();
    });

    connection.on('UnsubscriptionConfirmed', function(talkGroupId) {
        console.log(`Unsubscription confirmed for talk group: ${talkGroupId}`);
        subscribedGroups.delete(talkGroupId);
        updateGroupDisplay();
    });

    connection.on('NewCall', function(call) {
        console.log('New call received:', call);
        displayNewCall(call);
        
        if (isAutoPlayEnabled && call.recordings && call.recordings.length > 0) {
            playCallAudio(call.recordings[0]);
        }
    });

    // Connection state change handlers
    connection.onclose(function() {
        updateConnectionStatus('disconnected', 'Disconnected');
    });

    connection.onreconnecting(function() {
        updateConnectionStatus('connecting', 'Reconnecting...');
    });

    connection.onreconnected(function() {
        updateConnectionStatus('connected', 'Connected');
        // Re-subscribe to all groups after reconnection
        resubscribeAll();
    });

    // Start the connection
    try {
        updateConnectionStatus('connecting', 'Connecting...');
        await connection.start();
        updateConnectionStatus('connected', 'Connected');
        console.log('SignalR connection established');
    } catch (err) {
        console.error('SignalR connection failed:', err);
        updateConnectionStatus('disconnected', 'Connection failed');
    }
}

// Subscribe to a talk group
async function subscribeTalkGroup() {
    const input = document.getElementById('talkgroupInput');
    const talkGroupId = input.value.trim();
    
    if (!talkGroupId) {
        alert('Please enter a talk group ID');
        return;
    }
    
    if (subscribedGroups.has(talkGroupId)) {
        alert('Already subscribed to this talk group');
        return;
    }
    
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        alert('Not connected to server. Please wait for connection.');
        return;
    }
    
    try {
        await connection.invoke('SubscribeToTalkGroup', talkGroupId);
        subscribedGroups.add(talkGroupId);
        input.value = '';
        updateGroupDisplay();
        console.log(`Subscribed to talk group: ${talkGroupId}`);
    } catch (err) {
        console.error('Failed to subscribe:', err);
        alert('Failed to subscribe to talk group');
    }
}

// Unsubscribe from a talk group
async function unsubscribeTalkGroup(talkGroupId) {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        return;
    }
    
    try {
        await connection.invoke('UnsubscribeFromTalkGroup', talkGroupId);
        subscribedGroups.delete(talkGroupId);
        updateGroupDisplay();
        console.log(`Unsubscribed from talk group: ${talkGroupId}`);
    } catch (err) {
        console.error('Failed to unsubscribe:', err);
    }
}

// Re-subscribe to all groups (after reconnection)
async function resubscribeAll() {
    for (const talkGroupId of subscribedGroups) {
        try {
            await connection.invoke('SubscribeToTalkGroup', talkGroupId);
        } catch (err) {
            console.error(`Failed to re-subscribe to ${talkGroupId}:`, err);
        }
    }
}

// Update connection status display
function updateConnectionStatus(status, text) {
    const indicator = document.getElementById('statusIndicator');
    const statusText = document.getElementById('statusText');
    
    indicator.className = `status-indicator ${status}`;
    statusText.textContent = text;
}

// Update subscribed groups display
function updateGroupDisplay() {
    const groupsList = document.getElementById('groupsList');
    
    if (subscribedGroups.size === 0) {
        groupsList.innerHTML = '<p class="no-subscriptions">No talk groups subscribed yet</p>';
        return;
    }
    
    const groupsHtml = Array.from(subscribedGroups).map(talkGroupId => `
        <div class="group-item">
            <div class="group-info">
                <div class="group-id">Talk Group ${talkGroupId}</div>
                <div class="group-status">Active</div>
            </div>
            <button class="unsubscribe-btn" onclick="unsubscribeTalkGroup('${talkGroupId}')">
                Unsubscribe
            </button>
        </div>
    `).join('');
    
    groupsList.innerHTML = groupsHtml;
}

// Display a new call in the live feed
function displayNewCall(call) {
    const callsFeed = document.getElementById('callsFeed');
    
    // Remove "no calls" message if present
    const noCallsMsg = callsFeed.querySelector('.no-calls');
    if (noCallsMsg) {
        noCallsMsg.remove();
    }
    
    const callElement = createCallElement(call, true);
    callsFeed.insertBefore(callElement, callsFeed.firstChild);
    
    // Keep only the most recent 20 calls in the live feed
    const callItems = callsFeed.querySelectorAll('.call-item');
    if (callItems.length > 20) {
        callItems[callItems.length - 1].remove();
    }
}

// Create a call element
function createCallElement(call, isNew = false) {
    const div = document.createElement('div');
    div.className = `call-item ${isNew ? 'new-call' : ''}`;
    
    const recordingTime = new Date(call.recordingTime);
    const duration = call.duration ? formatDuration(call.duration) : 'Unknown';
    
    div.innerHTML = `
        <div class="call-header">
            <div class="call-talkgroup">Talk Group ${call.talkgroupId}</div>
            <div class="call-time">${recordingTime.toLocaleString()}</div>
        </div>
        <div class="call-details">
            System: ${call.systemName} | Frequency: ${call.frequency} | Duration: ${duration}
        </div>
        <div class="call-actions">
            ${call.recordings && call.recordings.length > 0 ? 
                `<button class="play-btn" onclick="playCallAudio(${JSON.stringify(call.recordings[0]).replace(/"/g, '&quot;')})">
                    ▶ Play
                </button>` : 
                '<span style="color: #666;">No recording available</span>'
            }
        </div>
    `;
    
    return div;
}

// Play audio for a call recording
async function playCallAudio(recording) {
    if (!recording || !recording.blobName) {
        alert('No audio file available for this call');
        return;
    }
    
    try {
        // Get the audio file URL from the API
        const audioUrl = `${API_BASE_URL}/api/Recording/stream/${recording.id}`;
        
        audioPlayer.src = audioUrl;
        await audioPlayer.play();
        
        console.log(`Playing audio for recording: ${recording.fileName}`);
    } catch (err) {
        console.error('Failed to play audio:', err);
        alert('Failed to play audio file');
    }
}

// Load recent calls from the API
async function loadRecentCalls() {
    const recentCallsContainer = document.getElementById('recentCalls');
    recentCallsContainer.innerHTML = '<p class="loading">Loading recent calls...</p>';
    
    try {
        const response = await fetch(`${API_BASE_URL}/api/calls?limit=20`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        const calls = data.calls || [];
        
        if (calls.length === 0) {
            recentCallsContainer.innerHTML = '<p class="no-calls">No recent calls found</p>';
            return;
        }
        
        const callsHtml = calls.map(call => createCallElement(call).outerHTML).join('');
        recentCallsContainer.innerHTML = callsHtml;
        
    } catch (err) {
        console.error('Failed to load recent calls:', err);
        recentCallsContainer.innerHTML = '<p class="error">Failed to load recent calls</p>';
    }
}

// Utility function to format duration
function formatDuration(duration) {
    // Duration comes as "00:00:30.1234567" format from C#
    const parts = duration.split(':');
    if (parts.length >= 3) {
        const seconds = parseFloat(parts[2]);
        const minutes = parseInt(parts[1]);
        const hours = parseInt(parts[0]);
        
        if (hours > 0) {
            return `${hours}h ${minutes}m ${Math.floor(seconds)}s`;
        } else if (minutes > 0) {
            return `${minutes}m ${Math.floor(seconds)}s`;
        } else {
            return `${Math.floor(seconds)}s`;
        }
    }
    return duration;
}
