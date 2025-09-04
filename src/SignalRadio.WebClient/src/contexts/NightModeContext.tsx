import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react'

interface NightModeContextType {
  isNightMode: boolean
  toggleNightMode: () => void
}

const NightModeContext = createContext<NightModeContextType | null>(null)

interface NightModeProviderProps {
  children: ReactNode
}

export function NightModeProvider({ children }: NightModeProviderProps) {
  const [isNightMode, setIsNightMode] = useState(() => {
    // Try to get saved preference from localStorage
    const saved = localStorage.getItem('nightMode')
    if (saved !== null) {
      return JSON.parse(saved)
    }
    
    // Default to night mode between 10 PM and 6 AM
    const hour = new Date().getHours()
    return hour >= 22 || hour < 6
  })

  // Save to localStorage whenever night mode changes
  useEffect(() => {
    localStorage.setItem('nightMode', JSON.stringify(isNightMode))
    
    // Apply night mode class to document root
    if (isNightMode) {
      document.documentElement.classList.add('night-mode')
    } else {
      document.documentElement.classList.remove('night-mode')
    }
  }, [isNightMode])

  // Apply initial night mode class
  useEffect(() => {
    if (isNightMode) {
      document.documentElement.classList.add('night-mode')
    }
  }, [])

  const toggleNightMode = () => {
    setIsNightMode((prev: boolean) => !prev)
  }

  return (
    <NightModeContext.Provider value={{ isNightMode, toggleNightMode }}>
      {children}
    </NightModeContext.Provider>
  )
}

export function useNightMode() {
  const context = useContext(NightModeContext)
  if (!context) {
    throw new Error('useNightMode must be used within a NightModeProvider')
  }
  return context
}
