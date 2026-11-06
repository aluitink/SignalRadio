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
import { HubConnectionBuilder } from '@microsoft/signalr'
import type { CallDto } from '../types/dtos'
import { getCall, recordingDownloadUrlByCall } from '../api'
import { useAudioPlayer } from './AudioPlayerContext'
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
  const { playFromCall, nowPlaying, isPlaying } = useAudioPlayer()
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
      title: `TG ${next.talkGroupNumber} · ${formatDateTime(next.recordingTime)}`,
      callId: next.id,
    })
  }, [playFromCall])

  // When the current queued call finishes (or is stopped), advance the queue.
  useEffect(() => {
    if (!current) return
    if (isPlaying) return // still playing
    // Give the audio element a tick to settle after 'ended' before advancing.
    const t = setTimeout(() => {
      if (currentRef.current?.id === current.id) {
        startNext()
      }
    }, 250)
    return () => clearTimeout(t)
  }, [current, isPlaying, startNext])

  // If the queue is non-empty but nothing is playing, resume — either because
  // the user paused mid-session, or because new calls arrived while idle.
  // While a manual session is actively playing, the clicked call is NOT in the
  // queue, so this effect must stay out of the way.
  useEffect(() => {
    if (queue.length === 0 || current) return
    if (playingRef.current) return
    if (sessionActiveRef.current) return
    if (sessionResumeRef.current < sessionStartRef.current) {
      // A manual session was started and never finished — user paused it.
      sessionResumeRef.current = sessionStartRef.current
    }
    startNext()
  }, [queue, current, startNext])

  // When the session's queue drains (the clicked call is still playing), the
  // resume effect takes over and plays the remaining queued calls (e.g.
  // subscribed calls that arrived during the session).
  useEffect(() => {
    if (queue.length === 0) sessionActiveRef.current = false
  }, [queue])

  // ---- Manual playback sessions ----

  const playFromCallSession = useCallback((call: CallDto, stream: CallDto[]) => {
    sessionStartRef.current += 1
    sessionActiveRef.current = true
    // The session plays the clicked call, then the consecutive calls in the
    // visible (filtered) stream that are newer than it, up to the top.
    const sessionCalls = stream.filter((c) => c.id !== call.id && c.recordingTime > call.recordingTime)
    // Merge with the existing queue (subscribed auto-play calls), de-duplicated
    // and sorted chronologically so everything plays in time order.
    const merged = [...queueRef.current, ...sessionCalls]
    const seen = new Set<number>()
    const deduped = merged.filter((c) => (seen.has(c.id) ? false : (seen.add(c.id), true)))
    deduped.sort((a, b) => a.recordingTime.localeCompare(b.recordingTime))
    setQueue(deduped)
    setCurrent(null)
    // Start the clicked call immediately; the resume effect advances the queue.
    playFromCall({
      url: recordingDownloadUrlByCall(call.id),
      title: `TG ${call.talkGroupNumber} · ${formatDateTime(call.recordingTime)}`,
      callId: call.id,
    })
  }, [playFromCall])

  // ---- Global SignalR connection for new calls ----

  useEffect(() => {
    let connection: import('@microsoft/signalr').HubConnection | null = null
    let disposed = false
    let reconnectTimer: ReturnType<typeof setTimeout> | null = null

    const start = async () => {
      if (disposed) return
      connection = new HubConnectionBuilder()
        .withUrl('/hubs/talkgroups')
        .withAutomaticReconnect()
        .configureLogging(0)
        .build()

      connection.on('NewCall', async (payload: CallDto | number) => {
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
      })

      connection.onclose(() => {
        if (!disposed) {
          reconnectTimer = setTimeout(start, 3000)
        }
      })

      try {
        await connection.start()
      } catch {
        if (!disposed) reconnectTimer = setTimeout(start, 3000)
      }
    }

    void start()

    return () => {
      disposed = true
      if (reconnectTimer) clearTimeout(reconnectTimer)
      connection?.stop().catch(() => { /* ignore */ })
    }
  }, [])

  const value = useMemo<SubscriptionsValue>(
    () => ({
      subscribedIds,
      isSubscribed,
      toggleSubscription,
      queue,
      current,
      playFromCall: playFromCallSession,
      removeFromQueue,
      clearQueue,
    }),
    [subscribedIds, isSubscribed, toggleSubscription, queue, current, playFromCallSession, removeFromQueue, clearQueue],
  )

  return <SubscriptionsContext.Provider value={value}>{children}</SubscriptionsContext.Provider>
}

export function useSubscriptions(): SubscriptionsValue {
  const ctx = useContext(SubscriptionsContext)
  if (!ctx) throw new Error('useSubscriptions must be used within SubscriptionsProvider')
  return ctx
}
