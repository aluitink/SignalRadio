import React, { useEffect, useState, useRef } from 'react'
import CallCard from '../components/CallCard'
import type { Call, PagedResult } from '../types'
import { useSignalR } from '../hooks/useSignalR'
import { apiGet, API_BASE } from '../api'

export default function CallStream() {
  const { connection, connected } = useSignalR('talkgroup')
  const [calls, setCalls] = useState<Call[]>([])
  const [sortDir, setSortDir] = useState<'desc' | 'asc'>('desc')
  const allCallsSubscribedRef = useRef(false)
  // Cache of talkgroups keyed by talkGroupId -> description
  const talkgroupCache = useRef<Record<string,string>>({})
  // Helper to coerce various priority shapes to a number or undefined
  const toNumber = (v: any): number | undefined => {
    if (v === undefined || v === null) return undefined
    const n = Number(v)
    return Number.isFinite(n) ? n : undefined
  }

  // Load initial recent calls from API
  useEffect(() => {
    let mounted = true

    ;(async () => {
      try {
        // Load all talkgroups with paging and build a cache map
        const map: Record<string,string> = {}
        let page = 1
        const pageSize = 1000 // controller clamps to max 1000
        let totalPages = 1

        do {
          const res = await apiGet<PagedResult<any>>(`/talkgroups?page=${page}&pageSize=${pageSize}`)
          if (!mounted || !res) break
          const items = res.items ?? []
          for (const tg of items) {
            const id = (tg.TalkGroupNumber ?? tg.talkGroupNumber ?? tg.talkGroupId ?? tg.id ?? tg.TalkGroupId ?? '').toString()
            const desc = tg.Description ?? tg.description ?? tg.TalkGroupDescription ?? tg.talkGroupDescription ?? tg.Name ?? tg.name
            if (id) map[id] = desc
          }
          // Prefer TotalPages if provided, otherwise compute from totalCount/pageSize
          if ((res as any).TotalPages) totalPages = (res as any).TotalPages
          else if (typeof (res as any).TotalCount === 'number') totalPages = Math.max(1, Math.ceil(((res as any).TotalCount ?? 0) / pageSize))
          page++
        } while (page <= totalPages && mounted)

        if (!mounted) return
        talkgroupCache.current = map

        // Now load initial calls so we can match descriptions from the populated cache
        const callsRes = await apiGet<PagedResult<Call>>(`/calls?page=1&pageSize=100&sortBy=recordingTime&sortDir=${sortDir}`)
        if (!mounted || !callsRes) return
        const items = (callsRes.items ?? []).map((it: any) => ({
          ...it,
          // Ensure the client has a talkGroupDescription when the API returned a description field
          talkGroupDescription: it.TalkGroupDescription ?? it.talkGroupDescription ?? it.Description ?? it.description ?? talkgroupCache.current[(it.TalkGroupNumber ?? it.talkGroupId ?? it.TalkGroupId ?? it.talkGroupNumber ?? '')?.toString()] ?? undefined,
          // Coerce common priority fields into a number so CallCard displays correctly
          priority: toNumber(it.Priority ?? it.priority ?? it.PriorityLevel ?? it.priorityLevel ?? it.Pri ?? it.TagPriority ?? undefined),
          recordings: (it.recordings ?? it.Recordings ?? []).map((r: any) => ({
            id: r.Id ?? r.id ?? r.id ?? '',
            url: `${API_BASE}/recordings/${r.Id ?? r.id ?? ''}/file`,
            durationSeconds: r.DurationSeconds ?? r.durationSeconds ?? 0
          }))
        }))
        setCalls(items ?? [])
      } catch {}
    })()

    return () => { mounted = false }
  }, [sortDir])

  useEffect(() => {
    if (!connection) return

    const conn = connection

    // Handler for incoming calls from the hub
    const handler = (call: Call) => {
      setCalls(prev => {
        if (!call || !call.id) return prev
        if (prev.length > 0 && prev[0].id === call.id) return prev
        const filtered = prev.filter(c => c.id !== call.id)
        return [call, ...filtered].slice(0, 100)
      })
    }

    // Server broadcasts 'CallUpdated' for new/updated calls (see TranscriptionBackgroundService)
    const normalize = (payload: any): Call => {
      // Expect API-shaped payload (PascalCase). Minimal, predictable mapping.
      const recs = (payload.Recordings ?? []) as any[]
      return {
        id: String(payload.Id ?? ''),
        talkGroupId: String(payload.TalkGroupId ?? payload.TalkGroupNumber ?? ''),
        talkGroupDescription: undefined, // resolved from cache or talkgroup lookup below
        priority: undefined,
        recordings: recs.map(r => ({ id: String(r.Id ?? ''), url: `${API_BASE}/recordings/${r.Id ?? ''}/file`, durationSeconds: Number(r.DurationSeconds ?? 0) })),
        transcriptions: undefined,
        startedAt: (payload.RecordingTime ?? new Date().toISOString()).toString(),
        endedAt: undefined
      }
    }

    conn.on('CallUpdated', async (callPayload: any) => {
      const incomingCall = normalize(callPayload)
      // If description missing, attempt to fetch single talkgroup by id and cache it
      try {
        const tgId = (incomingCall.talkGroupId ?? '').toString()
        if (!incomingCall.talkGroupDescription && tgId) {
          // Check cache first
          const cached = talkgroupCache.current[tgId]
          if (cached) incomingCall.talkGroupDescription = cached
          else {
            try {
              const tg = await apiGet<any>(`/talkgroups/${tgId}`)
              if (tg) {
                const desc = tg.Description ?? tg.description ?? tg.TalkGroupDescription ?? tg.talkGroupDescription ?? tg.Name ?? tg.name
                if (desc) {
                  talkgroupCache.current[tgId] = desc
                  incomingCall.talkGroupDescription = desc
                }
              }
            } catch {}
          }
        }
      } catch {}

      handler(incomingCall)
    })

    // Handle server confirmation events to avoid "No client method ... found" warnings
    conn.on('AllCallsStreamSubscribed', () => {
      // Server confirms subscription; ensure our ref is in sync
      allCallsSubscribedRef.current = true
      try {
        // eslint-disable-next-line no-console
        console.info('[signalr] AllCallsStreamSubscribed')
      } catch {}
    })

    conn.on('AllCallsStreamUnsubscribed', () => {
      allCallsSubscribedRef.current = false
      try {
        // eslint-disable-next-line no-console
        console.info('[signalr] AllCallsStreamUnsubscribed')
      } catch {}
    })

    // Subscribe to the global all-calls stream so this component receives all incoming calls
    if (!allCallsSubscribedRef.current) {
      conn.invoke('SubscribeToAllCallsStream').then(() => {
        allCallsSubscribedRef.current = true
      }).catch(() => {})
    }

    return () => {
      try {
        if (conn) {
          conn.off('CallUpdated')
          conn.off('AllCallsStreamSubscribed')
          conn.off('AllCallsStreamUnsubscribed')
          if (allCallsSubscribedRef.current) {
            conn.invoke('UnsubscribeFromAllCallsStream').catch(() => {})
            allCallsSubscribedRef.current = false
          }
        }
      } catch {}
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
