import React, { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { audioPlayerService, type PlayerState } from '../services/AudioPlayerService'
import { audioQueue } from '../services/AudioQueue'

export default function AudioPlayer() {
  const [playerState, setPlayerState] = useState<PlayerState>('stopped')
  const [currentCall, setCurrentCall] = useState<CallDto | null>(null)
  const [isPlaying, setIsPlaying] = useState(false)
  const [hasUserInteracted, setHasUserInteracted] = useState(false)
  const [queue, setQueue] = useState<CallDto[]>([])
  const [isExpanded, setIsExpanded] = useState(false)

  // Subscribe to audio player service
  useEffect(() => {
    const unsubscribe = audioPlayerService.subscribe({
      onStateChanged: setPlayerState,
      onCurrentCallChanged: setCurrentCall,
      onPlaybackChanged: setIsPlaying,
      onUserInteractionChanged: setHasUserInteracted
    })

    return unsubscribe
  }, [])

  // Subscribe to queue changes
  useEffect(() => {
    const unsubscribe = audioQueue.subscribe({
      onQueueChanged: setQueue
    })

    return unsubscribe
  }, [])

  const handlePlayPause = async () => {
    if (playerState === 'stopped') {
      try {
        await audioPlayerService.play()
      } catch (error) {
        console.error('Failed to start player:', error)
      }
    } else {
      audioPlayerService.pause()
    }
  }

  const handleClearQueue = () => {
    audioPlayerService.clearQueue()
  }

  const handleQueueItemClick = (call: CallDto) => {
    audioPlayerService.moveToFront(call.id)
    setIsExpanded(false)
  }

  const handleRemoveFromQueue = (callId: number, e: React.MouseEvent) => {
    e.stopPropagation()
    audioPlayerService.removeFromQueue(callId)
  }

  const getCurrentTalkGroupDisplay = () => {
    if (!hasUserInteracted) {
      return '🚫 Click to enable audio'
    }
    
    if (isPlaying && currentCall?.talkGroup) {
      return currentCall.talkGroup.description || 
             currentCall.talkGroup.alphaTag || 
             currentCall.talkGroup.name || 
             `TG ${currentCall.talkGroup.number || currentCall.talkGroupId}`
    }
    
    if (isPlaying && currentCall) {
      return `TG ${currentCall.talkGroupId}`
    }
    
    if (playerState === 'stopped') {
      return queue.length > 0 ? `${queue.length} calls queued` : '⏸️ Player stopped'
    }
    
    return ''
  }

  const getPlayButtonState = () => {
    if (!hasUserInteracted) {
      return { icon: '▶️', title: 'Click to enable audio and start player' }
    }
    
    if (playerState === 'playing') {
      return { icon: '⏸️', title: 'Pause player' }
    }
    
    return { icon: '▶️', title: 'Start player' }
  }

  const formatTime = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  }

  const formatDuration = (seconds: number) => {
    if (!isFinite(seconds) || seconds <= 0) return '0s'
    const m = Math.floor(seconds / 60)
    const s = Math.floor(seconds % 60)
    return m ? `${m}m ${s}s` : `${s}s`
  }

  const playButtonState = getPlayButtonState()

  return (
    <div className="new-audio-player">
      <div className="player-main">
        {/* Left section - Controls */}
        <div className="player-controls">
          <button
            className={`play-pause-btn ${playerState === 'playing' ? 'playing' : ''} ${!hasUserInteracted ? 'blocked' : ''}`}
            onClick={handlePlayPause}
            title={playButtonState.title}
          >
            <span className="play-icon">{playButtonState.icon}</span>
          </button>
        </div>

        {/* Center section - Now playing info */}
        <div className="player-left">
          <div className="track-info">
            <div className={`track-title ${!hasUserInteracted ? 'blocked' : ''} ${playerState === 'stopped' && hasUserInteracted ? 'stopped' : ''}`}>
              {getCurrentTalkGroupDisplay()}
            </div>
            {currentCall && isPlaying && (
              <div className="track-subtitle">
                {formatTime(currentCall.recordingTime)}
              </div>
            )}
          </div>
        </div>

        {/* Right section - Queue info */}
        <div className="player-center">
          {queue.length > 0 && (
            <>
              <button
                className="queue-toggle"
                onClick={() => setIsExpanded(!isExpanded)}
                title={`${queue.length} calls in queue`}
              >
                <span className="queue-icon">🎵</span>
                <span className="queue-count">{queue.length}</span>
                <span className="expand-arrow">{isExpanded ? '▼' : '▲'}</span>
              </button>
              
              <button
                className="clear-queue-btn"
                onClick={handleClearQueue}
                title="Clear queue"
              >
                🗑️
              </button>
            </>
          )}
        </div>
      </div>

      {/* Expanded queue */}
      {isExpanded && queue.length > 0 && (
        <div className="queue-expanded">
          <div className="queue-header">
            <h3>Playback Queue ({queue.length})</h3>
            <button
              className="queue-close"
              onClick={() => setIsExpanded(false)}
              title="Close queue"
            >
              ✕
            </button>
          </div>
          
          <div className="queue-list">
            {queue.map((call, index) => (
              <div 
                key={call.id} 
                className={`queue-item ${call.id === currentCall?.id ? 'current' : ''}`}
                onClick={() => handleQueueItemClick(call)}
              >
                <div className="queue-position">
                  {index + 1}.
                </div>
                
                <div className="queue-call-info">
                  <div className="queue-talkgroup">
                    {call.talkGroup?.description ? (
                      <Link 
                        to={`/talkgroup/${call.talkGroupId}`} 
                        className="talkgroup-link" 
                        onClick={e => e.stopPropagation()}
                      >
                        {call.talkGroup.description}
                      </Link>
                    ) : (
                      <Link 
                        to={`/talkgroup/${call.talkGroupId}`} 
                        className="talkgroup-link" 
                        onClick={e => e.stopPropagation()}
                      >
                        TG {call.talkGroupId}
                        {call.talkGroup?.alphaTag && (
                          <span className="alpha-tag"> - {call.talkGroup.alphaTag}</span>
                        )}
                      </Link>
                    )}
                  </div>
                  
                  <div className="queue-details">
                    <span className="queue-time">{formatTime(call.recordingTime)}</span>
                    <span className="queue-duration">
                      {formatDuration(call.durationSeconds || 0)}
                    </span>
                  </div>
                </div>

                <button
                  className="queue-remove"
                  onClick={(e) => handleRemoveFromQueue(call.id, e)}
                  title="Remove from queue"
                >
                  ✕
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      <style>{`
        .new-audio-player {
          position: fixed;
          bottom: 0;
          left: 0;
          right: 0;
          background: var(--bg-card);
          border-top: 1px solid var(--border);
          backdrop-filter: blur(10px);
          z-index: 1000;
        }

        .player-main {
          display: flex;
          align-items: center;
          padding: var(--space-2);
          gap: var(--space-2);
          max-width: var(--content-width);
          margin: 0 auto;
        }

        .player-left {
          flex: 1;
          min-width: 0;
        }

        .track-info {
          display: flex;
          flex-direction: column;
          gap: var(--space-1);
        }

        .track-title {
          font-weight: 600;
          color: var(--text-primary);
          font-size: var(--font-size-sm);
          truncate: ellipsis;
          white-space: nowrap;
          overflow: hidden;
        }

        .track-title.blocked {
          color: var(--error-text);
        }

        .track-title.stopped {
          color: var(--text-secondary);
        }

        .track-subtitle {
          font-size: var(--font-size-xs);
          color: var(--text-secondary);
        }

        .player-center {
          display: flex;
          align-items: center;
          gap: var(--space-1);
        }

        .queue-toggle {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          background: var(--bg-secondary);
          border: 1px solid var(--border);
          color: var(--text-primary);
          padding: var(--space-1) var(--space-2);
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          font-size: var(--font-size-sm);
        }

        .queue-toggle:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .queue-count {
          font-weight: 600;
        }

        .expand-arrow {
          transition: transform 0.2s ease;
        }

        .player-controls {
          display: flex;
          align-items: center;
          gap: var(--space-1);
        }

        .play-pause-btn {
          background: var(--accent-primary);
          border: none;
          color: white;
          width: 48px;
          height: 48px;
          border-radius: 50%;
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: var(--font-size-lg);
        }

        .play-pause-btn:hover {
          background: var(--accent-primary-hover);
          transform: scale(1.05);
        }

        .play-pause-btn.blocked {
          background: var(--error-bg);
          color: var(--error-text);
        }

        .clear-queue-btn {
          background: none;
          border: 1px solid var(--border);
          color: var(--text-secondary);
          width: 32px;
          height: 32px;
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .clear-queue-btn:hover {
          background: var(--error-bg);
          border-color: var(--error-border);
          color: var(--error-text);
        }

        .queue-expanded {
          border-top: 1px solid var(--border);
          background: var(--bg-secondary);
          max-height: 300px;
          overflow-y: auto;
        }

        .queue-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: var(--space-2);
          border-bottom: 1px solid var(--border);
        }

        .queue-header h3 {
          margin: 0;
          font-size: var(--font-size-md);
          color: var(--text-primary);
        }

        .queue-close {
          background: none;
          border: none;
          color: var(--text-secondary);
          cursor: pointer;
          font-size: var(--font-size-lg);
          padding: var(--space-1);
        }

        .queue-close:hover {
          color: var(--text-primary);
        }

        .queue-list {
          display: flex;
          flex-direction: column;
        }

        .queue-item {
          display: flex;
          align-items: center;
          gap: var(--space-2);
          padding: var(--space-2);
          border-bottom: 1px solid var(--border);
          cursor: pointer;
          transition: var(--transition);
        }

        .queue-item:hover {
          background: var(--bg-card-hover);
        }

        .queue-item.current {
          background: var(--accent-bg);
          color: var(--accent-text);
        }

        .queue-position {
          font-weight: 600;
          color: var(--text-secondary);
          min-width: 24px;
        }

        .queue-call-info {
          flex: 1;
          min-width: 0;
        }

        .queue-talkgroup {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          margin-bottom: var(--space-1);
        }

        .talkgroup-link {
          color: var(--accent-primary);
          text-decoration: none;
          font-weight: 600;
          transition: var(--transition);
        }

        .talkgroup-link:hover {
          color: var(--accent-primary-hover);
        }

        .alpha-tag {
          color: var(--text-secondary);
          font-weight: normal;
        }

        .talkgroup-description {
          color: var(--text-secondary);
          font-weight: normal;
        }

        .queue-details {
          display: flex;
          gap: var(--space-2);
          font-size: var(--font-size-xs);
          color: var(--text-secondary);
        }

        .queue-remove {
          background: none;
          border: none;
          color: var(--text-secondary);
          cursor: pointer;
          padding: var(--space-1);
          font-size: var(--font-size-sm);
          border-radius: var(--radius);
          transition: var(--transition);
        }

        .queue-remove:hover {
          background: var(--error-bg);
          color: var(--error-text);
        }

        @media (max-width: 767px) {
          .player-main {
            gap: var(--space-1);
            padding: var(--space-1);
          }

          .play-pause-btn {
            width: 40px;
            height: 40px;
            font-size: var(--font-size-md);
          }

          .track-title {
            font-size: var(--font-size-xs);
          }

          .queue-expanded {
            max-height: 200px;
          }
        }
      `}</style>
    </div>
  )
}
