import { useEffect, useMemo, useState } from 'react'
import type { CallVolumeByTalkGroupDto, CallVolumeDto } from '../types/dtos'
import { getCallVolume, getCallVolumeByTalkGroup } from '../api'
import { formatDuration } from '../utils/format'

// Time-span options for the dropdown. `minutes` drives the query window.
const SPANS = [
  { label: '1 hour', minutes: 60 },
  { label: '3 hours', minutes: 180 },
  { label: '6 hours', minutes: 360 },
  { label: '12 hours', minutes: 720 },
  { label: '24 hours', minutes: 1440 },
  { label: '3 days', minutes: 4320 },
  { label: '7 days', minutes: 10080 },
  { label: '14 days', minutes: 20160 },
  { label: '30 days', minutes: 43200 },
] as const

// Candidate bucket sizes (minutes) used to auto-pick a resolution that yields
// roughly TARGET_BUCKETS bars for the selected window.
const BUCKET_CANDIDATES = [
  1, 5, 10, 15, 30, 60, 120, 180, 240, 360, 720, 1440, 4320, 10080, 20160, 43200,
]
const TARGET_BUCKETS = 48

function pickBucketMinutes(windowMinutes: number): number {
  const ideal = windowMinutes / TARGET_BUCKETS
  return BUCKET_CANDIDATES.reduce((best, c) =>
    Math.abs(c - ideal) < Math.abs(best - ideal) ? c : best,
  )
}

/**
 * Deterministic per-talkgroup color, derived from the talk group id.
 *
 * Uses a small FNV-1a hash of the id to pick a hue, then renders it in HSL
 * with fixed saturation/lightness tuned for the dark theme. The same id
 * always yields the same color (across reloads and pages), so call cards,
 * chart segments, and legends stay in sync without any shared state.
 */
export function talkGroupColor(talkGroupId: number): string {
  // FNV-1a 32-bit hash — cheap, well-distributed for small integer inputs.
  let h = 0x811c9dc5
  for (let i = 0; i < 4; i++) {
    h ^= (talkGroupId >>> (i * 8)) & 0xff
    h = Math.imul(h, 0x01000193) >>> 0
  }
  const hue = h % 360
  return `hsl(${hue}, 70%, 62%)`
}

interface CallVolumeChartProps {
  /** When set, only this talk group's volume is shown (single color). */
  talkGroupId?: number
  /** Optional title override. */
  title?: string
  /** Initial window in minutes (must match one of SPANS). */
  initialMinutes?: number
  /** Bump to force a refresh (e.g. when a new call arrives via SignalR). */
  refreshKey?: number
  /** Bar color (defaults to the accent blue). */
  barColor?: string
}

