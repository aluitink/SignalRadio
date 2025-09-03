import React from 'react'
import { useWakeLockContext } from '../contexts/WakeLockContext'

interface WakeLockIndicatorProps {
  showLabel?: boolean
  className?: string
}

/**
 * Component that shows the current wake lock status and allows manual control
 */
export default function WakeLockIndicator({ 
  showLabel = false, 
  className = '' 
}: WakeLockIndicatorProps) {
  const { isSupported, isActive, error, toggleWakeLock } = useWakeLockContext()

  if (!isSupported) {
    return null // Don't show anything if not supported
  }

  const handleClick = async () => {
    try {
      await toggleWakeLock()
    } catch (error) {
      console.error('Failed to toggle wake lock:', error)
    }
  }

  return (
    <button
      onClick={handleClick}
      className={`wake-lock-indicator ${className}`}
      title={
        error 
          ? `Wake lock error: ${error.message}` 
          : isActive 
            ? 'Screen wake lock is active (click to disable)' 
            : 'Screen wake lock is inactive (click to enable)'
      }
      aria-label={
        isActive 
          ? 'Disable screen wake lock' 
          : 'Enable screen wake lock'
      }
    >
      <div className={`wake-lock-toggle ${error ? 'error' : isActive ? 'active' : 'inactive'}`}>
        <div className="toggle-slider"></div>
      </div>
      {showLabel && (
        <span className="wake-lock-label">
          {error ? 'Error' : isActive ? 'Screen Locked' : 'Screen Unlocked'}
        </span>
      )}
      
      <style>{`
        .wake-lock-indicator {
          display: inline-flex;
          align-items: center;
          gap: var(--space-2);
          background: none;
          border: none;
          color: inherit;
          cursor: pointer;
          padding: var(--space-2);
          border-radius: 4px;
          transition: background-color 0.2s ease;
          font-size: 14px;
        }

        .wake-lock-indicator:hover {
          background-color: rgba(255, 255, 255, 0.1);
        }

        .wake-lock-indicator:focus {
          outline: 2px solid var(--color-primary);
          outline-offset: 2px;
        }

        .wake-lock-toggle {
          width: 32px;
          height: 18px;
          background: rgba(255, 255, 255, 0.2);
          border-radius: 12px;
          position: relative;
          transition: all 0.3s ease;
          border: 1px solid rgba(255, 255, 255, 0.3);
        }

        .wake-lock-toggle.active {
          background: var(--accent-primary, #3b82f6);
          border-color: var(--accent-primary, #3b82f6);
        }

        .wake-lock-toggle.error {
          background: #ef4444;
          border-color: #ef4444;
        }

        .toggle-slider {
          width: 14px;
          height: 14px;
          background: white;
          border-radius: 50%;
          position: absolute;
          top: 50%;
          transform: translateY(-50%);
          left: 2px;
          transition: all 0.3s ease;
          box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
        }

        .wake-lock-toggle.active .toggle-slider {
          left: 16px;
        }

        .wake-lock-toggle.error .toggle-slider {
          background: rgba(255, 255, 255, 0.9);
        }

        .wake-lock-label {
          font-size: 12px;
          font-weight: 500;
          white-space: nowrap;
        }

        /* Hide on desktop by default */
        @media (min-width: 768px) {
          .wake-lock-indicator {
            display: none;
          }
        }

        /* Mobile adjustments - only show on mobile */
        @media (max-width: 767px) {
          .wake-lock-indicator {
            padding: var(--space-1);
            min-width: 36px;
            min-height: 36px;
            justify-content: center;
            display: flex !important; /* Ensure the button is always visible on mobile */
          }
          
          .wake-lock-label {
            display: none; /* Hide label on mobile to save space */
          }

          .wake-lock-toggle {
            width: 36px;
            height: 20px;
            display: block !important; /* Ensure toggle is always shown on mobile */
          }

          .wake-lock-toggle .toggle-slider {
            width: 16px;
            height: 16px;
            left: 2px;
          }

          .wake-lock-toggle.active .toggle-slider {
            left: 18px;
          }
        }

        /* When used in navigation */
        .nav-wake-lock.wake-lock-indicator {
          padding: var(--space-1);
          background: rgba(255, 255, 255, 0.05);
          border-radius: 6px;
        }

        .nav-wake-lock.wake-lock-indicator:hover {
          background: rgba(255, 255, 255, 0.15);
        }

        @media (max-width: 767px) {
          .nav-wake-lock.wake-lock-indicator {
            min-width: 32px;
            min-height: 32px;
            padding: var(--space-1);
            position: relative;
            z-index: 102; /* Stay above other nav elements */
            display: flex !important;
            align-items: center;
            justify-content: center;
          }
          
          /* Ensure visibility in fullscreen */
          .nav-wake-lock.wake-lock-indicator .wake-lock-toggle {
            display: block !important;
            width: 32px;
            height: 18px;
          }

          .nav-wake-lock .wake-lock-toggle .toggle-slider {
            width: 14px;
            height: 14px;
            left: 2px;
          }

          .nav-wake-lock .wake-lock-toggle.active .toggle-slider {
            left: 16px;
          }
        }
      `}</style>
    </button>
  )
}
