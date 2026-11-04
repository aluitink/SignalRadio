import { useCallback, useEffect, useState } from 'react'
import type { CallDto } from '../types/dtos'
import { getRecentCalls } from '../api'
import { useSignalR } from '../hooks/useSignalR'
import CallCard from '../components/CallCard'

const PAGE_SIZE = 30

export default function CallStreamPage() {
  const [calls, setCalls] = useState<CallDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const data = await getRecentCalls(PAGE_SIZE)
      setCalls(data)
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load calls')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const status = useSignalR(null, {
    onNewCall: () => void load(),
    onNewTranscription: () => void load(),
  })

  return (
    <div>
      <div className="page-header">
        <h1>Live Calls</h1>
        <span className="live">
          <span className={`status-dot ${status}`} aria-hidden="true" />
          {status === 'connected' ? 'Live' : status === 'connecting' ? 'Connecting…' : 'Offline'}
        </span>
      </div>

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
            <button className="btn" onClick={() => void load()}>Retry</button>
          </div>
        </div>
      ) : calls.length === 0 ? (
        <div className="empty-state">No calls yet. Waiting for the simulator…</div>
      ) : (
        <div>
          {calls.map((c) => (
            <CallCard key={c.id} call={c} />
          ))}
        </div>
      )}
    </div>
  )
}
