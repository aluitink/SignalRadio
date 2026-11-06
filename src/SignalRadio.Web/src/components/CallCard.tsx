import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { useAudioPlayer } from '../contexts/AudioPlayerContext'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import { recordingDownloadUrlByCall } from '../api'
import { formatDuration, formatDateTime, formatFrequency, formatRelativeTime } from '../utils/format'
import { useNow } from '../hooks/useNow'

export default function CallCard({
  call,
  transcript,
  accentColor,
}: {
  call: CallDto
  transcript?: string | null
  /** Optional per-talkgroup accent color (matches the call-volume histogram). */
  accentColor?: string
}) {
  const { nowPlaying, isPlaying, play } = useAudioPlayer()
  const { isSubscribed, toggleSubscription } = useSubscriptions()
  const now = useNow()
  const url = recordingDownloadUrlByCall(call.id)
  const isThis = nowPlaying?.url === url
  const playing = isThis && isPlaying
  const subscribed = isSubscribed(call.talkGroupId)

  const handlePlay = () => {
    play({
      url,
      title: `TG ${call.talkGroupNumber} · ${formatDateTime(call.recordingTime)}`,
      callId: call.id,
    })
  }

  const handleSubscribe = (e: React.MouseEvent) => {
    e.stopPropagation()
    toggleSubscription(call.talkGroupId)
  }

  return (
    <div
      className={`card clickable call-card${playing ? ' playing' : ''}`}
      style={accentColor ? ({
        '--tg-accent': accentColor,
        // ~15% tint of the accent over transparent, works with any color format (hsl, hex, …)
        '--tg-accent-soft': `color-mix(in srgb, ${accentColor} 15%, transparent)`,
      } as React.CSSProperties) : undefined}
      role="button"
      tabIndex={0}
      aria-label={`Play call on talk group ${call.talkGroupNumber} at ${formatDateTime(call.recordingTime)}`}
      onClick={handlePlay}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          handlePlay()
        }
      }}
    >
      <span className={`play-btn${playing ? ' playing' : ''}`} aria-hidden="true">
        {playing ? '⏸' : '▶'}
      </span>

      <div className="card-body">
        <div className="card-row">
          <Link
            className="tg-link"
            to={`/talkgroup/${call.talkGroupId}`}
            onClick={(e) => e.stopPropagation()}
          >
            {call.talkGroupName ?? `TG ${call.talkGroupNumber}`}
          </Link>
          <span className="row-end">
            <span className="time" title={formatDateTime(call.recordingTime)}>
              {formatRelativeTime(call.recordingTime, now)}
            </span>
            <button
              className={`subscribe-icon${subscribed ? ' subscribed' : ''}`}
              onClick={handleSubscribe}
              aria-pressed={subscribed}
              aria-label={subscribed ? `Unsubscribe from talk group ${call.talkGroupNumber}` : `Subscribe to talk group ${call.talkGroupNumber}`}
              title={subscribed ? 'Unsubscribe from auto-play' : 'Subscribe to auto-play new calls'}
            >
              {subscribed ? (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.64 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z" />
                </svg>
              ) : (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
                  <path d="M13.73 21a2 2 0 0 1-3.46 0" />
                </svg>
              )}
            </button>
          </span>
        </div>

        <div className="card-row sub">
          <span className="sub">
            <span>{formatDuration(call.durationSeconds)}</span>
            <span className="dot">•</span>
            <span>{formatFrequency(call.frequencyHz)}</span>
          </span>
        </div>

        {transcript ? (
          <p className="transcript">{transcript}</p>
        ) : null}
      </div>
    </div>
  )
}
