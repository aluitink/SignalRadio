import type { CallDto } from '../types/dtos'
import { audioQueue } from './AudioQueue'

export type PlayerState = 'stopped' | 'playing'

export interface AudioPlayerService {
  // Player state
  getState(): PlayerState
  getCurrentCall(): CallDto | null
  isPlaying(): boolean
  
  // Player controls
  play(): Promise<void>
  pause(): void
  stop(): void
  
  // Queue management (delegated to AudioQueue)
  addToQueue(call: CallDto): void
  clearQueue(): void
  removeFromQueue(callId: number): void
  moveToFront(callId: number): void
  
  // User interaction and autoplay
  hasUserInteracted(): boolean
  setUserInteracted(): void
  canAutoplay(): boolean
  
  // Event subscription
  subscribe(listener: AudioPlayerListener): () => void
}

export interface AudioPlayerListener {
  onStateChanged(state: PlayerState): void
  onCurrentCallChanged(call: CallDto | null): void
  onPlaybackChanged(isPlaying: boolean): void
  onUserInteractionChanged(hasInteracted: boolean): void
}

export class AudioPlayerServiceImpl implements AudioPlayerService {
  private state: PlayerState = 'stopped'
  private currentCall: CallDto | null = null
  private currentAudio: HTMLAudioElement | null = null
  private isCurrentlyPlaying = false
  private userHasInteracted = false
  private listeners: Set<AudioPlayerListener> = new Set()
  private isProcessingQueue = false

  constructor() {
    // Set up user interaction detection
    this.setupUserInteractionDetection()
    
    // Subscribe to queue changes
    audioQueue.subscribe({
      onQueueChanged: () => {
        this.processQueueIfNeeded()
      }
    })
  }

  private setupUserInteractionDetection(): void {
    const markInteraction = () => {
      if (!this.userHasInteracted) {
        this.userHasInteracted = true
        this.notifyListeners('onUserInteractionChanged', true)
        
        // Remove listeners after first interaction
        document.removeEventListener('click', markInteraction)
        document.removeEventListener('keydown', markInteraction)
        document.removeEventListener('touchstart', markInteraction)
      }
    }

    document.addEventListener('click', markInteraction)
    document.addEventListener('keydown', markInteraction)
    document.addEventListener('touchstart', markInteraction)
  }

  getState(): PlayerState {
    return this.state
  }

  getCurrentCall(): CallDto | null {
    return this.currentCall
  }

  isPlaying(): boolean {
    return this.isCurrentlyPlaying
  }

  hasUserInteracted(): boolean {
    return this.userHasInteracted
  }

  setUserInteracted(): void {
    if (!this.userHasInteracted) {
      this.userHasInteracted = true
      this.notifyListeners('onUserInteractionChanged', true)
    }
  }

  canAutoplay(): boolean {
    return this.userHasInteracted
  }

  async play(): Promise<void> {
    // Mark user interaction since they clicked play
    this.setUserInteracted()
    
    // Set state to playing
    this.setState('playing')
    
    // Start processing queue
    await this.processQueueIfNeeded()
  }

  pause(): void {
    this.setState('stopped')
    
    if (this.currentAudio) {
      this.currentAudio.pause()
    }
  }

  stop(): void {
    this.setState('stopped')
    
    if (this.currentAudio) {
      this.currentAudio.pause()
      this.currentAudio.currentTime = 0
    }
    
    this.setCurrentCall(null)
  }

  addToQueue(call: CallDto): void {
    audioQueue.add(call)
  }

  clearQueue(): void {
    audioQueue.clear()
  }

  removeFromQueue(callId: number): void {
    audioQueue.remove(callId)
  }

  moveToFront(callId: number): void {
    audioQueue.moveToFront(callId)
  }

  private async processQueueIfNeeded(): Promise<void> {
    if (this.isProcessingQueue || this.state !== 'playing') {
      return
    }

    this.isProcessingQueue = true

    try {
      while (audioQueue.getLength() > 0 && this.state === 'playing') {
        const nextCall = audioQueue.getNext()
        if (!nextCall) break

        try {
          await this.playCallDirectly(nextCall)
          await this.waitForAudioCompletion()
        } catch (error) {
          console.error('Error playing call:', error)
          
          if (error instanceof Error) {
            if (error.message.includes('autoplay')) {
              // Autoplay blocked - stop processing
              this.setState('stopped')
              break
            } else if (error.message.includes('No recordings available') || error.message.includes('Invalid recording ID')) {
              // Recording not ready yet - skip this call and continue
              console.warn(`Skipping call ${nextCall.id} - recordings not available yet`)
              continue
            }
          }
          
          // Other errors - continue to next call
          continue
        }

        // Small delay between calls
        await new Promise(resolve => setTimeout(resolve, 100))
      }
    } finally {
      this.isProcessingQueue = false
    }
  }

