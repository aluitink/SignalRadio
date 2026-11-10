import { Link } from 'react-router-dom'
import type { TranscriptSummaryDto } from '../types/dtos'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import { formatDateTime, formatDuration } from '../utils/format'
import { talkGroupColor } from './CallVolumeChart'
import './SummaryCard.css'

/**
 * A search-result card for a transcript summary. Clicking the card opens the
 * summary detail page (which lists the calls that made it up); the play
 * button starts playing those calls oldest → newest.
 */
export default function SummaryCard({ summary, to }: { summary: TranscriptSummaryDto; to?: string }) {
  const { playSummarySession } = useSubscriptions()
  const accent = talkGroupColor(summary.talkGroupId)

  const handlePlay = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    void playSummarySession(summary)
  }

  return (
    <Link
      to={to ?? `/summary/${summary.id}`}
      className="card clickable summary-card"
      style={{
        '--tg-accent': accent,
        '--tg-accent-soft': `color-mix(in srgb, ${accent} 15%, transparent)`,
      } as React.CSSProperties}
    >
      <div className="summary-card-head">
        <span className="summary-card-tg">
          {summary.talkGroupDescription || summary.talkGroupName || `TG ${summary.talkGroupNumber}`}
        </span>
        <span className="summary-card-time">
          {formatDateTime(summary.startTime)} – {formatDateTime(summary.endTime)}
        </span>
      </div>

      <p className="summary-card-text">{summary.summary}</p>

      <div className="summary-card-meta">
        <span className="summary-card-stat">
          <span className="stat-label">Calls</span> {summary.transcriptCount}
        </span>
        <span className="summary-card-stat">
          <span className="stat-label">Duration</span> {formatDuration(summary.totalDurationSeconds)}
        </span>
        {summary.severity != null && (
          <span className="summary-card-stat">
            <span className="stat-label">Severity</span> {summary.severity}/10
          </span>
        )}
        <button
          type="button"
          className="btn ghost summary-card-play"
          onClick={handlePlay}
          aria-label={`Play ${summary.transcriptCount} calls from this summary`}
          title="Play all calls in this summary (oldest first)"
        >
          ▶ Play all
        </button>
      </div>
    </Link>
  )
}
