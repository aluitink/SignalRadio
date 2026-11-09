import { useCallback, useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { sendInvestigationChat } from '../api'
import type { ChatMessage, InvestigationChatEvent } from '../types/dtos'

let msgCounter = 0
function nextId() {
  msgCounter += 1
  return `msg-${Date.now()}-${msgCounter}`
}

/**
 * Manages a live investigation chat session. Connects to the SignalR hub,
 * joins the investigation group, and accumulates streamed agent events into
 * a list of rendered chat messages.
 */
export function useInvestigationChat(investigationId: number | null) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [isRunning, setIsRunning] = useState(false)
  const [status, setStatus] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected')
  const connectionRef = useRef<import('@microsoft/signalr').HubConnection | null>(null)
  const runningRef = useRef(false)

  // Append or update the last assistant message with a text delta.
  const appendAssistantText = useCallback((delta: string) => {
    setMessages((prev) => {
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
    setMessages((prev) => {
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
      switch (evt.type) {
        case 'text':
          if (evt.text) appendAssistantText(evt.text)
          break
        case 'tool_call':
          // Finalize any in-progress assistant text before showing the tool card.
          finalizeAssistant()
          setMessages((prev) => [
            ...prev,
            {
              id: nextId(),
              role: 'tool',
              toolName: evt.toolName ?? 'tool',
              toolArgs: evt.toolArgs ?? '{}',
            },
          ])
          break
        case 'tool_result':
          setMessages((prev) => [
            ...prev,
            {
              id: nextId(),
              role: 'tool',
              toolName: evt.toolName ?? 'tool',
              toolResult: evt.toolResult ?? '',
            },
          ])
          break
        case 'run_finished':
          finalizeAssistant()
          if (evt.reportJson) {
            const reportJson = evt.reportJson
            setMessages((prev) => [
              ...prev,
              { id: nextId(), role: 'assistant', reportJson },
            ])
          }
          setIsRunning(false)
          runningRef.current = false
          break
        case 'run_failed':
          finalizeAssistant()
          setMessages((prev) => [
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
      setMessages((prev) => [...prev, { id: nextId(), role: 'user', text: message }])
      try {
        await sendInvestigationChat(investigationId, message)
      } catch (err) {
        runningRef.current = false
        setIsRunning(false)
        setMessages((prev) => [
          ...prev,
          { id: nextId(), role: 'assistant', error: err instanceof Error ? err.message : 'Failed to send message.' },
        ])
      }
    },
    [investigationId],
  )

  return { messages, isRunning, status, send }
}
