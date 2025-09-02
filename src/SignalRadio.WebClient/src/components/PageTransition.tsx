import React, { useState, useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import LoadingSpinner from './LoadingSpinner'

interface PageTransitionProps {
  children: React.ReactNode
}

export default function PageTransition({ children }: PageTransitionProps) {
  const [isLoading, setIsLoading] = useState(false)
  const location = useLocation()
  
  useEffect(() => {
    // Show loading for a brief moment on route change
    setIsLoading(true)
    const timer = setTimeout(() => {
      setIsLoading(false)
    }, 100) // Brief loading to show visual feedback
    
    return () => clearTimeout(timer)
  }, [location.pathname])
  
  if (isLoading) {
    return (
      <div className="page-transition-loading">
        <LoadingSpinner />
        <style>{`
          .page-transition-loading {
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 200px;
            padding: var(--space-4);
          }
        `}</style>
      </div>
    )
  }
  
  return <>{children}</>
}
