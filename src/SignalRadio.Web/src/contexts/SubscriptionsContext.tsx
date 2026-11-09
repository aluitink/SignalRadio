import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import type { CallDto, TranscriptSummaryDto } from '../types/dtos'
import { getCall, getCallsCursor, recordingDownloadUrlByCall } from '../api'
import { useAudioPlayer } from './AudioPlayerContext'
import { useSignalRBus } from './SignalRBusContext'
import { formatDateTime } from '../utils/format'

/**
 * Talk group subscriptions with auto-play, plus the shared playback queue.
 *
 * - Users subscribe to talk groups (persisted in localStorage).
 * - A single global SignalR connection (owned here) receives NewCall for all
 *   clients. Calls on subscribed talk groups are enqueued.
 * - The queue is played back through the shared audio player, one call at a
 *   time, in chronological order. When multiple subscribed groups are busy at
 *   once, calls queue up and play in order — nothing is dropped.
 *
 * Manual playback feeds the same queue: clicking a call card starts it and
 * keeps playing the consecutive calls above it in the current (filtered)
 * stream until the top is reached. Calls arriving while a session is active
 * are appended and play after the session's calls finish.
 *
 * Autoplay note: browsers block audio until the user interacts with the page.
 * The first manual play (any call card / player control) unlocks audio; queued
 * calls then play automatically.
 */

const STORAGE_KEY = 'signalradio.subscriptions'

interface SubscriptionsValue {
  /** Talk group ids the user has subscribed to. */
  subscribedIds: number[]
  isSubscribed: (talkGroupId: number) => boolean
  toggleSubscription: (talkGroupId: number) => void
  /** Calls waiting in the playback queue (oldest first). */
  queue: CallDto[]
  /** The call currently being played from the queue, if any. */
  current: CallDto | null
  /**
   * Start a playback session from a call: play it, then keep playing the
   * consecutive calls in `stream` (newest first) that are newer than it,
   * until the top of the stream is reached. `stream` is the currently
   * visible (filtered) list, so only filtered calls are played.
   */
  playFromCall: (call: CallDto, stream: CallDto[]) => void
  /**
   * Start a playback session from a summary: fetch the calls belonging to
   * the summary's talk group within its time window, play them in
   * chronological order, then continue with newer calls on that talk group
   * (like "start listening from here").
   */
  playSummarySession: (summary: TranscriptSummaryDto) => Promise<void>
  /**
   * Update the stream reference used by an active playback session. Called by
   * the stream component whenever new calls arrive, so the session's
   * re-evaluation picks up calls that arrived after the initial click.
   */
  updateSessionStream: (stream: CallDto[]) => void
  /** Remove a queued call (e.g. user clicked it and it's now playing manually). */
  removeFromQueue: (callId: number) => void
  /** Clear the whole queue. */
  clearQueue: () => void
}

const SubscriptionsContext = createContext<SubscriptionsValue | null>(null)

function loadSubscriptions(): number[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw)
    return Array.isArray(parsed) ? parsed.filter((n) => typeof n === 'number') : []
  } catch {
    return []
  }
}

