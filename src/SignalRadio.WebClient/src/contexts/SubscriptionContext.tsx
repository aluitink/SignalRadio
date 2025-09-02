import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react'
import { subscriptionManager } from '../hooks/useSubscriptions'
import { useSignalR } from '../hooks/useSignalR'
import { useAudioManager } from '../hooks/useAudioManager'
import type { CallDto } from '../types/dtos'

interface SubscriptionContextType {
  subscriptions: Set<number>
  subscribe: (talkGroupId: number) => Promise<void>
  unsubscribe: (talkGroupId: number) => Promise<void>
  toggle: (talkGroupId: number) => Promise<void>
  clearAll: () => Promise<void>
  isSubscribed: (talkGroupId: number) => boolean
  isPending: (talkGroupId: number) => boolean
  getSubscriptionsList: () => number[]
  subscriptionCount: number
  connected: boolean
}

const SubscriptionContext = createContext<SubscriptionContextType | null>(null)

interface SubscriptionProviderProps {
  children: ReactNode
}

export function SubscriptionProvider({ children }: SubscriptionProviderProps) {
  const { connection, connected } = useSignalR('/hubs/talkgroup')
  const { playFromSubscription } = useAudioManager()
  
  const [subscriptions, setSubscriptions] = useState<Set<number>>(
    new Set(subscriptionManager.getSubscriptions())
  )
  const [isConnected, setIsConnected] = useState(false)
  const [hasInitialized, setHasInitialized] = useState(false)

  // Set up SignalR connection and handlers - only once per app
  useEffect(() => {
    if (connection && !hasInitialized) {
      console.log('[SubscriptionProvider] Setting up SignalR connection')
      subscriptionManager.setSignalRConnection(connection)
      setHasInitialized(true)
      
      // Set up call handler for new calls
      const handleCallUpdated = (call: CallDto) => {
        console.log('[SubscriptionContext] Call updated received via SignalR:', {
          callId: call.id,
          talkGroupId: call.talkGroupId,
          recordingTime: call.recordingTime,
          recordingsCount: call.recordings?.length || 0
        })
        
        // Check if this call is from a subscribed talk group
        if (subscriptionManager.isSubscribed(call.talkGroupId)) {
          console.log(`[SubscriptionContext] New call from subscribed talk group ${call.talkGroupId}`)
          
          // Check if this is a recent call (within last 5 minutes) to avoid auto-playing old calls
          // that get transcript updates
          const now = Date.now()
          const callTime = new Date(call.recordingTime).getTime()
          const fiveMinutesAgo = now - (5 * 60 * 1000)
          
          console.log('[SubscriptionContext] Time check:', {
            now: new Date(now).toISOString(),
            callTime: new Date(callTime).toISOString(),
            fiveMinutesAgo: new Date(fiveMinutesAgo).toISOString(),
            isRecent: callTime >= fiveMinutesAgo,
            hasRecordings: !!call.recordings?.length
          })
          
          if (callTime >= fiveMinutesAgo && call.recordings?.length) {
            console.log(`[SubscriptionContext] Playing subscribed call ${call.id}`)
            // Auto-play if conditions are met (handled by audio manager)
            playFromSubscription(call).catch(error => {
              console.error('[SubscriptionContext] Failed to play subscribed call:', error)
            })
          } else {
            console.log(`[SubscriptionContext] Skipping auto-play for older call ${call.id} (${call.recordingTime}) or no recordings`)
          }
        } else {
          console.log(`[SubscriptionContext] Call ${call.id} is not from a subscribed talk group (${call.talkGroupId})`)
        }
      }

      connection.on('CallUpdated', handleCallUpdated)

      // Note: We don't subscribe to all calls stream here since CallStream component already does it
      // Both components will receive the same CallUpdated events through the shared connection

      return () => {
        connection.off('CallUpdated', handleCallUpdated)
      }
    }
  }, [connection, hasInitialized, playFromSubscription])

  // Handle connection state changes - only resubscribe once per connection
  useEffect(() => {
    setIsConnected(connected)
    
    // Only resubscribe when connection is established and we haven't done initial setup
    if (connected && connection && hasInitialized) {
      console.log('[SubscriptionProvider] Connection established, resubscribing to talkgroups')
      subscriptionManager.resubscribeToSignalR()
    }
  }, [connected, connection, hasInitialized])

  // Set up subscription manager listener - only once
  useEffect(() => {
    const cleanup = subscriptionManager.addListener((newSubscriptions) => {
      setSubscriptions(new Set(newSubscriptions))
    })

    return cleanup
  }, [])

  const subscribe = async (talkGroupId: number) => {
    try {
      await subscriptionManager.subscribe(talkGroupId)
    } catch (error) {
      console.error('Failed to subscribe:', error)
      throw error
    }
  }

  const unsubscribe = async (talkGroupId: number) => {
    try {
      await subscriptionManager.unsubscribe(talkGroupId)
    } catch (error) {
      console.error('Failed to unsubscribe:', error)
      throw error
    }
  }

  const toggle = async (talkGroupId: number) => {
    try {
      await subscriptionManager.toggle(talkGroupId)
    } catch (error) {
      console.error('Failed to toggle subscription:', error)
      throw error
    }
  }

  const clearAll = async () => {
    try {
      await subscriptionManager.clear()
    } catch (error) {
      console.error('Failed to clear subscriptions:', error)
      throw error
    }
  }

  const isSubscribed = (talkGroupId: number): boolean => {
    return subscriptions.has(talkGroupId)
  }

  const isPending = (talkGroupId: number): boolean => {
    return subscriptionManager.isPending(talkGroupId)
  }

  const getSubscriptionsList = (): number[] => {
    return Array.from(subscriptions)
  }

  const value: SubscriptionContextType = {
    subscriptions,
    subscribe,
    unsubscribe,
    toggle,
    clearAll,
    isSubscribed,
    isPending,
    getSubscriptionsList,
    subscriptionCount: subscriptions.size,
    connected: isConnected
  }

  return (
    <SubscriptionContext.Provider value={value}>
      {children}
    </SubscriptionContext.Provider>
  )
}

export function useSubscriptions(): SubscriptionContextType {
  const context = useContext(SubscriptionContext)
  if (!context) {
    throw new Error('useSubscriptions must be used within a SubscriptionProvider')
  }
  return context
}