export default function CallVolumeChart({
  talkGroupId,
  title,
  initialMinutes = 60,
  refreshKey = 0,
  barColor,
}: CallVolumeChartProps) {
  const [minutes, setMinutes] = useState<number>(initialMinutes)
  const [data, setData] = useState<CallVolumeDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [hover, setHover] = useState<number | null>(null)

  const bucketMinutes = useMemo(() => pickBucketMinutes(minutes), [minutes])

  const load = (m: number, bucket: number) => {
    setError(null)
    const from = new Date(Date.now() - m * 60_000).toISOString()
    getCallVolume(from, undefined, talkGroupId, bucket)
      .then((d) => setData(d))
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load call volume'))
  }

  useEffect(() => {
    load(minutes, bucketMinutes)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [minutes, talkGroupId, refreshKey])

  const { bars, maxCount, labels } = useMemo(() => {
    if (!data || data.length === 0) return { bars: [] as number[], maxCount: 0, labels: [] as string[] }
    const counts = data.map((d) => d.callCount)
    const max = Math.max(1, ...counts)
    const fmt = (iso: string) => {
      const d = new Date(iso)
      if (bucketMinutes < 1440) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      return d.toLocaleDateString([], { month: 'short', day: 'numeric' })
    }
    // Show a label every ~6th bucket to avoid clutter.
    const step = Math.max(1, Math.ceil(data.length / 6))
    const lbls = data.map((d, i) => (i % step === 0 ? fmt(d.bucketStart) : ''))
    return { bars: counts, maxCount: max, labels: lbls }
  }, [data, bucketMinutes])

  const totalCalls = useMemo(() => (data ? data.reduce((s, d) => s + d.callCount, 0) : 0), [data])
  const totalDuration = useMemo(
    () => (data ? data.reduce((s, d) => s + d.totalDurationSeconds, 0) : 0),
    [data],
  )

  const tooltip =
    hover !== null && data && data[hover]
      ? {
          time: new Date(data[hover].bucketStart).toLocaleString([], {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
          }),
          count: data[hover].callCount,
          duration: formatDuration(data[hover].totalDurationSeconds),
        }
      : null

  return (
    <section className="card volume-chart" aria-label={title ?? 'Call volume'}>
      <div className="volume-chart-header">
        <h3>{title ?? 'Call volume'}</h3>
        <div className="volume-chart-controls">
          <select
            className="volume-resolution"
            aria-label="Time window"
            value={minutes}
            onChange={(e) => setMinutes(Number(e.target.value))}
          >
            {SPANS.map((s) => (
              <option key={s.minutes} value={s.minutes}>
                {s.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {error ? (
        <div className="error-state" role="alert">{error}</div>
      ) : !data ? (
        <div className="skeleton skeleton-chart" aria-busy="true" />
      ) : (
        <>
          <div className="volume-chart-plot">
            <div
              className="volume-chart-bars"
              role="img"
              aria-label={`Bar chart of call volume: ${totalCalls} calls in the last ${minutes / 60} hours`}
              onMouseLeave={() => setHover(null)}
            >
              {bars.map((count, i) => {
                const h = maxCount > 0 ? (count / maxCount) * 100 : 0
                return (
                  <div
                    key={i}
                    className={`volume-bar${hover === i ? ' hovered' : ''}${count === 0 ? ' empty' : ''}`}
                    style={{
                      height: `${Math.max(h, count > 0 ? 4 : 2)}%`,
                      ...(barColor ? { background: barColor } : {}),
                    }}
                    onMouseEnter={() => setHover(i)}
                  />
                )
              })}
            </div>
            {tooltip && (
              <div
                className="volume-tooltip"
                style={{ left: `${((hover! + 0.5) / bars.length) * 100}%` }}
                role="status"
              >
                <span className="volume-tooltip-time">{tooltip.time}</span>
                <span>
                  {tooltip.count} call{tooltip.count === 1 ? '' : 's'} · {tooltip.duration}
                </span>
              </div>
            )}
          </div>
          <div className="volume-chart-axis" aria-hidden="true">
            {labels.map((l, i) => (
              <span key={i}>{l}</span>
            ))}
          </div>
          <div className="volume-chart-footer">
            <span>
              {totalCalls} calls · {formatDuration(totalDuration)} talk time
            </span>
          </div>
        </>
      )}
    </section>
  )
}

/**
 * Stacked bar chart of call volume across all talk groups, color-coded per group.
 */
export function CallVolumeStackedChart({
  title,
  initialMinutes = 60,
  refreshKey = 0,
  selected,
  onToggleGroup,
  onClear,
}: {
  title?: string
  initialMinutes?: number
  refreshKey?: number
  /** Talk group ids selected via the legend. Empty = show all groups. */
  selected?: Set<number>
  onToggleGroup?: (id: number) => void
  onClear?: () => void
}) {
  const [minutes, setMinutes] = useState<number>(initialMinutes)
  const [data, setData] = useState<CallVolumeByTalkGroupDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [hover, setHover] = useState<number | null>(null)
  // Internal selection used when the filter is not controlled by the parent.
  const [internalSelected, setInternalSelected] = useState<Set<number>>(new Set())

  const isControlled = selected !== undefined
  const activeSelected = isControlled ? selected! : internalSelected

  const toggleGroup = (id: number) => {
    if (isControlled) {
      onToggleGroup?.(id)
      return
    }
    setInternalSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const clearSelection = () => {
    if (isControlled) {
      onClear?.()
      return
    }
    setInternalSelected(new Set())
  }

  const bucketMinutes = useMemo(() => pickBucketMinutes(minutes), [minutes])

  const load = (m: number, bucket: number) => {
    setError(null)
    const from = new Date(Date.now() - m * 60_000).toISOString()
    getCallVolumeByTalkGroup(from, undefined, bucket)
      .then((d) => setData(d))
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load call volume'))
  }

  useEffect(() => {
    load(minutes, bucketMinutes)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [minutes, refreshKey])

  // All talk groups present in the data, by total call count (desc).
  // Used by the legend so every option stays visible while multi-selecting.
  const allGroups = useMemo(() => {
    if (!data) return [] as { id: number; number: string; name: string | null }[]
    const totals = new Map<number, { number: string; name: string | null; count: number }>()
    for (const bucket of data) {
      for (const tg of bucket.talkGroups) {
        const cur = totals.get(tg.talkGroupId) ?? { number: tg.talkGroupNumber, name: tg.talkGroupName ?? null, count: 0 }
        cur.count += tg.callCount
        totals.set(tg.talkGroupId, cur)
      }
    }
    return [...totals.entries()]
      .sort((a, b) => b[1].count - a[1].count)
      .map(([id, v]) => ({ id, number: v.number, name: v.name }))
  }, [data])

  // Groups actually rendered in the chart: the legend filter applied.
  const groupOrder = useMemo(
    () => allGroups.filter((g) => activeSelected.size === 0 || activeSelected.has(g.id)),
    [allGroups, activeSelected],
  )

  const { totals, maxTotal, labels } = useMemo(() => {
    if (!data || data.length === 0) return { totals: [] as number[], maxTotal: 0, labels: [] as string[] }
    const totalsArr = data.map((b) =>
      b.talkGroups.reduce((s, tg) => (activeSelected.size === 0 || activeSelected.has(tg.talkGroupId) ? s + tg.callCount : s), 0),
    )
    const max = Math.max(1, ...totalsArr)
    const fmt = (iso: string) => {
      const d = new Date(iso)
      if (bucketMinutes < 1440) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      return d.toLocaleDateString([], { month: 'short', day: 'numeric' })
    }
    const step = Math.max(1, Math.ceil(data.length / 6))
    const lbls = data.map((d, i) => (i % step === 0 ? fmt(d.bucketStart) : ''))
    return { totals: totalsArr, maxTotal: max, labels: lbls }
  }, [data, bucketMinutes, activeSelected])

  const totalCalls = useMemo(() => totals.reduce((s, n) => s + n, 0), [totals])

  const tooltip =
    hover !== null && data && data[hover]
      ? {
          time: new Date(data[hover].bucketStart).toLocaleString([], {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
          }),
          total: totals[hover] ?? 0,
          groups: [...data[hover].talkGroups]
            .filter((tg) => activeSelected.size === 0 || activeSelected.has(tg.talkGroupId))
            .sort((a, b) => b.callCount - a.callCount),
        }
      : null

  return (
    <section className="card volume-chart" aria-label={title ?? 'Call volume'}>
      <div className="volume-chart-header">
        <h3>{title ?? 'Call volume'}</h3>
        <div className="volume-chart-controls">
          <select
            className="volume-resolution"
            aria-label="Time window"
            value={minutes}
            onChange={(e) => setMinutes(Number(e.target.value))}
          >
            {SPANS.map((s) => (
              <option key={s.minutes} value={s.minutes}>
                {s.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {error ? (
        <div className="error-state" role="alert">{error}</div>
      ) : !data ? (
        <div className="skeleton skeleton-chart" aria-busy="true" />
      ) : (
        <>
          <div className="volume-chart-plot">
            <div
              className="volume-chart-bars"
              role="img"
              aria-label={`Stacked bar chart of call volume by talk group: ${totalCalls} calls in the last ${minutes / 60} hours`}
              onMouseLeave={() => setHover(null)}
            >
              {data.map((bucket, i) => {
                const total = totals[i]
                return (
                  <div
                    key={i}
                    className={`volume-bar-stack${hover === i ? ' hovered' : ''}${total === 0 ? ' empty' : ''}`}
                    onMouseEnter={() => setHover(i)}
                  >
                    {total > 0 &&
                      groupOrder.map((g) => {
                        const tg = bucket.talkGroups.find((t) => t.talkGroupId === g.id)
                        if (!tg || tg.callCount === 0) return null
                        const h = (tg.callCount / maxTotal) * 100
                        return (
                          <div
                            key={g.id}
                            className="volume-bar-segment"
                            style={{
                              height: `${h}%`,
                              background: talkGroupColor(g.id),
                            }}
                          />
                        )
                      })}
                  </div>
                )
              })}
            </div>
            {tooltip && (
              <div
                className="volume-tooltip"
                style={{ left: `${((hover! + 0.5) / data.length) * 100}%` }}
                role="status"
              >
                <span className="volume-tooltip-time">{tooltip.time}</span>
                <span>
                  {tooltip.total} call{tooltip.total === 1 ? '' : 's'}
                </span>
                {tooltip.groups.slice(0, 5).map((tg) => (
                  <span key={tg.talkGroupId} className="volume-tooltip-row">
                    <span
                      className="volume-tooltip-swatch"
                      style={{ background: talkGroupColor(tg.talkGroupId) }}
                      aria-hidden="true"
                    />
                    TG {tg.talkGroupNumber}: {tg.callCount}
                  </span>
                ))}
                {tooltip.groups.length > 5 && (
                  <span className="volume-tooltip-more">
                    +{tooltip.groups.length - 5} more
                  </span>
                )}
              </div>
            )}
          </div>
          <div className="volume-chart-axis" aria-hidden="true">
            {labels.map((l, i) => (
              <span key={i}>{l}</span>
            ))}
          </div>
          {allGroups.length > 0 && (
            <div className="volume-legend-wrap">
              <div className="volume-legend" aria-label="Talk group colors — click to filter">
                {allGroups.map((g) => {
                  const isSel = activeSelected.has(g.id)
                  const isDim = activeSelected.size > 0 && !isSel
                  return (
                    <button
                      key={g.id}
                      type="button"
                      className={`volume-legend-item${isSel ? ' selected' : ''}${isDim ? ' dimmed' : ''}`}
                      onClick={() => toggleGroup(g.id)}
                      aria-pressed={isSel}
                      title={isSel ? 'Click to show all groups' : 'Click to filter to this group'}
                    >
                      <span
                        className="volume-legend-swatch"
                        style={{ background: talkGroupColor(g.id) }}
                        aria-hidden="true"
                      />
                      {g.name ?? `TG ${g.number}`}
                    </button>
                  )
                })}
              </div>
              {activeSelected.size > 0 && (
                <button
                  type="button"
                  className="volume-legend-clear"
                  onClick={clearSelection}
                >
                  Clear ({activeSelected.size})
                </button>
              )}
            </div>
          )}
          <div className="volume-chart-footer">
            <span>
              {totalCalls} calls · {groupOrder.length} talk group{groupOrder.length === 1 ? '' : 's'}
              {activeSelected.size > 0 ? ' · filtered' : ''}
            </span>
          </div>
        </>
      )}
    </section>
  )
}
