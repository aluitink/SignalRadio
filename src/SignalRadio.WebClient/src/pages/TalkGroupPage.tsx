import React, { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import CallCard from '../components/CallCard'
import FrequencyTabs from '../components/FrequencyTabs'
import { CallCardSkeleton } from '../components/LoadingSpinner'
import Pagination from '../components/Pagination'
import type { CallDto, PagedResult, TalkGroupDto } from '../types/dtos'
import { audioPlayerService } from '../services/AudioPlayerService'
import { useSubscriptions } from '../contexts/SubscriptionContext'
import { usePageTitle } from '../hooks/usePageTitle'
import { apiGet } from '../api'

export default function TalkGroupPage() {
  const { id } = useParams<{ id: string }>()
  const talkGroupId = parseInt(id || '0', 10)
  
  const [calls, setCalls] = useState<CallDto[]>([])
  const [talkGroup, setTalkGroup] = useState<TalkGroupDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [callsLoading, setCallsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const [viewMode, setViewMode] = useState<'chronological' | 'frequency'>('chronological')
  const pageSize = 20
  
  const { toggle: toggleSubscription, isSubscribed } = useSubscriptions()

  useEffect(() => {
    if (!talkGroupId) {
      setError('Invalid talkgroup ID')
      setLoading(false)
      return
    }

    loadTalkGroupData()
  }, [talkGroupId, currentPage])

  const loadTalkGroupData = async () => {
    const isInitialLoad = !talkGroup
    
    if (isInitialLoad) {
      setLoading(true)
    } else {
      setCallsLoading(true)
    }
    
    setError(null)

    try {
      // For initial load, get both talkgroup and calls
      // For pagination, only get calls
      if (isInitialLoad) {
        const [talkGroupRes, callsRes] = await Promise.all([
          apiGet<TalkGroupDto>(`/talkgroups/${talkGroupId}`).catch(() => null),
          apiGet<PagedResult<CallDto>>(`/talkgroups/${talkGroupId}/calls?page=${currentPage}&pageSize=${pageSize}&sortBy=recordingTime&sortDir=desc`)
        ])

        if (talkGroupRes) {
          setTalkGroup(talkGroupRes)
        }

        if (callsRes) {
          setCalls(callsRes.items || [])
          setTotalPages(callsRes.totalPages || 1)
          setTotalItems(callsRes.totalCount || 0)
        }
      } else {
        // Just get calls for pagination
        const callsRes = await apiGet<PagedResult<CallDto>>(`/talkgroups/${talkGroupId}/calls?page=${currentPage}&pageSize=${pageSize}&sortBy=recordingTime&sortDir=desc`)
        
        if (callsRes) {
          setCalls(callsRes.items || [])
          setTotalPages(callsRes.totalPages || 1)
          setTotalItems(callsRes.totalCount || 0)
        }
      }
    } catch (err) {
      console.error('Failed to load talkgroup data:', err)
      setError('Failed to load talkgroup data')
    } finally {
      setLoading(false)
      setCallsLoading(false)
    }
  }

  const talkGroupDisplay = talkGroup?.description || 
                          talkGroup?.alphaTag || 
                          talkGroup?.name || 
                          `TalkGroup ${talkGroupId}`

  // Update page title and breadcrumb when talkgroup data loads
  usePageTitle(talkGroupDisplay, talkGroupDisplay)

  if (loading) {
    return (
      <section className="talkgroup-page">
                <div className="loading-skeleton">
          {Array.from({ length: 5 }).map((_, i) => (
            <CallCardSkeleton key={i} />
          ))}
        </div>

        <style>{`
          .loading-skeleton {
            display: flex;
            flex-direction: column;
            gap: var(--space-2);
          }

          .skeleton-header {
            height: 80px;
            background: var(--bg-card);
            border-radius: var(--radius);
            animation: pulse 1.5s ease-in-out infinite;
          }

          @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
          }
        `}</style>
      </section>
    )
  }

  if (error) {
    return (
      <section className="talkgroup-page">
        <div className="error-state">
          <div className="error-icon">⚠️</div>
          <h2>Error</h2>
          <p className="text-muted">{error}</p>
          <Link to="/" className="back-link">← Back to Live Stream</Link>
        </div>

        <style>{`
          .error-state {
            text-align: center;
            padding: var(--space-6) var(--space-2);
            color: var(--text-secondary);
          }

          .error-icon {
            font-size: 48px;
            margin-bottom: var(--space-2);
          }

          .error-state h2 {
            color: var(--text-primary);
            margin-bottom: var(--space-1);
          }

          .back-link {
            color: var(--accent-primary);
            text-decoration: none;
            font-weight: 500;
            transition: var(--transition);
          }

          .back-link:hover {
            text-decoration: underline;
          }
        `}</style>
      </section>
    )
  }

  return (
    <section className="talkgroup-page">
      <header className="talkgroup-header">
        <div className="header-main">
          <h1>{talkGroupDisplay}</h1>
          
          {talkGroup && (
            <div className="talkgroup-meta">
              {talkGroup.number && (
                <span className="meta-item">TG {talkGroup.number}</span>
              )}
              {talkGroup.category && (
                <span className="meta-item">{talkGroup.category}</span>
              )}
              {talkGroup.priority && (
                <span className="meta-item priority">Priority {talkGroup.priority}</span>
              )}
            </div>
          )}
        </div>

        <div className="header-actions">
          <button
            className={`talkgroup-subscribe-btn ${isSubscribed(talkGroupId) ? 'subscribed' : ''}`}
            onClick={() => toggleSubscription(talkGroupId)}
          >
            <span className="subscribe-icon">
              {isSubscribed(talkGroupId) ? '⭐' : '☆'}
            </span>
            <span className="subscribe-text">
              {isSubscribed(talkGroupId) ? 'Subscribed' : 'Subscribe'}
            </span>
          </button>
        </div>
      </header>

      <div className="talkgroup-stats">
        <div className="stat">
          <span className="stat-label">Showing</span>
          <span className="stat-value">{calls.length} of {totalItems.toLocaleString()}</span>
        </div>
        {talkGroup?.number && (
          <div className="stat">
            <span className="stat-label">TG Number</span>
            <span className="stat-value">{talkGroup.number}</span>
          </div>
        )}
        {isSubscribed(talkGroupId) && (
          <div className="stat">
            <span className="stat-label">Auto-play</span>
            <span className="stat-value enabled">Enabled</span>
          </div>
        )}
      </div>

      <div className="calls-section">
        <div className="calls-header">
          <h2>Recent Calls</h2>
          <div className="header-right">
            <div className="view-toggle">
              <button
                className={`toggle-btn ${viewMode === 'chronological' ? 'active' : ''}`}
                onClick={() => setViewMode('chronological')}
                title="Chronological view"
              >
                📅
              </button>
              <button
                className={`toggle-btn ${viewMode === 'frequency' ? 'active' : ''}`}
                onClick={() => setViewMode('frequency')}
                title="Group by frequency"
              >
                📡
              </button>
            </div>
            <div className="calls-count">
              {totalItems.toLocaleString()} total calls
            </div>
          </div>
        </div>

        {calls.length === 0 && !callsLoading ? (
          <div className="empty-state">
            <div className="empty-icon">📻</div>
            <h3>No Recent Calls</h3>
            <p className="text-muted">
              {totalItems === 0 
                ? `No calls found for ${talkGroupDisplay}. This talkgroup may be inactive or new.`
                : "No calls found for the current page. Try navigating to a different page."
              }
            </p>
            {totalItems === 0 && (
              <p className="text-muted">
                You can subscribe to this talkgroup to get notified when new calls arrive.
              </p>
            )}
          </div>
        ) : viewMode === 'frequency' ? (
          <FrequencyTabs talkGroupId={talkGroupId} limit={100} />
        ) : (
          <>
            <div className="calls-list">
              {callsLoading && (
                <div className="calls-loading">
                  {Array.from({ length: 3 }).map((_, i) => (
                    <CallCardSkeleton key={i} />
                  ))}
                </div>
              )}
              {!callsLoading && calls.map(call => (
                <CallCard 
                  key={call.id} 
                  call={call}
                />
              ))}
            </div>
            
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalItems}
              itemsPerPage={pageSize}
              onPageChange={setCurrentPage}
              loading={callsLoading}
            />
          </>
        )}
      </div>

      <style>{`
        .talkgroup-page {
          min-height: 60vh;
        }

        .talkgroup-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--space-4);
          gap: var(--space-3);
          padding: var(--space-4);
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius-lg);
        }

        .header-main {
          flex: 1;
          min-width: 0;
        }

        .talkgroup-header h1 {
          margin-bottom: var(--space-2);
          word-break: break-word;
        }

        .talkgroup-meta {
          display: flex;
          gap: var(--space-2);
          flex-wrap: wrap;
        }

        .meta-item {
          background: var(--bg-card);
          color: var(--text-secondary);
          padding: var(--space-1) var(--space-2);
          border-radius: var(--radius-sm);
          font-size: var(--font-size-sm);
          border: 1px solid var(--border);
        }

        .meta-item.priority {
          background: var(--accent-secondary);
          color: white;
          border-color: var(--accent-secondary);
        }

        .header-actions {
          flex-shrink: 0;
        }

        .talkgroup-subscribe-btn {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          background: var(--bg-card);
          border: 1px solid var(--border);
          color: var(--text-secondary);
          padding: var(--space-2) var(--space-3);
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          font-weight: 500;
          min-width: 120px;
          justify-content: center;
        }

        .talkgroup-subscribe-btn:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
          transform: translateY(-1px);
        }

        .talkgroup-subscribe-btn.subscribed {
          background: var(--accent-primary);
          border-color: var(--accent-primary);
          color: white;
        }

        .talkgroup-subscribe-btn.subscribed:hover {
          background: var(--accent-primary-hover);
          border-color: var(--accent-primary-hover);
          transform: translateY(-1px);
        }

        .subscribe-icon {
          font-size: var(--font-size-lg);
        }

        .talkgroup-stats {
          display: flex;
          gap: var(--space-4);
          margin-bottom: var(--space-6);
          flex-wrap: wrap;
          padding: var(--space-3);
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
        }

        .stat {
          display: flex;
          flex-direction: column;
          gap: var(--space-1);
        }

        .stat-label {
          font-size: var(--font-size-sm);
          color: var(--text-muted);
          text-transform: uppercase;
          letter-spacing: 0.5px;
        }

        .stat-value {
          font-size: var(--font-size-xl);
          font-weight: 600;
          color: var(--text-primary);
        }

        .stat-value.enabled {
          color: #10b981;
        }

        .calls-section h2 {
          margin-bottom: var(--space-3);
        }

        .calls-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: var(--space-4);
          padding-bottom: var(--space-2);
          border-bottom: 1px solid var(--border);
        }

        .calls-header h2 {
          margin: 0;
          color: var(--text-primary);
        }

        .header-right {
          display: flex;
          align-items: center;
          gap: var(--space-3);
        }

        .view-toggle {
          display: flex;
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          overflow: hidden;
        }

        .toggle-btn {
          background: transparent;
          border: none;
          padding: var(--space-1) var(--space-2);
          cursor: pointer;
          transition: var(--transition);
          font-size: 16px;
          display: flex;
          align-items: center;
          justify-content: center;
          min-width: 40px;
        }

        .toggle-btn:hover {
          background: var(--bg-card-hover);
        }

        .toggle-btn.active {
          background: var(--accent-primary);
          color: white;
        }

        .toggle-btn:not(:last-child) {
          border-right: 1px solid var(--border);
        }

        .calls-count {
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
          font-weight: 500;
        }

        .calls-loading {
          display: flex;
          flex-direction: column;
          gap: var(--space-2);
        }        .calls-count {
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
          font-weight: 500;
        }

        .calls-list {
          display: flex;
          flex-direction: column;
          gap: var(--space-2);
          margin-bottom: var(--space-4);
        }

        .calls-loading {
          display: flex;
          flex-direction: column;
          gap: var(--space-2);
          opacity: 0.6;
        }

        .empty-state {
          text-align: center;
          padding: var(--space-6) var(--space-2);
          color: var(--text-secondary);
        }

        .empty-icon {
          font-size: 48px;
          margin-bottom: var(--space-2);
        }

        .empty-state h3 {
          color: var(--text-primary);
          margin-bottom: var(--space-1);
        }

        @media (max-width: 767px) {
          .talkgroup-header {
            flex-direction: column;
            align-items: stretch;
          }

          .header-actions {
            align-self: flex-start;
          }

          .talkgroup-meta {
            gap: var(--space-1);
          }

          .meta-item {
            font-size: var(--font-size-xs);
            padding: 4px var(--space-1);
          }

          .talkgroup-stats {
            gap: var(--space-2);
          }

          .calls-header {
            flex-direction: column;
            align-items: flex-start;
            gap: var(--space-2);
          }

          .header-right {
            align-self: stretch;
            justify-content: space-between;
            gap: var(--space-2);
          }

          .calls-count {
            font-size: var(--font-size-xs);
          }

          .toggle-btn {
            min-width: 36px;
            font-size: 14px;
          }
        }
      `}</style>
    </section>
  )
}
