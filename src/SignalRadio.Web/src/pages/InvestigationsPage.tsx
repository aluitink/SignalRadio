import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { InvestigationDto, InvestigationReport } from '../types/dtos'
import {
  deleteInvestigation,
  getInvestigations,
  markInvestigationViewed,
} from '../api'
import { useSignalR } from '../hooks/useSignalR'
import InvestigationReportView from '../components/InvestigationReportView'
import { formatRelativeTime } from '../utils/format'
import { useNow } from '../hooks/useNow'

type StatusFilter = '' | 'Pending' | 'Running' | 'Completed' | 'Failed'

export default function InvestigationsPage() {
  const [investigations, setInvestigations] = useState<InvestigationDto[]>([])
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('')
  const [loading, setLoading] = useState(true)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const now = useNow(15000)

  const load = useCallback(async () => {
    try {
      const data = await getInvestigations(statusFilter || undefined, undefined, true, 100)
      setInvestigations(data)
    } catch {
      // Non-fatal
    } finally {
      setLoading(false)
    }
  }, [statusFilter])

  useEffect(() => {
    void load()
  }, [load])

  // Refresh when investigation events fire.
  useSignalR(null, {
    onInvestigationStarted: () => void load(),
    onInvestigationCompleted: () => void load(),
  })

  const handleView = async (inv: InvestigationDto) => {
    setExpandedId(expandedId === inv.id ? null : inv.id)
    if (!inv.isViewed && inv.status === 'Completed') {
      await markInvestigationViewed(inv.id)
      setInvestigations(prev => prev.map(i => i.id === inv.id ? { ...i, isViewed: true } : i))
    }
  }

  const handleDelete = async (id: number) => {
    await deleteInvestigation(id)
    setInvestigations(prev => prev.filter(i => i.id !== id))
  }

  const parseReport = (json?: string | null): InvestigationReport | null => {
    if (!json) return null
    try {
      return JSON.parse(json) as InvestigationReport
    } catch {
      return null
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Investigations</h1>
      </div>

      <div className="investigations-filters">
        {(['', 'Pending', 'Running', 'Completed', 'Failed'] as StatusFilter[]).map(s => (
          <button
            key={s || 'all'}
            className={`btn ${statusFilter === s ? 'primary' : 'ghost'}`}
            onClick={() => setStatusFilter(s)}
          >
            {s || 'All'}
          </button>
        ))}
      </div>

      {loading && (
        <div aria-busy="true">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="skeleton skeleton-card" />
          ))}
        </div>
      )}

      {!loading && investigations.length === 0 && (
        <div className="empty-state">
          <p>No investigations yet.</p>
          <p className="sub">Use the 🔍 button on any call card to start one.</p>
        </div>
      )}

      {!loading && investigations.length > 0 && (
        <div className="investigations-list">
          {investigations.map(inv => {
            const report = parseReport(inv.resultJson)
            const isExpanded = expandedId === inv.id
            return (
              <div key={inv.id} className={`card investigation-item ${inv.status.toLowerCase()}${!inv.isViewed && inv.status === 'Completed' ? ' unviewed' : ''}`}>
                <div className="investigation-item-header" onClick={() => handleView(inv)} role="button" tabIndex={0}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleView(inv) }}>
                  <span className={`badge status-badge status-${inv.status.toLowerCase()}`}>{inv.status}</span>
                  <Link to={`/call/${inv.callId}`} className="tg-link" onClick={(e) => e.stopPropagation()}>
                    TG {inv.callTalkGroupNumber}
                  </Link>
                  <span className="sub">{formatRelativeTime(inv.callRecordingTime, now)}</span>
                  <span className="investigation-prompt-preview">{inv.prompt}</span>
                  <span className="row-end">
                    {inv.parentInvestigationId && <span className="sub" title="Follow-up">↳</span>}
                    <Link to={`/investigations/${inv.id}`} className="btn ghost" onClick={(e) => e.stopPropagation()}>💬 Chat</Link>
                    <button className="btn ghost danger" onClick={(e) => { e.stopPropagation(); void handleDelete(inv.id) }} aria-label="Delete">🗑</button>
                  </span>
                </div>

                {isExpanded && (
                  <div className="investigation-item-body">
                    {inv.status === 'Pending' && <p className="sub">Waiting in queue…</p>}
                    {inv.status === 'Running' && <p className="sub">Agent is analyzing…</p>}
                    {inv.status === 'Failed' && <div className="error-state">{inv.error ?? 'Unknown error'}</div>}

                    {inv.status === 'Completed' && report && (
                      <InvestigationReportView report={report} />
                    )}

                    {inv.status === 'Completed' && !report && (
                      <p className="sub">No structured result available.</p>
                    )}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
