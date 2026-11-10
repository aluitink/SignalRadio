import { useCallback, useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { sendInvestigationChat } from '../api'
import type {
  ChatMessage,
  InvestigationChatEvent,
  InvestigationChatMessageDto,
} from '../types/dtos'

let msgCounter = 0
function nextId() {
  msgCounter += 1
  return `msg-${Date.now()}-${msgCounter}`
}

/**
 * Map a persisted chat message (from the API) into a rendered ChatMessage.
 * The `id` is derived from the persisted row id so React keys are stable.
 */
function toChatMessage(m: InvestigationChatMessageDto): ChatMessage {
  return {
    id: `db-${m.id}`,
    role: (m.role === 'user' || m.role === 'assistant' || m.role === 'tool' ? m.role : 'assistant') as ChatMessage['role'],
    text: m.text ?? undefined,
    toolName: m.toolName ?? undefined,
    toolArgs: m.toolArgs ?? undefined,
    toolResult: m.toolResult ?? undefined,
    toolCallId: m.toolCallId ?? undefined,
    error: m.error ?? undefined,
  }
}

/**
 * Manages a live investigation chat. Connects to the SignalR hub, joins the
 * investigation group, and accumulates streamed agent events into a list of
 * rendered chat messages.
 *
 * Hydration: when the investigation's persisted messages are provided, they are
 * loaded as the initial thread (so the full conversation is visible when the user
 * navigates back into the chat). Live SignalR events are only applied while a
 * turn is running.
 */
export function useInvestigationChat(
  investigationId: number | null,
  messages: InvestigationChatMessageDto[],
) {
  const [chat, setChat] = useState<ChatMessage[]>([])
  const [isRunning, setIsRunning] = useState(false)
  const [status, setStatus] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected')
  const connectionRef = useRef<import('@microsoft/signalr').HubConnection | null>(null)
  const runningRef = useRef(false)

  // Hydrate the thread from the investigation's persisted messages.
  useEffect(() => {
    setChat(messages.map(toChatMessage))
  }, [messages])

  // Append or update the last assistant message with a text delta.
  const appendAssistantText = useCallback((delta: string) => {
    setChat((prev) => {
      const last = prev[prev.length - 1]
      if (last && last.role === 'assistant' && last.streaming) {
        const updated = [...prev]
        updated[updated.length - 1] = { ...last, text: (last.text ?? '') + delta }
        return updated
      }
      return [...prev, { id: nextId(), role: 'assistant', text: delta, streaming: true }]
    })
  }, [])

  const finalizeAssistant = useCallback(() => {
    setChat((prev) => {
      const last = prev[prev.length - 1]
      if (last && last.role === 'assistant' && last.streaming) {
        const updated = [...prev]
        updated[updated.length - 1] = { ...last, streaming: false }
        return updated
      }
      return prev
    })
  }, [])

  const handleEvent = useCallback(
    (evt: InvestigationChatEvent) => {
      // Only apply live events while a turn is running.
      if (!runningRef.current) return

      switch (evt.type) {
        case 'text':
          if (evt.text) appendAssistantText(evt.text)
          break
        case 'tool_call':
          // Finalize any in-progress assistant text before showing the tool card.
          finalizeAssistant()
          setChat((prev) => [
            ...prev,
            {
              id: nextId(),
              role: 'tool',
              toolName: evt.toolName ?? 'tool',
              toolArgs: evt.toolArgs ?? '{}',
              toolCallId: evt.toolCallId ?? undefined,
            },
          ])
          break
        case 'tool_result':
          // Attach the result to the matching tool-call message so the call and
          // its result render as a single node. Fall back to the most recent
          // tool message still awaiting a result (positional pairing).
          setChat((prev) => {
            let idx = -1
            if (evt.toolCallId) {
              idx = prev.findIndex((m) => m.role === 'tool' && m.toolCallId === evt.toolCallId && m.toolResult == null)
            } else {
              for (let i = prev.length - 1; i >= 0; i--) {
                if (prev[i].role === 'tool' && prev[i].toolResult == null) {
                  idx = i
                  break
                }
              }
            }
            if (idx === -1) {
              // No matching call (e.g. reconnected mid-run) — render as a standalone result.
              return [
                ...prev,
                {
                  id: nextId(),
                  role: 'tool',
                  toolName: evt.toolName ?? 'tool',
                  toolResult: evt.toolResult ?? '',
                },
              ]
            }
            const updated = [...prev]
            updated[idx] = { ...updated[idx], toolResult: evt.toolResult ?? '' }
            return updated
          })
          break
        case 'run_finished':
          finalizeAssistant()
          setIsRunning(false)
          runningRef.current = false
          break
        case 'run_failed':
          finalizeAssistant()
          setChat((prev) => [
            ...prev,
            { id: nextId(), role: 'assistant', error: evt.error ?? 'Investigation failed.' },
          ])
          setIsRunning(false)
          runningRef.current = false
          break
      }
    },
    [appendAssistantText, finalizeAssistant],
  )

  // Connect to the hub and join the investigation group.
  useEffect(() => {
    if (investigationId == null) return
    let disposed = false
    let reconnectTimer: ReturnType<typeof setTimeout> | null = null

    const start = async () => {
      if (disposed) return
      const connection = new HubConnectionBuilder()
        .withUrl('/hubs/talkgroups')
        .withAutomaticReconnect()
        .configureLogging(0)
        .build()

      connection.on('InvestigationChatEvent', (payload: InvestigationChatEvent) => {
        if (!disposed) handleEvent(payload)
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
        if (!disposed) {
          connectionRef.current = connection
          setStatus(connection.state === HubConnectionState.Connected ? 'connected' : 'disconnected')
          await connection.invoke('JoinInvestigation', investigationId)
        }
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
      connectionRef.current?.stop().catch(() => { /* ignore */ })
      connectionRef.current = null
    }
  }, [investigationId, handleEvent])

  const send = useCallback(
    async (message: string) => {
      if (investigationId == null || runningRef.current || !message.trim()) return
      runningRef.current = true
      setIsRunning(true)
      setChat((prev) => [...prev, { id: nextId(), role: 'user', text: message }])
      try {
        await sendInvestigationChat(investigationId, message)
      } catch (err) {
        runningRef.current = false
        setIsRunning(false)
        setChat((prev) => [
          ...prev,
          { id: nextId(), role: 'assistant', error: err instanceof Error ? err.message : 'Failed to send message.' },
        ])
      }
    },
    [investigationId],
  )

  return { messages: chat, isRunning, status, send }
}
