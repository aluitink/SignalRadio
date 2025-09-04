import React, { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { useSubscriptions } from '../hooks/useSubscriptions'
import { usePageTitle } from '../hooks/usePageTitle'
import { audioPlayerService } from '../services/AudioPlayerService'
import { apiGet } from '../api'

export default function CallDetailPage() {
  const { id } = useParams<{ id: string }>()
  const callId = parseInt(id || '0', 10)
  
  const [call, setCall] = useState<CallDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  const { toggle: toggleSubscription, isSubscribed } = useSubscriptions()

  // Always call hooks - use conditional values for the hook parameters
  const talkGroupDisplay = call?.talkGroup?.description || 
    call?.talkGroup?.tag || 
    call?.talkGroup?.alphaTag || 
    'Unknown'
  const callDisplay = call ? 
    `${talkGroupDisplay} - ${new Date(call.recordingTime).toLocaleString()}` : 
    'Loading...'
  
  usePageTitle(
    call ? `Call ${call.id}` : 'Call Detail',
    callDisplay
  )

  useEffect(() => {
    if (!callId) {
      setError('Invalid call ID')
      setLoading(false)
      return
    }

    const loadCall = async () => {
      try {
        setLoading(true)
        setError(null)
        const response = await apiGet<CallDto>(`/calls/${callId}`)
        setCall(response)
      } catch (err) {
        console.error('Failed to load call:', err)
        setError('Failed to load call details')
      } finally {
        setLoading(false)
      }
    }

    loadCall()
  }, [callId])

  const handlePlayToggle = async () => {
    if (!call?.recordings?.length) return

    try {
      // Add to queue and play
      audioPlayerService.addToQueue(call)
      await audioPlayerService.play()
    } catch (error) {
      console.error('Failed to control audio player:', error)
    }
  }

  const handleShare = async () => {
    if (!call) return
    
    const url = window.location.href
    try {
      await navigator.share({
        title: `Call ${call.id} - ${call.talkGroup?.description || 'Unknown Talk Group'}`,
        text: call.transcriptions?.[0]?.text || 'Radio call recording',
        url: url
      })
    } catch (err) {
      // Fallback to clipboard
      await navigator.clipboard.writeText(url)
      // You could show a toast notification here
    }
  }

  if (loading) {
    return (
      <section className="call-detail-page">
        <header className="detail-header">
          <div className="breadcrumb">
            <Link to="/" className="breadcrumb-link">Live Stream</Link>
            <span className="breadcrumb-separator">›</span>
            <span className="breadcrumb-current">Loading...</span>
          </div>
        </header>
        
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Loading call details...</p>
        </div>
      </section>
    )
  }

  if (error || !call) {
    return (
      <section className="call-detail-page">
        <header className="detail-header">
          <div className="breadcrumb">
            <Link to="/" className="breadcrumb-link">Live Stream</Link>
            <span className="breadcrumb-separator">›</span>
            <span className="breadcrumb-current">Call Not Found</span>
          </div>
        </header>
        
        <div className="error-container">
          <div className="error-content">
            <h1>Call Not Found</h1>
            <p className="text-secondary">{error || 'The requested call could not be found.'}</p>
            <Link to="/" className="back-btn">← Back to Live Stream</Link>
          </div>
        </div>
      </section>
    )
  }

  return (
    <section className="call-detail-page">
      <header className="detail-header">
        <div className="breadcrumb">
          <Link to="/" className="breadcrumb-link">Live Stream</Link>
          <span className="breadcrumb-separator">›</span>
          <Link to={`/talkgroup/${call.talkGroupId}`} className="breadcrumb-link">
            {call.talkGroup?.description || call.talkGroup?.tag || 'Talk Group'}
          </Link>
          <span className="breadcrumb-separator">›</span>
          <span className="breadcrumb-current">Call {call.id}</span>
        </div>
      </header>

      <div className="call-detail-card">
        <div className="call-header">
          <div className="call-title-section">
            <h1>Call {call.id}</h1>
            <div className="call-meta">
              <span className="timestamp">
                {new Date(call.recordingTime).toLocaleString()}
              </span>
              <span className="duration">
                {call.durationSeconds ? `${call.durationSeconds.toFixed(1)}s` : 'Unknown duration'}
              </span>
              {call.frequencyHz && (
                <span className="frequency">
                  {(call.frequencyHz / 1000000).toFixed(3)} MHz
                </span>
              )}
            </div>
          </div>
          
          <div className="call-actions">
            {call.recordings?.length ? (
              <button
                onClick={handlePlayToggle}
                className="action-btn primary"
              >
                ▶️ Play
              </button>
            ) : (
              <button disabled className="action-btn disabled">
                No Recording
              </button>
            )}
            
            <button
              onClick={handleShare}
              className="action-btn secondary"
            >
              🔗
            </button>
          </div>
        </div>

        <div className="talkgroup-section">
          <div className="talkgroup-info">
            <Link 
              to={`/talkgroup/${call.talkGroupId}`} 
              className="talkgroup-link"
            >
              {call.talkGroup?.description || call.talkGroup?.tag || 'Unknown'}
            </Link>
            <div className="badges">
              <span className="badge talkgroup-id">ID: {call.talkGroupId}</span>
              {call.talkGroup?.category && (
                <span className="badge category-badge">{call.talkGroup.category}</span>
              )}
            </div>
          </div>
          <button
            onClick={() => toggleSubscription(call.talkGroupId)}
            className={`subscribe-btn ${isSubscribed(call.talkGroupId) ? 'subscribed' : ''}`}
          >
            {isSubscribed(call.talkGroupId) ? '🔔' : '🔕'}
          </button>
        </div>

        {call.transcriptions?.length && (
          <div className="transcript-section">
            <div className="transcript-content">
              <p>{call.transcriptions[0].text}</p>
            </div>
          </div>
        )}

        {call.recordings?.length && (
          <div className="recordings-section">
            <h3>Recordings</h3>
            <div className="recordings-list">
              {call.recordings.map((recording, index) => (
                <div key={index} className="recording-item">
                  <span className="recording-filename">{recording.fileName}</span>
                  {recording.durationSeconds && (
                    <span className="recording-duration">
                      {recording.durationSeconds.toFixed(1)}s
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      <style>{`
        .call-detail-page {
          max-width: var(--content-width, 800px);
          margin: 0 auto;
          padding: var(--space-2);
        }

        .detail-header {
          margin-bottom: var(--space-3);
          padding-bottom: var(--space-2);
          border-bottom: 1px solid var(--border);
        }

        .breadcrumb {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
        }

        .breadcrumb-link {
          color: var(--accent-primary);
          text-decoration: none;
          transition: var(--transition);
        }

        .breadcrumb-link:hover {
          color: var(--accent-primary-hover);
        }

        .breadcrumb-separator {
          color: var(--text-muted);
        }

        .breadcrumb-current {
          color: var(--text-primary);
          font-weight: 500;
        }

        .loading-container,
        .error-container {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: var(--space-6);
          text-align: center;
        }

        .error-content {
          max-width: 400px;
        }

        .loading-spinner {
          width: 32px;
          height: 32px;
          border: 3px solid var(--border);
          border-top: 3px solid var(--accent-primary);
          border-radius: 50%;
          animation: spin 1s linear infinite;
          margin-bottom: var(--space-2);
        }

        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }

        .back-btn {
          display: inline-flex;
          align-items: center;
          gap: var(--space-1);
          padding: var(--space-2) var(--space-3);
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          color: var(--text-primary);
          text-decoration: none;
          transition: var(--transition);
          margin-top: var(--space-2);
        }

        .back-btn:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .call-detail-card {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-3);
          margin-bottom: var(--space-3);
        }

        .call-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--space-3);
          gap: var(--space-2);
        }

        .call-title-section {
          flex: 1;
          min-width: 0;
        }

        .call-title-section h1 {
          margin: 0 0 var(--space-1) 0;
          color: var(--text-primary);
          font-size: var(--font-size-xl);
        }

        .call-meta {
          display: flex;
          gap: var(--space-2);
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
          flex-wrap: wrap;
        }

        .call-meta span {
          white-space: nowrap;
        }

        .call-actions {
          display: flex;
          gap: var(--space-1);
          flex-shrink: 0;
        }

        .action-btn {
          padding: var(--space-1) var(--space-2);
          border: 1px solid var(--border);
          border-radius: var(--radius-sm);
          background: var(--bg-card);
          color: var(--text-primary);
          cursor: pointer;
          transition: var(--transition);
          font-size: var(--font-size-sm);
          min-width: 44px;
          height: 32px;
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .action-btn:hover:not(.disabled) {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .action-btn.primary {
          background: var(--accent-primary);
          border-color: var(--accent-primary);
          color: white;
        }

        .action-btn.primary:hover {
          background: var(--accent-primary-hover);
          border-color: var(--accent-primary-hover);
        }

        .action-btn.disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .talkgroup-section {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          padding: var(--space-2) 0;
          border-bottom: 1px solid var(--border);
          margin-bottom: var(--space-3);
        }

        .talkgroup-info {
          flex: 1;
          min-width: 0;
        }

        .talkgroup-link {
          color: var(--text-primary);
          text-decoration: none;
          font-weight: 600;
          font-size: var(--font-size-lg);
          transition: var(--transition);
          display: block;
          margin-bottom: var(--space-1);
        }

        .talkgroup-link:hover {
          color: var(--accent-primary);
        }

        .badges {
          display: flex;
          gap: var(--space-1);
          flex-wrap: wrap;
        }

        .badge {
          padding: 2px 6px;
          border-radius: var(--radius-sm);
          font-size: var(--font-size-xs);
          font-weight: 600;
          line-height: 1.2;
        }

        .talkgroup-id {
          background: var(--accent-secondary);
          color: white;
        }

        .category-badge {
          background: rgba(147, 51, 234, 0.9);
          color: white;
        }

        .subscribe-btn {
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

        .subscribe-btn:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
        }

        .subscribe-btn.subscribed {
          background: var(--accent-primary);
          border-color: var(--accent-primary);
          color: white;
        }

        .transcript-section {
          margin-bottom: var(--space-3);
          padding: var(--space-2);
          background: rgba(255, 255, 255, 0.01);
          border-radius: var(--radius-sm);
          border-left: 3px solid var(--accent-primary);
        }

        .transcript-content p {
          margin: 0;
          line-height: 1.5;
          color: var(--text-primary);
        }

        .recordings-section h3 {
          color: var(--text-primary);
          margin-bottom: var(--space-2);
          font-size: var(--font-size-base);
        }

        .recordings-list {
          display: flex;
          flex-direction: column;
          gap: var(--space-1);
        }

        .recording-item {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: var(--space-1);
          background: rgba(255, 255, 255, 0.01);
          border-radius: var(--radius-sm);
          font-size: var(--font-size-sm);
        }

        .recording-filename {
          color: var(--text-primary);
          word-break: break-all;
          flex: 1;
        }

        .recording-duration {
          color: var(--text-secondary);
          margin-left: var(--space-2);
          flex-shrink: 0;
        }

        @media (max-width: 767px) {
          .call-detail-page {
            padding: var(--space-1);
          }

          .call-header {
            flex-direction: column;
            align-items: flex-start;
            gap: var(--space-2);
          }

          .call-actions {
            align-self: flex-end;
          }

          .call-meta {
            flex-direction: column;
            gap: var(--space-1);
          }

          .talkgroup-section {
            flex-direction: column;
            align-items: flex-start;
            gap: var(--space-2);
          }

          .subscribe-btn {
            align-self: flex-end;
          }
        }
      `}</style>
    </section>
  )
}
