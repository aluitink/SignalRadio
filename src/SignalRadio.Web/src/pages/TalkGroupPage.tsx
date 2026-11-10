import { useCallback, useEffect, useRef, useState } from 'react'
import { useParams, Link, useSearchParams } from 'react-router-dom'
import type { CallDto, TalkGroupDto, TranscriptSummaryDto } from '../types/dtos'
import { getTalkGroup, getSummariesForTalkGroup } from '../api'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import CallStream from '../components/CallStream'
import SummaryCard from '../components/SummaryCard'
import CallVolumeChart, { talkGroupColor } from '../components/CallVolumeChart'
import InvestigationPromptModal from '../components/InvestigationPromptModal'
import './TalkGroupPage.css'

type Tab = 'calls' | 'summaries'

export default function TalkGroupPage() {
  const { id } = useParams<{ id: string }>()
  const talkGroupId = id ? Number(id) : NaN
  const [searchParams, setSearchParams] = useSearchParams()

  const [group, setGroup] = useState<TalkGroupDto | null>(null)
  const [investigateCall, setInvestigateCall] = useState<CallDto | null>(null)
  const { isSubscribed, toggleSubscription } = useSubscriptions()

  // ---- Summaries tab state ----
  const [summaries, setSummaries] = useState<TranscriptSummaryDto[]>([])
  const [summariesLoading, setSummariesLoading] = useState(false)
  const [summariesError, setSummariesError] = useState<string | null>(null)
  const summariesLoadedRef = useRef(false)

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

  const loadSummaries = useCallback(async () => {
    if (!Number.isFinite(talkGroupId)) return
    setSummariesLoading(true)
    setSummariesError(null)
    try {
      const data = await getSummariesForTalkGroup(talkGroupId)
      setSummaries(data)
    } catch (e) {
      setSummariesError(e instanceof Error ? e.message : 'Failed to load summaries')
    } finally {
      setSummariesLoading(false)
    }
  }, [talkGroupId])

  // Load summaries lazily the first time the tab is shown.
  useEffect(() => {
    if (searchParams.get('tab') === 'summaries' && !summariesLoadedRef.current) {
      summariesLoadedRef.current = true
      void loadSummaries()
    }
  }, [searchParams, loadSummaries])

  if (!Number.isFinite(talkGroupId)) {
    return <div className="error-state">Invalid talk group id.</div>
  }

  const activeTab: Tab = searchParams.get('tab') === 'summaries' ? 'summaries' : 'calls'

  const setTab = (tab: Tab) => {
    const next = new URLSearchParams(searchParams)
    if (tab === 'calls') next.delete('tab')
    else next.set('tab', tab)
    setSearchParams(next, { replace: true })
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <Link to="/talkgroups" className="back-link">← All talk groups</Link>
          <h1>
            {group?.name || group?.description || `TG ${group?.number ?? talkGroupId}`}
          </h1>
        </div>
        <div className="page-header-actions">
          <button
            className={`subscribe-btn${isSubscribed(talkGroupId) ? ' subscribed' : ''}`}
            onClick={() => toggleSubscription(talkGroupId)}
            aria-pressed={isSubscribed(talkGroupId)}
            aria-label={isSubscribed(talkGroupId) ? 'Unsubscribe from auto-play' : 'Subscribe to auto-play new calls'}
            title={isSubscribed(talkGroupId) ? 'Unsubscribe from auto-play' : 'Subscribe to auto-play new calls'}
          >
            {isSubscribed(talkGroupId) ? (
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.64 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z" />
              </svg>
            ) : (
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
                <path d="M13.73 21a2 2 0 0 1-3.46 0" />
              </svg>
            )}
          </button>
        </div>
      </div>

      <CallVolumeChart
        talkGroupId={talkGroupId}
        barColor={talkGroupColor(talkGroupId)}
      />

      <div className="result-tabs" role="tablist" aria-label="Talk group views">
        <button
          role="tab"
          aria-selected={activeTab === 'calls'}
          className={`result-tab${activeTab === 'calls' ? ' active' : ''}`}
          onClick={() => setTab('calls')}
        >
          Calls
        </button>
        <button
          role="tab"
          aria-selected={activeTab === 'summaries'}
          className={`result-tab${activeTab === 'summaries' ? ' active' : ''}`}
          onClick={() => setTab('summaries')}
        >
          Summaries
        </button>
      </div>

      {activeTab === 'calls' ? (
        <CallStream
          talkGroupId={talkGroupId}
          onInvestigate={setInvestigateCall}
          emptyMessage="No calls for this talk group yet."
          hideSubscribe
        />
      ) : (
        <section role="tabpanel" aria-label="Summaries">
          {summariesError && <div className="error-state" role="alert">{summariesError}</div>}

          {summariesLoading && (
            <div aria-busy="true">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="skeleton skeleton-card" />
              ))}
            </div>
          )}

          {!summariesLoading && !summariesError && summaries.length === 0 && (
            <div className="empty-state">No summaries for this talk group yet.</div>
          )}

          {!summariesLoading && summaries.length > 0 && (
            <div>
              {summaries.map((s) => (
                <SummaryCard key={s.id} summary={s} to={`/summary/${s.id}?from=talkgroup`} />
              ))}
            </div>
          )}
        </section>
      )}

      {investigateCall && (
        <InvestigationPromptModal
          call={investigateCall}
          onClose={() => setInvestigateCall(null)}
          onCreated={() => { /* investigation queued */ }}
        />
      )}
    </div>
  )
}
