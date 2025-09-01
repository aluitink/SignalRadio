import React, { useEffect, useState, useRef } from 'react'
import CallCard from '../components/CallCard'
import type { CallDto, PagedResult } from '../types/dtos'
import { useSignalR } from '../hooks/useSignalR'
import { apiGet } from '../api'

export default function CallStream() {
  const { connection, connected } = useSignalR('talkgroup')
  const [calls, setCalls] = useState<CallDto[]>([])
  const [sortDir, setSortDir] = useState<'desc' | 'asc'>('desc')
  const allCallsSubscribedRef = useRef(false)

  // Load initial recent calls from API
  useEffect(() => {
    let mounted = true

    ;(async () => {
      try {
        const callsRes = await apiGet<PagedResult<CallDto>>(`/calls?page=1&pageSize=100&sortBy=recordingTime&sortDir=${sortDir}`)
        if (!mounted || !callsRes) return
        
        setCalls(callsRes.items ?? [])
      } catch (error) {
        console.error('Failed to load initial calls:', error)
      }
    })()

    return () => { mounted = false }
  }, [sortDir])

  // Handle SignalR connection and messages
  useEffect(() => {
    if (!connection) return

    const conn = connection

    // Handler for incoming calls from the hub
    const handleCallUpdated = (callDto: CallDto) => {
      setCalls(prev => {
        if (!callDto || !callDto.id) return prev
        
        // Remove any existing call with same ID and add new one at the top
        const filtered = prev.filter(c => c.id !== callDto.id)
        return [callDto, ...filtered].slice(0, 100) // Keep max 100 calls
      })
    }

    // Server broadcasts 'CallUpdated' for new/updated calls
    conn.on('CallUpdated', handleCallUpdated)

    // Handle server confirmation events
    conn.on('AllCallsStreamSubscribed', () => {
      allCallsSubscribedRef.current = true
      console.info('[signalr] AllCallsStreamSubscribed')
    })

    conn.on('AllCallsStreamUnsubscribed', () => {
      allCallsSubscribedRef.current = false
      console.info('[signalr] AllCallsStreamUnsubscribed')
    })

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
          conn.off('AllCallsStreamSubscribed')
          conn.off('AllCallsStreamUnsubscribed')
          if (allCallsSubscribedRef.current) {
            conn.invoke('UnsubscribeFromAllCallsStream').catch(() => {})
            allCallsSubscribedRef.current = false
          }
        }
      } catch (error) {
        console.error('Error cleaning up SignalR handlers:', error)
      }
    }
  }, [connection])

  return (
    <section>
      <h2>Live Call Stream {connected ? '(connected)' : '(disconnected)'}</h2>
      <div style={{ marginBottom: 8 }}>
        <label style={{ marginRight: 8 }}>Sort:</label>
        <select value={sortDir} onChange={e => setSortDir(e.target.value as 'asc' | 'desc')}>
          <option value="desc">Newest</option>
          <option value="asc">Oldest</option>
        </select>
      </div>

      {calls.length === 0 ? (
        <p className="muted">No calls yet. Waiting for incoming calls...</p>
      ) : (
        calls.map(call => <CallCard key={call.id} call={call} />)
      )}
    </section>
  )
}
