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
 * Talk group subscriptions with auto-play.
 *
 * - Users subscribe to talk groups (persisted in localStorage).
 * - A single global SignalR connection (owned here) receives NewCall for all
 *   clients. Calls on subscribed talk groups are enqueued.
 * - The queue is played back through the shared audio player, one call at a
 *   time. When multiple subscribed groups are busy at once, calls queue up and
 *   play in arrival order — nothing is dropped.
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
  /** Calls waiting in the auto-play queue (oldest first). */
  queue: CallDto[]
  /** The call currently being auto-played, if any. */
  current: CallDto | null
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
  const { play, nowPlaying, isPlaying } = useAudioPlayer()
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

  // ---- Auto-play engine ----

  const startNext = useCallback(async () => {
    // Grab the next queued call atomically.
    const next = queueRef.current[0]
    if (!next) {
      setCurrent(null)
      return
    }
    setQueue((prev) => prev.slice(1))
    setCurrent(next)

    const url = recordingDownloadUrlByCall(next.id)
    play({
      url,
      title: `TG ${next.talkGroupNumber} · ${formatDateTime(next.recordingTime)}`,
      callId: next.id,
    })
  }, [play])

  // When the current auto-played call finishes (or is stopped), advance the queue.
  useEffect(() => {
    if (!current) return
    if (isPlaying) return // still playing
    // Give the audio element a tick to settle after 'ended' before advancing.
    const t = setTimeout(() => {
      if (currentRef.current?.id === current.id) {
        void startNext()
      }
    }, 250)
    return () => clearTimeout(t)
  }, [current, isPlaying, startNext])

  // If the queue is non-empty but nothing is playing (e.g. user paused), resume.
  useEffect(() => {
    if (queue.length > 0 && !current && !playingRef.current) {
      void startNext()
    }
  }, [queue, current, startNext])

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
      removeFromQueue,
      clearQueue,
    }),
    [subscribedIds, isSubscribed, toggleSubscription, queue, current, removeFromQueue, clearQueue],
  )

  return <SubscriptionsContext.Provider value={value}>{children}</SubscriptionsContext.Provider>
}

export function useSubscriptions(): SubscriptionsValue {
  const ctx = useContext(SubscriptionsContext)
  if (!ctx) throw new Error('useSubscriptions must be used within SubscriptionsProvider')
  return ctx
}
