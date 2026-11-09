import { useCallback, useState } from 'react'
import type { CallDto } from '../types/dtos'
import { useSubscriptions } from '../contexts/SubscriptionsContext'
import CallStream from '../components/CallStream'
import { CallVolumeStackedChart } from '../components/CallVolumeChart'
import InvestigationPromptModal from '../components/InvestigationPromptModal'

export default function CallStreamPage() {
  const { subscribedIds } = useSubscriptions()
  // Talk group ids selected via the chart legend. Empty = show all groups.
  const [selectedGroups, setSelectedGroups] = useState<Set<number>>(new Set())
  // When on, only show calls from subscribed talk groups.
  const [subscribedOnly, setSubscribedOnly] = useState(false)
  // Investigation modal state.
  const [investigateCall, setInvestigateCall] = useState<CallDto | null>(null)

  const toggleGroup = useCallback((id: number) => {
    setSelectedGroups((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }, [])

  const clearGroups = useCallback(() => setSelectedGroups(new Set()), [])

  // Toggle a talk group in the stream filter (from a call card's filter button).
  // Clicking a group that is already the only selected group clears the filter;
  // otherwise the group is added to / removed from the current selection.
  const filterToGroup = useCallback((id: number) => {
    setSelectedGroups((prev) => {
      if (prev.size === 1 && prev.has(id)) return new Set()
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }, [])

  return (
    <div>
      <CallVolumeStackedChart
        selected={selectedGroups}
        onToggleGroup={toggleGroup}
        onClear={clearGroups}
        subscribedOnly={subscribedOnly}
        onSubscribedOnlyChange={setSubscribedOnly}
      />

      <CallStream
        talkGroupIds={selectedGroups.size > 0 ? [...selectedGroups] : undefined}
        subscribedOnly={subscribedOnly}
        subscribedIds={subscribedIds}
        onInvestigate={setInvestigateCall}
        onFilterTalkGroup={filterToGroup}
        emptyMessage={
          subscribedOnly
            ? 'No calls from subscribed talk groups. Subscribe to a talk group to see it here.'
            : selectedGroups.size > 0
              ? 'No calls for the selected talk groups.'
              : 'No calls yet. Waiting for the simulator…'
        }
      />

      {investigateCall && (
        <InvestigationPromptModal
          call={investigateCall}
          onClose={() => setInvestigateCall(null)}
          onCreated={() => { /* investigation queued, ticker will show it */ }}
        />
      )}
    </div>
  )
}
