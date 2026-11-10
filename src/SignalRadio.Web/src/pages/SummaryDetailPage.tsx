import { useCallback, useEffect, useRef, useState } from 'react'
import { useParams, Link, useSearchParams } from 'react-router-dom'
import type { CallDto, TranscriptSummaryDto } from '../types/dtos'
import { getSummary, getCallsCursor, getFinalTranscription } from '../api'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import { useSignalR } from '../hooks/useSignalR'
import CallCard from '../components/CallCard'
import { talkGroupColor } from '../components/CallVolumeChart'
import { formatDateTime, formatDuration } from '../utils/format'
import './SummaryDetailPage.css'

const PAGE_SIZE = 20

/**
 * Detail view for a single transcript summary: the summary text plus the
 * calls that made it up, rendered as a call stream (newest first, same look
 * and feel as the live call stream). Clicking a card plays that call and
 * continues with the newer calls in the summary; "Play all" starts from the
 * oldest.
 */
export default function SummaryDetailPage() {
  const { id } = useParams<{ id: string }>()
  const summaryId = id ? Number(id) : NaN
  const [searchParams] = useSearchParams()
  const from = searchParams.get('from')

  const [summary, setSummary] = useState<TranscriptSummaryDto | null>(null)
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
  const { playSummarySession } = useSubscriptions()

  // Load the summary header.
  useEffect(() => {
    if (!Number.isFinite(summaryId)) return
    let active = true
    getSummary(summaryId)
      .then((s) => active && setSummary(s))
      .catch((e) => active && setError(e instanceof Error ? e.message : 'Failed to load summary'))
    return () => {
      active = false
    }
  }, [summaryId])

  const fetchTranscripts = useCallback(async (items: CallDto[]) => {
    const withRecording = items.filter((c) => c.latestRecordingId)
    if (withRecording.length === 0) return
    const results = await Promise.all(
      withRecording.map(async (c) => {
        try {
          const t = await getFinalTranscription(c.latestRecordingId!)
          return { callId: c.id, text: t?.fullText ?? null }
        } catch {
          return { callId: c.id, text: null as string | null }
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

  // Load calls for the summary's talk group, newest first, stopping once we
  // page past the summary's start time.
  const load = useCallback(
    async (append: boolean, before: { time: string; id: number } | null) => {
      if (!summary || loadingRef.current) return
      loadingRef.current = true
      try {
        const data = await getCallsCursor(PAGE_SIZE, before, summary.talkGroupId)
        const startTime = new Date(summary.startTime).getTime()
        const endTime = new Date(summary.endTime).getTime()
        const inWindow = data.items.filter((c) => {
          const t = new Date(c.recordingTime).getTime()
          return t >= startTime && t <= endTime
        })
        setCalls((prev) => {
          if (!append) return inWindow
          const existing = new Set(prev.map((c) => c.id))
          return [...prev, ...inWindow.filter((c) => !existing.has(c.id))]
        })
        // Stop paging once we've gone past the start of the summary window.
        const oldest = data.items[data.items.length - 1]
        const pastStart = oldest != null && new Date(oldest.recordingTime).getTime() < startTime
        setHasMore(data.hasMore && !pastStart)
        if (data.items.length > 0) {
          const last = data.items[data.items.length - 1]
          setCursor({ time: last.recordingTime, id: last.id })
        } else if (!append) {
          setCursor(null)
        }
        setError(null)
        void fetchTranscripts(inWindow)
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load calls')
      } finally {
        loadingRef.current = false
        setLoading(false)
        setLoadingMore(false)
      }
    },
    [summary, fetchTranscripts],
  )

  // Reload from the top whenever the summary changes.
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

  // Live updates: prepend new calls on this talk group that fall in the window.
  useSignalR(summary?.talkGroupId ?? null, {
    onNewCall: (payload) => {
      const call = typeof payload === 'number' ? null : payload
      if (!call || !summary) return
      if (call.talkGroupId !== summary.talkGroupId) return
      const t = new Date(call.recordingTime).getTime()
      if (t < new Date(summary.startTime).getTime() || t > new Date(summary.endTime).getTime()) return
      setCalls((prev) => (prev.some((c) => c.id === call.id) ? prev : [call, ...prev]))
      void fetchTranscripts([call])
    },
    onNewTranscription: () => {
      void fetchTranscripts(calls)
    },
  })

  if (!Number.isFinite(summaryId)) {
    return <div className="error-state">Invalid summary id.</div>
  }

  if (error && !summary) {
    return <div className="error-state" role="alert">{error}</div>
  }

  if (!summary) {
    return (
      <div aria-busy="true">
        <div className="skeleton skeleton-card" />
        <div className="skeleton skeleton-card" />
        <div className="skeleton skeleton-card" />
      </div>
    )
  }

  const accent = talkGroupColor(summary.talkGroupId)

  return (
    <div>
      <div className="page-header">
        <div>
          {from === 'search' ? (
            <Link to="/search" className="back-link">← Back to search results</Link>
          ) : (
            <Link to={`/talkgroup/${summary.talkGroupId}`} className="back-link">
              ← {summary.talkGroupDescription || summary.talkGroupName || `TG ${summary.talkGroupNumber}`}
            </Link>
          )}
          <h1>Summary</h1>
        </div>
        <button
          className="btn primary"
          onClick={() => void playSummarySession(summary)}
          disabled={calls.length === 0}
        >
          ▶ Play all {calls.length > 0 ? `(${calls.length})` : ''}
        </button>
      </div>

      <div className="card summary-detail-card" style={{ '--tg-accent': accent } as React.CSSProperties}>
        <div className="summary-detail-meta">
          <span>
            {formatDateTime(summary.startTime)} – {formatDateTime(summary.endTime)}
          </span>
          <span className="dot">•</span>
          <span>{summary.transcriptCount} calls</span>
          <span className="dot">•</span>
          <span>{formatDuration(summary.totalDurationSeconds)}</span>
          {summary.severity != null && (
            <>
              <span className="dot">•</span>
              <span>Severity {summary.severity}/10</span>
            </>
          )}
        </div>
        <p className="summary-detail-text">{summary.summary}</p>
      </div>

      <h2 className="summary-calls-heading">
        Calls in this summary <span className="sub">({calls.length}, newest first)</span>
      </h2>

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
      ) : calls.length === 0 ? (
        <div className="empty-state">
          No calls are linked to this summary. Older summaries were created
          before call tracking was added.
        </div>
      ) : (
        <div>
          {calls.map((c) => (
            <CallCard
              key={c.id}
              call={c}
              transcript={transcripts.get(c.id)}
              accentColor={accent}
              stream={calls}
              showTranscript={false}
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
