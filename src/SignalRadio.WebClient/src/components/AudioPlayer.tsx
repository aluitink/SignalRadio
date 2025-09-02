import React, { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { useAudioManager } from '../hooks/useAudioManager'

export default function AudioPlayer() {
  const { 
    queueLength, 
    currentCallId, 
    currentCall,
    clearQueue, 
    removeFromQueue, 
    queue, 
    isPlaying,
    stopPlayback,
    pausePlayback,
    resumePlayback,
    togglePlayback,
    autoplayEnabled,
    autoplayChecked,
    playerState,
    needsUserInteraction,
    enableAutoplayAndStartPlaying,
    togglePlayerState,
    testAutoplayWithSilence,
    playCall,
    setVolume,
    getVolume,
    moveToFront,
    getUserHasInteracted
  } = useAudioManager()
  
  const [isExpanded, setIsExpanded] = useState(false)
  const [volume, setVolumeState] = useState(getVolume())
  const [autoplayTestResult, setAutoplayTestResult] = useState<'testing' | 'passed' | 'failed' | null>(null)

  // Get current call info - now using currentCall directly from the hook
  const userHasInteracted = getUserHasInteracted()

  // Test autoplay on component mount
  useEffect(() => {
    const testAutoplay = async () => {
      if (!autoplayChecked && !userHasInteracted) {
        setAutoplayTestResult('testing')
        try {
          const result = await testAutoplayWithSilence()
          setAutoplayTestResult(result ? 'passed' : 'failed')
          
          // Auto-clear the test result after a few seconds
          setTimeout(() => {
            setAutoplayTestResult(null)
          }, 3000)
        } catch (error) {
          console.error('Autoplay test failed:', error)
          setAutoplayTestResult('failed')
          setTimeout(() => {
            setAutoplayTestResult(null)
          }, 3000)
        }
      }
    }

    testAutoplay()
  }, [autoplayChecked, userHasInteracted, testAutoplayWithSilence])

  const formatDuration = (seconds: number) => {
    const mins = Math.floor(seconds / 60)
    const secs = Math.floor(seconds % 60)
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  const formatTime = (timestamp: string) => {
    const date = new Date(timestamp)
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  }

  const toggleExpanded = () => {
    setIsExpanded(!isExpanded)
  }

  const handlePlayClick = async () => {
    if (autoplayTestResult === 'testing') {
      // Don't allow clicks while testing
      return
    }
    
    if (needsUserInteraction) {
      // First click - enable autoplay and start playing
      try {
        await enableAutoplayAndStartPlaying()
      } catch (error) {
        console.error('Failed to enable autoplay:', error)
      }
    } else {
      // User has already interacted - toggle player state
      togglePlayerState()
    }
  }

  const handleVolumeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newVolume = parseInt(e.target.value)
    setVolumeState(newVolume)
    setVolume(newVolume)
  }

  const handleQueueItemClick = (call: CallDto) => {
    // Move the clicked call to the front of the queue and start playing
    moveToFront(call.id)
    playCall(call).catch((error) => {
      console.warn('Failed to play selected call:', error.message)
    })
    setIsExpanded(false)
  }

  const getCurrentTalkGroupDisplay = () => {
    if (autoplayTestResult === 'testing') {
      return '🔊 Testing audio...'
    }
    if (autoplayTestResult === 'passed') {
      return '✅ Audio ready!'
    }
    if (autoplayTestResult === 'failed') {
      return '❌ Click to enable audio'
    }
    
    // When actively playing, show what's currently playing
    if (isPlaying && currentCall?.talkGroup) {
      return currentCall.talkGroup.description || 
             currentCall.talkGroup.alphaTag || 
             currentCall.talkGroup.name || 
             `TG ${currentCall.talkGroup.number || currentCall.talkGroupId}`
    }
    if (isPlaying && currentCall) {
      return `TG ${currentCall.talkGroupId}`
    }
    
    // When not playing, show appropriate status
    if (needsUserInteraction) {
      return '🚫 Click to enable audio'
    }
    if (playerState === 'stopped') {
      return '⏸️ Player stopped'
    }
    if (queueLength > 0) {
      return `${queueLength} calls queued`
    }
    return '' // Show nothing when idle
  }

  const getPlayButtonState = () => {
    if (autoplayTestResult === 'testing') {
      return { icon: '⏳', title: 'Testing autoplay capability...' }
    }
    if (needsUserInteraction) {
      return { icon: '🔊', title: 'Click to enable autoplay and start audio' }
    }
    if (playerState === 'playing') {
      return { icon: '⏸️', title: 'Stop playback (pause autoplay)' }
    }
    return { icon: '▶️', title: 'Start playback (resume autoplay)' }
  }

  const playButtonState = getPlayButtonState()

  // Always show the player - this is the core requirement
  return (
    <div className="audio-player">
      {/* Expanded queue view */}
      {isExpanded && queueLength > 0 && (
        <div className="queue-expanded">
          <div className="queue-header">
            <h4>Call Queue ({queueLength})</h4>
            <button 
              type="button" 
              className="clear-queue-btn"
              onClick={clearQueue}
              title="Clear all calls from queue"
            >
              Clear All
            </button>
          </div>
          
          <div className="queue-list">
            {queue.map((call, index) => (
              <div 
                key={call.id} 
                className={`queue-item ${call.id === currentCallId ? 'current' : ''}`}
                onClick={() => handleQueueItemClick(call)}
              >
                <div className="queue-position">
                  {index + 1}.
                </div>
                
                <div className="queue-call-info">
                  <div className="queue-talkgroup">
                    <Link to={`/talkgroup/${call.talkGroupId}`} className="talkgroup-link" onClick={e => e.stopPropagation()}>
                      TG {call.talkGroupId}
                    </Link>
                    {call.talkGroup?.alphaTag && (
                      <span className="alpha-tag"> - {call.talkGroup.alphaTag}</span>
                    )}
                  </div>
                  
                  <div className="queue-details">
                    <span className="queue-time">{formatTime(call.recordingTime)}</span>
                    <span className="queue-duration">
                      {formatDuration(call.durationSeconds)}
                    </span>
                  </div>
                </div>
                
                <button
                  type="button"
                  className="remove-from-queue-btn"
                  onClick={(e) => {
                    e.stopPropagation()
                    removeFromQueue(call.id)
                  }}
                  title="Remove from queue"
                >
                  ×
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Main player controls */}
      <div className="player-controls">
        {/* Left section - Play/Pause and track info */}
        <div className="player-left">
          <button
            className={`play-pause-btn ${playerState === 'playing' ? 'playing' : 'paused'} ${autoplayTestResult === 'testing' ? 'testing' : needsUserInteraction ? 'consent-needed' : ''}`}
            onClick={handlePlayClick}
            title={playButtonState.title}
          >
            {playButtonState.icon}
          </button>
          
          <div className="track-info">
            <div className={`track-title ${autoplayTestResult ? `test-${autoplayTestResult}` : ''} ${needsUserInteraction ? 'blocked' : ''} ${playerState === 'stopped' && !needsUserInteraction ? 'paused' : ''}`}>
              {getCurrentTalkGroupDisplay()}
            </div>
            {currentCall && isPlaying && (
              <div className="track-subtitle">
                {formatTime(currentCall.recordingTime)}
              </div>
            )}
          </div>
        </div>

        {/* Center section - Queue info and status */}
        <div className="player-center">
          {queueLength > 0 && (
            <button
              className="queue-toggle"
              onClick={toggleExpanded}
              title={`${queueLength} calls in queue`}
            >
              <span className="queue-icon">🎵</span>
              <span className="queue-count">{queueLength}</span>
              <span className="expand-arrow">{isExpanded ? '▼' : '▲'}</span>
            </button>
          )}
          
          {userHasInteracted && autoplayEnabled && (
            <div className="status-indicator">
              <span className="status-dot live"></span>
              <span className="status-text">Live</span>
            </div>
          )}

          {userHasInteracted && !autoplayEnabled && (
            <div className="status-indicator">
              <span className="status-dot disabled"></span>
              <span className="status-text">Manual</span>
            </div>
          )}
        </div>

        {/* Right section - Volume controls */}
        <div className="player-right">
          <div className="volume-control">
            <span className="volume-icon">🔊</span>
            <input
              type="range"
              min="0"
              max="100"
              value={volume}
              onChange={handleVolumeChange}
              className="volume-slider"
              title={`Volume: ${volume}%`}
            />
            <span className="volume-value">{volume}%</span>
          </div>
        </div>
      </div>

      <style>{`
        .audio-player {
          position: fixed;
          bottom: 0;
          left: 0;
          right: 0;
          background: var(--bg-secondary);
          border-top: 1px solid var(--border);
          z-index: 1000;
          box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.3);
        }

        .queue-expanded {
          border-bottom: 1px solid var(--border);
          background: rgba(0, 0, 0, 0.4);
          max-height: 250px;
          overflow-y: auto;
        }

        .queue-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: var(--space-2);
          border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        .queue-header h4 {
          margin: 0;
          font-size: 0.9rem;
          font-weight: 600;
          color: var(--text-primary);
        }

        .clear-queue-btn {
          background: var(--bg-danger);
          border: none;
          border-radius: var(--radius-sm);
          padding: var(--space-1) var(--space-2);
          color: white;
          font-size: 0.8rem;
          font-weight: 500;
          cursor: pointer;
          transition: var(--transition);
        }

        .clear-queue-btn:hover {
          background: var(--bg-danger-hover);
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
          border-bottom: 1px solid rgba(255, 255, 255, 0.05);
          cursor: pointer;
          transition: var(--transition);
        }

        .queue-item:hover {
          background: rgba(255, 255, 255, 0.05);
        }

        .queue-item.current {
          background: rgba(var(--accent-primary-rgb), 0.1);
          border-left: 3px solid var(--accent-primary);
        }

        .queue-item:last-child {
          border-bottom: none;
        }

        .queue-position {
          font-weight: 600;
          color: var(--accent-primary);
          min-width: 24px;
          font-size: 0.8rem;
        }

        .queue-call-info {
          flex: 1;
          min-width: 0;
        }

        .queue-talkgroup {
          display: flex;
          align-items: center;
          margin-bottom: var(--space-1);
        }

        .talkgroup-link {
          color: var(--accent-primary);
          text-decoration: none;
          font-weight: 500;
          font-size: 0.85rem;
          transition: var(--transition);
        }

        .talkgroup-link:hover {
          color: var(--accent-primary-hover);
          text-decoration: underline;
        }

        .alpha-tag {
          color: var(--text-secondary);
          font-size: 0.8em;
          font-weight: normal;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .queue-details {
          display: flex;
          gap: var(--space-2);
          font-size: 0.75rem;
          color: var(--text-secondary);
        }

        .queue-time::before {
          content: '🕐 ';
        }

        .queue-duration::before {
          content: '⏱️ ';
        }

        .remove-from-queue-btn {
          background: none;
          border: none;
          color: var(--text-secondary);
          cursor: pointer;
          font-size: 1rem;
          font-weight: bold;
          padding: var(--space-1);
          border-radius: var(--radius-sm);
          transition: var(--transition);
          min-width: 24px;
          height: 24px;
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .remove-from-queue-btn:hover {
          background: var(--bg-danger);
          color: white;
        }

        .player-controls {
          display: flex;
          align-items: center;
          padding: var(--space-2) var(--space-3);
          min-height: 64px;
        }

        .player-left {
          display: flex;
          align-items: center;
          gap: var(--space-2);
          flex: 1;
          min-width: 0;
        }

        .play-pause-btn {
          background: var(--accent-primary);
          border: none;
          border-radius: 50%;
          width: 40px;
          height: 40px;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          transition: var(--transition);
          font-size: 1.2rem;
          flex-shrink: 0;
        }

        .play-pause-btn:hover {
          background: var(--accent-primary-hover);
          transform: scale(1.05);
        }

        .play-pause-btn.playing {
          background: var(--accent-secondary);
        }

        .play-pause-btn.consent-needed {
          background: var(--bg-warning);
          animation: pulse-consent 2s infinite;
        }

        .play-pause-btn.testing {
          background: var(--text-secondary);
          cursor: not-allowed;
          animation: pulse-testing 1.5s infinite;
        }

        .play-pause-btn.testing:hover {
          transform: none;
          background: var(--text-secondary);
        }

        @keyframes pulse-consent {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.7; }
        }

        @keyframes pulse-testing {
          0%, 100% { opacity: 0.6; }
          50% { opacity: 1; }
        }

        .track-info {
          min-width: 0;
          overflow: hidden;
        }

        .track-title {
          font-weight: 600;
          color: var(--text-primary);
          font-size: 0.9rem;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .track-title.test-testing {
          color: var(--text-secondary);
          animation: pulse-text 1.5s infinite;
        }

        .track-title.test-passed {
          color: var(--text-success, #10b981);
        }

        .track-title.test-failed,
        .track-title.blocked {
          color: var(--text-warning, #f59e0b);
        }

        .track-title.paused {
          color: var(--text-secondary);
          font-style: italic;
        }

        @keyframes pulse-text {
          0%, 100% { opacity: 0.7; }
          50% { opacity: 1; }
        }

        .track-subtitle {
          font-size: 0.8rem;
          color: var(--text-secondary);
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .consent-subtitle {
          font-size: 0.75rem;
          color: var(--bg-warning);
          font-style: italic;
        }

        .player-center {
          display: flex;
          align-items: center;
          gap: var(--space-3);
          flex-shrink: 0;
        }

        .queue-toggle {
          background: rgba(255, 255, 255, 0.05);
          border: 1px solid rgba(255, 255, 255, 0.1);
          border-radius: var(--radius);
          padding: var(--space-1) var(--space-2);
          display: flex;
          align-items: center;
          gap: var(--space-1);
          cursor: pointer;
          transition: var(--transition);
          color: var(--text-secondary);
          font-size: 0.8rem;
        }

        .queue-toggle:hover {
          background: rgba(255, 255, 255, 0.1);
          border-color: var(--accent-primary);
        }

        .queue-count {
          background: var(--accent-primary);
          color: white;
          border-radius: 50%;
          min-width: 16px;
          height: 16px;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 0.7rem;
          font-weight: 600;
          line-height: 1;
        }

        .status-indicator {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          color: var(--text-secondary);
          font-size: 0.8rem;
        }

        .status-dot {
          width: 8px;
          height: 8px;
          border-radius: 50%;
        }

        .status-dot.live {
          background: var(--accent-primary);
          animation: pulse 2s infinite;
        }

        .status-dot.disabled {
          background: var(--text-secondary);
        }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.4; }
        }

        .player-right {
          flex: 1;
          display: flex;
          justify-content: flex-end;
          min-width: 0;
        }

        .volume-control {
          display: flex;
          align-items: center;
          gap: var(--space-2);
          color: var(--text-secondary);
        }

        .volume-icon {
          font-size: 0.9rem;
          flex-shrink: 0;
        }

        .volume-slider {
          width: 100px;
          height: 4px;
          border-radius: 2px;
          background: rgba(255, 255, 255, 0.2);
          outline: none;
          appearance: none;
          cursor: pointer;
        }

        .volume-slider::-webkit-slider-thumb {
          appearance: none;
          width: 16px;
          height: 16px;
          border-radius: 50%;
          background: var(--accent-primary);
          cursor: pointer;
          transition: var(--transition);
        }

        .volume-slider::-webkit-slider-thumb:hover {
          background: var(--accent-primary-hover);
          transform: scale(1.1);
        }

        .volume-slider::-moz-range-thumb {
          width: 16px;
          height: 16px;
          border-radius: 50%;
          background: var(--accent-primary);
          border: none;
          cursor: pointer;
        }

        .volume-value {
          font-size: 0.8rem;
          min-width: 35px;
          text-align: right;
        }

        /* Mobile responsive */
        @media (max-width: 768px) {
          .player-controls {
            padding: var(--space-2);
            min-height: 56px;
          }

          .player-left {
            flex: 2;
          }

          .player-center {
            gap: var(--space-2);
          }

          .player-right {
            flex: 1;
          }

          .play-pause-btn {
            width: 36px;
            height: 36px;
            font-size: 1rem;
          }

          .track-title {
            font-size: 0.85rem;
          }

          .track-subtitle {
            font-size: 0.75rem;
          }

          .volume-slider {
            width: 80px;
          }

          .volume-control {
            display: none;
          }

          .queue-expanded {
            max-height: 200px;
          }

          .queue-item {
            padding: var(--space-1) var(--space-2);
          }
          
          .alpha-tag {
            display: none;
          }
        }
      `}</style>
    </div>
  )
}
