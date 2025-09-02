import React, { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import AudioPlayer from '../components/AudioPlayer'
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
        const response = await apiGet<CallDto>(`/api/calls/${callId}`)
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
        <div className="loading-state">
          <div className="loading-spinner"></div>
          <p>Loading call details...</p>
        </div>
        <AudioPlayer />
      </section>
    )
  }

  if (error || !call) {
    return (
      <section className="call-detail-page">
        <div className="error-state">
          <h1>Call Not Found</h1>
          <p className="text-secondary">{error || 'The requested call could not be found.'}</p>
          <Link to="/" className="back-link">← Back to Live Stream</Link>
        </div>
        <AudioPlayer />
      </section>
    )
  }

  return (
    <section className="call-detail-page">
      <div className="call-detail-content">
        <div className="breadcrumb">
          <Link to="/" className="breadcrumb-link">Live Stream</Link>
          <span className="breadcrumb-separator">›</span>
          <Link to={`/talkgroup/${call.talkGroupId}`} className="breadcrumb-link">
            {call.talkGroup?.description || call.talkGroup?.tag || 'Talk Group'}
          </Link>
          <span className="breadcrumb-separator">›</span>
          <span className="breadcrumb-current">Call {call.id}</span>
        </div>

        <div className="call-header">
          <div className="call-title">
            <h1>Call {call.id}</h1>
            <div className="call-meta">
              <span className="timestamp">
                {new Date(call.recordingTime).toLocaleString()}
              </span>
              <span className="duration">
                {call.durationSeconds ? `${call.durationSeconds.toFixed(1)}s` : 'Unknown duration'}
              </span>
            </div>
          </div>
          
          <div className="call-actions">
            {call.recordings?.length ? (
              <button
                onClick={handlePlayToggle}
                className="play-btn primary"
              >
                ▶️ Play Recording
              </button>
            ) : (
              <button disabled className="play-btn disabled">
                No Recording Available
              </button>
            )}
            
            <button
              onClick={handleShare}
              className="share-btn secondary"
            >
              🔗 Share
            </button>
          </div>
        </div>

        <div className="call-details">
          <div className="detail-section">
            <h3>Talk Group</h3>
            <div className="talkgroup-info">
              <div className="talkgroup-main">
                <strong>{call.talkGroup?.description || call.talkGroup?.tag || 'Unknown'}</strong>
                <span className="talkgroup-id">ID: {call.talkGroupId}</span>
              </div>
              {call.talkGroup?.category && (
                <span className="talkgroup-mode">{call.talkGroup.category}</span>
              )}
              <button
                onClick={() => toggleSubscription(call.talkGroupId)}
                className={`subscribe-btn ${isSubscribed(call.talkGroupId) ? 'subscribed' : ''}`}
              >
                {isSubscribed(call.talkGroupId) ? '🔔 Subscribed' : '🔕 Subscribe'}
              </button>
            </div>
          </div>

          <div className="detail-section">
            <h3>Call Information</h3>
            <div className="call-info">
              <div className="info-row">
                <span className="label">Frequency:</span>
                <span className="value">{call.frequencyHz ? `${(call.frequencyHz / 1000000).toFixed(3)} MHz` : 'Unknown'}</span>
              </div>
              <div className="info-row">
                <span className="label">Duration:</span>
                <span className="value">{call.durationSeconds ? `${call.durationSeconds.toFixed(1)}s` : 'Unknown'}</span>
              </div>
            </div>
          </div>

          {call.transcriptions?.length && (
            <div className="detail-section">
              <h3>Transcription</h3>
              <div className="transcript">
                <p>{call.transcriptions[0].text}</p>
              </div>
            </div>
          )}

          {call.recordings?.length && (
            <div className="detail-section">
              <h3>Recordings</h3>
              <div className="recordings-list">
                {call.recordings.map((recording, index) => (
                  <div key={index} className="recording-item">
                    <span className="recording-path">{recording.fileName}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      <AudioPlayer />
    </section>
  )
}
