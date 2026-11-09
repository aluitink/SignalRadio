import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { TalkGroupDto } from '../types/dtos'
import { getTalkGroups } from '../api'
import { CallVolumeStackedChart } from '../components/CallVolumeChart'

export default function TalkGroupsPage() {
  const [groups, setGroups] = useState<TalkGroupDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    getTalkGroups()
      .then((data) => {
        if (active) {
          setGroups(data)
          setError(null)
        }
      })
      .catch((e) => {
        if (active) setError(e instanceof Error ? e.message : 'Failed to load talk groups')
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => {
      active = false
    }
  }, [])

  return (
    <div>
      <div className="page-header">
        <h1>Talk Groups</h1>
      </div>

      <CallVolumeStackedChart />

      {loading ? (
        <div aria-busy="true">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="skeleton skeleton-card" />
          ))}
        </div>
      ) : error ? (
        <div className="error-state" role="alert">{error}</div>
      ) : groups.length === 0 ? (
        <div className="empty-state">No talk groups configured.</div>
      ) : (
        <div className="tg-list">
          {groups.map((g) => (
            <Link key={g.id} to={`/talkgroup/${g.id}`} className="tg-item">
              <span className="tg-name">{g.name ?? g.alphaTag ?? '—'}</span>
              {g.category && <span className="badge">{g.category}</span>}
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
