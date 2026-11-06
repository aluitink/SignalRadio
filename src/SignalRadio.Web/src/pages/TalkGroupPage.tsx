import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import type { TalkGroupDto } from '../types/dtos'
import { getTalkGroup } from '../api'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import CallStream from '../components/CallStream'
import CallVolumeChart, { talkGroupColor } from '../components/CallVolumeChart'

export default function TalkGroupPage() {
  const { id } = useParams<{ id: string }>()
  const talkGroupId = id ? Number(id) : NaN

  const [group, setGroup] = useState<TalkGroupDto | null>(null)
  const { isSubscribed, toggleSubscription } = useSubscriptions()

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
        barColor={talkGroupColor(talkGroupId)}
      />

      <CallStream
        talkGroupId={talkGroupId}
        emptyMessage="No calls for this talk group yet."
      />
    </div>
  )
}
