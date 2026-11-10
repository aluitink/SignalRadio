import { useCallback, useEffect, useRef, useState } from 'react'
import type { CallDto, SearchResultsDto, TranscriptSummaryDto } from '../types/dtos'
import { search, searchSummariesPaged } from '../api'
import CallCard from '../components/CallCard'
import SummaryCard from '../components/SummaryCard'
import InvestigationPromptModal from '../components/InvestigationPromptModal'
import { talkGroupColor } from '../components/CallVolumeChart'
import './SearchPage.css'

const PAGE_SIZE = 20

type SearchMode = 'keyword' | 'semantic' | 'hybrid'
type ResultTab = 'calls' | 'summaries'

export default function SearchPage() {
  const [q, setQ] = useState('')
  const [mode, setMode] = useState<SearchMode>('keyword')
  const [tab, setTab] = useState<ResultTab>('calls')
  const [investigateCall, setInvestigateCall] = useState<CallDto | null>(null)

  // ---- Calls (transcripts) results ----
  const [calls, setCalls] = useState<CallDto[]>([])
  const [callTranscripts, setCallTranscripts] = useState<Map<number, string>>(new Map())
  const [callsTotal, setCallsTotal] = useState(0)
  const [callsPage, setCallsPage] = useState(1)
  const [callsHasMore, setCallsHasMore] = useState(false)
  const [callsLoading, setCallsLoading] = useState(false)
  const [callsLoadingMore, setCallsLoadingMore] = useState(false)
  const [callsError, setCallsError] = useState<string | null>(null)

  // ---- Summaries results ----
  const [summaries, setSummaries] = useState<TranscriptSummaryDto[]>([])
  const [summariesCursor, setSummariesCursor] = useState<{ time: string; id: number } | null>(null)
  const [summariesHasMore, setSummariesHasMore] = useState(false)
  const [summariesLoading, setSummariesLoading] = useState(false)
  const [summariesLoadingMore, setSummariesLoadingMore] = useState(false)
  const [summariesError, setSummariesError] = useState<string | null>(null)

  const [searched, setSearched] = useState(false)
  const callsLoadingRef = useRef(false)
  const summariesLoadingRef = useRef(false)
  const callsSentinelRef = useRef<HTMLDivElement | null>(null)
  const summariesSentinelRef = useRef<HTMLDivElement | null>(null)

  const activeQuery = q.trim()

  // ---- Calls: load a page (append = infinite scroll) ----
  const loadCalls = useCallback(
    async (page: number, append: boolean) => {
      if (callsLoadingRef.current || !activeQuery) return
      callsLoadingRef.current = true
      if (append) setCallsLoadingMore(true)
      else setCallsLoading(true)
      setCallsError(null)
      try {
        const data: SearchResultsDto = await search(activeQuery, page, PAGE_SIZE, mode === 'semantic', mode === 'hybrid')
        const newCalls: CallDto[] = data.transcriptions.map((t) => ({
          id: t.callId,
          talkGroupId: t.talkGroupId,
          talkGroupNumber: t.talkGroupNumber,
          talkGroupName: t.talkGroupName,
          recordingTime: t.recordingTime,
          frequencyHz: 0,
          durationSeconds: 0,
          createdAt: t.recordingTime,
          recordingCount: 0,
          latestRecordingId: t.recordingId,
        }))
        setCalls((prev) => {
          if (!append) return newCalls
          const existing = new Set(prev.map((c) => c.id))
          return [...prev, ...newCalls.filter((c) => !existing.has(c.id))]
        })
        setCallTranscripts((prev) => {
          const next = new Map(prev)
          for (const t of data.transcriptions) next.set(t.callId, t.fullText)
          return next
        })
        setCallsTotal(data.totalCount)
        setCallsPage(page)
        setCallsHasMore(page * PAGE_SIZE < data.totalCount)
      } catch (e) {
        setCallsError(e instanceof Error ? e.message : 'Search failed')
      } finally {
        callsLoadingRef.current = false
        setCallsLoading(false)
        setCallsLoadingMore(false)
      }
    },
    [activeQuery, mode],
  )

  // ---- Summaries: load a page (cursor-based infinite scroll) ----
  const loadSummaries = useCallback(
    async (before: { time: string; id: number } | null, append: boolean) => {
      if (summariesLoadingRef.current || !activeQuery) return
      summariesLoadingRef.current = true
      if (append) setSummariesLoadingMore(true)
      else setSummariesLoading(true)
      setSummariesError(null)
      try {
        const data = await searchSummariesPaged(activeQuery, PAGE_SIZE, before)
        setSummaries((prev) => (append ? [...prev, ...data.summaries] : data.summaries))
        setSummariesCursor(
          data.hasMore && data.nextCursorTime && data.nextCursorId != null
            ? { time: data.nextCursorTime, id: data.nextCursorId }
            : null,
        )
        setSummariesHasMore(data.hasMore)
      } catch (e) {
        setSummariesError(e instanceof Error ? e.message : 'Search failed')
      } finally {
        summariesLoadingRef.current = false
        setSummariesLoading(false)
        setSummariesLoadingMore(false)
      }
    },
    [activeQuery],
  )

  // Run both searches when the query or mode changes.
  useEffect(() => {
    if (!activeQuery) return
    setSearched(true)
    setCalls([])
    setCallTranscripts(new Map())
    setCallsPage(1)
    setCallsHasMore(false)
    setSummaries([])
    setSummariesCursor(null)
    setSummariesHasMore(false)
    void loadCalls(1, false)
    void loadSummaries(null, false)
  }, [activeQuery, mode, loadCalls, loadSummaries])

  // Infinite scroll: observe the active tab's sentinel.
  useEffect(() => {
    const sentinel = tab === 'calls' ? callsSentinelRef.current : summariesSentinelRef.current
    if (!sentinel) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (!entries[0].isIntersecting) return
        if (tab === 'calls') {
          if (callsHasMore && !callsLoadingRef.current) void loadCalls(callsPage + 1, true)
        } else {
          if (summariesHasMore && !summariesLoadingRef.current) void loadSummaries(summariesCursor, true)
        }
      },
      { rootMargin: '200px' },
    )
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [tab, callsHasMore, callsPage, summariesHasMore, summariesCursor, loadCalls, loadSummaries])

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    // The effect above reacts to `q` changes; submitting just ensures the
    // latest input value is committed.
  }

  const showCalls = tab === 'calls'
  const showSummaries = tab === 'summaries'

  return (
    <div>
      <div className="page-header">
        <h1>Search</h1>
      </div>

      <form className="search-bar" onSubmit={onSubmit} role="search">
        <input
          className="input"
          type="search"
          placeholder="Search transcripts and summaries…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          aria-label="Search query"
        />
        <select
          className="input mode-select"
          value={mode}
          onChange={(e) => setMode(e.target.value as SearchMode)}
          aria-label="Search mode"
        >
          <option value="keyword">Keyword</option>
          <option value="semantic">Semantic</option>
          <option value="hybrid">Hybrid</option>
        </select>
        <button className="btn primary" type="submit" disabled={!q.trim()}>
          Search
        </button>
      </form>

      {searched && (
        <div className="result-tabs" role="tablist" aria-label="Search results">
          <button
            role="tab"
            aria-selected={showCalls}
            className={`result-tab${showCalls ? ' active' : ''}`}
            onClick={() => setTab('calls')}
          >
            Calls{callsTotal > 0 ? ` (${callsTotal})` : ''}
          </button>
          <button
            role="tab"
            aria-selected={showSummaries}
            className={`result-tab${showSummaries ? ' active' : ''}`}
            onClick={() => setTab('summaries')}
          >
            Summaries{summaries.length > 0 ? ` (${summaries.length})` : ''}
          </button>
        </div>
      )}

      {showCalls && (
        <section className="result-section" role="tabpanel" aria-label="Call results">
          {callsError && <div className="error-state" role="alert">{callsError}</div>}

          {callsLoading && (
            <div aria-busy="true">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="skeleton skeleton-card" />
              ))}
            </div>
          )}

          {!callsLoading && !callsError && calls.length === 0 && (
            <div className="empty-state">No matching calls.</div>
          )}

          {!callsLoading && calls.length > 0 && (
            <div>
              {calls.map((c) => (
                <CallCard
                  key={c.id}
                  call={c}
                  transcript={callTranscripts.get(c.id)}
                  accentColor={talkGroupColor(c.talkGroupId)}
                  onInvestigate={setInvestigateCall}
                />
              ))}
              <div ref={callsSentinelRef} className="scroll-sentinel" />
              {callsLoadingMore && <div className="loading-more">Loading more…</div>}
              {!callsHasMore && (
                <div className="end-of-list">
                  {callsTotal} call{callsTotal === 1 ? '' : 's'} found
                </div>
              )}
            </div>
          )}
        </section>
      )}

      {showSummaries && (
        <section className="result-section" role="tabpanel" aria-label="Summary results">
          {summariesError && <div className="error-state" role="alert">{summariesError}</div>}

          {summariesLoading && (
            <div aria-busy="true">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="skeleton skeleton-card" />
              ))}
            </div>
          )}

          {!summariesLoading && !summariesError && summaries.length === 0 && (
            <div className="empty-state">No matching summaries.</div>
          )}

          {!summariesLoading && summaries.length > 0 && (
            <div>
              {summaries.map((s) => (
                <SummaryCard key={s.id} summary={s} to={`/summary/${s.id}?from=search`} />
              ))}
              <div ref={summariesSentinelRef} className="scroll-sentinel" />
              {summariesLoadingMore && <div className="loading-more">Loading more…</div>}
              {!summariesHasMore && <div className="end-of-list">You're all caught up</div>}
            </div>
          )}
        </section>
      )}

      {investigateCall && (
        <InvestigationPromptModal
          call={investigateCall}
          onClose={() => setInvestigateCall(null)}
          onCreated={() => { /* investigation queued, ticker will show it */ }}
        />
      )}
    </div>
  )
}
