import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { useAudioPlayer } from '../contexts/AudioPlayerContext'
import { recordingDownloadUrlByCall } from '../api'
import { formatDuration, formatDateTime, formatRelativeTime } from '../utils/format'
import { useNow } from '../hooks/useNow'

export default function CallCard({
  call,
  transcript,
}: {
  call: CallDto
  transcript?: string | null
}) {
  const { nowPlaying, isPlaying, play } = useAudioPlayer()
  const now = useNow()
  const url = recordingDownloadUrlByCall(call.id)
  const isThis = nowPlaying?.url === url
  const playing = isThis && isPlaying

  const handlePlay = () => {
    play({
      url,
      title: `TG ${call.talkGroupNumber} · ${formatDateTime(call.recordingTime)}`,
      callId: call.id,
    })
  }

  return (
    <div
      className={`card clickable call-card${playing ? ' playing' : ''}`}
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
        </span>
      </div>

      <span className="time" title={formatDateTime(call.recordingTime)}>
        {formatRelativeTime(call.recordingTime, now)}
      </span>

      {transcript ? (
        <p className="transcript">{transcript}</p>
      ) : null}
    </div>
  )
}