  private async playCallDirectly(call: CallDto): Promise<void> {
    if (!call.recordings?.length) {
      throw new Error('No recordings available')
    }

    const recording = call.recordings[0]
    if (!recording?.id) {
      throw new Error('Invalid recording ID')
    }

    // Clean up previous audio
    if (this.currentAudio) {
      this.currentAudio.pause()
      this.currentAudio.src = ''
    }

    // Set current call
    this.setCurrentCall(call)

    // Get audio URL with validation
    const recordingId = recording.id
    const audioUrl = `/api/recordings/${recordingId}/file`

    // Create new audio element
    this.currentAudio = new Audio(audioUrl)
    
    // Wait for audio to load
    await new Promise<void>((resolve, reject) => {
      const audio = this.currentAudio!
      
      const onLoad = () => {
        audio.removeEventListener('canplay', onLoad)
        audio.removeEventListener('error', onError)
        resolve()
      }
      
      const onError = (e: any) => {
        audio.removeEventListener('canplay', onLoad)
        audio.removeEventListener('error', onError)
        reject(new Error(`Failed to load audio: ${e.error?.message || 'Unknown error'}`))
      }
      
      audio.addEventListener('canplay', onLoad)
      audio.addEventListener('error', onError)
      
      if (audio.readyState >= 3) {
        onLoad()
      }
    })

    // Try to play
    try {
      await this.currentAudio.play()
      this.setPlaying(true)
    } catch (error: any) {
      if (error.name === 'NotAllowedError') {
        throw new Error('Autoplay blocked by browser')
      }
      throw error
    }
  }

  private async waitForAudioCompletion(): Promise<void> {
    if (!this.currentAudio) return

    return new Promise<void>((resolve) => {
      const audio = this.currentAudio!
      
      const onEnded = () => {
        audio.removeEventListener('ended', onEnded)
        audio.removeEventListener('pause', onPause)
        this.setPlaying(false)
        resolve()
      }
      
      const onPause = () => {
        // Only resolve if paused by user (not by ending)
        if (!audio.ended && this.state === 'stopped') {
          audio.removeEventListener('ended', onEnded)
          audio.removeEventListener('pause', onPause)
          this.setPlaying(false)
          resolve()
        }
      }
      
      audio.addEventListener('ended', onEnded)
      audio.addEventListener('pause', onPause)
      
      // If already ended, resolve immediately
      if (audio.ended) {
        onEnded()
      }
    })
  }

  private setState(newState: PlayerState): void {
    if (this.state !== newState) {
      this.state = newState
      this.notifyListeners('onStateChanged', newState)
    }
  }

  private setCurrentCall(call: CallDto | null): void {
    if (this.currentCall?.id !== call?.id) {
      this.currentCall = call
      this.notifyListeners('onCurrentCallChanged', call)
    }
  }

  private setPlaying(playing: boolean): void {
    if (this.isCurrentlyPlaying !== playing) {
      this.isCurrentlyPlaying = playing
      this.notifyListeners('onPlaybackChanged', playing)
    }
  }

  subscribe(listener: AudioPlayerListener): () => void {
    this.listeners.add(listener)
    
    // Immediately notify with current state
    listener.onStateChanged(this.state)
    listener.onCurrentCallChanged(this.currentCall)
    listener.onPlaybackChanged(this.isCurrentlyPlaying)
    listener.onUserInteractionChanged(this.userHasInteracted)
    
    return () => {
      this.listeners.delete(listener)
    }
  }

  private notifyListeners<K extends keyof AudioPlayerListener>(
    method: K,
    ...args: Parameters<AudioPlayerListener[K]>
  ): void {
    this.listeners.forEach(listener => {
      try {
        ;(listener[method] as any)(...args)
      } catch (error) {
        console.error(`Error in ${method} listener:`, error)
      }
    })
  }
}

// Singleton instance
export const audioPlayerService = new AudioPlayerServiceImpl()
