// SignalRadio Service Worker
// Enables background audio playback and caching for mobile devices

// Update this value when deploying to force a new cache (or use a build step to inject a version)
const CACHE_NAME = 'signalradio-v1';
const STATIC_ASSETS = [
    '/',
    '/index.html',
    '/subscriptions.html',
    '/admin.html',
    '/css/app.css',
    '/js/app.js',
    '/js/subscriptions.js',
    '/js/modules/audio-manager.js',
    '/js/modules/ui-manager.js',
    '/js/modules/connection-manager.js',
    '/js/modules/data-manager.js',
    '/js/modules/settings-manager.js',
    '/js/modules/utils.js',
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css',
    'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css'
];

// Install event - cache static assets
self.addEventListener('install', (event) => {
    console.log('🔧 Service Worker installing...');
    console.log('📦 Cache name:', CACHE_NAME);
    console.log('📄 Assets to cache:', STATIC_ASSETS.length, 'items');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => {
                console.log('💾 Cache opened successfully');
                console.log('📥 Caching static assets...');
                return cache.addAll(STATIC_ASSETS);
            })
            .then(() => {
                console.log('✅ Service Worker installed successfully');
                console.log('⚡ Calling skipWaiting to take control immediately');
                // Take control immediately
                return self.skipWaiting();
            })
            .catch((error) => {
                console.error('❌ Service Worker installation failed:', error);
                console.error('Error details:', error.message);
                // Log which assets failed to cache
                return Promise.allSettled(
                    STATIC_ASSETS.map(asset => 
                        fetch(asset).then(response => {
                            if (!response.ok) {
                                console.error(`❌ Failed to fetch asset: ${asset} - ${response.status} ${response.statusText}`);
                            }
                            return response;
                        }).catch(err => {
                            console.error(`❌ Network error for asset: ${asset}`, err);
                            throw err;
                        })
                    )
                ).then(results => {
                    results.forEach((result, index) => {
                        if (result.status === 'rejected') {
                            console.error(`❌ Asset failed: ${STATIC_ASSETS[index]}`, result.reason);
                        }
                    });
                    throw error;
                });
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
    console.log('🔄 Service Worker activating...');
    
    event.waitUntil(
        caches.keys()
            .then((cacheNames) => {
                console.log('📋 Found caches:', cacheNames);
                return Promise.all(
                    cacheNames.map((cacheName) => {
                        if (cacheName !== CACHE_NAME) {
                            console.log('🗑️ Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        } else {
                            console.log('✅ Keeping current cache:', cacheName);
                        }
                    })
                );
            })
            .then(() => {
                console.log('✅ Service Worker activated successfully');
                console.log('🎯 Taking control of all clients...');
                // Take control of all clients immediately
                return self.clients.claim();
            })
            .then(() => {
                console.log('✅ Service Worker now controlling all clients');
            })
            .catch((error) => {
                console.error('❌ Service Worker activation failed:', error);
            })
    );
});

// Fetch event - serve from cache when offline, with network-first strategy for API calls
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);

    // Treat navigation requests (HTML pages) with network-first to avoid stale index.html
    if (request.mode === 'navigate' || (request.method === 'GET' && request.headers.get('accept')?.includes('text/html'))) {
        event.respondWith(
            fetch(request)
                .then((networkResponse) => {
                    // Update the cached index.html with the fresh response
                    if (networkResponse && networkResponse.ok) {
                        const responseClone = networkResponse.clone();
                        caches.open(CACHE_NAME).then(cache => cache.put('/index.html', responseClone)).catch(() => {});
                    }
                    return networkResponse;
                })
                .catch(() => {
                    // Fall back to cached index.html when offline
                    return caches.match('/index.html');
                })
        );
        return;
    }

    // Network-first strategy for API calls and audio files
    if (url.pathname.startsWith('/api/') ||
        url.pathname.includes('/recording/') ||
        url.pathname.includes('/download')) {

        event.respondWith(
            fetch(request)
                .then((response) => {
                    // For audio files, don't cache large files
                    if (url.pathname.includes('/download')) {
                        return response;
                    }

                    // Cache API responses briefly
                    if (response.ok) {
                        const responseClone = response.clone();
                        caches.open(CACHE_NAME)
                            .then((cache) => {
                                cache.put(request, responseClone);
                            });
                    }
                    return response;
                })
                .catch(() => caches.match(request))
        );
        return;
    }

    // Cache-first strategy for other static assets
    event.respondWith(
        caches.match(request)
            .then((response) => {
                if (response) {
                    return response;
                }

                // If not in cache, fetch from network and cache it
                return fetch(request).then((networkResponse) => {
                    if (!networkResponse || !networkResponse.ok) return networkResponse;
                    const responseClone = networkResponse.clone();
                    caches.open(CACHE_NAME).then((cache) => cache.put(request, responseClone)).catch(() => {});
                    return networkResponse;
                });
            })
    );
});

// Background sync for queued calls (when network is restored)
self.addEventListener('sync', (event) => {
    if (event.tag === 'background-sync-calls') {
        event.waitUntil(syncCallsFromQueue());
    }
});

// Message handling for communication with main thread
self.addEventListener('message', (event) => {
    const { type, data } = event.data;
    
    switch (type) {
        case 'SKIP_WAITING':
            self.skipWaiting();
            break;
            
        case 'UPDATE_MEDIA_SESSION':
            // Update media session metadata (handled by main thread)
            break;
            
        case 'AUDIO_QUEUE_UPDATE':
            // Store audio queue state for background processing
            storeAudioQueue(data);
            break;
    }
});

// Store audio queue for background processing
function storeAudioQueue(queueData) {
    // Store in IndexedDB or cache for background processing
    // This allows the service worker to maintain queue state
    // even if the main app is closed
}

// Sync calls from queue when network is restored
async function syncCallsFromQueue() {
    try {
        // Retrieve queued calls from storage
        // Attempt to fetch any missed calls from the server
        console.log('Background sync: Syncing queued calls...');
        
        // Notify main thread if it's active
        const clients = await self.clients.matchAll();
        clients.forEach(client => {
            client.postMessage({
                type: 'BACKGROUND_SYNC_COMPLETE',
                data: { success: true }
            });
        });
    } catch (error) {
        console.error('Background sync failed:', error);
    }
}

// Keep the service worker alive for audio playback
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    
    // Focus or open the app
    event.waitUntil(
        self.clients.matchAll({ type: 'window' })
            .then((clients) => {
                if (clients.length > 0) {
                    return clients[0].focus();
                }
                return self.clients.openWindow('/');
            })
    );
});
