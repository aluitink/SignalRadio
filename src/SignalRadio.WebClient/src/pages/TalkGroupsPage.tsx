import React, { useState, useEffect, useMemo, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { useSubscriptions } from '../contexts/SubscriptionContext'
import { usePageTitle } from '../hooks/usePageTitle'
import { apiGet } from '../api'
import type { TalkGroupDto, TalkGroupStats, PagedResult } from '../types/dtos'

interface TalkGroupWithStats extends TalkGroupDto {
  callCount?: number
  lastActivity?: string
  totalDurationSeconds?: number
}

type SortBy = 'name' | 'number' | 'callCount' | 'lastActivity' | 'totalDuration'
type SortDirection = 'asc' | 'desc'

// Helper functions for formatting
const formatDuration = (seconds: number): string => {
  if (seconds < 60) return `${Math.round(seconds)}s`
  if (seconds < 3600) return `${Math.round(seconds / 60)}m`
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.round((seconds % 3600) / 60)
  return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`
}

const formatLastActivity = (lastActivity: string): string => {
  const date = new Date(lastActivity)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))
  
  if (diffDays === 0) {
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60))
    if (diffHours === 0) {
      const diffMinutes = Math.floor(diffMs / (1000 * 60))
      return diffMinutes <= 1 ? 'Just now' : `${diffMinutes}m ago`
    }
    return `${diffHours}h ago`
  }
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays} days ago`
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`
  return date.toLocaleDateString()
}

export default function TalkGroupsPage() {
  const [allTalkGroups, setAllTalkGroups] = useState<TalkGroupWithStats[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [subscriptionFilter, setSubscriptionFilter] = useState<'all' | 'subscribed' | 'unsubscribed'>('all')
  
  // Sorting
  const [sortBy, setSortBy] = useState<SortBy>('name')
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc')
  
  // Pagination (client-side)
  const [currentPage, setCurrentPage] = useState(1)
  const pageSize = 100

  const { toggle: toggleSubscription, isSubscribed, subscriptionCount } = useSubscriptions()

  usePageTitle('Talk Groups')

  useEffect(() => {
    loadAllTalkGroups()
  }, [])

  // Reset to page 1 when filters or sorting change
  useEffect(() => {
    setCurrentPage(1)
  }, [searchTerm, subscriptionFilter, sortBy, sortDirection])

  const loadAllTalkGroups = async () => {
    try {
      setLoading(true)
      setError(null)
      
      // Load talkgroups and statistics in parallel
      const [talkGroupsResponse, statsResponse] = await Promise.all([
        apiGet<PagedResult<TalkGroupDto>>('/talkgroups?page=1&pageSize=1000'),
        apiGet<Record<number, TalkGroupStats>>('/talkgroups/stats')
      ])
      
      if (talkGroupsResponse && statsResponse) {
        // Merge talkgroups with their statistics
        const talkGroupsWithStats: TalkGroupWithStats[] = talkGroupsResponse.items.map(tg => ({
          ...tg,
          callCount: statsResponse[tg.id]?.callCount || 0,
          lastActivity: statsResponse[tg.id]?.lastActivity,
          totalDurationSeconds: statsResponse[tg.id]?.totalDurationSeconds || 0
        }))
        
        setAllTalkGroups(talkGroupsWithStats)
      }
    } catch (err) {
      console.error('Failed to load talkgroups:', err)
      setError('Failed to load talkgroups')
    } finally {
      setLoading(false)
    }
  }

  // Filter and sort talkgroups based on search term, subscription status, and sorting preference
  const filteredAndSortedTalkGroups = useMemo(() => {
    let filtered = allTalkGroups.filter(tg => {
      // Search filter
      if (searchTerm.trim()) {
        const term = searchTerm.toLowerCase()
        const matchesSearch = 
          tg.number?.toString().includes(term) ||
          tg.name?.toLowerCase().includes(term) ||
          tg.alphaTag?.toLowerCase().includes(term) ||
          tg.description?.toLowerCase().includes(term) ||
          tg.tag?.toLowerCase().includes(term)
        
        if (!matchesSearch) return false
      }

      // Subscription filter
      if (subscriptionFilter !== 'all') {
        const isSubscribedToTalkGroup = isSubscribed(tg.number || 0)
        if (subscriptionFilter === 'subscribed' && !isSubscribedToTalkGroup) return false
        if (subscriptionFilter === 'unsubscribed' && isSubscribedToTalkGroup) return false
      }

      return true
    })

    // Sort the filtered results
    filtered.sort((a, b) => {
      let aValue: any, bValue: any
      
      switch (sortBy) {
        case 'name':
          aValue = a.name || a.alphaTag || `Talk Group ${a.number}`
          bValue = b.name || b.alphaTag || `Talk Group ${b.number}`
          break
        case 'number':
          aValue = a.number || 0
          bValue = b.number || 0
          break
        case 'callCount':
          aValue = a.callCount || 0
          bValue = b.callCount || 0
          break
        case 'lastActivity':
          aValue = a.lastActivity ? new Date(a.lastActivity).getTime() : 0
          bValue = b.lastActivity ? new Date(b.lastActivity).getTime() : 0
          break
        case 'totalDuration':
          aValue = a.totalDurationSeconds || 0
          bValue = b.totalDurationSeconds || 0
          break
        default:
          return 0
      }

      if (aValue === bValue) return 0
      
      const comparison = aValue < bValue ? -1 : 1
      return sortDirection === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [allTalkGroups, searchTerm, subscriptionFilter, isSubscribed, sortBy, sortDirection])

  // Paginate the filtered and sorted results
  const startIndex = (currentPage - 1) * pageSize
  const endIndex = startIndex + pageSize
  const paginatedTalkGroups = filteredAndSortedTalkGroups.slice(startIndex, endIndex)
  
  // Calculate pagination info
  const totalFilteredItems = filteredAndSortedTalkGroups.length
  const totalPages = Math.ceil(totalFilteredItems / pageSize)

  const handleSubscriptionToggle = async (talkGroupNumber: number) => {
    try {
      await toggleSubscription(talkGroupNumber)
    } catch (error) {
      console.error('Failed to toggle subscription:', error)
    }
  }

  if (loading && allTalkGroups.length === 0) {
    return (
      <section className="talkgroups-page">
        <div className="loading-state">
          <div className="loading-spinner"></div>
          <p>Loading talkgroups...</p>
        </div>
        
        <style>{`
          .loading-state {
            text-align: center;
            padding: var(--space-6) var(--space-2);
          }

          .loading-spinner {
            width: 40px;
            height: 40px;
            border: 3px solid var(--border);
            border-top: 3px solid var(--accent-primary);
            border-radius: 50%;
            animation: spin 1s linear infinite;
            margin: 0 auto var(--space-3) auto;
          }

          @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
          }
        `}</style>
      </section>
    )
  }

  if (error) {
    return (
      <section className="talkgroups-page">
        <div className="error-state">
          <div className="error-icon">⚠️</div>
          <h3>Error Loading Talk Groups</h3>
          <p className="text-muted">{error}</p>
          <button 
            className="btn-primary"
            onClick={() => loadAllTalkGroups()}
          >
            Try Again
          </button>
        </div>

        <style>{`
          .error-state {
            text-align: center;
            padding: var(--space-6) var(--space-2);
          }

          .error-icon {
            font-size: 48px;
            margin-bottom: var(--space-2);
          }

          .error-state h3 {
            color: var(--text-primary);
            margin-bottom: var(--space-2);
          }
        `}</style>
      </section>
    )
  }

  return (
    <section className="talkgroups-page">
      <header className="talkgroups-header">
        <h1>Talk Groups</h1>
        <div className="header-stats">
          <div className="stat">
            <span className="stat-value">{allTalkGroups.length.toLocaleString()}</span>
            <span className="stat-label">Total Talk Groups</span>
          </div>
          <div className="stat">
            <span className="stat-value">{subscriptionCount}</span>
            <span className="stat-label">Subscribed</span>
          </div>
          <div className="stat">
            <span className="stat-value">{totalFilteredItems.toLocaleString()}</span>
            <span className="stat-label">
              {searchTerm.trim() || subscriptionFilter !== 'all' ? 'Filtered Results' : 'Showing'}
            </span>
          </div>
        </div>
      </header>

      <div className="filters-section">
        <div className="search-bar">
          <div className="search-input-group">
            <input
              type="text"
              className="search-input"
              placeholder="Search by number, name, tag, or description..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
            <span className="search-icon">🔍</span>
          </div>
        </div>

        <div className="filter-controls">
          <div className="filter-group">
            <label htmlFor="subscription-filter">Subscription:</label>
            <select
              id="subscription-filter"
              className="filter-select"
              value={subscriptionFilter}
              onChange={(e) => setSubscriptionFilter(e.target.value as any)}
            >
              <option value="all">All Talk Groups</option>
              <option value="subscribed">Subscribed Only</option>
              <option value="unsubscribed">Unsubscribed Only</option>
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="sort-by">Sort by:</label>
            <select
              id="sort-by"
              className="filter-select"
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as SortBy)}
            >
              <option value="name">Name</option>
              <option value="number">Number</option>
              <option value="callCount">Call Count</option>
              <option value="lastActivity">Last Activity</option>
              <option value="totalDuration">Total Duration</option>
            </select>
          </div>

          <div className="filter-group">
            <label htmlFor="sort-direction">Order:</label>
            <select
              id="sort-direction"
              className="filter-select"
              value={sortDirection}
              onChange={(e) => setSortDirection(e.target.value as SortDirection)}
            >
              <option value="asc">Ascending</option>
              <option value="desc">Descending</option>
            </select>
          </div>

          <button
            className="clear-filters-btn"
            onClick={() => {
              setSearchTerm('')
              setSubscriptionFilter('all')
              setSortBy('name')
              setSortDirection('asc')
              setCurrentPage(1)
            }}
            title="Clear all filters and reset sorting"
          >
            Reset
          </button>
        </div>
      </div>

      <div className="talkgroups-list">
        {paginatedTalkGroups.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📻</div>
            <h3>No Talk Groups Found</h3>
            <p className="text-muted">
              {searchTerm.trim() || subscriptionFilter !== 'all'
                ? 'Try adjusting your filters to see more results.'
                : 'No talkgroups are available in the system.'
              }
            </p>
          </div>
        ) : (
          <div className="talkgroups-grid">
            {paginatedTalkGroups.map(talkGroup => (
              <TalkGroupCard
                key={talkGroup.id}
                talkGroup={talkGroup}
                isSubscribed={isSubscribed(talkGroup.number || 0)}
                onSubscriptionToggle={() => handleSubscriptionToggle(talkGroup.number || 0)}
              />
            ))}
          </div>
        )}
        
        {loading && (
          <div className="loading-overlay">
            <div className="loading-spinner"></div>
            <p>Loading...</p>
          </div>
        )}
      </div>

      {totalPages > 1 && (
        <div className="pagination">
          <button
            className="pagination-btn"
            disabled={currentPage === 1}
            onClick={() => setCurrentPage(currentPage - 1)}
          >
            ← Previous
          </button>
          
          <div className="pagination-info">
            Page {currentPage} of {totalPages}
            <span className="pagination-note">
              Showing {paginatedTalkGroups.length} of {totalFilteredItems} talkgroups
            </span>
          </div>
          
          <button
            className="pagination-btn"
            disabled={currentPage === totalPages}
            onClick={() => setCurrentPage(currentPage + 1)}
          >
            Next →
          </button>
        </div>
      )}

      <style>{`
        .talkgroups-page {
          min-height: 60vh;
        }

        .talkgroups-header {
          margin-bottom: var(--space-4);
        }

        .talkgroups-header h1 {
          margin-bottom: var(--space-2);
        }

        .header-stats {
          display: flex;
          gap: var(--space-4);
          flex-wrap: wrap;
        }

        .stat {
          display: flex;
          flex-direction: column;
          align-items: center;
          text-align: center;
        }

        .stat-value {
          font-size: var(--font-size-xl);
          font-weight: 700;
          color: var(--accent-primary);
        }

        .stat-label {
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
        }

        .filters-section {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-3);
          margin-bottom: var(--space-4);
        }

        .search-bar {
          margin-bottom: var(--space-3);
        }

        .search-input-group {
          position: relative;
          max-width: 600px;
        }

        .search-input {
          width: 100%;
          padding: var(--space-2) var(--space-4) var(--space-2) var(--space-2);
          background: var(--bg-primary);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          color: var(--text-primary);
          font-size: var(--font-size-base);
          transition: var(--transition);
        }

        .search-input:focus {
          outline: none;
          border-color: var(--accent-primary);
          box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
        }

        .search-icon {
          position: absolute;
          right: var(--space-2);
          top: 50%;
          transform: translateY(-50%);
          color: var(--text-muted);
          pointer-events: none;
        }

        .filter-controls {
          display: flex;
          gap: var(--space-3);
          align-items: end;
          flex-wrap: wrap;
        }

        .filter-group {
          display: flex;
          flex-direction: column;
          gap: var(--space-1);
        }

        .filter-group label {
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
          font-weight: 500;
        }

        .filter-select {
          padding: var(--space-1) var(--space-2);
          background: var(--bg-primary);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          color: var(--text-primary);
          font-size: var(--font-size-sm);
          min-width: 140px;
        }

        .clear-filters-btn {
          background: none;
          border: 1px solid var(--border);
          color: var(--text-secondary);
          padding: var(--space-1) var(--space-2);
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          font-size: var(--font-size-sm);
        }

        .clear-filters-btn:hover {
          background: var(--bg-card-hover);
          color: var(--text-primary);
        }

        .talkgroups-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
          gap: var(--space-3);
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
          margin-bottom: var(--space-2);
        }

        .pagination {
          display: flex;
          justify-content: center;
          align-items: center;
          gap: var(--space-3);
          margin-top: var(--space-4);
          padding: var(--space-3);
        }

        .pagination-btn {
          background: var(--bg-card);
          border: 1px solid var(--border);
          color: var(--text-primary);
          padding: var(--space-2) var(--space-3);
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
        }

        .pagination-btn:hover:not(:disabled) {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .pagination-btn:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .pagination-info {
          color: var(--text-secondary);
          font-size: var(--font-size-sm);
          text-align: center;
        }

        .pagination-note {
          display: block;
          font-size: var(--font-size-xs);
          color: var(--text-muted);
          margin-top: 2px;
        }

        .loading-overlay {
          position: relative;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: var(--space-4);
          color: var(--text-secondary);
        }

        .loading-overlay .loading-spinner {
          width: 32px;
          height: 32px;
          border: 2px solid var(--border);
          border-top: 2px solid var(--accent-primary);
          border-radius: 50%;
          animation: spin 1s linear infinite;
          margin-bottom: var(--space-2);
        }

        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }

        @media (max-width: 767px) {
          .header-stats {
            justify-content: center;
          }

          .filter-controls {
            flex-direction: column;
            align-items: stretch;
          }

          .filter-group {
            width: 100%;
          }

          .filter-select {
            min-width: unset;
          }

          .talkgroups-grid {
            grid-template-columns: 1fr;
          }

          .pagination {
            flex-direction: column;
            gap: var(--space-2);
          }
        }
      `}</style>
    </section>
  )
}

interface TalkGroupCardProps {
  talkGroup: TalkGroupWithStats
  isSubscribed: boolean
  onSubscriptionToggle: () => void
}

function TalkGroupCard({ talkGroup, isSubscribed, onSubscriptionToggle }: TalkGroupCardProps) {
  const talkGroupNumber = talkGroup.number || 0
  const talkGroupId = talkGroup.id || 0
  const displayName = talkGroup.name || talkGroup.alphaTag || `Talk Group ${talkGroupNumber}`
  const mainTitle = talkGroup.description || displayName
  const subtitle = talkGroup.description ? displayName : null
  
  return (
    <article className="talkgroup-card">
      <div className="talkgroup-header">
        <div className="talkgroup-info">
          <h3 className="talkgroup-title">
            <Link 
              to={`/talkgroup/${talkGroupId}`}
              className="talkgroup-link"
            >
              {mainTitle}
            </Link>
          </h3>
          {subtitle && (
            <div className="talkgroup-subtitle">
              {subtitle}
            </div>
          )}
          <div className="talkgroup-number">
            Number: {talkGroupNumber}
          </div>
        </div>
        
        <button
          className={`subscription-btn ${isSubscribed ? 'subscribed' : ''}`}
          onClick={onSubscriptionToggle}
          title={isSubscribed ? 'Unsubscribe from this talkgroup' : 'Subscribe to this talkgroup'}
        >
          {isSubscribed ? '⭐' : '☆'}
        </button>
      </div>

      <div className="talkgroup-meta">
        <div className="meta-row">
          {talkGroup.category && (
            <span className="meta-item">
              <span className="meta-label">Category:</span>
              <span className="meta-value">{talkGroup.category}</span>
            </span>
          )}
          {talkGroup.tag && (
            <span className="meta-item">
              <span className="meta-label">Tag:</span>
              <span className="meta-value">{talkGroup.tag}</span>
            </span>
          )}
        </div>
        
        {talkGroup.priority && (
          <div className="meta-row">
            <span className="meta-item">
              <span className="meta-label">Priority:</span>
              <span className={`priority-badge priority-${talkGroup.priority}`}>
                P{talkGroup.priority}
              </span>
            </span>
          </div>
        )}

        {/* Call Statistics */}
        <div className="stats-section">
          <div className="stats-row">
            <span className="meta-item">
              <span className="meta-label">📞 Calls:</span>
              <span className="meta-value">{talkGroup.callCount?.toLocaleString() || '0'}</span>
            </span>
            {talkGroup.totalDurationSeconds !== undefined && (
              <span className="meta-item">
                <span className="meta-label">⏱️ Duration:</span>
                <span className="meta-value">{formatDuration(talkGroup.totalDurationSeconds)}</span>
              </span>
            )}
          </div>
          {talkGroup.lastActivity && (
            <div className="stats-row">
              <span className="meta-item">
                <span className="meta-label">🕐 Last Activity:</span>
                <span className="meta-value">{formatLastActivity(talkGroup.lastActivity)}</span>
              </span>
            </div>
          )}
        </div>
      </div>

      <div className="talkgroup-actions">
        <Link 
          to={`/talkgroup/${talkGroupId}`}
          className="btn-action"
        >
          📻 View Stream
        </Link>
        <button
          className={`btn-action ${isSubscribed ? 'btn-unsubscribe' : 'btn-subscribe'}`}
          onClick={onSubscriptionToggle}
        >
          {isSubscribed ? '⭐ Unsubscribe' : '☆ Subscribe'}
        </button>
      </div>

      <style>{`
        .talkgroup-card {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-3);
          transition: var(--transition);
        }

        .talkgroup-card:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .talkgroup-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--space-2);
        }

        .talkgroup-info {
          flex: 1;
        }

        .talkgroup-title {
          margin: 0 0 var(--space-1) 0;
          font-size: var(--font-size-lg);
          font-weight: 600;
        }

        .talkgroup-link {
          color: var(--text-primary);
          text-decoration: none;
          transition: var(--transition);
        }

        .talkgroup-link:hover {
          color: var(--accent-primary);
          text-decoration: underline;
        }

        .talkgroup-number {
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
          font-family: var(--font-mono, monospace);
        }

        .talkgroup-subtitle {
          margin: var(--space-1) 0;
          color: var(--text-secondary);
          font-size: var(--font-size-sm);
          font-weight: 500;
        }

        .subscription-btn {
          background: none;
          border: 1px solid var(--border);
          color: var(--text-muted);
          width: 36px;
          height: 36px;
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 16px;
        }

        .subscription-btn:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .subscription-btn.subscribed {
          color: var(--accent-primary);
          border-color: var(--accent-primary);
          background: rgba(59, 130, 246, 0.1);
        }

        .talkgroup-meta {
          margin-bottom: var(--space-3);
        }

        .meta-row {
          display: flex;
          flex-wrap: wrap;
          gap: var(--space-2);
          margin-bottom: var(--space-1);
        }

        .meta-item {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          font-size: var(--font-size-xs);
        }

        .meta-label {
          color: var(--text-muted);
          font-weight: 500;
        }

        .meta-value {
          color: var(--text-secondary);
        }

        .priority-badge {
          background: var(--bg-secondary);
          color: var(--text-primary);
          padding: 2px 6px;
          border-radius: var(--radius-sm);
          font-weight: 600;
          font-size: var(--font-size-xs);
        }

        .priority-1 { background: #ef4444; color: white; }
        .priority-2 { background: #f97316; color: white; }
        .priority-3 { background: #eab308; color: white; }
        .priority-4 { background: #22c55e; color: white; }
        .priority-5 { background: #6b7280; color: white; }

        .stats-section {
          margin-top: var(--space-2);
          padding-top: var(--space-2);
          border-top: 1px solid var(--border);
        }

        .stats-row {
          display: flex;
          flex-wrap: wrap;
          gap: var(--space-2);
          margin-bottom: var(--space-1);
        }

        .stats-section .meta-item {
          background: var(--bg-secondary);
          padding: 2px 6px;
          border-radius: var(--radius-sm);
        }

        .talkgroup-actions {
          display: flex;
          gap: var(--space-2);
        }

        .btn-action {
          flex: 1;
          padding: var(--space-2);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
          text-decoration: none;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: var(--font-size-sm);
          font-weight: 500;
          background: var(--bg-primary);
          color: var(--text-primary);
        }

        .btn-action:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
          text-decoration: none;
        }

        .btn-subscribe {
          border-color: var(--accent-primary);
          color: var(--accent-primary);
        }

        .btn-subscribe:hover {
          background: var(--accent-primary);
          color: white;
        }

        .btn-unsubscribe {
          border-color: #ef4444;
          color: #ef4444;
        }

        .btn-unsubscribe:hover {
          background: #ef4444;
          color: white;
        }
      `}</style>
    </article>
  )
}
