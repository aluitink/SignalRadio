import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getInvestigation } from '../api'
import { useInvestigationChat } from '../hooks/useInvestigationChat'
import InvestigationReportView from '../components/InvestigationReportView'
import type { ChatMessage, InvestigationDto, InvestigationReport } from '../types/dtos'
import { formatDateTime } from '../utils/format'

function parseReport(json?: string | null): InvestigationReport | null {
  if (!json) return null
  try {
    return JSON.parse(json) as InvestigationReport
  } catch {
    return null
  }
}

function MessageBubble({ msg }: { msg: ChatMessage }) {
  if (msg.role === 'user') {
    return (
      <div className="chat-msg user">
        <div className="chat-bubble">{msg.text}</div>
      </div>
    )
  }

  if (msg.role === 'tool') {
    const isCall = msg.toolResult == null
    return (
      <div className="chat-msg tool">
        <div className={`tool-card ${isCall ? 'tool-call' : 'tool-result'}`}>
          <div className="tool-card-header">
            <span className="tool-icon">{isCall ? '🔧' : '📥'}</span>
            <span className="tool-name">{msg.toolName}</span>
            <span className="tool-kind">{isCall ? 'call' : 'result'}</span>
          </div>
          {isCall && msg.toolArgs && (
            <pre className="tool-args">{formatJson(msg.toolArgs)}</pre>
          )}
          {!isCall && (
            <pre className="tool-result-text">{truncate(msg.toolResult ?? '', 600)}</pre>
          )}
        </div>
      </div>
    )
  }

  // assistant
  const report = parseReport(msg.reportJson)
  return (
    <div className="chat-msg assistant">
      {msg.error ? (
        <div className="error-state">{msg.error}</div>
      ) : (
        <>
          {msg.text && <div className="chat-bubble assistant-bubble">{msg.text}{msg.streaming && <span className="streaming-cursor" />}</div>}
          {report && (
            <div className="chat-report">
              <InvestigationReportView report={report} />
            </div>
          )}
          {!msg.text && !report && msg.streaming && (
            <div className="chat-bubble assistant-bubble"><span className="streaming-cursor" /></div>
          )}
        </>
      )}
    </div>
  )
}

function formatJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    return raw
  }
}

function truncate(s: string, max: number): string {
  return s.length > max ? s.slice(0, max) + '…' : s
}

export default function InvestigationChatPage() {
  const { id } = useParams<{ id: string }>()
  const investigationId = id ? Number(id) : null
  const [inv, setInv] = useState<InvestigationDto | null>(null)
  const [input, setInput] = useState('')
  const { messages, isRunning, status, send } = useInvestigationChat(investigationId)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (investigationId == null) return
    getInvestigation(investigationId)
      .then(setInv)
      .catch(() => { /* ignore */ })
  }, [investigationId])

  // Auto-scroll to the newest message.
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const handleSend = useCallback(() => {
    const text = input.trim()
    if (!text || isRunning) return
    setInput('')
    void send(text)
  }, [input, isRunning, send])

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  return (
    <div className="investigation-chat-page">
      <div className="page-header chat-header">
        <Link to="/investigations" className="btn ghost" aria-label="Back">←</Link>
        <div className="chat-header-info">
          <h1>Investigation #{investigationId}</h1>
          {inv && (
            <div className="chat-header-meta">
              <Link to={`/call/${inv.callId}`} className="tg-link">TG {inv.callTalkGroupNumber}</Link>
              <span className="sub">{formatDateTime(inv.callRecordingTime)}</span>
              <span className={`badge status-badge status-${inv.status.toLowerCase()}`}>{inv.status}</span>
            </div>
          )}
        </div>
        <span className={`conn-dot ${status}`} title={`SignalR: ${status}`} />
      </div>

      <div className="chat-messages" aria-live="polite">
        {messages.length === 0 && (
          <div className="empty-state chat-empty">
            <p>{inv?.status === 'Running' ? 'Agent is analyzing…' : 'No messages yet.'}</p>
            <p className="sub">Ask a follow-up question to continue the investigation.</p>
          </div>
        )}
        {messages.map(msg => (
          <MessageBubble key={msg.id} msg={msg} />
        ))}
        <div ref={bottomRef} />
      </div>

      <div className="chat-input-row">
        <textarea
          className="input chat-input"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Ask a follow-up question…"
          rows={1}
          disabled={isRunning}
          aria-label="Chat message"
        />
        <button className="btn primary" onClick={handleSend} disabled={isRunning || !input.trim()}>
          {isRunning ? '…' : 'Send'}
        </button>
      </div>
    </div>
  )
}
