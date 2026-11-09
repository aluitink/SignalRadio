import { createContext, useContext, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import type { CallDto } from '../types/dtos'
import type { HubStatus } from '../hooks/useSignalR'

/**
 * Handlers for the hub's broadcast events. All events are broadcast to every
 * connected client by the server (NewCall / NewTranscription /
 * InvestigationStarted / InvestigationCompleted go to all clients; NewSummary
 * goes to the talkgroup group, which the bus joins for every talk group that
 * has at least one subscriber).
 */
export interface SignalRBusHandlers {
  onNewCall?: (call: CallDto | number) => void
  onNewTranscription?: (recordingId: number, transcriptionId: number) => void
  onNewSummary?: (summaryId: number) => void
  onInvestigationStarted?: (investigationId: number, callId: number) => void
  onInvestigationCompleted?: (investigationId: number, callId: number, status: string) => void
}

interface SignalRBusContextValue {
  status: HubStatus
  /** Subscribe to hub events. Returns an unsubscribe function. */
  subscribe: (handlers: SignalRBusHandlers) => () => void
  /**
   * Join the hub group for a talk group (needed to receive its NewSummary
   * broadcasts). Idempotent; safe to call repeatedly.
   */
  joinTalkGroup: (talkGroupId: number) => void
}

const SignalRBusContext = createContext<SignalRBusContextValue | null>(null)

/**
 * Owns the single app-wide SignalR connection to the TalkGroup hub and
 * dispatches its broadcast events to all subscribers. Previously every
 * component that called `useSignalR` opened its own connection and registered
 * handlers for only the events it cared about — the SignalR client logs a
 * "client method not found" warning for every broadcast event without a
 * handler (e.g. NewCall / NewTranscription on the talk group page). With a
 * single connection that registers a handler for every event the server can
 * send, those warnings go away and pages no longer open redundant sockets.
 */
export function SignalRBusProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<HubStatus>('disconnected')
  const handlersRef = useRef<Set<SignalRBusHandlers>>(new Set())
  const connectionRef = useRef<import('@microsoft/signalr').HubConnection | null>(null)

  const subscribe = useMemo(
    () => (handlers: SignalRBusHandlers) => {
      handlersRef.current.add(handlers)
      return () => {
        handlersRef.current.delete(handlers)
      }
    },
    [],
  )

  const joinTalkGroup = useMemo(
    () => (talkGroupId: number) => {
      void connectionRef.current?.invoke('JoinTalkGroup', talkGroupId).catch(() => {
        /* ignore — the connection may be mid-reconnect */
      })
    },
    [],
  )

  useEffect(() => {
    let disposed = false
    let reconnectTimer: ReturnType<typeof setTimeout> | null = null

    const start = async () => {
      if (disposed) return
      setStatus('connecting')
      const connection = new HubConnectionBuilder()
        .withUrl('/hubs/talkgroups')
        .withAutomaticReconnect()
        .configureLogging(0) // none
        .build()
      connectionRef.current = connection

      // Register a handler for every event the server broadcasts so the
      // client never logs "client method not found" warnings.
      connection.on('NewCall', (payload: CallDto | number) => {
        for (const h of handlersRef.current) h.onNewCall?.(payload)
      })
      connection.on('NewTranscription', (recordingId: number, transcriptionId: number) => {
        for (const h of handlersRef.current) h.onNewTranscription?.(recordingId, transcriptionId)
      })
      connection.on('NewSummary', (summaryId: number) => {
        for (const h of handlersRef.current) h.onNewSummary?.(summaryId)
      })
      connection.on('InvestigationStarted', (investigationId: number, callId: number) => {
        for (const h of handlersRef.current) h.onInvestigationStarted?.(investigationId, callId)
      })
      connection.on('InvestigationCompleted', (investigationId: number, callId: number, status: string) => {
        for (const h of handlersRef.current) h.onInvestigationCompleted?.(investigationId, callId, status)
      })

      connection.onreconnecting(() => setStatus('disconnected'))
      connection.onreconnected(() => setStatus('connected'))
      connection.onclose(() => {
        if (!disposed) {
          setStatus('disconnected')
          reconnectTimer = setTimeout(start, 3000)
        }
      })

      try {
        await connection.start()
        if (!disposed) setStatus(connection.state === HubConnectionState.Connected ? 'connected' : 'disconnected')
      } catch {
        if (!disposed) {
          setStatus('disconnected')
          reconnectTimer = setTimeout(start, 3000)
        }
      }
    }

    void start()

    return () => {
      disposed = true
      if (reconnectTimer) clearTimeout(reconnectTimer)
      const connection = connectionRef.current
      connectionRef.current = null
      connection?.stop().catch(() => { /* ignore */ })
    }
  }, [])

  const value = useMemo<SignalRBusContextValue>(
    () => ({ status, subscribe, joinTalkGroup }),
    [status, subscribe, joinTalkGroup],
  )

  return <SignalRBusContext.Provider value={value}>{children}</SignalRBusContext.Provider>
}

export function useSignalRBus(): SignalRBusContextValue {
  const ctx = useContext(SignalRBusContext)
  if (!ctx) throw new Error('useSignalRBus must be used within SignalRBusProvider')
  return ctx
}
