import React, { useEffect, useState } from 'react'
import { useSignalR } from '../hooks/useSignalR'
import { useSubscriptions } from '../contexts/SubscriptionContext'
import { callStreamService } from '../services/CallStreamService'
import { audioPlayerService } from '../services/AudioPlayerService'
import type { CallDto } from '../types/dtos'
import CallCard from './CallCard'
import LoadingSpinner from './LoadingSpinner'

export default function CallStream() {
  const { connection } = useSignalR('/hubs/talkgroup')
  const { isSubscribed } = useSubscriptions()
  const [calls, setCalls] = useState<CallDto[]>([])
  const [loading, setLoading] = useState(true)

  // Load initial calls
  useEffect(() => {
    let mounted = true

    const loadCalls = async () => {
      try {
        setLoading(true)
        await callStreamService.loadRecentCalls(1, 20)
      } catch (error) {
        console.error('Failed to load initial calls:', error)
      } finally {
        if (mounted) setLoading(false)
      }
    }

    loadCalls()

    return () => { mounted = false }
  }, [])

  // Subscribe to call stream service
  useEffect(() => {
    return callStreamService.subscribe({
      onCallsChanged: (newCalls) => {
        setCalls(newCalls)
      },
      onNewCall: (call) => {
        // If this call is from a subscribed talkgroup, add to audio queue
        if (call.recordings?.length && isSubscribed(call.talkGroupId)) {
          console.log(`[CallStream] Adding call ${call.id} from subscribed talkgroup ${call.talkGroupId} to queue`)
          audioPlayerService.addToQueue(call)
        }
      },
      onCallUpdated: (call) => {
        // Call was updated (e.g., transcription added)
        console.log(`[CallStream] Call ${call.id} updated`)
      }
    })
  }, [isSubscribed])

  // Handle SignalR messages
  useEffect(() => {
    if (!connection) return

    const handleCallUpdated = (callDto: CallDto) => {
      console.log('[CallStream] CallUpdated received via SignalR:', {
        callId: callDto.id,
        talkGroupId: callDto.talkGroupId,
        recordingTime: callDto.recordingTime,
        recordingsCount: callDto.recordings?.length || 0
      })
      
      // Determine if this is a new call or update
      const now = Date.now()
      const callTime = new Date(callDto.recordingTime).getTime()
      const fiveMinutesAgo = now - (5 * 60 * 1000)
      
      if (callTime >= fiveMinutesAgo) {
        // Recent call - treat as new
        callStreamService.handleNewCall(callDto)
      } else {
        // Older call - treat as update
        callStreamService.handleCallUpdate(callDto)
      }
    }

    const handleAllCallsStreamSubscribed = () => {
      console.info('[CallStream] AllCallsStreamSubscribed')
    }

    const handleAllCallsStreamUnsubscribed = () => {
      console.info('[CallStream] AllCallsStreamUnsubscribed')
    }

    // Clean up existing handlers
    connection.off('CallUpdated', handleCallUpdated)
    connection.off('AllCallsStreamSubscribed', handleAllCallsStreamSubscribed)
    connection.off('AllCallsStreamUnsubscribed', handleAllCallsStreamUnsubscribed)

    // Add new handlers
    connection.on('CallUpdated', handleCallUpdated)
    connection.on('AllCallsStreamSubscribed', handleAllCallsStreamSubscribed)
    connection.on('AllCallsStreamUnsubscribed', handleAllCallsStreamUnsubscribed)

    // Subscribe to all calls stream
    connection.invoke('SubscribeToAllCallsStream').catch((error) => {
      console.error('Failed to subscribe to all calls stream:', error)
    })

    return () => {
      try {
        connection.off('CallUpdated', handleCallUpdated)
        connection.off('AllCallsStreamSubscribed', handleAllCallsStreamSubscribed)
        connection.off('AllCallsStreamUnsubscribed', handleAllCallsStreamUnsubscribed)
        connection.invoke('UnsubscribeFromAllCallsStream').catch(() => {})
      } catch (error) {
        console.error('Error cleaning up SignalR handlers:', error)
      }
    }
  }, [connection])

  if (loading) {
    return (
      <section className="call-stream">
        <div className="loading-container">
          <LoadingSpinner />
          <p>Loading recent calls...</p>
        </div>
      </section>
    )
  }

  return (
    <section className="call-stream">
      <div className="call-list">
        {calls.length === 0 ? (
          <div className="empty-state">
            <p>No calls available. Waiting for new calls...</p>
          </div>
        ) : (
          calls.map((call) => (
            <CallCard key={call.id} call={call} />
          ))
        )}
      </div>

      <style>{`
        .call-stream {
          max-width: var(--content-width);
          margin: 0 auto;
          padding: var(--space-2);
        }

        .loading-container {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: var(--space-2);
          padding: var(--space-4);
          color: var(--text-secondary);
        }

        .call-list {
          display: flex;
          flex-direction: column;
          gap: var(--space-2);
        }

        .empty-state {
          text-align: center;
          padding: var(--space-4);
          color: var(--text-secondary);
        }

        @media (max-width: 767px) {
          .call-stream {
            padding: var(--space-1-5) var(--space-1);
          }
        }
      `}</style>
    </section>
  )
}