export function SubscriptionsProvider({ children }: { children: ReactNode }) {
  const { playFromCall, nowPlaying, isPlaying, isPaused } = useAudioPlayer()
  const [subscribedIds, setSubscribedIds] = useState<number[]>(loadSubscriptions)
  const [queue, setQueue] = useState<CallDto[]>([])
  const [current, setCurrent] = useState<CallDto | null>(null)

  // Refs so the global SignalR handler (registered once) always sees fresh state.
  const subscribedRef = useRef(subscribedIds)
  subscribedRef.current = subscribedIds
  const queueRef = useRef(queue)
  queueRef.current = queue
  const currentRef = useRef(current)
  currentRef.current = current
  const playingRef = useRef(false)
  playingRef.current = isPlaying && nowPlaying != null

  // Bumped whenever the user starts a manual playback session. The resume
  // effect uses this to distinguish "user paused mid-session" (resume) from
  // "session finished" (stay stopped).
  const sessionStartRef = useRef(0)
  const sessionResumeRef = useRef(0)
  // Set while a manual session is actively playing (cleared when the session's
  // queue drains). While set, the resume effect must not steal queued calls —
  // otherwise it would start playing the next queued call *while* the clicked
  // call is still playing, skipping it.
  const sessionActiveRef = useRef(false)

  // Persist subscriptions.
  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(subscribedIds))
    } catch {
      /* ignore quota errors */
    }
  }, [subscribedIds])

  const toggleSubscription = useCallback((talkGroupId: number) => {
    setSubscribedIds((prev) =>
      prev.includes(talkGroupId) ? prev.filter((id) => id !== talkGroupId) : [...prev, talkGroupId],
    )
  }, [])

  const isSubscribed = useCallback(
    (talkGroupId: number) => subscribedIds.includes(talkGroupId),
    [subscribedIds],
  )

  const removeFromQueue = useCallback((callId: number) => {
    setQueue((prev) => prev.filter((c) => c.id !== callId))
  }, [])

  const clearQueue = useCallback(() => setQueue([]), [])

  // ---- Playback queue engine ----

  const startNext = useCallback(() => {
    // Grab the next queued call atomically.
    const next = queueRef.current[0]
    if (!next) {
      setCurrent(null)
      return
    }
    setQueue((prev) => prev.slice(1))
    setCurrent(next)

    playFromCall({
      url: recordingDownloadUrlByCall(next.id),
      title: `${next.talkGroupDescription || `TG ${next.talkGroupNumber}`} · ${formatDateTime(next.recordingTime)}`,
      callId: next.id,
    })
  }, [playFromCall])

  // When the current queued call finishes (or is stopped), advance the queue.
  // While the user has paused, hold the queue on the current call.
  useEffect(() => {
    if (!current) return
    if (isPlaying) return // still playing
    if (isPaused) return // user paused — stay on this call
    // Give the audio element a tick to settle after 'ended' before advancing.
    const t = setTimeout(() => {
      if (currentRef.current?.id === current.id) {
        startNext()
      }
    }, 250)
    return () => clearTimeout(t)
  }, [current, isPlaying, isPaused, startNext])

  // If the queue is non-empty but nothing is playing, resume — either because
  // the user paused mid-session, or because new calls arrived while idle.
  // While a manual session is actively playing, the clicked call is NOT in the
  // queue, so this effect must stay out of the way.
  useEffect(() => {
    if (queue.length === 0 || current) return
    if (playingRef.current) return
    if (isPaused) return // user paused — don't steal queued calls
    if (sessionActiveRef.current) return
    if (sessionResumeRef.current < sessionStartRef.current) {
      // A manual session was started and never finished — user paused it.
      sessionResumeRef.current = sessionStartRef.current
    }
    startNext()
  }, [queue, current, isPaused, startNext])

  // ---- Manual playback sessions ----

  // The stream (visible, filtered list, newest first) captured when the
  // session started. Re-evaluated on every call completion so calls that
  // arrive mid-session are picked up.
  const sessionStreamRef = useRef<CallDto[] | null>(null)
  // The clicked call's recording time — the session plays everything newer.
  const sessionAnchorRef = useRef<string | null>(null)
  // All call IDs that have been played or are in the queue during this
  // session. Prevents re-adding calls that have already been consumed.
  const sessionPlayedRef = useRef<Set<number>>(new Set())

  const playFromCallSession = useCallback((call: CallDto, stream: CallDto[]) => {
    sessionStartRef.current += 1
    sessionActiveRef.current = true
    sessionStreamRef.current = stream
    sessionAnchorRef.current = call.recordingTime
    sessionPlayedRef.current = new Set([call.id])
    // The session plays the clicked call, then the consecutive calls in the
    // visible (filtered) stream that are newer than it, up to the top.
    const sessionCalls = stream.filter((c) => c.id !== call.id && c.recordingTime > call.recordingTime)
    // Merge with the existing queue (subscribed auto-play calls), de-duplicated
    // and sorted chronologically so everything plays in time order.
    const merged = [...queueRef.current, ...sessionCalls]
    const seen = new Set<number>()
    const deduped = merged.filter((c) => (seen.has(c.id) ? false : (seen.add(c.id), true)))
    deduped.sort((a, b) => new Date(a.recordingTime).getTime() - new Date(b.recordingTime).getTime())
    // Track all queued calls as "played" so they aren't re-added later.
    for (const c of deduped) sessionPlayedRef.current.add(c.id)
    setQueue(deduped)
    // Track the clicked call as `current` so the advance effect hands off to
    // the queue when it ends.
    setCurrent(call)
    playFromCall({
      url: recordingDownloadUrlByCall(call.id),
      title: `${call.talkGroupDescription || `TG ${call.talkGroupNumber}`} · ${formatDateTime(call.recordingTime)}`,
      callId: call.id,
    })
  }, [playFromCall])

  // ---- Summary playback sessions ----

  const playSummarySession = useCallback(async (summary: TranscriptSummaryDto) => {
    // Fetch calls for this talk group, newest first, and filter to the
    // summary's time window. We page back until we're past the start time.
    const calls: CallDto[] = []
    let before: { time: string; id: number } | null = null
    const startTime = new Date(summary.startTime).getTime()
    const endTime = new Date(summary.endTime).getTime()

    for (let page = 0; page < 20; page++) {
      const result = await getCallsCursor(50, before, summary.talkGroupId)
      for (const c of result.items) {
        const t = new Date(c.recordingTime).getTime()
        if (t > endTime) continue // newer than the summary window
        if (t < startTime) break // older than the summary window — stop paging
        calls.push(c)
      }
      // Stop paging if we've gone past the start time or there are no more.
      if (!result.hasMore) break
      const oldest = result.items[result.items.length - 1]
      if (oldest && new Date(oldest.recordingTime).getTime() < startTime) break
      before = { time: result.nextCursorTime!, id: result.nextCursorId! }
    }

    if (calls.length === 0) return

    // Sort chronologically (oldest first) for playback order.
    calls.sort((a, b) => new Date(a.recordingTime).getTime() - new Date(b.recordingTime).getTime())

    // Start the session: play the first call, queue the rest, then continue
    // with newer calls on this talk group (anchor = summary end time).
    sessionStartRef.current += 1
    sessionActiveRef.current = true
    sessionStreamRef.current = calls
    sessionAnchorRef.current = summary.endTime
    sessionPlayedRef.current = new Set(calls.map((c) => c.id))

    const first = calls[0]
    const rest = calls.slice(1)

    // Merge with existing queue (subscribed auto-play calls), de-duplicated
    // and sorted chronologically.
    const merged = [...queueRef.current, ...rest]
    const seen = new Set<number>()
    const deduped = merged.filter((c) => (seen.has(c.id) ? false : (seen.add(c.id), true)))
    deduped.sort((a, b) => new Date(a.recordingTime).getTime() - new Date(b.recordingTime).getTime())
    for (const c of deduped) sessionPlayedRef.current.add(c.id)
    setQueue(deduped)

    setCurrent(first)
    playFromCall({
      url: recordingDownloadUrlByCall(first.id),
      title: `${first.talkGroupDescription || `TG ${first.talkGroupNumber}`} · ${formatDateTime(first.recordingTime)}`,
      callId: first.id,
    })
  }, [playFromCall])

  // Re-evaluate the session's stream on every call completion: pick up calls
  // that arrived since the session started (or since the last re-evaluation)
  // and add them to the queue if they're newer than the anchor and haven't
  // been played yet.
  useEffect(() => {
    if (!current) return
    if (isPlaying) return // still playing
    const t = setTimeout(() => {
      if (currentRef.current?.id !== current.id) return
      if (!sessionActiveRef.current) return
      const stream = sessionStreamRef.current
      const anchor = sessionAnchorRef.current
      if (!stream || !anchor) return
      const newCalls = stream.filter(
        (c) => !sessionPlayedRef.current.has(c.id) && c.recordingTime > anchor,
      )
      if (newCalls.length > 0) {
        for (const c of newCalls) sessionPlayedRef.current.add(c.id)
        setQueue((prev) => {
          const merged = [...prev, ...newCalls]
          merged.sort((a, b) => new Date(a.recordingTime).getTime() - new Date(b.recordingTime).getTime())
          return merged
        })
      }
    }, 250)
    return () => clearTimeout(t)
  }, [current, isPlaying])

  // Once the session's queue drains (current is null and the queue is empty),
  // the session is over — clear the flag so the resume effect can pick up any
  // calls that arrived during the session.
  useEffect(() => {
    if (!current && queue.length === 0) {
      sessionActiveRef.current = false
      sessionStreamRef.current = null
      sessionAnchorRef.current = null
      sessionPlayedRef.current = new Set()
    }
  }, [current, queue])

  // Update the session's stream reference. Called by the stream component
  // whenever new calls arrive, so the re-evaluation effect sees the latest
  // list of calls.
  const updateSessionStream = useCallback((stream: CallDto[]) => {
    if (sessionActiveRef.current) {
      sessionStreamRef.current = stream
    }
  }, [])

  // ---- New calls for subscribed talk groups (via the shared SignalR bus) ----

  const { subscribe } = useSignalRBus()

  useEffect(() => {
    return subscribe({
      onNewCall: async (payload: CallDto | number) => {
        let call: CallDto | null = null
        if (typeof payload === 'number') {
          // Legacy numeric form: fetch the call.
          try {
            call = await getCall(payload)
          } catch {
            return
          }
        } else {
          call = payload
        }
        if (!call) return
        // Only enqueue calls on subscribed talk groups.
        if (!subscribedRef.current.includes(call.talkGroupId)) return
        // De-duplicate (the call may already be queued or playing).
        if (queueRef.current.some((c) => c.id === call.id)) return
        if (currentRef.current?.id === call.id) return
        setQueue((prev) => (prev.some((c) => c.id === call.id) ? prev : [...prev, call]))
      },
    })
  }, [subscribe])

  const value = useMemo<SubscriptionsValue>(
    () => ({
      subscribedIds,
      isSubscribed,
      toggleSubscription,
      queue,
      current,
      playFromCall: playFromCallSession,
      playSummarySession,
      updateSessionStream,
      removeFromQueue,
      clearQueue,
    }),
    [subscribedIds, isSubscribed, toggleSubscription, queue, current, playFromCallSession, playSummarySession, updateSessionStream, removeFromQueue, clearQueue],
  )

  return <SubscriptionsContext.Provider value={value}>{children}</SubscriptionsContext.Provider>
}

export function useSubscriptions(): SubscriptionsValue {
  const ctx = useContext(SubscriptionsContext)
  if (!ctx) throw new Error('useSubscriptions must be used within SubscriptionsProvider')
  return ctx
}
