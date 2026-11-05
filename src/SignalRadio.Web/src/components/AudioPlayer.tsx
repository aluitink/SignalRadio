import { useAudioPlayer } from '../contexts/AudioPlayerContext'
import { formatDuration } from '../utils/format'

export default function AudioPlayer() {
  const { nowPlaying, isPlaying, currentTime, duration, toggle, stop, seek } = useAudioPlayer()

  if (!nowPlaying) return null

  const handleSeek = (e: React.ChangeEvent<HTMLInputElement>) => {
    seek(Number(e.target.value))
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
        <button className="btn ghost" onClick={stop} aria-label="Close player">
          ✕
        </button>
      </div>
    </div>
  )
}
