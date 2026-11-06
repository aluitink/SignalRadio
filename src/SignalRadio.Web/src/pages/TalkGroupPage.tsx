import { useCallback, useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { CallDto, TalkGroupDto, PagedResult } from '../types/dtos'
import { getTalkGroup, getCalls, getFinalTranscription } from '../api'
import { useSignalR } from '../hooks/useSignalR'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import CallCard from '../components/CallCard'
import Pagination from '../components/Pagination'
import CallVolumeChart, { talkGroupColor } from '../components/CallVolumeChart'

const PAGE_SIZE = 20

export default function TalkGroupPage() {
  const { id } = useParams<{ id: string }>()
  const talkGroupId = id ? Number(id) : NaN

  const [group, setGroup] = useState<TalkGroupDto | null>(null)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<CallDto> | null>(null)
  const [transcripts, setTranscripts] = useState<Map<number, string>>(new Map())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [volumeRefresh, setVolumeRefresh] = useState(0)
  const { isSubscribed, toggleSubscription } = useSubscriptions()

  const fetchTranscripts = useCallback(async (items: CallDto[]) => {
    const withRecording = items.filter((c) => c.latestRecordingId)
    if (withRecording.length === 0) return
    const results = await Promise.all(
      withRecording.map(async (c) => {
        try {
          const t = await getFinalTranscription(c.latestRecordingId!)
          return { callId: c.id, text: t?.fullText ?? null }
        } catch {
          return { callId: c.id, text: null }
        }
      }),
    )
    setTranscripts((prev) => {
      const next = new Map(prev)
      for (const r of results) {
        if (r.text) next.set(r.callId, r.text)
      }
      return next
    })
  }, [])

  const load = useCallback(async (p: number) => {
    if (!Number.isFinite(talkGroupId)) return
    try {
      const data = await getCalls(talkGroupId, p, PAGE_SIZE)
      setResult(data)
      setError(null)
      void fetchTranscripts(data.items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load calls')
    } finally {
      setLoading(false)
    }
  }, [talkGroupId, fetchTranscripts])

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

  useSignalR(Number.isFinite(talkGroupId) ? talkGroupId : null, {
    onNewCall: (payload) => {
      const call = typeof payload === 'number' ? null : payload
      if (call) {
        // Prepend the new call to the top of the list (de-duplicated).
        setResult((prev) => {
          if (!prev) return prev
          if (prev.items.some((c) => c.id === call.id)) return prev
          return {
            ...prev,
            items: [call, ...prev.items],
            totalCount: prev.totalCount + 1,
          }
        })
        setVolumeRefresh((k) => k + 1)
        void fetchTranscripts([call])
      } else {
        setPage(1)
        void load(1)
      }
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
        <div className="page-header-actions">
          <button
            className={`btn subscribe-btn${isSubscribed(talkGroupId) ? ' subscribed' : ''}`}
            onClick={() => toggleSubscription(talkGroupId)}
            aria-pressed={isSubscribed(talkGroupId)}
            title={isSubscribed(talkGroupId) ? 'Unsubscribe from auto-play' : 'Subscribe to auto-play new calls'}
          >
            {isSubscribed(talkGroupId) ? '✓ Subscribed' : '＋ Subscribe'}
          </button>
        </div>
      </div>

      {group?.description && <p className="tg-desc">{group.description}</p>}

      <CallVolumeChart
        talkGroupId={talkGroupId}
        title={`Call volume · TG ${group?.number ?? talkGroupId}`}
        refreshKey={volumeRefresh}
        barColor={talkGroupColor(talkGroupId)}
      />

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
            <CallCard key={c.id} call={c} transcript={transcripts.get(c.id)} accentColor={talkGroupColor(talkGroupId)} />
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
