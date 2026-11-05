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

// Distinct, dark-theme-friendly palette for per-talkgroup color coding.
const PALETTE = [
  '#4d9fff', // blue
  '#3ddc84', // green
  '#ffb454', // amber
  '#c792ea', // purple
  '#ff6b9d', // pink
  '#4dd0e1', // cyan
  '#ffd54f', // yellow
  '#a1887f', // brown
]

export function talkGroupColor(index: number): string {
  return PALETTE[index % PALETTE.length]
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
  initialMinutes = 1440,
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
  initialMinutes = 1440,
  refreshKey = 0,
}: {
  title?: string
  initialMinutes?: number
  refreshKey?: number
}) {
  const [minutes, setMinutes] = useState<number>(initialMinutes)
  const [data, setData] = useState<CallVolumeByTalkGroupDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [hover, setHover] = useState<number | null>(null)

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

  // Stable talk-group ordering: by total call count (desc) so the most active
  // groups get the first (most visible) palette colors.
  const groupOrder = useMemo(() => {
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

  const { totals, maxTotal, labels } = useMemo(() => {
    if (!data || data.length === 0) return { totals: [] as number[], maxTotal: 0, labels: [] as string[] }
    const totalsArr = data.map((b) => b.talkGroups.reduce((s, tg) => s + tg.callCount, 0))
    const max = Math.max(1, ...totalsArr)
    const fmt = (iso: string) => {
      const d = new Date(iso)
      if (bucketMinutes < 1440) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      return d.toLocaleDateString([], { month: 'short', day: 'numeric' })
    }
    const step = Math.max(1, Math.ceil(data.length / 6))
    const lbls = data.map((d, i) => (i % step === 0 ? fmt(d.bucketStart) : ''))
    return { totals: totalsArr, maxTotal: max, labels: lbls }
  }, [data, bucketMinutes])

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
          total: data[hover].talkGroups.reduce((s, tg) => s + tg.callCount, 0),
          groups: [...data[hover].talkGroups].sort((a, b) => b.callCount - a.callCount),
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
                      groupOrder.map((g, gi) => {
                        const tg = bucket.talkGroups.find((t) => t.talkGroupId === g.id)
                        if (!tg || tg.callCount === 0) return null
                        // Minimum 3% height so even small buckets stay visible.
                        const h = Math.max((tg.callCount / maxTotal) * 100, 3)
                        return (
                          <div
                            key={g.id}
                            className="volume-bar-segment"
                            style={{
                              height: `${h}%`,
                              background: talkGroupColor(gi),
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
                {tooltip.groups.slice(0, 5).map((tg) => {
                  const gi = groupOrder.findIndex((g) => g.id === tg.talkGroupId)
                  return (
                    <span key={tg.talkGroupId} className="volume-tooltip-row">
                      <span
                        className="volume-tooltip-swatch"
                        style={{ background: talkGroupColor(gi) }}
                        aria-hidden="true"
                      />
                      TG {tg.talkGroupNumber}: {tg.callCount}
                    </span>
                  )
                })}
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
          {groupOrder.length > 0 && (
            <div className="volume-legend" aria-label="Talk group colors">
              {groupOrder.map((g, gi) => (
                <span key={g.id} className="volume-legend-item">
                  <span
                    className="volume-legend-swatch"
                    style={{ background: talkGroupColor(gi) }}
                    aria-hidden="true"
                  />
                  TG {g.number}
                  {g.name ? ` · ${g.name}` : ''}
                </span>
              ))}
            </div>
          )}
          <div className="volume-chart-footer">
            <span>{totalCalls} calls · {groupOrder.length} talk groups</span>
          </div>
        </>
      )}
    </section>
  )
}
