import { useCallback, useState } from 'react'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import CallStream from '../components/CallStream'
import { CallVolumeStackedChart } from '../components/CallVolumeChart'

export default function CallStreamPage() {
  const { subscribedIds } = useSubscriptions()
  // Talk group ids selected via the chart legend. Empty = show all groups.
  const [selectedGroups, setSelectedGroups] = useState<Set<number>>(new Set())
  // When on, only show calls from subscribed talk groups.
  const [subscribedOnly, setSubscribedOnly] = useState(false)

  const toggleGroup = useCallback((id: number) => {
    setSelectedGroups((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }, [])

  const clearGroups = useCallback(() => setSelectedGroups(new Set()), [])

  return (
    <div>
      <CallVolumeStackedChart
        title="Call volume · all talk groups"
        selected={selectedGroups}
        onToggleGroup={toggleGroup}
        onClear={clearGroups}
      />

      <div className="stream-toolbar">
        <label className="toggle-switch">
          <input
            type="checkbox"
            checked={subscribedOnly}
            onChange={(e) => setSubscribedOnly(e.target.checked)}
            aria-label="Show subscribed talk groups only"
          />
          <span className="toggle-track" aria-hidden="true">
            <span className="toggle-thumb" />
          </span>
          <span className="toggle-label">Subscribed only</span>
        </label>
      </div>

      <CallStream
        talkGroupIds={selectedGroups.size > 0 ? [...selectedGroups] : undefined}
        subscribedOnly={subscribedOnly}
        subscribedIds={subscribedIds}
        emptyMessage={
          subscribedOnly
            ? 'No calls from subscribed talk groups. Subscribe to a talk group to see it here.'
            : selectedGroups.size > 0
              ? 'No calls for the selected talk groups.'
              : 'No calls yet. Waiting for the simulator…'
        }
      />
    </div>
  )
}
