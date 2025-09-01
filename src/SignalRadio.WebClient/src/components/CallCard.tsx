import React from 'react'
import type { CallDto } from '../types/dtos'

function secondsToHuman(s: number) {
  if (!isFinite(s) || s <= 0) return '0s'
  const m = Math.floor(s / 60)
  const sec = Math.floor(s % 60)
  return m ? `${m}m ${sec}s` : `${sec}s`
}

export default function CallCard({ call }: { call: CallDto }) {
  // Calculate total duration from call duration or recordings
  const duration = call.durationSeconds || call.recordings.reduce((acc, r) => acc + (r.durationSeconds || 0), 0)
  const started = new Date(call.recordingTime)
  const ageSec = Math.floor((Date.now() - started.getTime()) / 1000)

  // Get talkgroup display name
  const talkGroupDisplay = call.talkGroup?.description || 
                          call.talkGroup?.alphaTag || 
                          call.talkGroup?.name || 
                          `TG ${call.talkGroup?.number || call.talkGroupId}`

  return (
    <article className="call-card">
      <div className="call-meta">
        <div className="talkgroup">
          <strong>{talkGroupDisplay}</strong>
          <span className="priority">Priority: {call.talkGroup?.priority ?? '—'}</span>
        </div>
        <div className="info">
          <span>Duration: {secondsToHuman(duration)}</span>
          <span>Age: {secondsToHuman(ageSec)}</span>
          <span>Freq: {(call.frequencyHz / 1000000).toFixed(3)} MHz</span>
        </div>
      </div>

      <div className="call-actions">
        <button className="btn-play" onClick={() => {
          if (!call.recordings[0]) return
          const audio = new Audio(call.recordings[0].url)
          audio.play().catch(() => {})
        }}>Play</button>

        <button className="btn-share" onClick={() => {
          const u = call.recordings[0]?.url
          if (!u) return
          if (navigator.share) {
            navigator.share({ title: talkGroupDisplay, url: u }).catch(() => {})
          } else {
            navigator.clipboard?.writeText(u).catch(() => {})
            alert('Recording URL copied to clipboard')
          }
        }}>Share</button>
      </div>

      <div className="transcription">
        {call.transcriptions && call.transcriptions.length > 0 ? (
          <p>{call.transcriptions.map(t => t.text).join('\n')}</p>
        ) : (
          <p className="muted">No transcription available</p>
        )}
      </div>

  <style>{`
        .call-card { background: rgba(255,255,255,0.02); padding:12px; border-radius:8px; margin-bottom:12px }
        .call-meta { display:flex; justify-content:space-between; align-items:center }
        .talkgroup strong { display:block }
        .info span { margin-left:12px; color: #bcd }
        .call-actions { margin-top:8px }
        .btn-play, .btn-share { background: #1f6feb; color:white; border:0; padding:8px 10px; border-radius:6px; margin-right:8px }
        .btn-share { background: #6b7280 }
        .muted { opacity: 0.7 }
      `}</style>
    </article>
  )
}
