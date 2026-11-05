import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { getCall, recordingDownloadUrlByCall, getFinalTranscription } from '../api'
import { useAudioPlayer } from '../contexts/AudioPlayerContext'
import { formatDuration, formatFrequency, formatDateTime } from '../utils/format'

export default function CallDetailPage() {
  const { id } = useParams<{ id: string }>()
  const callId = id ? Number(id) : NaN

  const [call, setCall] = useState<CallDto | null>(null)
  const [transcript, setTranscript] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const { play } = useAudioPlayer()

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
          try {
            const t = await getFinalTranscription(c.latestRecordingId)
            if (active) setTranscript(t?.fullText ?? null)
          } catch {
            // transcription not available yet
          }
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
  }, [callId])

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

  const audioUrl = recordingDownloadUrlByCall(call.id)

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
            onClick={() =>
              play({
                url: audioUrl,
                title: `TG ${call.talkGroupNumber} · ${formatDateTime(call.recordingTime)}`,
                callId: call.id,
              })
            }
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
