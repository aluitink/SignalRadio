import React, { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { useAudioManager } from '../hooks/useAudioManager'
import { useSubscriptions } from '../hooks/useSubscriptions'
import { usePageTitle } from '../hooks/usePageTitle'
import { apiGet } from '../api'

export default function CallDetailPage() {
  const { id } = useParams<{ id: string }>()
  const callId = parseInt(id || '0', 10)
  
  const [call, setCall] = useState<CallDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  const { 
    playCall, 
    isCallPlaying, 
    playerState, 
    togglePlayerState, 
    enableAutoplayAndStartPlaying, 
    needsUserInteraction,
    currentCallId,
    queue 
  } = useAudioManager()
  const { toggle: toggleSubscription, isSubscribed } = useSubscriptions()

  // Always call hooks - use conditional values for the hook parameters
  const talkGroupDisplay = call?.talkGroup?.description || 
                          call?.talkGroup?.alphaTag || 
                          call?.talkGroup?.name || 
                          `TG ${call?.talkGroup?.number || call?.talkGroupId}` ||
                          'Loading...'
  
  usePageTitle(
    call ? `${talkGroupDisplay} Call` : 'Loading Call...',
    call ? `Call ${call.id}` : 'Call Details'
  )

  useEffect(() => {
    if (!callId) {
      setError('Invalid call ID')
      setLoading(false)
      return
    }

    loadCall()
  }, [callId])

  // Note: We don't auto-queue the call anymore to prevent double-playing
  // The call will be queued when the user clicks the play button

  const loadCall = async () => {
    setLoading(true)
    setError(null)

    try {
      const callData = await apiGet<CallDto>(`/calls/${callId}`)
      setCall(callData)
    } catch (err) {
      console.error('Failed to load call:', err)
      setError('Call not found')
    } finally {
      setLoading(false)
    }
  }

  const handlePlay = async () => {
    if (!call?.recordings?.length) return

    try {
      // Just queue/play the call - let the audio manager handle the rest
      await playCall(call)
    } catch (error) {
      console.error('Failed to control audio player:', error)
    }
  }

  const handleShare = async () => {
    if (!call) return

    const shareUrl = window.location.href
    const talkGroupDisplay = call.talkGroup?.description || 
                            call.talkGroup?.alphaTag || 
                            call.talkGroup?.name || 
                            `TG ${call.talkGroup?.number || call.talkGroupId}`

    if (navigator.share) {
      try {
        await navigator.share({
          title: `${talkGroupDisplay} - SignalRadio`,
          text: call.transcriptions?.[0]?.text || 'Radio call',
          url: shareUrl
        })
      } catch (error) {
        // User cancelled share
      }
    } else {
      try {
        await navigator.clipboard.writeText(shareUrl)
        // Could show a toast here
      } catch (error) {
        console.error('Copy failed:', error)
      }
    }
  }

  if (loading) {
    return (
      <section className="call-detail-page">
        <div className="loading-skeleton">
          <div className="skeleton-header" />
          <div className="skeleton-player" />
          <div className="skeleton-content" />
        </div>

        <style>{`
          .loading-skeleton {
            display: flex;
            flex-direction: column;
            gap: var(--space-3);
          }

          .skeleton-header,
          .skeleton-player,
          .skeleton-content {
            background: var(--bg-card);
            border-radius: var(--radius);
            animation: pulse 1.5s ease-in-out infinite;
          }

          .skeleton-header {
            height: 60px;
          }

          .skeleton-player {
            height: 120px;
          }

          .skeleton-content {
            height: 200px;
          }

          @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
          }
        `}</style>
      </section>
    )
  }

  if (error || !call) {
    return (
      <section className="call-detail-page">
        <div className="error-state">
          <div className="error-icon">⚠️</div>
          <h2>Call Not Found</h2>
          <p className="text-muted">{error || 'This call could not be found.'}</p>
          <Link to="/" className="back-link">← Back to Live Stream</Link>
        </div>

        <style>{`
          .error-state {
            text-align: center;
            padding: var(--space-6) var(--space-2);
            color: var(--text-secondary);
          }

          .error-icon {
            font-size: 48px;
            margin-bottom: var(--space-2);
          }

          .error-state h2 {
            color: var(--text-primary);
            margin-bottom: var(--space-1);
          }

          .back-link {
            color: var(--accent-primary);
            text-decoration: none;
            font-weight: 500;
            transition: var(--transition);
          }

          .back-link:hover {
            text-decoration: underline;
          }
        `}</style>
      </section>
    )
  }

  const duration = call.durationSeconds || call.recordings.reduce((acc, r) => acc + (r.durationSeconds || 0), 0)
  const started = new Date(call.recordingTime)

  const transcriptionText = call.transcriptions && call.transcriptions.length > 0 
    ? call.transcriptions.map(t => t.text).join(' ')
    : null

  function secondsToHuman(s: number) {
    if (!isFinite(s) || s <= 0) return '0s'
    const m = Math.floor(s / 60)
    const sec = Math.floor(s % 60)
    return m ? `${m}m ${sec}s` : `${sec}s`
  }

  return (
    <section className="call-detail-page">
      <header className="call-header">
        <div className="breadcrumb">
          <Link to="/" className="breadcrumb-link">Live Stream</Link>
          <span className="breadcrumb-separator">›</span>
          <Link to={`/talkgroup/${call.talkGroupId}`} className="breadcrumb-link">
            {talkGroupDisplay}
          </Link>
          <span className="breadcrumb-separator">›</span>
          <span className="breadcrumb-current">Call {call.id}</span>
        </div>
        
        <h1>{talkGroupDisplay}</h1>
        
        <div className="call-meta">
          <span className="meta-item">
            📅 {started.toLocaleString()}
          </span>
          <span className="meta-item">
            ⏱️ {secondsToHuman(duration)}
          </span>
          <span className="meta-item">
            📡 {(call.frequencyHz / 1000000).toFixed(3)} MHz
          </span>
          {call.talkGroup?.priority && (
            <span className="meta-item priority">
              Priority {call.talkGroup.priority}
            </span>
          )}
        </div>
      </header>

      <div className="call-player">
        <div className="player-content">
          <div className="player-info">
            {playerState === 'playing' && currentCallId ? (() => {
              // Find the currently playing call from the queue or check if it's this call
              const playingCall = currentCallId === call.id ? call : 
                                 queue.find(queuedCall => queuedCall.id === currentCallId)
              
              if (playingCall) {
                const playingTalkGroup = playingCall.talkGroup?.description || 
                                       playingCall.talkGroup?.alphaTag || 
                                       playingCall.talkGroup?.name || 
                                       `TG ${playingCall.talkGroup?.number || playingCall.talkGroupId}`
                
                return (
                  <>
                    <h3>Now Playing</h3>
                    <p className="text-secondary">
                      {playingCall.id === call.id 
                        ? `Playing this call from ${playingTalkGroup}`
                        : `Playing call from ${playingTalkGroup}`
                      }
                    </p>
                  </>
                )
              }
              
              return (
                <>
                  <h3>Audio Player Active</h3>
                  <p className="text-secondary">Player is running</p>
                </>
              )
            })() : (
              <>
                <h3>Radio Call Recording</h3>
                <p className="text-secondary">
                  {needsUserInteraction 
                    ? 'Click play to start audio player'
                    : 'Audio player is stopped'
                  }
                </p>
              </>
            )}
          </div>
          
          <div className="player-controls">
            <button
              className={`play-btn ${playerState === 'playing' ? 'playing' : ''}`}
              onClick={handlePlay}
              disabled={!call.recordings[0]?.url}
            >
              <span className="play-icon">
                {needsUserInteraction 
                  ? '▶️'
                  : playerState === 'playing' ? '⏸️' : '▶️'
                }
              </span>
              <span className="play-text">
                {needsUserInteraction 
                  ? 'Start Player'
                  : playerState === 'playing' ? 'Stop Player' : 'Start Player'
                }
              </span>
            </button>

            <button
              className={`subscribe-btn ${isSubscribed(call.talkGroupId) ? 'subscribed' : ''}`}
              onClick={() => toggleSubscription(call.talkGroupId)}
            >
              <span className="subscribe-icon">
                {isSubscribed(call.talkGroupId) ? '⭐' : '☆'}
              </span>
            </button>

            <button className="share-btn" onClick={handleShare}>
              <span>📤</span>
            </button>
          </div>
        </div>
      </div>

      <div className="call-content">
        <div className="transcription-section">
          <h3>Transcription</h3>
          {transcriptionText ? (
            <div className="transcription-content">
              <p>{transcriptionText}</p>
            </div>
          ) : (
            <div className="no-transcription">
              <p className="text-muted">No transcription available for this call.</p>
            </div>
          )}
        </div>

        <div className="call-details">
          <h3>Call Details</h3>
          <div className="details-grid">
            <div className="detail-item">
              <span className="detail-label">Call ID</span>
              <span className="detail-value">{call.id}</span>
            </div>
            <div className="detail-item">
              <span className="detail-label">TalkGroup ID</span>
              <span className="detail-value">{call.talkGroupId}</span>
            </div>
            <div className="detail-item">
              <span className="detail-label">Frequency</span>
              <span className="detail-value">{(call.frequencyHz / 1000000).toFixed(3)} MHz</span>
            </div>
            <div className="detail-item">
              <span className="detail-label">Duration</span>
              <span className="detail-value">{secondsToHuman(duration)}</span>
            </div>
            <div className="detail-item">
              <span className="detail-label">Recorded</span>
              <span className="detail-value">{started.toLocaleString()}</span>
            </div>
            {call.talkGroup?.category && (
              <div className="detail-item">
                <span className="detail-label">Category</span>
                <span className="detail-value">{call.talkGroup.category}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      <style>{`
        .call-detail-page {
          min-height: 60vh;
        }

        .call-header {
          margin-bottom: var(--space-4);
        }

        .breadcrumb {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          margin-bottom: var(--space-2);
          font-size: var(--font-size-sm);
          flex-wrap: wrap;
        }

        .breadcrumb-link {
          color: var(--accent-primary);
          text-decoration: none;
          transition: var(--transition);
        }

        .breadcrumb-link:hover {
          text-decoration: underline;
        }

        .breadcrumb-separator {
          color: var(--text-muted);
        }

        .breadcrumb-current {
          color: var(--text-secondary);
        }

        .call-header h1 {
          margin-bottom: var(--space-2);
        }

        .call-meta {
          display: flex;
          gap: var(--space-2);
          flex-wrap: wrap;
        }

        .meta-item {
          background: var(--bg-card);
          color: var(--text-secondary);
          padding: var(--space-1) var(--space-2);
          border-radius: var(--radius-sm);
          font-size: var(--font-size-sm);
          border: 1px solid var(--border);
        }

        .meta-item.priority {
          background: var(--accent-secondary);
          color: white;
          border-color: var(--accent-secondary);
        }

        .call-player {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-4);
          margin-bottom: var(--space-4);
        }

        .player-content {
          display: flex;
          justify-content: space-between;
          align-items: center;
          gap: var(--space-3);
        }

        .player-info h3 {
          margin: 0 0 var(--space-1) 0;
          color: var(--text-primary);
        }

        .player-info p {
          margin: 0;
        }

        .player-controls {
          display: flex;
          gap: var(--space-2);
          align-items: center;
        }

        .play-btn {
          display: flex;
          align-items: center;
          gap: var(--space-2);
          background: var(--accent-primary);
          border: 1px solid var(--accent-primary);
          color: white;
          padding: var(--space-2) var(--space-3);
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          font-weight: 500;
          font-size: var(--font-size-base);
        }

        .play-btn:hover:not(:disabled) {
          background: var(--accent-secondary);
          border-color: var(--accent-secondary);
        }

        .play-btn:disabled {
          opacity: 0.7;
          cursor: not-allowed;
        }

        .play-btn.playing {
          background: #ef4444;
          border-color: #ef4444;
        }

        .play-icon {
          font-size: var(--font-size-lg);
        }

        .subscribe-btn,
        .share-btn {
          background: var(--bg-card);
          border: 1px solid var(--border);
          color: var(--text-secondary);
          width: 44px;
          height: 44px;
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: var(--font-size-lg);
        }

        .subscribe-btn:hover,
        .share-btn:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
        }

        .subscribe-btn.subscribed {
          background: var(--accent-primary);
          border-color: var(--accent-primary);
          color: white;
        }

        .call-content {
          display: grid;
          grid-template-columns: 1fr;
          gap: var(--space-4);
        }

        .transcription-section,
        .call-details {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-3);
        }

        .transcription-section h3,
        .call-details h3 {
          margin: 0 0 var(--space-3) 0;
          color: var(--text-primary);
        }

        .transcription-content p {
          margin: 0;
          line-height: 1.6;
          color: var(--text-primary);
        }

        .no-transcription p {
          margin: 0;
          font-style: italic;
        }

        .details-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          gap: var(--space-3);
        }

        .detail-item {
          display: flex;
          flex-direction: column;
          gap: var(--space-1);
        }

        .detail-label {
          font-size: var(--font-size-sm);
          color: var(--text-muted);
          text-transform: uppercase;
          letter-spacing: 0.5px;
        }

        .detail-value {
          font-weight: 500;
          color: var(--text-primary);
        }

        @media (min-width: 768px) {
          .call-content {
            grid-template-columns: 2fr 1fr;
          }
        }

        @media (max-width: 767px) {
          .player-content {
            flex-direction: column;
            align-items: stretch;
            text-align: center;
          }

          .player-controls {
            justify-content: center;
          }

          .call-meta {
            gap: var(--space-1);
          }

          .meta-item {
            font-size: var(--font-size-xs);
            padding: 4px var(--space-1);
          }

          .details-grid {
            grid-template-columns: 1fr;
          }
        }
      `}</style>
    </section>
  )
}
