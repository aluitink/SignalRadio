import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { createInvestigation } from '../api'
import { formatDateTime } from '../utils/format'

interface Props {
  call: CallDto
  onClose: () => void
  onCreated: (investigationId: number) => void
}

const DEFAULT_PROMPT = 'Investigate this call and determine what event it is part of.'

export default function InvestigationPromptModal({ call, onClose, onCreated }: Props) {
  const [prompt, setPrompt] = useState(DEFAULT_PROMPT)
  const [windowMinutes, setWindowMinutes] = useState(30)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  useEffect(() => {
    textareaRef.current?.focus()
  }, [])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!prompt.trim() || submitting) return
    setSubmitting(true)
    setError(null)
    try {
      const inv = await createInvestigation(call.id, prompt.trim(), windowMinutes, call.talkGroupId)
      onCreated(inv.id)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create investigation')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true" aria-label="New investigation">
      <div className="modal-card investigation-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>🔍 New Investigation</h2>
          <button className="modal-close" onClick={onClose} aria-label="Close">✕</button>
        </div>

        <div className="modal-body">
          <div className="investigation-modal-call">
            <Link to={`/call/${call.id}`} className="tg-link" onClick={onClose}>
              TG {call.talkGroupNumber}
            </Link>
            <span className="sub">{formatDateTime(call.recordingTime)}</span>
            <span className="sub">{call.talkGroupName ?? ''}</span>
          </div>

          <form onSubmit={handleSubmit}>
            <label className="field-label" htmlFor="inv-prompt">Prompt</label>
            <textarea
              id="inv-prompt"
              ref={textareaRef}
              className="input investigation-prompt"
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              rows={3}
              placeholder="What would you like the AI to investigate?"
            />

            <div className="investigation-modal-options">
              <label className="field-label">
                Window: <select className="input" value={windowMinutes} onChange={(e) => setWindowMinutes(Number(e.target.value))}>
                  <option value={15}>±15 min</option>
                  <option value={30}>±30 min</option>
                  <option value={60}>±60 min</option>
                  <option value={120}>±120 min</option>
                </select>
              </label>
            </div>

            {error && <div className="error-state" role="alert">{error}</div>}

            <div className="modal-actions">
              <button type="button" className="btn ghost" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn primary" disabled={submitting || !prompt.trim()}>
                {submitting ? 'Queuing…' : 'Start Investigation'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
