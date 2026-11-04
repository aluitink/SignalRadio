import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { SearchResultsDto } from '../types/dtos'
import { search } from '../api'
import { formatDateTime } from '../utils/format'

const PAGE_SIZE = 20

export default function SearchPage() {
  const [q, setQ] = useState('')
  const [semantic, setSemantic] = useState(false)
  const [results, setResults] = useState<SearchResultsDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [searched, setSearched] = useState(false)

  const runSearch = async (query: string, p: number) => {
    if (!query.trim()) return
    setLoading(true)
    setError(null)
    try {
      const data = await search(query, p, PAGE_SIZE, semantic)
      setResults(data)
      setSearched(true)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Search failed')
    } finally {
      setLoading(false)
    }
  }

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    void runSearch(q, 1)
  }

  return (
    <div>
      <div className="page-header">
        <h1>Search</h1>
      </div>

      <form className="search-bar" onSubmit={onSubmit} role="search">
        <input
          className="input"
          type="search"
          placeholder="Search transcripts…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          aria-label="Search query"
        />
        <label className="semantic-toggle">
          <input
            type="checkbox"
            checked={semantic}
            onChange={(e) => setSemantic(e.target.checked)}
          />
          Semantic
        </label>
        <button className="btn primary" type="submit" disabled={loading || !q.trim()}>
          {loading ? 'Searching…' : 'Search'}
        </button>
      </form>

      {error && <div className="error-state" role="alert">{error}</div>}

      {loading && (
        <div aria-busy="true">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="skeleton skeleton-card" />
          ))}
        </div>
      )}

      {!loading && searched && results && (
        <>
          <p className="result-count" aria-live="polite">
            {results.totalCount} result{results.totalCount === 1 ? '' : 's'}
          </p>

          {results.transcriptions.length > 0 && (
            <section className="result-section">
              <h2>Transcripts</h2>
              {results.transcriptions.map((t) => (
                <div key={t.transcriptionId} className="card result-item">
                  <div className="result-meta">
                    <Link to={`/talkgroup/${t.talkGroupId}`} className="tg-link">
                      TG {t.talkGroupNumber}
                    </Link>
                    <span className="sub">{formatDateTime(t.recordingTime)}</span>
                    {t.service && <span className="badge">{t.service}</span>}
                  </div>
                  <p className="transcript">{t.fullText}</p>
                </div>
              ))}
            </section>
          )}

          {results.summaries.length > 0 && (
            <section className="result-section">
              <h2>Summaries</h2>
              {results.summaries.map((s) => (
                <div key={s.id} className="card result-item">
                  <div className="result-meta">
                    <Link to={`/talkgroup/${s.talkGroupId}`} className="tg-link">
                      TG {s.talkGroupNumber}
                    </Link>
                    <span className="sub">
                      {formatDateTime(s.startTime)} – {formatDateTime(s.endTime)}
                    </span>
                  </div>
                  <p className="transcript">{s.summary}</p>
                </div>
              ))}
            </section>
          )}

          {results.incidents.length > 0 && (
            <section className="result-section">
              <h2>Notable Incidents</h2>
              {results.incidents.map((n) => (
                <div key={n.id} className="card result-item">
                  <p className="transcript">{n.description}</p>
                </div>
              ))}
            </section>
          )}

          {results.totalCount === 0 && (
            <div className="empty-state">No results found.</div>
          )}

          {results.totalCount > results.page * results.pageSize && (
            <div className="pagination">
              <button
                className="btn ghost"
                disabled={results.page <= 1}
                onClick={() => void runSearch(q, results.page - 1)}
              >
                ← Prev
              </button>
              <span className="page-info">Page {results.page}</span>
              <button
                className="btn ghost"
                onClick={() => void runSearch(q, results.page + 1)}
              >
                Next →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
