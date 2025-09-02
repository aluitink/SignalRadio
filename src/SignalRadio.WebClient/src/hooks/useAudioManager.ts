// Audio playback manager for SignalRadio
// Handles playing audio across the app and managing playback state

import type { CallDto } from '../types/dtos'

class AudioManager {
  private currentAudio: HTMLAudioElement | null = null
  private currentCallId: number | null = null
  private currentCall: CallDto | null = null  // Store the actual call data
  private listeners: Array<(callId: number | null, isPlaying: boolean) => void> = []
  private currentCallListeners: Array<(call: CallDto | null) => void> = []
  private autoplayListeners: Array<(enabled: boolean) => void> = []
  private queueListeners: Array<(queue: CallDto[]) => void> = []
  private playerStateListeners: Array<(state: 'stopped' | 'playing') => void> = []
  private autoplayEnabled: boolean = false
  private autoplayPermissionChecked: boolean = false
  private callQueue: CallDto[] = []
  private isProcessingQueue: boolean = false
  private silentAudio: HTMLAudioElement | null = null
  private keepAliveActive: boolean = false
  private volume: number = 50 // Default volume at 50%
  private userHasInteracted: boolean = false
  private progressUpdateInterval: number | null = null
  private playerState: 'stopped' | 'playing' = 'stopped' // Simple state: stopped (default) or playing
  private needsUserInteraction: boolean = true // Tracks if we need user interaction for audio

  constructor() {
    // Try to play silent audio on initialization to test autoplay
    this.testAutoplayOnInit()
  }

  private async testAutoplayOnInit(): Promise<void> {
    // Wait a bit for the page to fully load
    setTimeout(async () => {
      try {
        // Try to play the 500ms silence file to test autoplay permission
        const silentAudio = new Audio('/static/500-milliseconds-of-silence.mp3')
        silentAudio.muted = true
        silentAudio.volume = 0
        
        // Set up a timeout to catch silent failures
        const playPromise = Promise.race([
          silentAudio.play(),
          new Promise((_, reject) => 
            setTimeout(() => reject(new Error('Autoplay test timeout')), 3000)
          )
        ])
        
        await playPromise
        
        // If we get here, autoplay worked
        this.autoplayEnabled = true
        this.autoplayPermissionChecked = true
        this.userHasInteracted = true
        this.needsUserInteraction = false
        this.notifyAutoplayListeners(true)
        console.log('Autoplay permission check: ALLOWED (using silence file)')
        
        // Clean up the test audio
        silentAudio.pause()
        silentAudio.currentTime = 0
        
      } catch (error) {
        this.autoplayEnabled = false
        this.autoplayPermissionChecked = true
        this.needsUserInteraction = true
        this.notifyAutoplayListeners(false)
        console.log('Autoplay permission check: BLOCKED -', error instanceof Error ? error.message : 'Unknown error')
      }
    }, 500)
  }

  private async initializeAutoplayDetection(): Promise<void> {
    // Wait a bit for the page to fully load
    setTimeout(() => {
      this.checkAutoplayPermission()
    }, 500)
  }

  private getAudioUrl(call: CallDto): string {
    // Use the first recording's ID to build a consistent API endpoint
    // In development, Vite proxies /api to backend; in production, nginx handles /api routing
    if (call.recordings && call.recordings.length > 0) {
      const recordingId = call.recordings[0].id
      return `/api/recordings/${recordingId}/file`
    }
    throw new Error('No recordings available for this call')
  }

