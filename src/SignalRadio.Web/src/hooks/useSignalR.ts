import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import type { CallDto } from '../types/dtos'

export type HubStatus = 'connecting' | 'connected' | 'disconnected'

export interface SignalRHandlers {
  /**
   * New call notification. The server sends the full CallDto (rich payload) so
   * clients can prepend the card and enqueue it for auto-play without an extra
   * API round-trip. The legacy numeric callId form is still handled for
   * backwards compatibility.
   */
  onNewCall?: (call: CallDto | number) => void
  onNewTranscription?: (recordingId: number, transcriptionId: number) => void
  onNewSummary?: (summaryId: number) => void
}

// Connects to the TalkGroup hub. When talkGroupId is provided the connection
// joins that talkgroup's group (receives NewCall / NewSummary for it).
// NewTranscription is broadcast to all clients.
export function useSignalR(talkGroupId: number | null, handlers: SignalRHandlers) {
  const [status, setStatus] = useState<HubStatus>('disconnected')
  const handlersRef = useRef(handlers)
  handlersRef.current = handlers

  useEffect(() => {
    let connection: import('@microsoft/signalr').HubConnection | null = null
    let disposed = false
    let reconnectTimer: ReturnType<typeof setTimeout> | null = null

    const build = () => {
      const url = talkGroupId != null
        ? `/hubs/talkgroups?talkGroupId=${talkGroupId}`
        : '/hubs/talkgroups'
      return new HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect()
        .configureLogging(0) // none
        .build()
    }

    const start = async () => {
      if (disposed) return
      connection = build()
      setStatus('connecting')

      connection.on('NewCall', (payload: CallDto | number) => handlersRef.current.onNewCall?.(payload))
      connection.on('NewTranscription', (recordingId: number, transcriptionId: number) =>
        handlersRef.current.onNewTranscription?.(recordingId, transcriptionId),
      )
      connection.on('NewSummary', (summaryId: number) => handlersRef.current.onNewSummary?.(summaryId))

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

    start()

    return () => {
      disposed = true
      if (reconnectTimer) clearTimeout(reconnectTimer)
      connection?.stop().catch(() => { /* ignore */ })
    }
  }, [talkGroupId])

  return status
}
