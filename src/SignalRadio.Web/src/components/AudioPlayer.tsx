import { useAudioPlayer } from '../contexts/AudioPlayerContext'

export default function AudioPlayer() {
  const { nowPlaying, isPlaying, toggle, stop } = useAudioPlayer()

  if (!nowPlaying) return null

  return (
    <div className="audio-player" role="region" aria-label="Audio player">
      <div className="ap-inner">
        <button
          className="btn ghost"
          onClick={toggle}
          aria-label={isPlaying ? 'Pause' : 'Play'}
        >
          {isPlaying ? '⏸' : '▶'}
        </button>
        <span className="ap-title" title={nowPlaying.title}>{nowPlaying.title}</span>
        <audio
          src={nowPlaying.url}
          controls
          autoPlay={isPlaying}
          aria-label="Call audio"
        />
        <button className="btn ghost" onClick={stop} aria-label="Close player">
          ✕
        </button>
      </div>
    </div>
  )
}
