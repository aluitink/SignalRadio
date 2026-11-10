import { useEffect, useRef, useState } from 'react'
import { useAudioPlayer } from '../contexts/AudioPlayerContext'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import { formatDuration, formatDateTime } from '../utils/format'
import './AudioPlayer.css'

export default function AudioPlayer() {
  const { nowPlaying, isPlaying, currentTime, duration, toggle, stop, seek } = useAudioPlayer()
  const { queue, clearQueue, removeFromQueue } = useSubscriptions()
  const [showQueue, setShowQueue] = useState(false)
  const queueRef = useRef<HTMLDivElement | null>(null)

  // Close the queue popover on outside click or Escape.
  useEffect(() => {
    if (!showQueue) return
    const onDown = (e: MouseEvent) => {
      if (queueRef.current && !queueRef.current.contains(e.target as Node)) setShowQueue(false)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setShowQueue(false)
    }
    document.addEventListener('mousedown', onDown)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [showQueue])

  if (!nowPlaying) return null

  const handleSeek = (e: React.ChangeEvent<HTMLInputElement>) => {
    seek(Number(e.target.value))
  }

  const handleStop = () => {
    // Clear the queue so the resume effect doesn't immediately restart playback.
    clearQueue()
    stop()
  }

  return (
    <div className="audio-player" role="region" aria-label="Audio player">
      <div className="ap-inner">
        <button
          className="btn ghost ap-play-btn"
          onClick={toggle}
          aria-label={isPlaying ? 'Pause' : 'Play'}
        >
          {isPlaying ? '⏸' : '▶'}
        </button>
        <span className="ap-title" title={nowPlaying.title}>{nowPlaying.title}</span>
        <div className="ap-seek">
          <span className="ap-time">{formatDuration(currentTime)}</span>
          <input
            type="range"
            className="ap-seek-bar"
            min={0}
            max={duration || 0}
            step={0.1}
            value={currentTime}
            onChange={handleSeek}
            aria-label="Seek"
          />
          <span className="ap-time">{formatDuration(duration)}</span>
        </div>
        <div className="ap-queue-wrap" ref={queueRef}>
          <button
            className="btn ghost ap-queue-btn"
            onClick={() => setShowQueue((v) => !v)}
            aria-label="Show playback queue"
            aria-expanded={showQueue}
          >
            ☰{queue.length > 0 && <span className="ap-queue-count">{queue.length}</span>}
          </button>
          {showQueue && (
            <div className="ap-queue-pop" role="dialog" aria-label="Playback queue">
              <div className="ap-queue-head">
                <span>Up next</span>
                {queue.length > 0 && (
                  <button className="btn ghost ap-queue-clear" onClick={clearQueue}>
                    Clear
                  </button>
                )}
              </div>
              {queue.length === 0 ? (
                <div className="ap-queue-empty">Nothing queued</div>
              ) : (
                <ul className="ap-queue-list">
                  {queue.map((c) => (
                    <li key={c.id} className="ap-queue-item">
                      <div className="ap-queue-item-main">
                        <span
                          className="ap-queue-item-title"
                          title={c.talkGroupDescription || `TG ${c.talkGroupNumber}`}
                        >
                          {c.talkGroupDescription || `TG ${c.talkGroupNumber}`}
                        </span>
                        <span className="ap-queue-item-meta">
                          {formatDateTime(c.recordingTime)} · {formatDuration(c.durationSeconds)}
                        </span>
                      </div>
                      <button
                        className="btn ghost ap-queue-item-remove"
                        onClick={() => removeFromQueue(c.id)}
                        aria-label="Remove from queue"
                      >
                        ✕
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>
        <button className="btn ghost" onClick={handleStop} aria-label="Close player">
          ✕
        </button>
      </div>
    </div>
  )
}