  async checkAutoplayPermission(): Promise<boolean> {
    // This method now just tests if autoplay would work
    try {
      const silentAudio = new Audio('/static/500-milliseconds-of-silence.mp3')
      silentAudio.muted = true
      silentAudio.volume = 0
      
      const playPromise = Promise.race([
        silentAudio.play(),
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Autoplay test timeout')), 3000)
        )
      ])
      
      await playPromise
      
      silentAudio.pause()
      silentAudio.currentTime = 0
      
      return true
    } catch (error) {
      console.log('Autoplay capability test failed:', error instanceof Error ? error.message : 'Unknown error')
      return false
    }
  }

  async enableAutoplayAndStartPlaying(): Promise<boolean> {
    try {
      // User clicked play - try to enable autoplay by playing silent audio
      const silentAudio = new Audio('/static/500-milliseconds-of-silence.mp3')
      silentAudio.muted = true
      silentAudio.volume = 0
      
      const playPromise = Promise.race([
        silentAudio.play(),
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Autoplay enable timeout')), 5000)
        )
      ])
      
      await playPromise
      
      // Success - enable autoplay and set player state to playing
      this.autoplayEnabled = true
      this.autoplayPermissionChecked = true
      this.userHasInteracted = true
      this.needsUserInteraction = false
      this.playerState = 'playing'
      
      this.notifyAutoplayListeners(true)
      this.notifyPlayerStateListeners('playing')
      console.log('Autoplay enabled - starting playback')
      
      // Clean up the test audio
      silentAudio.pause()
      silentAudio.currentTime = 0
      
      // Start processing queue if there are calls
      if (this.callQueue.length > 0) {
        console.log('Processing queued calls after enabling autoplay')
        this.processQueue()
      }
      
      return true
    } catch (error) {
      console.warn('Failed to enable autoplay:', error instanceof Error ? error.message : 'Unknown error')
      this.autoplayEnabled = false
      this.autoplayPermissionChecked = true
      this.needsUserInteraction = true
      this.notifyAutoplayListeners(false)
      return false
    }
  }

  // Simple getters for the new state management
  getPlayerState(): 'stopped' | 'playing' {
    return this.playerState
  }

  getNeedsUserInteraction(): boolean {
    return this.needsUserInteraction
  }

  // Toggle player state between stopped and playing
  togglePlayerState(): void {
    if (this.playerState === 'stopped') {
      // User wants to start playing - enable autoplay
      this.enableAutoplayAndStartPlaying()
    } else {
      // User wants to stop - pause everything and clear queue processing
      this.playerState = 'stopped'
      this.stop()
      this.isProcessingQueue = false
      this.notifyPlayerStateListeners('stopped')
      console.log('Player stopped - autoplay paused')
    }
  }

  getAutoplayEnabled(): boolean {
    return this.autoplayEnabled
  }

  addPlayerStateListener(listener: (state: 'stopped' | 'playing') => void): () => void {
    this.playerStateListeners.push(listener)
    return () => {
      const index = this.playerStateListeners.indexOf(listener)
      if (index > -1) {
        this.playerStateListeners.splice(index, 1)
      }
    }
  }

  private notifyPlayerStateListeners(state: 'stopped' | 'playing') {
    this.playerStateListeners.forEach(listener => {
      try {
        listener(state)
      } catch (error) {
        console.error('Error in player state listener:', error)
      }
    })
  }

  addAutoplayListener(listener: (enabled: boolean) => void): () => void {
    this.autoplayListeners.push(listener)
    return () => {
      const index = this.autoplayListeners.indexOf(listener)
      if (index > -1) {
        this.autoplayListeners.splice(index, 1)
      }
    }
  }

  addQueueListener(listener: (queue: CallDto[]) => void): () => void {
    this.queueListeners.push(listener)
    return () => {
      const index = this.queueListeners.indexOf(listener)
      if (index > -1) {
        this.queueListeners.splice(index, 1)
      }
    }
  }

  private notifyQueueListeners() {
    console.log('[AudioManager] Notifying queue listeners. Queue length:', this.callQueue.length)
    this.queueListeners.forEach(listener => {
      try {
        listener([...this.callQueue])
      } catch (error) {
        console.error('Error in queue listener:', error)
      }
    })
  }

  private createSilentAudio(): HTMLAudioElement {
    // Use the 500ms silence file from static assets for keep-alive functionality
    const audio = new Audio('/static/500-milliseconds-of-silence.mp3')
    audio.loop = true
    audio.volume = 0 // Completely silent
    audio.preload = 'auto'
    
    return audio
  }

  getAutoplayPermissionChecked(): boolean {
    return this.autoplayPermissionChecked
  }

  getUserHasInteracted(): boolean {
    return this.userHasInteracted
  }

  markUserInteraction(): void {
    if (!this.userHasInteracted) {
      this.userHasInteracted = true
      console.log('User interaction detected - audio now available')
    }
  }

  async testAutoplayWithSilence(): Promise<boolean> {
    try {
      console.log('Testing autoplay capability with silence file...')
      const testAudio = new Audio('/static/500-milliseconds-of-silence.mp3')
      testAudio.volume = 0
      testAudio.muted = true
      
      // Create a promise that rejects after a timeout
      const timeoutPromise = new Promise<never>((_, reject) => 
        setTimeout(() => reject(new Error('Autoplay test timeout after 3 seconds')), 3000)
      )
      
      // Race the play attempt against the timeout
      await Promise.race([testAudio.play(), timeoutPromise])
      
      // Clean up
      testAudio.pause()
      testAudio.currentTime = 0
      
      console.log('Autoplay test successful - audio is available')
      return true
    } catch (error) {
      console.log('Autoplay test failed:', error instanceof Error ? error.message : 'Unknown error')
      return false
    }
  }

  private async startKeepAlive() {
    if (this.keepAliveActive || !this.autoplayEnabled) {
      return
    }

    try {
      if (!this.silentAudio) {
        this.silentAudio = this.createSilentAudio()
      }

      await this.silentAudio.play()
      this.keepAliveActive = true
      console.log('Keep-alive silent audio started')
    } catch (error) {
      console.warn('Failed to start keep-alive audio:', error)
      this.keepAliveActive = false
    }
  }

  private stopKeepAlive() {
    if (!this.keepAliveActive || !this.silentAudio) {
      return
    }

    this.silentAudio.pause()
    this.silentAudio.currentTime = 0
    this.keepAliveActive = false
    console.log('Keep-alive silent audio stopped')
  }

  private notifyAutoplayListeners(enabled: boolean) {
    this.autoplayListeners.forEach(listener => {
      try {
        listener(enabled)
      } catch (error) {
        console.error('Error in autoplay listener:', error)
      }
    })
  }

  play(call: CallDto): Promise<void> {
    // All play requests now go through the queue
    this.queueCall(call)
    
    // If not currently processing queue, start processing
    if (!this.isProcessingQueue) {
      return this.processQueue().catch((error) => {
        // Handle autoplay errors gracefully
        if (error instanceof Error && error.message.includes('Autoplay blocked')) {
          console.log('Autoplay blocked, user will need to enable it manually')
          // Don't throw - let the UI handle showing the autoplay banner
          return
        }
        // Re-throw other errors
        throw error
      })
    }
    
    return Promise.resolve()
  }

  playFromSubscription(call: CallDto): Promise<void> {
    // This is called when subscribed calls come in via SignalR
    console.log('[AudioManager] playFromSubscription called:', {
      callId: call.id,
      talkGroupId: call.talkGroupId,
      playerState: this.playerState,
      autoplayEnabled: this.autoplayEnabled,
      willAutoPlay: this.playerState === 'playing' && this.autoplayEnabled,
      queueLength: this.callQueue.length,
      hasRecordings: call.recordings?.length || 0
    })
    
    // Always queue the call first
    this.queueCall(call)
    console.log('[AudioManager] Call queued. New queue length:', this.callQueue.length)
    
    // Only auto-play if player state is 'playing' and autoplay is enabled
    if (this.playerState === 'playing' && this.autoplayEnabled) {
      console.log('[AudioManager] Auto-playing subscribed call')
      // Start processing queue if not already processing
      if (!this.isProcessingQueue) {
        return this.processQueue().catch((error) => {
          console.error('[AudioManager] Error processing queue after subscribed call:', error)
          return Promise.resolve()
        })
      }
      return Promise.resolve()
    } else {
      console.log('[AudioManager] Call queued but not auto-playing - player is stopped or autoplay not available')
      return Promise.resolve()
    }
  }

  // Add a call to the queue for auto-play (used by CallStream for new calls)
  addToAutoPlayQueue(call: CallDto): void {
    // Only add to queue if player is in 'playing' state (auto-play is active)
    if (this.playerState === 'playing' && this.autoplayEnabled) {
      this.queueCall(call)
      
      // Start processing queue if not already processing
      if (!this.isProcessingQueue) {
        this.processQueue().catch((error) => {
          console.error('Error processing queue after adding call:', error)
        })
      }
    } else {
      // If auto-play is not active, just add to queue without processing
      this.queueCall(call)
    }
  }

  private queueCall(call: CallDto): void {
    console.log('[AudioManager] queueCall called:', {
      callId: call.id,
      talkGroupId: call.talkGroupId,
      currentQueueLength: this.callQueue.length,
      currentCallId: this.currentCallId
    })
    
    const alreadyQueued = this.callQueue.some(queuedCall => queuedCall.id === call.id)
    if (alreadyQueued) {
      console.log('[AudioManager] Call already queued, skipping:', call.id)
      return
    }

    // Check if call is currently playing
    if (this.currentCallId === call.id) {
      console.log('[AudioManager] Call is currently playing, skipping:', call.id)
      return
    }

    this.callQueue.push(call)
    console.log('[AudioManager] Call added to queue:', {
      callId: call.id,
      newQueueLength: this.callQueue.length,
      queueIds: this.callQueue.map(c => c.id)
    })
    this.notifyQueueListeners()
  }

  private async processQueue(): Promise<void> {
    if (this.isProcessingQueue || this.callQueue.length === 0 || this.playerState !== 'playing') {
      return
    }

    this.isProcessingQueue = true

    try {
      while (this.callQueue.length > 0 && this.playerState === 'playing') {
        const call = this.callQueue.shift()!
        this.notifyQueueListeners()

        try {
          await this.playCallDirectly(call)
        
        // Wait for the audio to finish playing
        if (this.currentAudio) {
          await new Promise<void>((resolve) => {
            const audio = this.currentAudio!
            
            const onEnded = () => {
              audio.removeEventListener('ended', onEnded)
              audio.removeEventListener('error', onError)
              audio.removeEventListener('pause', onPaused)
              resolve()
            }
            
            const onError = () => {
              audio.removeEventListener('ended', onEnded)
              audio.removeEventListener('error', onError)
              audio.removeEventListener('pause', onPaused)
              console.error('Audio error during playback')
              resolve()
            }

            const onPaused = () => {
              // Only resolve if audio was paused by user, not by ending
              if (!audio.ended) {
                audio.removeEventListener('ended', onEnded)
                audio.removeEventListener('error', onError)
                audio.removeEventListener('pause', onPaused)
                resolve()
              }
            }
            
            audio.addEventListener('ended', onEnded)
            audio.addEventListener('error', onError)
            audio.addEventListener('pause', onPaused)
            
            // If audio has already ended, resolve immediately
            if (audio.ended) {
              onEnded()
            }
          })
        }

        // Small delay between calls for better UX
        await new Promise(resolve => setTimeout(resolve, 100))
        
      } catch (error) {
        console.warn('Error playing queued call:', error)
        
        // Check if this is an autoplay policy error
        if (error instanceof Error && error.message.includes('Autoplay blocked')) {
          // For autoplay errors, stop processing the queue and set player to stopped
          console.log('Autoplay blocked, stopping queue processing. Player set to stopped.')
          this.playerState = 'stopped'
          this.notifyPlayerStateListeners('stopped')
          break
        }
        
        // For other errors, continue to next call in queue
        console.error('Non-autoplay error, continuing to next call:', error)
        }
      }
    } catch (unexpectedError) {
      console.error('Unexpected error in processQueue:', unexpectedError)
    } finally {
      this.isProcessingQueue = false
    }
  }

  private playCallDirectly(call: CallDto): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        const audioUrl = this.getAudioUrl(call)

        // Stop current audio if playing
        if (this.currentAudio) {
          this.currentAudio.pause()
          this.currentAudio = null
          if (this.progressUpdateInterval) {
            clearInterval(this.progressUpdateInterval)
            this.progressUpdateInterval = null
          }
        }

        const audio = new Audio(audioUrl)
        
        // Set volume immediately
        audio.volume = this.volume / 100
        
        // Set up event listeners before any async operations
        audio.addEventListener('loadstart', () => {
          this.currentAudio = audio
          this.currentCallId = call.id
          this.currentCall = call  // Store the call data
          this.notifyListeners(call.id, false) // Not playing yet, just loading
          this.notifyCurrentCallListeners(call) // Notify about current call change
        })

        audio.addEventListener('play', () => {
          this.notifyListeners(call.id, true)
          
          // Start progress updates
          this.progressUpdateInterval = window.setInterval(() => {
            if (audio && !audio.paused && !audio.ended) {
              // Could emit progress events here if needed
            }
          }, 1000)
        })

        audio.addEventListener('pause', () => {
          this.notifyListeners(this.currentCallId, false)
          if (this.progressUpdateInterval) {
            clearInterval(this.progressUpdateInterval)
            this.progressUpdateInterval = null
          }
        })

        audio.addEventListener('ended', () => {
          // Don't call stop() here - let the processQueue handle the end
          this.notifyListeners(this.currentCallId, false)
        })

        audio.addEventListener('error', (e) => {
          const audioElement = e.target as HTMLAudioElement
          let errorMessage = 'Audio playback failed'
          
          if (audioElement?.error) {
            switch (audioElement.error.code) {
              case MediaError.MEDIA_ERR_ABORTED:
                errorMessage = 'Audio playback was aborted'
                break
              case MediaError.MEDIA_ERR_NETWORK:
                errorMessage = 'Network error while loading audio'
                break
              case MediaError.MEDIA_ERR_DECODE:
                errorMessage = 'Audio file is corrupted or unsupported'
                break
              case MediaError.MEDIA_ERR_SRC_NOT_SUPPORTED:
                errorMessage = 'Audio format not supported or file not found'
                break
              default:
                errorMessage = 'Unknown audio error occurred'
            }
          }
          
          console.error('Audio play failed:', {
            callId: call.id,
            recordingId: call.recordings?.[0]?.id,
            audioUrl,
            error: audioElement?.error,
            message: errorMessage
          })
          this.stop()
          reject(new Error(errorMessage))
        })

        audio.addEventListener('canplay', () => {
          resolve()
        })

        // Additional error handling for load failures
        audio.addEventListener('abort', () => {
          console.warn('Audio loading aborted:', { callId: call.id, audioUrl })
          this.stop()
          reject(new Error('Audio loading was aborted'))
        })

        audio.addEventListener('stalled', () => {
          console.warn('Audio loading stalled:', { callId: call.id, audioUrl })
        })

        // Try to play the audio
        audio.play().catch((playError) => {
          console.error('Audio.play() failed:', {
            callId: call.id,
            recordingId: call.recordings?.[0]?.id,
            audioUrl,
            error: playError,
            errorName: playError.name,
            errorMessage: playError.message
          })
          
          // Check if this is an autoplay policy error
          if (playError.name === 'NotAllowedError') {
            this.autoplayEnabled = false
            this.notifyAutoplayListeners(false)
            this.stop()
            reject(new Error('Autoplay blocked by browser. Please click to enable audio playback.'))
          } else if (playError.name === 'AbortError') {
            this.stop()
            reject(new Error('Audio playback was interrupted.'))
          } else if (playError.name === 'NotSupportedError') {
            this.stop()
            reject(new Error('This audio format is not supported by your browser.'))
          } else {
            this.stop()
            reject(new Error(`Audio play failed: ${playError.message || 'Unknown error'}`))
          }
        })
      } catch (error) {
        console.error('Audio setup failed:', {
          callId: call.id,
          error
        })
        reject(error)
      }
    })
  }

  stop() {
    if (this.currentAudio) {
      this.currentAudio.pause()
      this.currentAudio = null
    }

    if (this.progressUpdateInterval) {
      clearInterval(this.progressUpdateInterval)
      this.progressUpdateInterval = null
    }
    
    const wasPlaying = this.currentCallId !== null
    this.currentCallId = null
    this.currentCall = null  // Clear the call data
    
    if (wasPlaying) {
      this.notifyListeners(null, false)
      this.notifyCurrentCallListeners(null) // Notify about current call clearing
    }
  }

  pause() {
    if (this.currentAudio && !this.currentAudio.paused) {
      this.currentAudio.pause()
    }
  }

  resume() {
    if (this.currentAudio && this.currentAudio.paused && this.userHasInteracted) {
      this.currentAudio.play().catch(error => {
        console.error('Failed to resume audio:', error)
        if (error.name === 'NotAllowedError') {
          this.autoplayEnabled = false
          this.notifyAutoplayListeners(false)
        }
      })
    }
  }

  togglePlayback() {
    if (!this.currentAudio) {
      // No audio loaded, try to play first item in queue
      if (this.callQueue.length > 0) {
        this.play(this.callQueue[0])
      }
      return
    }

    if (this.currentAudio.paused) {
      this.resume()
    } else {
      this.pause()
    }
  }

  clearQueue() {
    this.callQueue = []
    this.notifyQueueListeners()
  }

  removeFromQueue(callId: number) {
    const index = this.callQueue.findIndex(call => call.id === callId)
    if (index > -1) {
      this.callQueue.splice(index, 1)
      this.notifyQueueListeners()
    }
  }

  moveToFront(callId: number) {
    const index = this.callQueue.findIndex(call => call.id === callId)
    if (index > 0) { // Don't move if already at front (index 0) or not found (-1)
      const call = this.callQueue.splice(index, 1)[0]
      this.callQueue.unshift(call)
      this.notifyQueueListeners()
    }
  }

  setVolume(volume: number) {
    this.volume = Math.max(0, Math.min(100, volume)) // Clamp between 0-100
    
    // Apply to current audio
    if (this.currentAudio) {
      this.currentAudio.volume = this.volume / 100
    }
    
    // Apply to silent audio for keep-alive
    if (this.silentAudio) {
      this.silentAudio.volume = 0 // Keep silent audio at 0
    }
  }

  getVolume(): number {
    return this.volume
  }

  getQueue(): CallDto[] {
    return [...this.callQueue]
  }

  getQueueLength(): number {
    return this.callQueue.length
  }

  isPlaying(callId: number): boolean {
    return this.currentCallId === callId && 
           this.currentAudio !== null && 
           !this.currentAudio.paused && 
           !this.currentAudio.ended
  }

  getCurrentCallId(): number | null {
    return this.currentCallId
  }

  getCurrentCall(): CallDto | null {
    return this.currentCall
  }

  addListener(listener: (callId: number | null, isPlaying: boolean) => void) {
    this.listeners.push(listener)
    
    // Return cleanup function
    return () => {
      const index = this.listeners.indexOf(listener)
      if (index > -1) {
        this.listeners.splice(index, 1)
      }
    }
  }

  addCurrentCallListener(listener: (call: CallDto | null) => void): () => void {
    this.currentCallListeners.push(listener)
    
    // Return cleanup function
    return () => {
      const index = this.currentCallListeners.indexOf(listener)
      if (index > -1) {
        this.currentCallListeners.splice(index, 1)
      }
    }
  }

  private notifyListeners(callId: number | null, isPlaying: boolean) {
    this.listeners.forEach(listener => {
      try {
        listener(callId, isPlaying)
      } catch (error) {
        console.error('Error in audio manager listener:', error)
      }
    })
  }

  private notifyCurrentCallListeners(call: CallDto | null) {
    this.currentCallListeners.forEach(listener => {
      try {
        listener(call)
      } catch (error) {
        console.error('Error in current call listener:', error)
      }
    })
  }

  cleanup() {
    this.stopKeepAlive()
    if (this.silentAudio) {
      this.silentAudio = null
    }
    if (this.currentAudio) {
      this.currentAudio.pause()
      this.currentAudio = null
    }
    if (this.progressUpdateInterval) {
      clearInterval(this.progressUpdateInterval)
      this.progressUpdateInterval = null
    }
    
    // Clear all listeners
    this.listeners = []
    this.currentCallListeners = []
    this.autoplayListeners = []
    this.queueListeners = []
  }
}

