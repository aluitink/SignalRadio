import React, { useEffect, useState, useRef } from 'react'
import CallCard from '../components/CallCard'
import AutoplayBanner from '../components/AutoplayBanner'
import { CallCardSkeleton } from '../components/LoadingSpinner'
import Pagination from '../components/Pagination'
import type { CallDto, PagedResult } from '../types/dtos'
import { useSignalR } from '../hooks/useSignalR'
import { useAudioManager } from '../hooks/useAudioManager'
import { useSubscriptions } from '../contexts/SubscriptionContext'
import { apiGet } from '../api'

export default function CallStream() {
  const { connection, connected } = useSignalR('talkgroup')
  const { isSubscribed } = useSubscriptions()
  const [calls, setCalls] = useState<CallDto[]>([])
  const [loading, setLoading] = useState(true)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const pageSize = 20
  const allCallsSubscribedRef = useRef(false)
  
  const { autoplayEnabled, addToAutoPlayQueue } = useAudioManager()

  // Load initial recent calls from API
  useEffect(() => {
    let mounted = true

    ;(async () => {
      try {
        setLoading(true)
        const callsRes = await apiGet<PagedResult<CallDto>>(`/calls?page=${currentPage}&pageSize=${pageSize}&sortBy=recordingTime&sortDir=desc`)
        if (!mounted || !callsRes) return
        
        setCalls(callsRes.items ?? [])
        setTotalPages(callsRes.totalPages ?? 1)
        setTotalItems(callsRes.totalCount ?? 0)
      } catch (error) {
        console.error('Failed to load initial calls:', error)
      } finally {
        if (mounted) setLoading(false)
      }
    })()

    return () => { mounted = false }
  }, [currentPage]) // Re-fetch when page changes

  // Handle SignalR connection and messages
  useEffect(() => {
    if (!connection) return

    const conn = connection

    // Handler for incoming calls from the hub
    const handleCallUpdated = (callDto: CallDto) => {
      let shouldAddToQueue = false
      
      console.log('[CallStream] CallUpdated received via SignalR:', {
        callId: callDto.id,
        talkGroupId: callDto.talkGroupId,
        recordingTime: callDto.recordingTime,
        recordingsCount: callDto.recordings?.length || 0,
        transcriptionsCount: callDto.transcriptions?.length || 0,
        hasTranscriptions: !!callDto.transcriptions?.length
      })
      
      setCalls(prev => {
        if (!callDto || !callDto.id) return prev
        
        // Check if this call already exists in our list
        const existingIndex = prev.findIndex(c => c.id === callDto.id)
        
        if (existingIndex >= 0) {
          // UpdatedCalls: Only update existing call in place (for transcription updates, etc.)
          console.log(`[CallStream] Updating existing call ${callDto.id} in stream (position ${existingIndex})`)
          const updated = [...prev]
          updated[existingIndex] = callDto
          return updated
        } else {
          // For calls not already in the stream, we need to determine if this is a new call
          // or just a transcript update for an older call. We only add new calls if they
          // have recent recording times (within the last 5 minutes) to avoid adding
          // old calls to the stream when they get transcript updates.
          const now = Date.now()
          const callTime = new Date(callDto.recordingTime).getTime()
          const fiveMinutesAgo = now - (5 * 60 * 1000)
          
          if (callTime >= fiveMinutesAgo) {
            // NewCalls: This appears to be a recent call - add to top
            console.log(`[CallStream] Adding new call ${callDto.id} to stream (TalkGroup=${callDto.talkGroupId}, Time=${callDto.recordingTime})`)
            const newCalls = [callDto, ...prev].slice(0, 100) // Keep max 100 calls
            
            // Mark that we should add to auto-play queue (but don't do it during render)
            // Only add to queue if user is subscribed to this talkgroup
            if (callDto.recordings && callDto.recordings.length > 0 && isSubscribed(callDto.talkGroupId)) {
              console.log(`[CallStream] Call ${callDto.id} from subscribed talkgroup ${callDto.talkGroupId} has ${callDto.recordings.length} recordings - marking for auto-play queue`)
              shouldAddToQueue = true
            } else if (callDto.recordings && callDto.recordings.length > 0) {
              console.log(`[CallStream] Call ${callDto.id} from unsubscribed talkgroup ${callDto.talkGroupId} - not adding to auto-play queue`)
            }
            
            return newCalls
          } else {
            // This is likely a transcript update for an older call not in the stream
            // Don't add it to the stream
            console.debug(`[CallStream] Ignoring call update for older call ${callDto.id} (${callDto.recordingTime}) - older than 5 minutes`)
            return prev
          }
        }
      })
      
      // Add to auto-play queue after state update is complete
      if (shouldAddToQueue) {
        // Use setTimeout to ensure this happens after the render cycle
        setTimeout(() => {
          console.log(`[CallStream] Adding call ${callDto.id} from subscribed talkgroup ${callDto.talkGroupId} to auto-play queue`)
          addToAutoPlayQueue(callDto)
        }, 0)
      }
    }

    const handleAllCallsStreamSubscribed = () => {
      allCallsSubscribedRef.current = true
      console.info('[signalr] AllCallsStreamSubscribed')
    }

    const handleAllCallsStreamUnsubscribed = () => {
      allCallsSubscribedRef.current = false
      console.info('[signalr] AllCallsStreamUnsubscribed')
    }

    // Remove any existing handlers first to prevent duplicates in React.StrictMode
    conn.off('CallUpdated', handleCallUpdated)
    conn.off('AllCallsStreamSubscribed', handleAllCallsStreamSubscribed)
    conn.off('AllCallsStreamUnsubscribed', handleAllCallsStreamUnsubscribed)

    // Server broadcasts 'CallUpdated' for new/updated calls
    conn.on('CallUpdated', handleCallUpdated)

    // Handle server confirmation events
    conn.on('AllCallsStreamSubscribed', handleAllCallsStreamSubscribed)
    conn.on('AllCallsStreamUnsubscribed', handleAllCallsStreamUnsubscribed)

    // Subscribe to the global all-calls stream
    if (!allCallsSubscribedRef.current) {
      conn.invoke('SubscribeToAllCallsStream').then(() => {
        allCallsSubscribedRef.current = true
      }).catch((error) => {
        console.error('Failed to subscribe to all calls stream:', error)
      })
    }

    return () => {
      try {
        if (conn) {
          conn.off('CallUpdated', handleCallUpdated)
          conn.off('AllCallsStreamSubscribed', handleAllCallsStreamSubscribed)
          conn.off('AllCallsStreamUnsubscribed', handleAllCallsStreamUnsubscribed)
          if (allCallsSubscribedRef.current) {
            conn.invoke('UnsubscribeFromAllCallsStream').catch(() => {})
            allCallsSubscribedRef.current = false
          }
        }
      } catch (error) {
        console.error('Error cleaning up SignalR handlers:', error)
      }
    }
  }, [connection, addToAutoPlayQueue])

  if (loading) {
    return (
      <section className="call-stream">
        <header className="stream-header">
          <h1>Live Call Stream</h1>
          <div className="connection-status">
            <span className={`status-badge ${connected ? 'connected' : 'disconnected'}`}>
              <span className="status-indicator" />
              {connected ? 'Connected' : 'Connecting...'}
            </span>
          </div>
        </header>
        
        <div className="loading-skeleton">
          {Array.from({ length: 5 }).map((_, i) => (
            <CallCardSkeleton key={i} />
          ))}
        </div>

        <style>{`
          .call-stream {
            min-height: 60vh;
          }

          .stream-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: var(--space-4);
            flex-wrap: wrap;
            gap: var(--space-2);
          }

          .connection-status {
            display: flex;
            align-items: center;
            justify-content: flex-end;
            font-size: var(--font-size-sm);
          }

          .status-badge {
            display: flex;
            align-items: center;
            gap: var(--space-1);
            padding: var(--space-1) var(--space-2);
            border-radius: var(--radius);
            font-weight: 500;
            font-size: var(--font-size-xs);
          }

          .status-badge.connected {
            background: #10b981;
            color: white;
          }

          .status-badge.disconnected {
            background: #ef4444;
            color: white;
          }

          .status-indicator {
            width: 6px;
            height: 6px;
            border-radius: 50%;
            background: currentColor;
          }

          .loading-skeleton {
            display: flex;
            flex-direction: column;
            gap: var(--space-2);
          }

          .skeleton-card {
            height: 120px;
            background: var(--bg-card);
            border-radius: var(--radius);
            animation: pulse 1.5s ease-in-out infinite;
          }

          @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
          }

          .empty-state {
            text-align: center;
            padding: var(--space-6) var(--space-2);
            color: var(--text-secondary);
          }

          .empty-icon {
            font-size: 48px;
            margin-bottom: var(--space-2);
          }

          .empty-state h3 {
            color: var(--text-primary);
            margin-bottom: var(--space-1);
          }

          .calls-list {
            display: flex;
            flex-direction: column;
          }

          @media (max-width: 767px) {
            .stream-header {
              flex-direction: column;
              align-items: flex-start;
            }
          }
        `}</style>
      </section>
    )
  }

  return (
    <section className="call-stream">
      <header className="stream-header">
        <h1>Live Call Stream</h1>
        <div className="connection-status">
          <span className={`status-badge ${connected ? 'connected' : 'disconnected'}`}>
            <span className="status-indicator" />
            {connected ? 'Connected' : 'Connecting...'}
          </span>
        </div>
      </header>

      <AutoplayBanner />
      
      {calls.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">📡</div>
          <h3>No calls yet</h3>
          <p className="text-muted">Waiting for incoming calls...</p>
        </div>
      ) : (
        <>
          <div className="calls-list">
            {calls.map(call => (
              <CallCard 
                key={call.id} 
                call={call}
              />
            ))}
          </div>
          
          <Pagination
            currentPage={currentPage}
            totalPages={totalPages}
            totalItems={totalItems}
            itemsPerPage={pageSize}
            onPageChange={setCurrentPage}
            loading={loading}
          />
        </>
      )}
    </section>
  )
}
