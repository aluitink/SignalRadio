import { useEffect, useState } from 'react'
import { audioPlayerService, type PlayerState } from '../services/AudioPlayerService'
import { audioQueue } from '../services/AudioQueue'
import { callStreamService } from '../services/CallStreamService'
import type { CallDto } from '../types/dtos'

export interface UnifiedAudioState {
  // Player state
  playerState: PlayerState
  currentCall: CallDto | null
  isPlaying: boolean
  hasUserInteracted: boolean
  
  // Queue state
  queue: CallDto[]
  queueLength: number
  
  // Call stream state
  calls: CallDto[]
  
  // Actions
  playPause: () => Promise<void>
  stop: () => void
  clearQueue: () => void
  removeFromQueue: (callId: number) => void
  moveToFront: (callId: number) => void
  playCall: (call: CallDto) => void
}

export function useUnifiedAudio(): UnifiedAudioState {
  // Player state
  const [playerState, setPlayerState] = useState<PlayerState>('stopped')
  const [currentCall, setCurrentCall] = useState<CallDto | null>(null)
  const [isPlaying, setIsPlaying] = useState(false)
  const [hasUserInteracted, setHasUserInteracted] = useState(false)
  
  // Queue state
  const [queue, setQueue] = useState<CallDto[]>([])
  
  // Call stream state
  const [calls, setCalls] = useState<CallDto[]>([])

  // Subscribe to audio player service
  useEffect(() => {
    const unsubscribe = audioPlayerService.subscribe({
      onStateChanged: setPlayerState,
      onCurrentCallChanged: setCurrentCall,
      onPlaybackChanged: setIsPlaying,
      onUserInteractionChanged: setHasUserInteracted
    })

    return unsubscribe
  }, [])

  // Subscribe to queue changes
  useEffect(() => {
    const unsubscribe = audioQueue.subscribe({
      onQueueChanged: setQueue
    })

    return unsubscribe
  }, [])

  // Subscribe to call stream changes
  useEffect(() => {
    const unsubscribe = callStreamService.subscribe({
      onCallsChanged: setCalls,
      onNewCall: () => {
        // New call handling is done in the stream service
      },
      onCallUpdated: () => {
        // Call update handling is done in the stream service
      }
    })

    return unsubscribe
  }, [])

  // Actions
  const playPause = async (): Promise<void> => {
    if (playerState === 'stopped') {
      await audioPlayerService.play()
    } else {
      audioPlayerService.pause()
    }
  }

  const stop = (): void => {
    audioPlayerService.stop()
  }

  const clearQueue = (): void => {
    audioPlayerService.clearQueue()
  }

  const removeFromQueue = (callId: number): void => {
    audioPlayerService.removeFromQueue(callId)
  }

  const moveToFront = (callId: number): void => {
    audioPlayerService.moveToFront(callId)
  }

  const playCall = (call: CallDto): void => {
    audioPlayerService.addToQueue(call)
    
    // If player is stopped, start it
    if (playerState === 'stopped') {
      audioPlayerService.play().catch(error => {
        console.error('Failed to start player:', error)
      })
    }
  }

  return {
    // State
    playerState,
    currentCall,
    isPlaying,
    hasUserInteracted,
    queue,
    queueLength: queue.length,
    calls,
    
    // Actions
    playPause,
    stop,
    clearQueue,
    removeFromQueue,
    moveToFront,
    playCall
  }
}
