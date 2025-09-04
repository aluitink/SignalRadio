import React from 'react'
import { useNightMode } from '../contexts/NightModeContext'

interface NightModeToggleProps {
  className?: string
  showLabel?: boolean
}

export default function NightModeToggle({ 
  className = '', 
  showLabel = true 
}: NightModeToggleProps) {
  const { isNightMode, toggleNightMode } = useNightMode()

  return (
    <button
      className={`night-mode-toggle ${className}`}
      onClick={toggleNightMode}
      aria-label={isNightMode ? 'Disable night mode' : 'Enable night mode'}
      title={isNightMode ? 'Disable night mode' : 'Enable night mode'}
    >
      <span className="toggle-icon">
        {isNightMode ? '🌙' : '☀️'}
      </span>
      {showLabel && (
        <span className="toggle-label">
          {isNightMode ? 'Night' : 'Day'}
        </span>
      )}

      <style>{`
        .night-mode-toggle {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          background: none;
          border: 1px solid var(--border);
          color: var(--text-secondary);
          padding: var(--space-1) var(--space-2);
          border-radius: var(--radius-sm);
          cursor: pointer;
          transition: var(--transition);
          font-size: var(--font-size-sm);
        }

        .night-mode-toggle:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
        }

        .toggle-icon {
          font-size: 16px;
          line-height: 1;
        }

        .toggle-label {
          font-weight: 500;
          white-space: nowrap;
        }

        /* Mobile adjustments */
        @media (max-width: 767px) {
          .night-mode-toggle {
            padding: var(--space-1);
            min-width: 36px;
            min-height: 36px;
            justify-content: center;
          }
          
          .toggle-label {
            display: none;
          }
        }
      `}</style>
    </button>
  )
}
