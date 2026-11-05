import { useCallback, useEffect, useRef, useState } from 'react'
import type { CallDto } from '../types/dtos'
import { getRecentCallsPaged, getFinalTranscription } from '../api'
import { useSignalR } from '../hooks/useSignalR'
import CallCard from '../components/CallCard'

const PAGE_SIZE = 20

export default function CallStreamPage() {
  const [calls, setCalls] = useState<CallDto[]>([])
  const [transcripts, setTranscripts] = useState<Map<number, string>>(new Map())
  const [page, setPage] = useState(1)
  const [hasMore, setHasMore] = useState(true)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const sentinelRef = useRef<HTMLDivElement | null>(null)
  const loadingRef = useRef(false)

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

  const load = useCallback(async (p: number, append: boolean) => {
    if (loadingRef.current) return
    loadingRef.current = true
    try {
      const data = await getRecentCallsPaged(p, PAGE_SIZE)
      setCalls((prev) => {
        if (!append) return data.items
        const existing = new Set(prev.map((c) => c.id))
        const newItems = data.items.filter((c) => !existing.has(c.id))
        return [...prev, ...newItems]
      })
      setHasMore(p < data.totalPages)
      setError(null)
      void fetchTranscripts(data.items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load calls')
    } finally {
      loadingRef.current = false
      setLoading(false)
      setLoadingMore(false)
    }
  }, [fetchTranscripts])

  useEffect(() => {
    void load(1, false)
  }, [load])

  // Infinite scroll: observe the sentinel at the bottom of the list.
  useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loadingRef.current) {
          const nextPage = page + 1
          setPage(nextPage)
          setLoadingMore(true)
          void load(nextPage, true)
        }
      },
      { rootMargin: '200px' },
    )
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [hasMore, page, load])

  const status = useSignalR(null, {
    onNewCall: (payload) => {
      // Prepend the new call to the top of the list (de-duplicated).
      const call = typeof payload === 'number' ? null : payload
      if (call) {
        setCalls((prev) => (prev.some((c) => c.id === call.id) ? prev : [call, ...prev]))
        void fetchTranscripts([call])
      } else {
        // Legacy numeric payload: refetch the first page.
        setPage(1)
        void load(1, false)
      }
    },
    onNewTranscription: () => {
      // Re-fetch transcripts for currently loaded calls.
      void fetchTranscripts(calls)
    },
  })

  return (
    <div>
      <div className="page-header">
        <h1>Live Calls</h1>
        <span className="live">
          <span className={`status-dot ${status}`} aria-hidden="true" />
          {status === 'connected' ? 'Live' : status === 'connecting' ? 'Connecting…' : 'Offline'}
        </span>
      </div>

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
            <button className="btn" onClick={() => void load(1, false)}>Retry</button>
          </div>
        </div>
      ) : calls.length === 0 ? (
        <div className="empty-state">No calls yet. Waiting for the simulator…</div>
      ) : (
        <div>
          {calls.map((c) => (
            <CallCard key={c.id} call={c} transcript={transcripts.get(c.id)} />
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
