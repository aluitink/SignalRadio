import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import type { TranscriptSummaryDto } from '../types/dtos'
import { getRecentSummaries } from '../api'
import { useSignalR } from '../hooks/useSignalR'
import { formatRelativeTime, formatDuration } from '../utils/format'
import { useNow } from '../hooks/useNow'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import './SummaryTicker.css'

const MAX_ITEMS = 10

// Derive a 0-5 "rating" from the summary's severity (0-10) or, as a fallback, the highest
// notable-incident importance score.
function ratingFor(s: TranscriptSummaryDto): number {
  if (s.severity != null) return Math.round((s.severity / 10) * 5)
  if (!s.notableIncidents || s.notableIncidents.length === 0) return 0
  const max = Math.max(...s.notableIncidents.map((n) => n.importanceScore ?? 0))
  return Math.round((max / 10) * 5)
}

function Stars({ value }: { value: number }) {
  const full = Math.round(value)
  return (
    <span className="ticker-stars" aria-label={`Rating ${full} of 5`}>
      {[0, 1, 2, 3, 4].map((i) => (
        <span key={i} className={i < full ? 'star filled' : 'star'}>★</span>
      ))}
    </span>
  )
}

export default function SummaryTicker() {
  const [summaries, setSummaries] = useState<TranscriptSummaryDto[]>([])
  const [open, setOpen] = useState(false)
  const now = useNow(30000)
  const rootRef = useRef<HTMLDivElement | null>(null)
  const { playSummarySession } = useSubscriptions()

  const load = useCallback(async () => {
    try {
      const data = await getRecentSummaries(MAX_ITEMS)
      setSummaries(data)
    } catch {
      // Non-fatal: ticker simply stays empty on error.
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  // Close the dropdown on outside click or Escape.
  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onDown)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  // Refresh when a new summary is generated (broadcast to all clients).
  useSignalR(null, {
    onNewSummary: () => void load(),
  })

  if (summaries.length === 0) return null

  return (
    <div className="summary-ticker" ref={rootRef}>
      <button
        type="button"
        className="ticker-trigger"
        aria-expanded={open}
        aria-haspopup="true"
        onClick={() => setOpen((v) => !v)}
      >
        <div className="ticker-viewport">
          <div className="ticker-track">
            {[...summaries, ...summaries].map((s, i) => (
              <span key={`${s.id}-${i}`} className="ticker-item" aria-hidden={i >= summaries.length}>
                <span className="ticker-tg">{s.talkGroupDescription || `TG ${s.talkGroupNumber}`}</span>
                <span className="ticker-text">{s.summary}</span>
                <span className="ticker-time">{formatRelativeTime(s.generatedAt, now)}</span>
              </span>
            ))}
          </div>
        </div>
        <span className="ticker-caret" aria-hidden="true">{open ? '▴' : '▾'}</span>
      </button>

      {open && (
        <div className="ticker-dropdown" role="menu" aria-label="Recent AI summaries">
          <ul className="ticker-dropdown-list">
            {[...summaries].sort((a, b) => (b.severity ?? 0) - (a.severity ?? 0)).map((s) => (
              <li key={s.id} role="none">
                <div className="ticker-row" role="menuitem">
                  <Link
                    to={`/talkgroup/${s.talkGroupId}`}
                    className="ticker-row-link"
                    onClick={() => setOpen(false)}
                  >
                    <div className="ticker-row-top">
                      <span className="ticker-tg">{s.talkGroupDescription || `TG ${s.talkGroupNumber}`}</span>
                      <span className="ticker-row-time">{formatRelativeTime(s.generatedAt, now)}</span>
                    </div>
                    <p className="ticker-row-summary">{s.summary}</p>
                    <div className="ticker-row-meta">
                      <span className="ticker-row-stat">
                        <span className="stat-label">Calls</span> {s.transcriptCount}
                      </span>
                      <span className="ticker-row-stat">
                        <span className="stat-label">Duration</span> {formatDuration(s.totalDurationSeconds)}
                      </span>
                      <Stars value={ratingFor(s)} />
                    </div>
                  </Link>
                  <button
                    type="button"
                    className="ticker-row-play"
                    title="Play calls from this summary"
                    aria-label={`Play calls from summary: ${s.summary.slice(0, 50)}`}
                    onClick={(e) => {
                      e.preventDefault()
                      e.stopPropagation()
                      setOpen(false)
                      void playSummarySession(s)
                    }}
                  >
                    <span className="ticker-row-play-icon" aria-hidden="true">▶</span>
                    <span className="ticker-row-play-label">Play</span>
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
