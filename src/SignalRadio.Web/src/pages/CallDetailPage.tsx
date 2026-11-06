import { useCallback, useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { getCall, getFinalTranscription } from '../api'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import { useSignalR } from '../hooks/useSignalR'
import { formatDuration, formatFrequency, formatDateTime } from '../utils/format'

export default function CallDetailPage() {
  const { id } = useParams<{ id: string }>()
  const callId = id ? Number(id) : NaN

  const [call, setCall] = useState<CallDto | null>(null)
  const [transcript, setTranscript] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const { playFromCall } = useSubscriptions()

  const loadTranscript = useCallback(async (recordingId: number) => {
    try {
      const t = await getFinalTranscription(recordingId)
      setTranscript(t?.fullText ?? null)
    } catch {
      // transcription not available yet
    }
  }, [])

  useEffect(() => {
    if (!Number.isFinite(callId)) return
    let active = true
    setLoading(true)
    setError(null)

    getCall(callId)
      .then(async (c) => {
        if (!active) return
        setCall(c)
        if (c.latestRecordingId) {
          await loadTranscript(c.latestRecordingId)
        }
      })
      .catch((e) => {
        if (active) setError(e instanceof Error ? e.message : 'Failed to load call')
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [callId, loadTranscript])

  // Live-update: when the backend finishes transcribing this call's recording,
  // fetch and show the transcript without a manual refresh.
  useSignalR(null, {
    onNewTranscription: (recordingId) => {
      if (call?.latestRecordingId === recordingId) {
        void loadTranscript(recordingId)
      }
    },
  })

  if (!Number.isFinite(callId)) {
    return <div className="error-state">Invalid call id.</div>
  }

  if (loading) {
    return (
      <div aria-busy="true">
        <div className="skeleton skeleton-card" />
        <div className="skeleton skeleton-card" />
      </div>
    )
  }

  if (error || !call) {
    return (
      <div className="error-state" role="alert">
        {error ?? 'Call not found.'}
      </div>
    )
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <Link to="/" className="back-link">← Back to live</Link>
          <h1>
            <Link to={`/talkgroup/${call.talkGroupId}`} className="tg-link">
              TG {call.talkGroupNumber}
            </Link>
            {call.talkGroupName ? ` · ${call.talkGroupName}` : ''}
          </h1>
        </div>
      </div>

      <div className="card">
        <div className="detail-grid">
          <div>
            <span className="detail-label">Time</span>
            <span>{formatDateTime(call.recordingTime)}</span>
          </div>
          <div>
            <span className="detail-label">Duration</span>
            <span>{formatDuration(call.durationSeconds)}</span>
          </div>
          <div>
            <span className="detail-label">Frequency</span>
            <span>{formatFrequency(call.frequencyHz)}</span>
          </div>
          <div>
            <span className="detail-label">Recordings</span>
            <span>{call.recordingCount}</span>
          </div>
        </div>
        <div style={{ marginTop: 16 }}>
          <button
            className="btn primary"
            onClick={() => playFromCall(call, [])}
          >
            ▶ Play audio
          </button>
        </div>
      </div>

      <div className="card">
        <h2 style={{ marginTop: 0 }}>Transcript</h2>
        {transcript ? (
          <p className="detail-transcript">{transcript}</p>
        ) : (
          <div className="empty-state">
            Transcription is processed asynchronously and will appear here once
            available.
          </div>
        )}
      </div>
    </div>
  )
}
