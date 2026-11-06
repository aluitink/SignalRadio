import { useCallback, useEffect, useRef, useState } from 'react'
import type { CallDto } from '../types/dtos'
import { getCallsCursor, getFinalTranscription } from '../api'
import { useSignalR } from '../hooks/useSignalR'
import CallCard from './CallCard'
import { talkGroupColor } from './CallVolumeChart'

const PAGE_SIZE = 20

export interface CallStreamProps {
  /** Scope the stream to a single talk group. Omit for the global stream. */
  talkGroupId?: number
  /** Restrict the stream to these talk group ids (global stream legend filter). */
  talkGroupIds?: number[]
  /** When true, only render calls whose talk group is in this set (client-side). */
  subscribedOnly?: boolean
  subscribedIds?: number[]
  /** Message shown when the (filtered) list is empty. */
  emptyMessage?: string
}

/**
 * Endless-scroll, live-updating list of calls. Cursor-based paging means it can
 * scroll back in time indefinitely without skipping or duplicating rows, even
 * while new calls are prepended over SignalR. Used by both the global call
 * stream and the single-talk-group page.
 */
export default function CallStream({
  talkGroupId,
  talkGroupIds,
  subscribedOnly = false,
  subscribedIds = [],
  emptyMessage = 'No calls yet.',
}: CallStreamProps) {
  const [calls, setCalls] = useState<CallDto[]>([])
  const [transcripts, setTranscripts] = useState<Map<number, string>>(new Map())
  // Cursor for "scroll back in time": the (time, id) of the oldest loaded call.
  const [cursor, setCursor] = useState<{ time: string; id: number } | null>(null)
  const [hasMore, setHasMore] = useState(true)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const sentinelRef = useRef<HTMLDivElement | null>(null)
  const loadingRef = useRef(false)

  const subscribedSet = new Set(subscribedIds)

  const fetchTranscripts = useCallback(async (items: CallDto[]) => {
    const withRecording = items.filter((c) => c.latestRecordingId)
    if (withRecording.length === 0) return
    const results = await Promise.all(
      withRecording.map(async (c) => {
        try {
          const t = await getFinalTranscription(c.latestRecordingId!)
          return { callId: c.id, text: t?.fullText ?? null }
        } catch {
          return { callId: c.id, text: null }
        }
      }),
    )
    setTranscripts((prev) => {
      const next = new Map(prev)
      for (const r of results) {
        if (r.text) next.set(r.callId, r.text)
      }
      return next
    })
  }, [])

  const load = useCallback(async (append: boolean, before: { time: string; id: number } | null) => {
    if (loadingRef.current) return
    loadingRef.current = true
    try {
      const data = await getCallsCursor(PAGE_SIZE, before, talkGroupId, talkGroupIds)
      setCalls((prev) => {
        if (!append) return data.items
        const existing = new Set(prev.map((c) => c.id))
        const newItems = data.items.filter((c) => !existing.has(c.id))
        return [...prev, ...newItems]
      })
      setHasMore(data.hasMore)
      // Advance the cursor to the oldest call just loaded.
      if (data.items.length > 0) {
        const last = data.items[data.items.length - 1]
        setCursor({ time: last.recordingTime, id: last.id })
      } else if (!append) {
        setCursor(null)
      }
      setError(null)
      void fetchTranscripts(data.items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load calls')
    } finally {
      loadingRef.current = false
      setLoading(false)
      setLoadingMore(false)
    }
  }, [fetchTranscripts, talkGroupId, talkGroupIds])

  // Reload from the top whenever the filter (talk group / selected groups) changes.
  useEffect(() => {
    setCursor(null)
    setHasMore(true)
    setLoading(true)
    void load(false, null)
  }, [load])

  // Infinite scroll: observe the sentinel at the bottom of the list.
  useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loadingRef.current) {
          setLoadingMore(true)
          void load(true, cursor)
        }
      },
      { rootMargin: '200px' },
    )
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [hasMore, cursor, load])

  // The hub broadcasts NewCall to all clients (and to the joined talkgroup group),
  // so drop any call that falls outside the active filter before prepending it.
  const matchesFilter = useCallback(
    (c: CallDto) => {
      if (talkGroupId != null) return c.talkGroupId === talkGroupId
      if (talkGroupIds && talkGroupIds.length > 0) return talkGroupIds.includes(c.talkGroupId)
      return true
    },
    [talkGroupId, talkGroupIds],
  )

  useSignalR(talkGroupId ?? null, {
    onNewCall: (payload) => {
      // Prepend the new call to the top of the list (de-duplicated).
      const call = typeof payload === 'number' ? null : payload
      if (call) {
        if (!matchesFilter(call)) return
        setCalls((prev) => (prev.some((c) => c.id === call.id) ? prev : [call, ...prev]))
        void fetchTranscripts([call])
      } else {
        // Legacy numeric payload: refetch the first page.
        setCursor(null)
        void load(false, null)
      }
    },
    onNewTranscription: () => {
      // Re-fetch transcripts for currently loaded calls.
      void fetchTranscripts(calls)
    },
  })

  const visibleCalls = subscribedOnly ? calls.filter((c) => subscribedSet.has(c.talkGroupId)) : calls

  return (
    <div>
      {loading ? (
        <div aria-busy="true">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="skeleton skeleton-card" />
          ))}
        </div>
      ) : error ? (
        <div className="error-state" role="alert">
          {error}
          <div style={{ marginTop: 8 }}>
            <button className="btn" onClick={() => void load(false, null)}>Retry</button>
          </div>
        </div>
      ) : visibleCalls.length === 0 ? (
        <div className="empty-state">{emptyMessage}</div>
      ) : (
        <div>
          {visibleCalls.map((c) => (
            <CallCard
              key={c.id}
              call={c}
              transcript={transcripts.get(c.id)}
              accentColor={talkGroupColor(c.talkGroupId)}
              stream={visibleCalls}
            />
          ))}
          <div ref={sentinelRef} className="scroll-sentinel" />
          {loadingMore && <div className="loading-more">Loading more…</div>}
          {!hasMore && calls.length > 0 && (
            <div className="end-of-list">You're all caught up</div>
          )}
        </div>
      )}
    </div>
  )
}
