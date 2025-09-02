import type { CallDto } from '../types/dtos'
import { apiGet } from '../api'

export interface CallStreamService {
  loadRecentCalls(page: number, pageSize: number): Promise<CallDto[]>
  handleNewCall(call: CallDto): void
  handleCallUpdate(call: CallDto): void
  subscribe(listener: CallStreamListener): () => void
  clearSeenCalls(): void // Add method to clear seen calls tracking
}

export interface CallStreamListener {
  onCallsChanged(calls: CallDto[]): void
  onNewCall(call: CallDto): void
  onCallUpdated(call: CallDto): void
}

export class CallStreamServiceImpl implements CallStreamService {
  private calls: CallDto[] = []
  private listeners: Set<CallStreamListener> = new Set()
  private maxCalls = 100
  private seenCallIds: Set<number> = new Set() // Track calls we've seen before

  async loadRecentCalls(page: number = 1, pageSize: number = 20): Promise<CallDto[]> {
    try {
      const response = await apiGet<{items: CallDto[]}>(`/calls?page=${page}&pageSize=${pageSize}&sortBy=recordingTime&sortDir=desc`)
      if (response?.items) {
        this.calls = response.items
        // Mark all loaded calls as seen to avoid treating them as "new" later
        response.items.forEach(call => this.seenCallIds.add(call.id))
        this.notifyListeners('onCallsChanged', this.calls)
        return response.items
      }
      return []
    } catch (error) {
      console.error('Failed to load recent calls:', error)
      return []
    }
  }

  handleNewCall(call: CallDto): void {
    // Check if this is actually a new call (within last 5 minutes)
    const now = Date.now()
    const callTime = new Date(call.recordingTime).getTime()
    const fiveMinutesAgo = now - (5 * 60 * 1000)
    
    if (callTime >= fiveMinutesAgo) {
      // Only emit onNewCall if this is the first time we've seen this call ID
      const isFirstTimeSeen = !this.seenCallIds.has(call.id)
      this.seenCallIds.add(call.id)
      
      if (isFirstTimeSeen) {
        // This is truly a new call - add to top of stream
        this.calls = [call, ...this.calls.filter(c => c.id !== call.id)]
          .slice(0, this.maxCalls)
        this.notifyListeners('onNewCall', call)
        this.notifyListeners('onCallsChanged', this.calls)
      } else {
        // This is an update to a call we've seen before - update in place
        const existingIndex = this.calls.findIndex(c => c.id === call.id)
        if (existingIndex >= 0) {
          this.calls[existingIndex] = call
          this.notifyListeners('onCallUpdated', call)
          this.notifyListeners('onCallsChanged', this.calls)
        }
      }
    }
  }

  handleCallUpdate(call: CallDto): void {
    const existingIndex = this.calls.findIndex(c => c.id === call.id)
    
    // Always mark this call as seen
    this.seenCallIds.add(call.id)
    
    if (existingIndex >= 0) {
      // Update existing call in place without changing its position
      this.calls[existingIndex] = call
      this.notifyListeners('onCallUpdated', call)
      this.notifyListeners('onCallsChanged', this.calls)
    }
    // If call is not in current list, don't add it - updates should only affect existing calls
    // New calls should come through handleNewCall instead
  }

  subscribe(listener: CallStreamListener): () => void {
    this.listeners.add(listener)
    
    // Immediately notify with current state
    if (this.calls.length > 0) {
      listener.onCallsChanged(this.calls)
    }
    
    return () => {
      this.listeners.delete(listener)
    }
  }

  private notifyListeners<K extends keyof CallStreamListener>(
    method: K, 
    ...args: Parameters<CallStreamListener[K]>
  ): void {
    this.listeners.forEach(listener => {
      try {
        ;(listener[method] as any)(...args)
      } catch (error) {
        console.error(`Error in ${method} listener:`, error)
      }
    })
  }

  getCalls(): CallDto[] {
    return [...this.calls]
  }

  clearSeenCalls(): void {
    this.seenCallIds.clear()
  }
}

// Singleton instance
export const callStreamService = new CallStreamServiceImpl()
