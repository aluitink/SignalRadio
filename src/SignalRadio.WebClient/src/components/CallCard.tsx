import React from 'react'
import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { audioPlayerService } from '../services/AudioPlayerService'
import { useSubscriptions } from '../contexts/SubscriptionContext'

interface CallCardProps {
  call: CallDto
}

function formatTime(dateString: string) {
  const date = new Date(dateString)
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

function secondsToHuman(s: number) {
  if (!isFinite(s) || s <= 0) return '0s'
  const m = Math.floor(s / 60)
  const sec = Math.floor(s % 60)
  return m ? `${m}m ${sec}s` : `${sec}s`
}

function secondsToAge(s: number) {
  if (!isFinite(s) || s <= 0) return '0s'
  
  const days = Math.floor(s / 86400)
  const hours = Math.floor((s % 86400) / 3600)
  const minutes = Math.floor((s % 3600) / 60)
  const seconds = Math.floor(s % 60)
  
  const parts = []
  if (days > 0) parts.push(`${days}d`)
  if (hours > 0) parts.push(`${hours}h`)
  if (minutes > 0) parts.push(`${minutes}m`)
  if (seconds > 0 || parts.length === 0) parts.push(`${seconds}s`)
  
  return parts.join(' ')
}

export default function CallCard({ call }: CallCardProps) {
  const { isSubscribed, toggle: toggleSubscription, isPending } = useSubscriptions()
  
  const isSubscribedToTalkGroup = isSubscribed(call.talkGroupId)
  const isSubscriptionPending = isPending(call.talkGroupId)

  // Calculate duration and age
  const duration = call.durationSeconds || call.recordings.reduce((acc, r) => acc + (r.durationSeconds || 0), 0)
  const started = new Date(call.recordingTime)
  const ageSec = Math.floor((Date.now() - started.getTime()) / 1000)

  // Get talkgroup display name
  const talkGroupDisplay = call.talkGroup?.description || 
                          call.talkGroup?.alphaTag || 
                          call.talkGroup?.name || 
                          `TG ${call.talkGroup?.number || call.talkGroupId}`

  // Get transcription text
  const transcriptionText = call.transcriptions && call.transcriptions.length > 0 
    ? call.transcriptions.map(t => t.text).join(' ')
    : null

  const handleCardClick = () => {
    if (!call.recordings?.length) return
    
    // Add call to queue and start playing if not already playing
    audioPlayerService.addToQueue(call)
    
    // If player is stopped, start it
    if (audioPlayerService.getState() === 'stopped') {
      audioPlayerService.play().catch(error => {
        console.error('Failed to start audio player:', error)
      })
    }
  }

  const handleSubscribe = async (e: React.MouseEvent) => {
    e.stopPropagation()
    
    try {
      await toggleSubscription(call.talkGroupId)
    } catch (error) {
      console.error('Failed to toggle subscription:', error)
    }
  }

  const handleShare = async (e: React.MouseEvent) => {
    e.stopPropagation()
    
    const shareUrl = `${window.location.origin}/call/${call.id}`
    
    if (navigator.share) {
      try {
        await navigator.share({
          title: `${talkGroupDisplay} - SignalRadio`,
          text: transcriptionText || 'Radio call',
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

  return (
    <article 
      className="new-call-card"
      onClick={handleCardClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          handleCardClick()
        }
      }}
      aria-label={`Play call from ${talkGroupDisplay}`}
    >
      <div className="call-header">
        <div className="call-talkgroup">
          <Link 
            to={`/talkgroup/${call.talkGroupId}`}
            className="talkgroup-link"
            onClick={(e) => e.stopPropagation()}
          >
            {talkGroupDisplay}
          </Link>
          <div className="badges">
            {call.talkGroup?.priority && (
              <span className="badge priority-badge">P{call.talkGroup.priority}</span>
            )}
            {call.talkGroup?.tag && (
              <span className="badge tag-badge">{call.talkGroup.tag}</span>
            )}
            {call.talkGroup?.category && (
              <span className="badge category-badge">{call.talkGroup.category}</span>
            )}
            {call.talkGroup?.alphaTag && call.talkGroup.alphaTag !== call.talkGroup.tag && (
              <span className="badge alpha-badge">{call.talkGroup.alphaTag}</span>
            )}
          </div>
        </div>
        
        <div className="call-actions">
          <button
            className={`subscribe-btn ${isSubscribedToTalkGroup ? 'subscribed' : ''} ${isSubscriptionPending ? 'pending' : ''}`}
            onClick={handleSubscribe}
            disabled={isSubscriptionPending}
            aria-label={isSubscribedToTalkGroup ? 'Unsubscribe from talkgroup' : 'Subscribe to talkgroup'}
            title={isSubscribedToTalkGroup ? 'Unsubscribe from talkgroup' : 'Subscribe to talkgroup'}
          >
            {isSubscriptionPending ? '⏳' : (isSubscribedToTalkGroup ? '⭐' : '☆')}
          </button>
          
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
        <span className="call-age">{secondsToAge(ageSec)} ago</span>
      </div>

      {transcriptionText && (
        <div className="call-transcript">
          <p>{transcriptionText}</p>
        </div>
      )}

      {!transcriptionText && (
        <div className="call-transcript">
          <p className="no-transcript">No transcription available</p>
        </div>
      )}

      <style>{`
        .new-call-card {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-2);
          cursor: pointer;
          transition: var(--transition);
          position: relative;
          overflow: hidden;
        }

        .new-call-card:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          transform: translateY(-1px);
        }

        .new-call-card:focus {
          outline: 2px solid var(--accent-primary);
          outline-offset: 2px;
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
          align-items: flex-start;
          gap: var(--space-2);
          flex: 1;
          min-width: 0;
          flex-direction: column;
        }

        .badges {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          flex-wrap: wrap;
          margin-top: var(--space-1);
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

        .badge {
          padding: 2px 6px;
          border-radius: var(--radius-sm);
          font-size: var(--font-size-xs);
          font-weight: 600;
          flex-shrink: 0;
          line-height: 1.2;
        }

        .priority-badge {
          background: var(--accent-secondary);
          color: white;
        }

        .tag-badge {
          background: var(--accent-primary);
          color: white;
        }

        .category-badge {
          background: rgba(147, 51, 234, 0.9);
          color: white;
        }

        .alpha-badge {
          background: rgba(34, 197, 94, 0.9);
          color: white;
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

        .call-transcript .no-transcript {
          color: var(--text-secondary);
          font-style: italic;
        }

        @media (max-width: 767px) {
          .new-call-card {
            padding: var(--space-1-5);
          }

          .call-header {
            margin-bottom: var(--space-1-5);
          }
          
          .call-talkgroup {
            flex-direction: column;
            align-items: flex-start;
            gap: var(--space-1);
          }

          .badges {
            margin-top: 0;
            gap: 4px;
          }

          .badge {
            padding: 1px 4px;
            font-size: 10px;
          }

          .talkgroup-link {
            font-size: var(--font-size-base);
            flex-shrink: 0;
          }
          
          .call-meta {
            display: grid;
            grid-template-columns: auto auto;
            gap: var(--space-1) var(--space-2);
            margin-bottom: var(--space-1-5);
            font-size: var(--font-size-xs);
          }

          .call-meta span:nth-child(1) { /* time */
            grid-column: 1;
            grid-row: 1;
          }

          .call-meta span:nth-child(2) { /* duration */
            grid-column: 2;
            grid-row: 1;
            justify-self: end;
          }

          .call-meta span:nth-child(3) { /* frequency */
            grid-column: 1;
            grid-row: 2;
          }

          .call-meta span:nth-child(4) { /* age */
            grid-column: 2;
            grid-row: 2;
            justify-self: end;
          }

          .call-transcript {
            margin-bottom: var(--space-1);
          }

          .call-transcript p {
            font-size: var(--font-size-sm);
            line-height: 1.4;
          }
        }
      `}</style>
    </article>
  )
}
