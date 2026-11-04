import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { useAudioPlayer } from '../contexts/AudioPlayerContext'
import { recordingDownloadUrl } from '../api'

function formatDuration(seconds: number): string {
  if (!isFinite(seconds) || seconds < 0) return '0:00'
  const m = Math.floor(seconds / 60)
  const s = Math.floor(seconds % 60)
  return `${m}:${s.toString().padStart(2, '0')}`
}

function formatTime(iso: string): string {
  const d = new Date(iso)
  if (isNaN(d.getTime())) return ''
  return d.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

export default function CallCard({
  call,
  transcript,
}: {
  call: CallDto
  transcript?: string | null
}) {
  const { nowPlaying, isPlaying, play } = useAudioPlayer()
  const url = recordingDownloadUrl(call.id)
  const isThis = nowPlaying?.url === url
  const playing = isThis && isPlaying

  const handlePlay = () => {
    play({
      url,
      title: `TG ${call.talkGroupNumber} · ${formatTime(call.recordingTime)}`,
      callId: call.id,
    })
  }

  return (
    <div
      className={`card clickable call-card${playing ? ' playing' : ''}`}
      role="button"
      tabIndex={0}
      aria-label={`Play call on talk group ${call.talkGroupNumber} at ${formatTime(call.recordingTime)}`}
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

      <div className="meta">
        <Link
          className="tg-link"
          to={`/talkgroup/${call.talkGroupId}`}
          onClick={(e) => e.stopPropagation()}
        >
          TG {call.talkGroupNumber}
          {call.talkGroupName ? ` · ${call.talkGroupName}` : ''}
        </Link>
        <span className="sub">
          <span>{formatDuration(call.durationSeconds)}</span>
          <span className="dot">•</span>
          <span>{(call.frequencyHz / 1000).toFixed(3)} MHz</span>
          {call.recordingCount > 0 && (
            <>
              <span className="dot">•</span>
              <span>{call.recordingCount} rec</span>
            </>
          )}
        </span>
      </div>

      <span className="time">{formatTime(call.recordingTime)}</span>

      {transcript ? (
        <p className="transcript">{transcript}</p>
      ) : null}
    </div>
  )
}
