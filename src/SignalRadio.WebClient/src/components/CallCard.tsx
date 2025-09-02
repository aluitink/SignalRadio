import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { useAudioManager } from '../hooks/useAudioManager'
import { useSubscriptions } from '../hooks/useSubscriptions'

interface CallCardProps {
  call: CallDto
  autoPlay?: boolean
  showSubscribeButton?: boolean
}

function secondsToHuman(s: number) {
  if (!isFinite(s) || s <= 0) return '0s'
  const m = Math.floor(s / 60)
  const sec = Math.floor(s % 60)
  return m ? `${m}m ${sec}s` : `${sec}s`
}

function formatTime(dateString: string) {
  const date = new Date(dateString)
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

export default function CallCard({ 
  call, 
  autoPlay = false, 
  showSubscribeButton = true
}: CallCardProps) {
  const [hasError, setHasError] = useState(false)
  const { playCall, isCallPlaying } = useAudioManager()
  const { isSubscribed, toggle: toggleSubscription, isPending } = useSubscriptions()
  
  const isCardPlaying = isCallPlaying(call.id)
  const isSubscribedToTalkGroup = isSubscribed(call.talkGroupId)
  const isSubscriptionPending = isPending(call.talkGroupId)

  // Calculate total duration from call duration or recordings
  const duration = call.durationSeconds || call.recordings.reduce((acc, r) => acc + (r.durationSeconds || 0), 0)
  const started = new Date(call.recordingTime)
  const ageSec = Math.floor((Date.now() - started.getTime()) / 1000)

  // Get talkgroup display name
  const talkGroupDisplay = call.talkGroup?.description || 
                          call.talkGroup?.alphaTag || 
                          call.talkGroup?.name || 
                          `TG ${call.talkGroup?.number || call.talkGroupId}`

  // Get recording URL
  const recordingUrl = call.recordings[0]?.url

  const handlePlay = async () => {
    if (!call.recordings?.length || hasError) return

    try {
      // Use the audio manager hook - clicking call now adds to queue
      await playCall(call)
    } catch (error) {
      console.error('Audio play failed:', error)
      setHasError(true)
    }
  }

  const handleCardClick = (e: React.MouseEvent) => {
    // Don't trigger play if clicking on links or buttons
    if ((e.target as HTMLElement).closest('a, button')) return
    
    e.preventDefault()
    handlePlay()
  }

  const handleSubscribe = async (e: React.MouseEvent) => {
    e.stopPropagation()
    
    try {
      await toggleSubscription(call.talkGroupId)
    } catch (error) {
      console.error('Failed to toggle subscription:', error)
      // Could show an error toast here
    }
  }

  const handleShare = async (e: React.MouseEvent) => {
    e.stopPropagation()
    
    const shareUrl = `${window.location.origin}/call/${call.id}`
    
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

  const transcriptionText = call.transcriptions && call.transcriptions.length > 0 
    ? call.transcriptions.map(t => t.text).join(' ')
    : null

  return (
    <article 
      className={`call-card ${isCardPlaying ? 'playing' : ''} ${hasError ? 'error' : ''}`}
      onClick={handleCardClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          handlePlay()
        }
      }}
      aria-label={`Play call from ${talkGroupDisplay}`}
    >
      {isCardPlaying && (
        <div className="audio-progress-indicator" />
      )}

      <div className="call-header">
        <div className="call-talkgroup">
          <Link 
            to={`/talkgroup/${call.talkGroupId}`}
            className="talkgroup-link"
            onClick={(e) => e.stopPropagation()}
          >
            {talkGroupDisplay}
          </Link>
          {call.talkGroup?.priority && (
            <span className="priority-badge">P{call.talkGroup.priority}</span>
          )}
        </div>
        
        <div className="call-actions">
          {showSubscribeButton && (
            <button
              className={`subscribe-btn ${isSubscribedToTalkGroup ? 'subscribed' : ''} ${isSubscriptionPending ? 'pending' : ''}`}
              onClick={handleSubscribe}
              disabled={isSubscriptionPending}
              aria-label={isSubscribedToTalkGroup ? 'Unsubscribe from talkgroup' : 'Subscribe to talkgroup'}
              title={isSubscribedToTalkGroup ? 'Unsubscribe from talkgroup' : 'Subscribe to talkgroup'}
            >
              {isSubscriptionPending ? '⏳' : (isSubscribedToTalkGroup ? '⭐' : '☆')}
            </button>
          )}
          
          <button
            className="share-btn"
            onClick={handleShare}
            aria-label="Share call"
            title="Share call"
          >
            📤
          </button>
        </div>
      </div>

      <div className="call-meta">
        <span className="call-time" title={started.toLocaleString()}>
          {formatTime(call.recordingTime)}
        </span>
        <span className="call-duration">{secondsToHuman(duration)}</span>
        <span className="call-frequency">
          {(call.frequencyHz / 1000000).toFixed(3)} MHz
        </span>
        <span className="call-age text-muted">{secondsToHuman(ageSec)} ago</span>
      </div>

      {transcriptionText && (
        <div className="call-transcript">
          <p>{transcriptionText}</p>
        </div>
      )}

      {!transcriptionText && (
        <div className="call-transcript">
          <p className="text-muted">No transcription available</p>
        </div>
      )}

      {isCardPlaying && (
        <div className="playing-indicator">
          <span className="playing-icon">🔊</span>
          <span className="playing-text">Playing...</span>
        </div>
      )}

      <style>{`
        .call-card {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-2);
          margin-bottom: var(--space-2);
          cursor: pointer;
          transition: var(--transition);
          position: relative;
          overflow: hidden;
        }

        .call-card:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          transform: translateY(-1px);
        }

        .call-card:focus {
          outline: 2px solid var(--accent-primary);
          outline-offset: 2px;
        }

        .call-card.playing {
          border-color: var(--accent-primary);
          background: var(--bg-card-hover);
        }

        .call-card.error {
          border-color: #ef4444;
          opacity: 0.7;
        }

        .audio-progress-indicator {
          position: absolute;
          top: 0;
          left: 0;
          right: 0;
          height: 2px;
          background: var(--accent-primary);
          animation: pulse 2s ease-in-out infinite;
          z-index: 1;
        }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }

        .call-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--space-2);
          gap: var(--space-2);
        }

        .call-talkgroup {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          flex: 1;
          min-width: 0;
        }

        .talkgroup-link {
          color: var(--text-primary);
          text-decoration: none;
          font-weight: 600;
          font-size: var(--font-size-lg);
          transition: var(--transition);
          word-break: break-word;
        }

        .talkgroup-link:hover {
          color: var(--accent-primary);
        }

        .priority-badge {
          background: var(--accent-secondary);
          color: white;
          padding: 2px 6px;
          border-radius: var(--radius-sm);
          font-size: var(--font-size-xs);
          font-weight: 600;
          flex-shrink: 0;
        }

        .call-actions {
          display: flex;
          gap: var(--space-1);
          flex-shrink: 0;
        }

        .subscribe-btn,
        .share-btn {
          background: none;
          border: 1px solid var(--border);
          color: var(--text-secondary);
          width: 32px;
          height: 32px;
          border-radius: var(--radius-sm);
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: var(--font-size-sm);
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

        .subscribe-btn.pending {
          opacity: 0.6;
          cursor: not-allowed;
        }

        .subscribe-btn.pending:hover {
          background: none;
          border-color: var(--border);
          color: var(--text-secondary);
        }

        .call-meta {
          display: flex;
          gap: var(--space-2);
          margin-bottom: var(--space-2);
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
          flex-wrap: wrap;
        }

        .call-meta span {
          white-space: nowrap;
        }

        .call-transcript {
          margin-bottom: var(--space-1);
        }

        .call-transcript p {
          margin: 0;
          line-height: 1.5;
          color: var(--text-primary);
        }

        .playing-indicator {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          color: var(--accent-primary);
          font-size: var(--font-size-sm);
          font-weight: 500;
        }

        .playing-icon {
          animation: pulse 1.5s ease-in-out infinite;
        }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }

        @media (max-width: 767px) {
          .call-meta {
            gap: var(--space-1);
            font-size: var(--font-size-xs);
          }
          
          .call-header {
            flex-direction: column;
            align-items: flex-start;
            gap: var(--space-1);
          }
          
          .call-actions {
            align-self: flex-end;
          }
        }
      `}</style>
    </article>
  )
}
