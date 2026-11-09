import { createContext, useContext } from 'react'
import type { ReactNode } from 'react'
import { useSignalRBus } from './SignalRBusContext'
import type { HubStatus } from '../hooks/useSignalR'

interface ConnectionStatusContextValue {
  status: HubStatus
}

const ConnectionStatusContext = createContext<ConnectionStatusContextValue>({
  status: 'disconnected',
})

/**
 * Exposes the app-wide SignalR connection status (owned by the SignalR bus)
 * for display. The header's brand dot uses this as the live indicator.
 */
export function ConnectionStatusProvider({ children }: { children: ReactNode }) {
  const { status } = useSignalRBus()

  return (
    <ConnectionStatusContext.Provider value={{ status }}>
      {children}
    </ConnectionStatusContext.Provider>
  )
}

export function useConnectionStatus(): HubStatus {
  return useContext(ConnectionStatusContext).status
}
