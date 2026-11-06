import { createContext, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import type { HubStatus } from '../hooks/useSignalR'

interface ConnectionStatusContextValue {
  status: HubStatus
}

const ConnectionStatusContext = createContext<ConnectionStatusContextValue>({
  status: 'disconnected',
})

/**
 * Maintains a single global SignalR connection to the TalkGroup hub (no
 * talkgroup group) and exposes its status app-wide. The header's brand dot
 * uses this as the live indicator.
 */
export function ConnectionStatusProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<HubStatus>('disconnected')

  useEffect(() => {
    let connection: import('@microsoft/signalr').HubConnection | null = null
    let disposed = false
    let reconnectTimer: ReturnType<typeof setTimeout> | null = null

    const start = async () => {
      if (disposed) return
      setStatus('connecting')
      connection = new HubConnectionBuilder()
        .withUrl('/hubs/talkgroups')
        .withAutomaticReconnect()
        .configureLogging(0) // none
        .build()

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
        if (!disposed) setStatus('connected')
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
      connection?.stop().catch(() => { /* ignore */ })
    }
  }, [])

  return (
    <ConnectionStatusContext.Provider value={{ status }}>
      {children}
    </ConnectionStatusContext.Provider>
  )
}

export function useConnectionStatus(): HubStatus {
  return useContext(ConnectionStatusContext).status
}