export const audioManager = new AudioManager()

// React hook for using the audio manager
import { useEffect, useState, useCallback } from 'react'

export function useAudioManager() {
  const [currentCallId, setCurrentCallId] = useState<number | null>(
    audioManager.getCurrentCallId()
  )
  const [currentCall, setCurrentCall] = useState<CallDto | null>(
    audioManager.getCurrentCall()
  )
  const [isPlaying, setIsPlaying] = useState(false)
  const [autoplayEnabled, setAutoplayEnabled] = useState(audioManager.getAutoplayEnabled())
  const [autoplayChecked, setAutoplayChecked] = useState(audioManager.getAutoplayPermissionChecked())
  const [playerState, setPlayerState] = useState<'stopped' | 'playing'>(audioManager.getPlayerState())
  const [needsUserInteraction, setNeedsUserInteraction] = useState(audioManager.getNeedsUserInteraction())
  const [queue, setQueue] = useState<CallDto[]>(audioManager.getQueue())

  useEffect(() => {
    const cleanup = audioManager.addListener((callId, playing) => {
      setCurrentCallId(callId)
      setIsPlaying(playing)
    })

    const currentCallCleanup = audioManager.addCurrentCallListener((call) => {
      setCurrentCall(call)
    })

    const autoplayCleanup = audioManager.addAutoplayListener((enabled) => {
      setAutoplayEnabled(enabled)
      setAutoplayChecked(true)
    })

    const playerStateCleanup = audioManager.addPlayerStateListener((state) => {
      setPlayerState(state)
    })

    const queueCleanup = audioManager.addQueueListener((newQueue) => {
      setQueue(newQueue)
    })

    // Setup user interaction detection
    const handleUserInteraction = () => {
      audioManager.markUserInteraction()
      setNeedsUserInteraction(false)
      document.removeEventListener('click', handleUserInteraction)
      document.removeEventListener('keydown', handleUserInteraction)
      document.removeEventListener('touchstart', handleUserInteraction)
    }

    document.addEventListener('click', handleUserInteraction)
    document.addEventListener('keydown', handleUserInteraction)
    document.addEventListener('touchstart', handleUserInteraction)

    return () => {
      cleanup()
      currentCallCleanup()
      autoplayCleanup()
      playerStateCleanup()
      queueCleanup()
      document.removeEventListener('click', handleUserInteraction)
      document.removeEventListener('keydown', handleUserInteraction)
      document.removeEventListener('touchstart', handleUserInteraction)
    }
  }, [])

  const playCall = async (call: CallDto) => {
    try {
      // Mark user interaction for manual play requests
      audioManager.markUserInteraction()
      setNeedsUserInteraction(false)
      
      await audioManager.play(call)
    } catch (error) {
      console.error('Failed to play audio:', error)
      throw error // Re-throw so components can handle it
    }
  }

  const playFromSubscription = useCallback(async (call: CallDto) => {
    try {
      await audioManager.playFromSubscription(call)
    } catch (error) {
      console.error('Failed to play subscribed call:', error)
      throw error
    }
  }, [])

  const stopPlayback = () => {
    audioManager.stop()
  }

  const pausePlayback = () => {
    audioManager.pause()
  }

  const resumePlayback = () => {
    audioManager.resume()
  }

  const togglePlayback = () => {
    audioManager.togglePlayback()
  }

  const togglePlayerState = () => {
    audioManager.togglePlayerState()
  }

  const enableAutoplayAndStartPlaying = async () => {
    return await audioManager.enableAutoplayAndStartPlaying()
  }

  const clearQueue = () => {
    audioManager.clearQueue()
  }

  const removeFromQueue = (callId: number) => {
    audioManager.removeFromQueue(callId)
  }

  const moveToFront = (callId: number) => {
    audioManager.moveToFront(callId)
  }

  const isCallPlaying = (callId: number) => {
    return audioManager.isPlaying(callId)
  }

  const checkAutoplayPermission = async () => {
    return await audioManager.checkAutoplayPermission()
  }

  const setVolume = (volume: number) => {
    audioManager.setVolume(volume)
  }

  const getVolume = () => {
    return audioManager.getVolume()
  }

  const getUserHasInteracted = () => {
    return audioManager.getUserHasInteracted()
  }

  const testAutoplayWithSilence = async () => {
    return await audioManager.testAutoplayWithSilence()
  }

  const addToAutoPlayQueue = useCallback((call: CallDto) => {
    audioManager.addToAutoPlayQueue(call)
  }, [])

  return {
    currentCallId,
    currentCall,
    isPlaying,
    playCall,
    playFromSubscription,
    stopPlayback,
    pausePlayback,
    resumePlayback,
    togglePlayback,
    togglePlayerState,
    enableAutoplayAndStartPlaying,
    isCallPlaying,
    autoplayEnabled,
    autoplayChecked,
    playerState,
    needsUserInteraction,
    checkAutoplayPermission,
    testAutoplayWithSilence,
    addToAutoPlayQueue,
    queue,
    clearQueue,
    removeFromQueue,
    moveToFront,
    queueLength: queue.length,
    setVolume,
    getVolume,
    getUserHasInteracted
  }
}
