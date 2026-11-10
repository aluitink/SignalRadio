import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { getInvestigation } from '../api'
import { useInvestigationChat } from '../hooks/useInvestigationChat'
import type {
  ChatMessage,
  InvestigationDto,
} from '../types/dtos'
import { formatDateTime } from '../utils/format'
import './InvestigationChatPage.css'

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

/**
 * A single collapsible node for a tool call and its result. Collapsed by
 * default; expanding shows the arguments and, once available, the result
 * underneath. While the result is still in flight it shows a pending state.
 */
function ToolPill({ msg }: { msg: ChatMessage }) {
  const [open, setOpen] = useState(false)
  const hasResult = msg.toolResult != null
  const pending = !hasResult

  return (
    <div className="chat-msg tool">
      <button
        type="button"
        className={`tool-pill ${hasResult ? 'tool-pill-done' : 'tool-pill-pending'} ${open ? 'open' : ''}`}
        onClick={() => setOpen(v => !v)}
        aria-expanded={open}
      >
        <span className="tool-pill-icon">{pending ? '⏳' : '🔧'}</span>
        <span className="tool-pill-name">{msg.toolName ?? 'tool'}</span>
        <span className="tool-pill-kind">{pending ? 'running' : 'done'}</span>
        <span className="tool-pill-chevron">{open ? '▾' : '▸'}</span>
      </button>
      {open && (
        <div className="tool-pill-body">
          {msg.toolArgs && (
            <>
              <div className="tool-pill-section-label">Arguments</div>
              <pre className="tool-args">{formatJson(msg.toolArgs)}</pre>
            </>
          )}
          {hasResult && (
            <>
              <div className="tool-pill-section-label">Result</div>
              <pre className="tool-result-text">{truncate(msg.toolResult ?? '', 4000)}</pre>
            </>
          )}
          {pending && <div className="tool-pill-pending-note">Waiting for result…</div>}
        </div>
      )}
    </div>
  )
}

function MessageBubble({ msg }: { msg: ChatMessage }) {
  if (msg.role === 'user') {
    return (
      <div className="chat-msg user">
        <div className="chat-bubble user-bubble">{msg.text}</div>
      </div>
    )
  }

  if (msg.role === 'tool') {
    return <ToolPill msg={msg} />
  }

  // assistant
  return (
    <div className="chat-msg assistant">
      {msg.error ? (
        <div className="chat-bubble assistant-bubble error-bubble">
          <span className="error-label">⚠ {msg.error}</span>
        </div>
      ) : (
        <>
          {msg.text && (
            <div className="chat-bubble assistant-bubble">
              <ReactMarkdown remarkPlugins={[remarkGfm]}>{msg.text}</ReactMarkdown>
              {msg.streaming && <span className="streaming-cursor" />}
            </div>
          )}
          {!msg.text && msg.streaming && (
            <div className="chat-bubble assistant-bubble">
              <span className="typing-dots"><span /><span /><span /></span>
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default function InvestigationChatPage() {
  const { id } = useParams<{ id: string }>()
  const investigationId = id ? Number(id) : null
  const [inv, setInv] = useState<InvestigationDto | null>(null)
  const [input, setInput] = useState('')
  const { messages, isRunning, send } = useInvestigationChat(
    investigationId,
    inv?.messages ?? [],
  )
  const scrollRef = useRef<HTMLDivElement>(null)
  const bottomRef = useRef<HTMLDivElement>(null)
  const stickToBottomRef = useRef(true)

  useEffect(() => {
    if (investigationId == null) return
    getInvestigation(investigationId)
      .then(setInv)
      .catch(() => { /* ignore */ })
  }, [investigationId])

  // Track whether the user is near the bottom so we only auto-scroll when they are.
  const handleScroll = useCallback(() => {
    const el = scrollRef.current
    if (!el) return
    stickToBottomRef.current = el.scrollHeight - el.scrollTop - el.clientHeight < 80
  }, [])

  // Auto-scroll to the newest message when stuck to the bottom.
  useEffect(() => {
    if (stickToBottomRef.current) {
      bottomRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
    }
  }, [messages])

  const handleSend = useCallback(() => {
    const text = input.trim()
    if (!text || isRunning) return
    setInput('')
    stickToBottomRef.current = true
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
      </div>

      <div className="chat-messages" ref={scrollRef} onScroll={handleScroll} aria-live="polite">
        <div className="chat-messages-inner">
          {messages.length === 0 && (
            <div className="empty-state chat-empty">
              <p>{inv?.status === 'Running' ? 'Agent is analyzing…' : 'No messages yet.'}</p>
              <p className="sub">Ask a question to start the conversation.</p>
            </div>
          )}
          {messages.map(msg => (
            <MessageBubble key={msg.id} msg={msg} />
          ))}
          <div ref={bottomRef} />
        </div>
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
        <button className="btn primary chat-send-btn" onClick={handleSend} disabled={isRunning || !input.trim()}>
          {isRunning ? '…' : 'Send'}
        </button>
      </div>
    </div>
  )
}
