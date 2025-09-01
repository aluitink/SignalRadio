import React, { useEffect, useState } from 'react'
import CallCard from '../components/CallCard'
import type { Call, PagedResult } from '../types'
import { useSignalR } from '../hooks/useSignalR'
import { apiGet, API_BASE } from '../api'

export default function CallStream() {
  const { connection, connected } = useSignalR('talkgroup')
  const [calls, setCalls] = useState<Call[]>([])
  const [sortDir, setSortDir] = useState<'desc' | 'asc'>('desc')

  // Load initial recent calls from API
  useEffect(() => {
    let mounted = true
    apiGet<PagedResult<Call>>(`/calls?page=1&pageSize=100&sortBy=recordingTime&sortDir=${sortDir}`).then(res => {
      if (!mounted) return
      // Ensure recordings have client-side URLs (in case server returned FileName)
      const items = (res.items ?? []).map((it: any) => ({
        ...it,
        recordings: (it.recordings ?? it.Recordings ?? []).map((r: any) => ({
          id: r.Id ?? r.id ?? r.id ?? '',
          url: `${API_BASE}/recordings/${r.Id ?? r.id ?? ''}/file`,
          durationSeconds: r.DurationSeconds ?? r.durationSeconds ?? 0
        }))
      }))
      setCalls(items ?? [])
    }).catch(() => {})
    return () => { mounted = false }
  }, [sortDir])

  useEffect(() => {
    if (!connection) return

  // Subscription to the server-wide all_calls_monitor group is handled centrally
  // by the shared signalRManager (subscribe/unsubscribe on connection start/stop).

    // Handler for incoming calls from the hub
    const handler = (call: any) => {
      // Ensure we always show the incoming call at the top and avoid duplicates
      const incoming = call as Call
      setCalls(prev => {
        if (!incoming || !incoming.id) return prev
        // If the newest call is already the incoming call, no change
        if (prev.length > 0 && prev[0].id === incoming.id) return prev
        // Remove any existing instance of this call (dedupe), then add to top
        const filtered = prev.filter(c => c.id !== incoming.id)
        return [incoming, ...filtered].slice(0, 100)
      })
    }

    // Server broadcasts 'CallUpdated' for new/updated calls (see TranscriptionBackgroundService)
    const normalize = (payload: any): Call => {
      // Map server's callNotification shape to client Call type
      return {
        id: payload.Id ?? payload.id ?? '',
        talkGroupId: payload.TalkGroupNumber ?? payload.talkGroupId ?? payload.TalkGroupId ?? '',
        talkGroupDescription: undefined,
        priority: undefined,
        recordings: (payload.Recordings ?? []).map((r: any) => {
          const recId = r.Id ?? r.id ?? ''
          const url = `${API_BASE}/recordings/${recId}/file`
          return { id: recId, url, durationSeconds: r.DurationSeconds ?? r.durationSeconds ?? 0 }
        }),
        transcriptions: undefined,
        startedAt: (payload.RecordingTime ?? payload.recordingTime ?? payload.RecordingTimeUtc ?? new Date().toISOString()),
        endedAt: undefined,
      }
    }

    connection.on('CallUpdated', (callPayload: any) => {
      const incomingCall = normalize(callPayload)
      handler(incomingCall)
    })

    return () => {
      connection.off('CallUpdated')
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
        <p className="muted">No calls yet. Subscribe to talkgroups to start receiving calls.</p>
      ) : (
        calls.map(c => <CallCard key={c.id} call={c} />)
      )}
    </section>
  )
}
