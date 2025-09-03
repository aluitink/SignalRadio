import React, { createContext, useContext, useEffect, useRef, useState, ReactNode } from 'react'

interface WakeLockState {
  isSupported: boolean
  isActive: boolean
  error: Error | null
}

interface WakeLockContextType extends WakeLockState {
  requestWakeLock: () => Promise<void>
  releaseWakeLock: () => Promise<void>
  toggleWakeLock: () => Promise<void>
}

const WakeLockContext = createContext<WakeLockContextType | null>(null)

interface WakeLockProviderProps {
  children: ReactNode
}

export function WakeLockProvider({ children }: WakeLockProviderProps) {
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
      
      if (import.meta.env.DEV) {
        console.log('🔒 Screen wake lock activated')
      }
    } catch (error) {
      setState(prev => ({ 
        ...prev, 
        error: error instanceof Error ? error : new Error('Failed to request wake lock') 
      }))
      
      if (import.meta.env.DEV) {
        console.error('Failed to request wake lock:', error)
      }
    }
  }

  const releaseWakeLock = async () => {
    if (wakeLockRef.current) {
      try {
        await wakeLockRef.current.release()
        wakeLockRef.current = null
        setState(prev => ({ ...prev, isActive: false, error: null }))
        
        if (import.meta.env.DEV) {
          console.log('🔓 Screen wake lock released')
        }
      } catch (error) {
        setState(prev => ({ 
          ...prev, 
          error: error instanceof Error ? error : new Error('Failed to release wake lock') 
        }))
        
        if (import.meta.env.DEV) {
          console.error('Failed to release wake lock:', error)
        }
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

  // Debug logging
  useEffect(() => {
    if (import.meta.env.DEV) {
      if (!state.isSupported) {
        console.warn('⚠️ Screen Wake Lock API is not supported in this browser')
      } else {
        console.log(`🔒 Screen wake lock: ${state.isActive ? 'active' : 'inactive'}`)
      }
      
      if (state.error) {
        console.error('❌ Wake lock error:', state.error.message)
      }
    }
  }, [state.isSupported, state.isActive, state.error])

  const contextValue: WakeLockContextType = {
    ...state,
    requestWakeLock,
    releaseWakeLock,
    toggleWakeLock
  }

  return (
    <WakeLockContext.Provider value={contextValue}>
      {children}
    </WakeLockContext.Provider>
  )
}

export function useWakeLockContext() {
  const context = useContext(WakeLockContext)
  if (!context) {
    throw new Error('useWakeLockContext must be used within a WakeLockProvider')
  }
  return context
}
