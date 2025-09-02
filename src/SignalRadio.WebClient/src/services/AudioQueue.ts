import type { CallDto } from '../types/dtos'

export interface AudioQueue {
  add(call: CallDto): void
  remove(callId: number): void
  clear(): void
  moveToFront(callId: number): void
  getNext(): CallDto | null
  getCalls(): CallDto[]
  getLength(): number
  subscribe(listener: AudioQueueListener): () => void
}

export interface AudioQueueListener {
  onQueueChanged(calls: CallDto[]): void
}

export class AudioQueueImpl implements AudioQueue {
  private queue: CallDto[] = []
  private listeners: Set<AudioQueueListener> = new Set()

  add(call: CallDto): void {
    // Don't add if already in queue
    if (this.queue.some(c => c.id === call.id)) {
      return
    }

    this.queue.push(call)
    this.notifyListeners()
  }

  remove(callId: number): void {
    const initialLength = this.queue.length
    this.queue = this.queue.filter(c => c.id !== callId)
    
    if (this.queue.length !== initialLength) {
      this.notifyListeners()
    }
  }

  clear(): void {
    if (this.queue.length > 0) {
      this.queue = []
      this.notifyListeners()
    }
  }

  moveToFront(callId: number): void {
    const callIndex = this.queue.findIndex(c => c.id === callId)
    if (callIndex > 0) {
      const [call] = this.queue.splice(callIndex, 1)
      this.queue.unshift(call)
      this.notifyListeners()
    }
  }

  getNext(): CallDto | null {
    const next = this.queue.shift()
    if (next) {
      this.notifyListeners()
    }
    return next || null
  }

  getCalls(): CallDto[] {
    return [...this.queue]
  }

  getLength(): number {
    return this.queue.length
  }

  subscribe(listener: AudioQueueListener): () => void {
    this.listeners.add(listener)
    
    // Immediately notify with current state
    listener.onQueueChanged(this.getCalls())
    
    return () => {
      this.listeners.delete(listener)
    }
  }

  private notifyListeners(): void {
    this.listeners.forEach(listener => {
      try {
        listener.onQueueChanged(this.getCalls())
      } catch (error) {
        console.error('Error in queue listener:', error)
      }
    })
  }
}

// Singleton instance
export const audioQueue = new AudioQueueImpl()
