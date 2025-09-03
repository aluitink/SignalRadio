import { useEffect, useRef, useState } from 'react'

interface WakeLockState {
  isSupported: boolean
  isActive: boolean
  error: Error | null
}

/**
 * Custom hook to manage Screen Wake Lock API
 * Keeps the screen awake while the app is active
 */
export function useWakeLock() {
  const [state, setState] = useState<WakeLockState>({
    isSupported: 'wakeLock' in navigator,
    isActive: false,
    error: null
  })
  
  const wakeLockRef = useRef<WakeLockSentinel | null>(null)

  const requestWakeLock = async () => {
    if (!state.isSupported) {
      setState(prev => ({ 
        ...prev, 
        error: new Error('Screen Wake Lock API is not supported in this browser') 
      }))
      return
    }

    try {
      wakeLockRef.current = await navigator.wakeLock.request('screen')
      
      wakeLockRef.current.addEventListener('release', () => {
        setState(prev => ({ ...prev, isActive: false }))
      })

      setState(prev => ({ ...prev, isActive: true, error: null }))
      console.log('🔒 Screen wake lock activated')
    } catch (error) {
      setState(prev => ({ 
        ...prev, 
        error: error instanceof Error ? error : new Error('Failed to request wake lock') 
      }))
      console.error('Failed to request wake lock:', error)
    }
  }

  const releaseWakeLock = async () => {
    if (wakeLockRef.current) {
      try {
        await wakeLockRef.current.release()
        wakeLockRef.current = null
        setState(prev => ({ ...prev, isActive: false }))
        console.log('🔓 Screen wake lock released')
      } catch (error) {
        console.error('Failed to release wake lock:', error)
      }
    }
  }

  const toggleWakeLock = async () => {
    if (state.isActive) {
      await releaseWakeLock()
    } else {
      await requestWakeLock()
    }
  }

  // Handle visibility change - re-request wake lock when page becomes visible
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible' && state.isSupported && !state.isActive) {
        // Small delay to ensure the page is fully visible
        setTimeout(() => {
          if (document.visibilityState === 'visible') {
            requestWakeLock()
          }
        }, 100)
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)
    
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [state.isSupported, state.isActive])

  // Request wake lock on mount if supported
  useEffect(() => {
    if (state.isSupported) {
      requestWakeLock()
    }

    // Cleanup on unmount
    return () => {
      if (wakeLockRef.current) {
        releaseWakeLock()
      }
    }
  }, [])

  return {
    ...state,
    requestWakeLock,
    releaseWakeLock,
    toggleWakeLock
  }
}
