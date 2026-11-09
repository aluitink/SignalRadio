import { useEffect, useRef } from 'react'
import type { CallDto } from '../types/dtos'
import { useSignalRBus } from '../contexts/SignalRBusContext'

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
  onInvestigationStarted?: (investigationId: number, callId: number) => void
  onInvestigationCompleted?: (investigationId: number, callId: number, status: string) => void
}

/**
 * Subscribes to the app-wide SignalR bus (see `SignalRBusContext`). The bus
 * owns the single hub connection and registers handlers for every event the
 * server broadcasts, so components only receive the events they ask for and
 * the client never logs "client method not found" warnings for the rest.
 *
 * When talkGroupId is provided the connection also joins that talk group's
 * hub group so its NewSummary broadcasts are received.
 */
export function useSignalR(talkGroupId: number | null, handlers: SignalRHandlers): HubStatus {
  const { status, subscribe, joinTalkGroup } = useSignalRBus()
  const handlersRef = useRef(handlers)
  handlersRef.current = handlers

  useEffect(() => {
    if (talkGroupId != null) joinTalkGroup(talkGroupId)
    return subscribe({
      onNewCall: (call) => handlersRef.current.onNewCall?.(call),
      onNewTranscription: (recordingId, transcriptionId) =>
        handlersRef.current.onNewTranscription?.(recordingId, transcriptionId),
      onNewSummary: (summaryId) => handlersRef.current.onNewSummary?.(summaryId),
      onInvestigationStarted: (investigationId, callId) =>
        handlersRef.current.onInvestigationStarted?.(investigationId, callId),
      onInvestigationCompleted: (investigationId, callId, status) =>
        handlersRef.current.onInvestigationCompleted?.(investigationId, callId, status),
    })
  }, [talkGroupId, subscribe, joinTalkGroup])

  return status
}
