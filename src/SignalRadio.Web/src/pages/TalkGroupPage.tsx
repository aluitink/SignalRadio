import { useCallback, useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { CallDto, TalkGroupDto, PagedResult } from '../types/dtos'
import { getTalkGroup, getCalls } from '../api'
import { useSignalR } from '../hooks/useSignalR'
import CallCard from '../components/CallCard'
import Pagination from '../components/Pagination'

const PAGE_SIZE = 20

export default function TalkGroupPage() {
  const { id } = useParams<{ id: string }>()
  const talkGroupId = id ? Number(id) : NaN

  const [group, setGroup] = useState<TalkGroupDto | null>(null)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<CallDto> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async (p: number) => {
    if (!Number.isFinite(talkGroupId)) return
    try {
      const data = await getCalls(talkGroupId, p, PAGE_SIZE)
      setResult(data)
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load calls')
    } finally {
      setLoading(false)
    }
  }, [talkGroupId])

  useEffect(() => {
    if (!Number.isFinite(talkGroupId)) return
    let active = true
    getTalkGroup(talkGroupId)
      .then((g) => active && setGroup(g))
      .catch(() => active && setGroup(null))
    return () => {
      active = false
    }
  }, [talkGroupId])

  useEffect(() => {
    void load(page)
  }, [load, page])

  const status = useSignalR(Number.isFinite(talkGroupId) ? talkGroupId : null, {
    onNewCall: () => {
      setPage(1)
      void load(1)
    },
    onNewTranscription: () => void load(page),
  })

  if (!Number.isFinite(talkGroupId)) {
    return <div className="error-state">Invalid talk group id.</div>
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <Link to="/talkgroups" className="back-link">← All talk groups</Link>
          <h1>
            TG {group?.number ?? talkGroupId}
            {group?.name ? ` · ${group.name}` : ''}
          </h1>
        </div>
        <span className="live">
          <span className={`status-dot ${status}`} aria-hidden="true" />
          {status === 'connected' ? 'Live' : status === 'connecting' ? 'Connecting…' : 'Offline'}
        </span>
      </div>

      {group?.description && <p className="tg-desc">{group.description}</p>}

      {loading ? (
        <div aria-busy="true">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="skeleton skeleton-card" />
          ))}
        </div>
      ) : error ? (
        <div className="error-state" role="alert">
          {error}
          <div style={{ marginTop: 8 }}>
            <button className="btn" onClick={() => void load(page)}>Retry</button>
          </div>
        </div>
      ) : !result || result.items.length === 0 ? (
        <div className="empty-state">No calls for this talk group yet.</div>
      ) : (
        <>
          {result.items.map((c) => (
            <CallCard key={c.id} call={c} />
          ))}
          {result.totalPages > 1 && (
            <Pagination
              page={result.page}
              totalPages={result.totalPages}
              onPageChange={setPage}
            />
          )}
        </>
      )}
    </div>
  )
}
