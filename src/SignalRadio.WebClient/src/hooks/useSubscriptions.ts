// Subscription manager for talkgroups
// Handles storing user subscriptions in localStorage and provides a React hook
// Integrates with SignalR for real-time subscription management

import { useSignalR } from './useSignalR'
import { useAudioManager } from './useAudioManager'
import type { CallDto } from '../types/dtos'

const STORAGE_KEY = 'signalradio_subscriptions'

class SubscriptionManager {
  private subscriptions = new Set<number>()
  private listeners: Array<(subscriptions: Set<number>) => void> = []
  private signalRConnection: any = null
  private pendingSubscriptions = new Set<number>() // Track subscriptions waiting for SignalR confirmation

  constructor() {
    this.loadFromStorage()
  }

  setSignalRConnection(connection: any) {
    this.signalRConnection = connection
    
    if (connection) {
      // Set up SignalR event handlers
      connection.on('SubscriptionConfirmed', (talkGroupId: string) => {
        const id = parseInt(talkGroupId)
        if (!isNaN(id)) {
          this.pendingSubscriptions.delete(id)
          if (!this.subscriptions.has(id)) {
            this.subscriptions.add(id)
            this.saveToStorage()
            this.notifyListeners()
          }
          console.log(`Subscription confirmed for talk group ${id}`)
        }
      })

      connection.on('UnsubscriptionConfirmed', (talkGroupId: string) => {
        const id = parseInt(talkGroupId)
        if (!isNaN(id)) {
          this.pendingSubscriptions.delete(id)
          if (this.subscriptions.has(id)) {
            this.subscriptions.delete(id)
            this.saveToStorage()
            this.notifyListeners()
          }
          console.log(`Unsubscription confirmed for talk group ${id}`)
        }
      })
    }
  }

  async resubscribeToSignalR() {
    if (!this.signalRConnection) return

    console.log(`Resubscribing to ${this.subscriptions.size} saved talk groups`)
    
    for (const talkGroupId of this.subscriptions) {
      try {
        await this.signalRConnection.invoke('SubscribeToTalkGroup', talkGroupId.toString())
      } catch (error) {
        console.error(`Failed to resubscribe to talk group ${talkGroupId}:`, error)
      }
    }
  }

  private loadFromStorage() {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (stored) {
        const parsed = JSON.parse(stored)
        if (Array.isArray(parsed)) {
          this.subscriptions = new Set(parsed.filter(id => typeof id === 'number'))
        }
      }
    } catch (error) {
      console.error('Failed to load subscriptions from storage:', error)
    }
  }

  private saveToStorage() {
    try {
      const subscriptionsArray = Array.from(this.subscriptions)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(subscriptionsArray))
    } catch (error) {
      console.error('Failed to save subscriptions to storage:', error)
    }
  }

  async subscribe(talkGroupId: number) {
    // Always update local state first for immediate UI feedback
    if (!this.subscriptions.has(talkGroupId)) {
      this.subscriptions.add(talkGroupId)
      this.saveToStorage()
      this.notifyListeners()
    }

    // Then try to subscribe via SignalR if connected
    if (this.signalRConnection) {
      try {
        this.pendingSubscriptions.add(talkGroupId)
        await this.signalRConnection.invoke('SubscribeToTalkGroup', talkGroupId.toString())
      } catch (error) {
        console.error(`Failed to subscribe to talk group ${talkGroupId} via SignalR:`, error)
        this.pendingSubscriptions.delete(talkGroupId)
        // Don't revert local state - user can retry later
        throw error
      }
    }
  }

  async unsubscribe(talkGroupId: number) {
    // Try to unsubscribe via SignalR first if connected
    if (this.signalRConnection) {
      try {
        this.pendingSubscriptions.add(talkGroupId)
        await this.signalRConnection.invoke('UnsubscribeFromTalkGroup', talkGroupId.toString())
      } catch (error) {
        console.error(`Failed to unsubscribe from talk group ${talkGroupId} via SignalR:`, error)
        this.pendingSubscriptions.delete(talkGroupId)
        throw error
      }
    }

    // Update local state regardless (for offline mode)
    if (this.subscriptions.has(talkGroupId)) {
      this.subscriptions.delete(talkGroupId)
      this.saveToStorage()
      this.notifyListeners()
    }
  }

  async toggle(talkGroupId: number) {
    if (this.subscriptions.has(talkGroupId)) {
      await this.unsubscribe(talkGroupId)
    } else {
      await this.subscribe(talkGroupId)
    }
  }

  isSubscribed(talkGroupId: number): boolean {
    return this.subscriptions.has(talkGroupId)
  }

  getSubscriptions(): number[] {
    return Array.from(this.subscriptions)
  }

  getSubscriptionCount(): number {
    return this.subscriptions.size
  }

  async clear() {
    // Try to unsubscribe from all via SignalR
    if (this.signalRConnection) {
      const subscriptionList = Array.from(this.subscriptions)
      for (const talkGroupId of subscriptionList) {
        try {
          await this.signalRConnection.invoke('UnsubscribeFromTalkGroup', talkGroupId.toString())
        } catch (error) {
          console.error(`Failed to unsubscribe from talk group ${talkGroupId}:`, error)
        }
      }
    }

    // Clear local state
    this.subscriptions.clear()
    this.saveToStorage()
    this.notifyListeners()
  }

  isPending(talkGroupId: number): boolean {
    return this.pendingSubscriptions.has(talkGroupId)
  }

  addListener(listener: (subscriptions: Set<number>) => void) {
    this.listeners.push(listener)
    
    // Return cleanup function
    return () => {
      const index = this.listeners.indexOf(listener)
      if (index > -1) {
        this.listeners.splice(index, 1)
      }
    }
  }

  private notifyListeners() {
    this.listeners.forEach(listener => {
      try {
        listener(new Set(this.subscriptions))
      } catch (error) {
        console.error('Error in subscription manager listener:', error)
      }
    })
  }
}

export const subscriptionManager = new SubscriptionManager()

// The useSubscriptions hook is now provided by SubscriptionContext
// This export is kept for backwards compatibility but should be imported from the context instead
export { useSubscriptions } from '../contexts/SubscriptionContext'
