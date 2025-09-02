import React, { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import CallCard from '../components/CallCard'
import AutoplayBanner from '../components/AutoplayBanner'
import { CallCardSkeleton } from '../components/LoadingSpinner'
import Pagination from '../components/Pagination'
import type { CallDto, PagedResult, TalkGroupDto } from '../types/dtos'
import { useAudioManager } from '../hooks/useAudioManager'
import { useSubscriptions } from '../hooks/useSubscriptions'
import { usePageTitle } from '../hooks/usePageTitle'
import { apiGet } from '../api'

export default function TalkGroupPage() {
  const { id } = useParams<{ id: string }>()
  const talkGroupId = parseInt(id || '0', 10)
  
  const [calls, setCalls] = useState<CallDto[]>([])
  const [talkGroup, setTalkGroup] = useState<TalkGroupDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const pageSize = 20
  
  const { playCall, isCallPlaying } = useAudioManager()
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
    setLoading(true)
    setError(null)

    try {
      // Load both talkgroup info and calls for this specific talkgroup
      const [talkGroupRes, callsRes] = await Promise.all([
        apiGet<TalkGroupDto>(`/talkgroups/${talkGroupId}`).catch(() => null),
        apiGet<PagedResult<CallDto>>(`/calls?page=${currentPage}&pageSize=${pageSize}&talkGroupId=${talkGroupId}&sortBy=recordingTime&sortDir=desc`)
      ])

      if (talkGroupRes) {
        setTalkGroup(talkGroupRes)
      }

      if (callsRes) {
        setCalls(callsRes.items || [])
        setTotalPages(callsRes.totalPages || 1)
        setTotalItems(callsRes.totalCount || 0)
      }
    } catch (err) {
      console.error('Failed to load talkgroup data:', err)
      setError('Failed to load talkgroup data')
    } finally {
      setLoading(false)
    }
  }

  const handlePlayStateChange = (callId: number, isPlaying: boolean) => {
    console.log(`Call ${callId} play state changed: ${isPlaying}`)
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
            className={`subscribe-btn ${isSubscribed(talkGroupId) ? 'subscribed' : ''}`}
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
          <span className="stat-label">Recent Calls</span>
          <span className="stat-value">{calls.length}</span>
        </div>
        {isSubscribed(talkGroupId) && (
          <div className="stat">
            <span className="stat-label">Auto-play</span>
            <span className="stat-value enabled">Enabled</span>
          </div>
        )}
      </div>

      <AutoplayBanner />

      {calls.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">📻</div>
          <h3>No Recent Calls</h3>
          <p className="text-muted">
            No calls found for this talkgroup. Check back later for new activity.
          </p>
        </div>
      ) : (
        <div className="calls-section">
          <h2>Recent Calls ({totalItems.toLocaleString()})</h2>
          <div className="calls-list">
            {calls.map(call => (
              <CallCard 
                key={call.id} 
                call={call}
                isPlaying={isCallPlaying(call.id)}
                onSubscribe={toggleSubscription}
                isSubscribed={isSubscribed(call.talkGroupId)}
                onPlayStateChange={handlePlayStateChange}
              />
            ))}
          </div>
          
          <Pagination
            currentPage={currentPage}
            totalPages={totalPages}
            totalItems={totalItems}
            itemsPerPage={pageSize}
            onPageChange={setCurrentPage}
            loading={loading}
          />
        </div>
      )}

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

        .subscribe-btn {
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
        }

        .subscribe-btn:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
        }

        .subscribe-btn.subscribed {
          background: var(--accent-primary);
          border-color: var(--accent-primary);
          color: white;
        }

        .subscribe-icon {
          font-size: var(--font-size-lg);
        }

        .talkgroup-stats {
          display: flex;
          gap: var(--space-4);
          margin-bottom: var(--space-4);
          flex-wrap: wrap;
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

        .calls-list {
          display: flex;
          flex-direction: column;
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
        }
      `}</style>
    </section>
  )
}
