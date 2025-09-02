import React, { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import CallCard from '../components/CallCard'
import AutoplayBanner from '../components/AutoplayBanner'
import LoadingSpinner, { CallCardSkeleton } from '../components/LoadingSpinner'
import Pagination from '../components/Pagination'
import type { CallDto, PagedResult } from '../types/dtos'
import { useAudioManager } from '../hooks/useAudioManager'
import { useSubscriptions } from '../hooks/useSubscriptions'
import { usePageTitle } from '../hooks/usePageTitle'
import { apiGet } from '../api'

export default function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [query, setQuery] = useState(searchParams.get('q') || '')
  const [calls, setCalls] = useState<CallDto[]>([])
  const [loading, setLoading] = useState(false)
  const [hasSearched, setHasSearched] = useState(false)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const pageSize = 20
  
  const { playCall, isCallPlaying } = useAudioManager()
  const { toggle: toggleSubscription, isSubscribed } = useSubscriptions()

  // Update page title based on search
  usePageTitle(
    query ? `Search: ${query}` : 'Search',
    query ? `Search "${query}"` : 'Search'
  )

  // Load search results when query param changes
  useEffect(() => {
    const q = searchParams.get('q')
    if (q && q !== query) {
      setQuery(q)
      setCurrentPage(1) // Reset to first page for new search
      performSearch(q, 1)
    }
  }, [searchParams])

  // Load search results when page changes
  useEffect(() => {
    const q = searchParams.get('q')
    if (q && hasSearched && currentPage > 1) {
      performSearch(q, currentPage)
    }
  }, [currentPage])

  const performSearch = async (searchQuery: string, page: number = 1) => {
    if (!searchQuery.trim()) return

    setLoading(true)
    setHasSearched(true)

    try {
      // Use the transcriptions search endpoint with pagination
      const response = await apiGet<PagedResult<CallDto>>(`/transcriptions/search?q=${encodeURIComponent(searchQuery)}&page=${page}&pageSize=${pageSize}`)
      
      setCalls(response.items || [])
      setTotalPages(response.totalPages || 1)
      setTotalItems(response.totalCount || 0)
    } catch (error) {
      console.error('Search failed:', error)
      setCalls([])
      setTotalPages(1)
      setTotalItems(0)
    } finally {
      setLoading(false)
    }
  }

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    if (!query.trim()) return

    setCurrentPage(1) // Reset to first page for new search
    setSearchParams({ q: query.trim() })
    performSearch(query.trim(), 1)
  }

  const handlePlayStateChange = (callId: number, isPlaying: boolean) => {
    console.log(`Call ${callId} play state changed: ${isPlaying}`)
  }

  return (
    <section className="search-page">
      <header className="search-header">
        <h1>Search Calls</h1>
        <p className="text-secondary">Search through call transcriptions</p>
      </header>

      <AutoplayBanner />

      <form onSubmit={handleSearch} className="search-form">
        <div className="search-input-group">
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search transcriptions..."
            className="search-input"
            autoFocus
          />
          <button type="submit" className="search-button" disabled={loading}>
            {loading ? '⏳' : '🔍'}
          </button>
        </div>
      </form>

      {loading && (
        <div className="loading-skeleton">
          {Array.from({ length: 3 }).map((_, i) => (
            <CallCardSkeleton key={i} />
          ))}
        </div>
      )}

      {!loading && hasSearched && calls.length === 0 && (
        <div className="empty-state">
          <div className="empty-icon">🔍</div>
          <h3>No results found</h3>
          <p className="text-muted">
            {query ? `No calls found matching "${query}"` : 'Try searching for something'}
          </p>
        </div>
      )}

      {!loading && !hasSearched && (
        <div className="empty-state">
          <div className="empty-icon">📝</div>
          <h3>Search Call Transcriptions</h3>
          <p className="text-muted">Enter keywords to search through call transcriptions</p>
        </div>
      )}

      {!loading && calls.length > 0 && (
        <div className="search-results">
          <p className="results-count text-secondary">
            Found {totalItems.toLocaleString()} result{totalItems !== 1 ? 's' : ''} for "{query}"
          </p>
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
        .search-page {
          min-height: 60vh;
        }

        .search-header {
          margin-bottom: var(--space-4);
        }

        .search-header h1 {
          margin-bottom: var(--space-1);
        }

        .search-form {
          margin-bottom: var(--space-4);
        }

        .search-input-group {
          display: flex;
          gap: var(--space-1);
          max-width: 600px;
        }

        .search-input {
          flex: 1;
          padding: var(--space-2);
          background: var(--bg-card);
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

        .search-input::placeholder {
          color: var(--text-muted);
        }

        .search-button {
          padding: var(--space-2);
          background: var(--accent-primary);
          border: 1px solid var(--accent-primary);
          border-radius: var(--radius);
          color: white;
          cursor: pointer;
          transition: var(--transition);
          min-width: 48px;
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .search-button:hover:not(:disabled) {
          background: var(--accent-secondary);
          border-color: var(--accent-secondary);
        }

        .search-button:disabled {
          opacity: 0.7;
          cursor: not-allowed;
        }

        .loading-skeleton {
          display: flex;
          flex-direction: column;
          gap: var(--space-2);
        }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
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

        .search-results {
          margin-top: var(--space-4);
        }

        .results-count {
          margin-bottom: var(--space-3);
          font-size: var(--font-size-sm);
        }

        .calls-list {
          display: flex;
          flex-direction: column;
        }

        @media (max-width: 767px) {
          .search-input-group {
            flex-direction: column;
          }
          
          .search-button {
            align-self: flex-start;
          }
        }
      `}</style>
    </section>
  )
}
