import React, { useState, useEffect } from 'react'
import CallCard from './CallCard'
import { CallCardSkeleton } from './LoadingSpinner'
import type { CallDto } from '../types/dtos'
import { apiGet } from '../api'

interface FrequencyTabsProps {
  talkGroupId: number
  limit?: number
}

interface FrequencyData {
  [frequency: string]: CallDto[]
}

export default function FrequencyTabs({ talkGroupId, limit = 50 }: FrequencyTabsProps) {
  const [frequencyData, setFrequencyData] = useState<FrequencyData>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeFrequency, setActiveFrequency] = useState<string | null>(null)

  useEffect(() => {
    loadFrequencyData()
  }, [talkGroupId, limit])

  const loadFrequencyData = async () => {
    setLoading(true)
    setError(null)

    try {
      const data = await apiGet<FrequencyData>(`/talkgroups/${talkGroupId}/calls-by-frequency?limit=${limit}`)
      
      if (data && Object.keys(data).length > 0) {
        setFrequencyData(data)
        // Set the first (most active) frequency as active
        const firstFreq = Object.keys(data)[0]
        setActiveFrequency(firstFreq)
      } else {
        setFrequencyData({})
        setActiveFrequency(null)
      }
    } catch (err) {
      console.error('Failed to load frequency data:', err)
      setError('Failed to load frequency data')
    } finally {
      setLoading(false)
    }
  }

  const formatFrequency = (freqHz: string): string => {
    const freq = parseFloat(freqHz)
    return `${(freq / 1000000).toFixed(3)} MHz`
  }

  const getFrequencyColor = (index: number): string => {
    const colors = [
      '#3b82f6', // blue
      '#10b981', // emerald
      '#f59e0b', // amber
      '#ef4444', // red
      '#8b5cf6', // violet
      '#06b6d4', // cyan
      '#84cc16', // lime
      '#f97316', // orange
    ]
    return colors[index % colors.length]
  }

  if (loading) {
    return (
      <div className="frequency-tabs">
        <div className="frequency-tabs-header">
          <div className="frequency-tab-list">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="frequency-tab skeleton">
                <div className="tab-frequency"></div>
                <div className="tab-count"></div>
              </div>
            ))}
          </div>
        </div>
        <div className="frequency-content">
          {Array.from({ length: 5 }).map((_, i) => (
            <CallCardSkeleton key={i} />
          ))}
        </div>
        
        <style>{`
          .frequency-tab.skeleton .tab-frequency,
          .frequency-tab.skeleton .tab-count {
            background: var(--bg-card);
            border-radius: var(--radius-sm);
            animation: pulse 1.5s ease-in-out infinite;
          }
          
          .frequency-tab.skeleton .tab-frequency {
            height: 16px;
            width: 80px;
            margin-bottom: 4px;
          }
          
          .frequency-tab.skeleton .tab-count {
            height: 12px;
            width: 40px;
          }
        `}</style>
      </div>
    )
  }

  if (error) {
    return (
      <div className="frequency-tabs-error">
        <div className="error-icon">⚠️</div>
        <p>{error}</p>
        <button onClick={loadFrequencyData} className="retry-btn">
          Try Again
        </button>
      </div>
    )
  }

  const frequencies = Object.keys(frequencyData)
  
  if (frequencies.length === 0) {
    return (
      <div className="frequency-tabs-empty">
        <div className="empty-icon">📻</div>
        <p>No frequency data available</p>
      </div>
    )
  }

  return (
    <div className="frequency-tabs">
      <div className="frequency-tabs-header">
        <h3>Calls by Frequency</h3>
        <div className="frequency-tab-list">
          {frequencies.map((freq, index) => {
            const calls = frequencyData[freq]
            const color = getFrequencyColor(index)
            const isActive = activeFrequency === freq
            
            return (
              <button
                key={freq}
                className={`frequency-tab ${isActive ? 'active' : ''}`}
                onClick={() => setActiveFrequency(freq)}
                style={{ 
                  ['--tab-color' as any]: color,
                  borderColor: isActive ? color : 'var(--border)'
                }}
              >
                <div className="tab-frequency" style={{ color: isActive ? color : 'var(--text-primary)' }}>
                  {formatFrequency(freq)}
                </div>
                <div className="tab-count">
                  {calls.length} call{calls.length !== 1 ? 's' : ''}
                </div>
                <div 
                  className="tab-indicator" 
                  style={{ backgroundColor: color }}
                />
              </button>
            )
          })}
        </div>
      </div>

      {activeFrequency && frequencyData[activeFrequency] && (
        <div className="frequency-content">
          <div className="frequency-content-header">
            <h4>
              {formatFrequency(activeFrequency)} 
              <span className="call-count">({frequencyData[activeFrequency].length} calls)</span>
            </h4>
          </div>
          <div className="calls-list">
            {frequencyData[activeFrequency].map(call => (
              <CallCard 
                key={call.id} 
                call={call}
              />
            ))}
          </div>
        </div>
      )}

      <style>{`
        .frequency-tabs {
          margin-top: var(--space-4);
        }

        .frequency-tabs-header {
          margin-bottom: var(--space-3);
        }

        .frequency-tabs-header h3 {
          margin-bottom: var(--space-2);
          color: var(--text-primary);
        }

        .frequency-tab-list {
          display: flex;
          gap: var(--space-2);
          overflow-x: auto;
          padding-bottom: var(--space-1);
        }

        .frequency-tab {
          position: relative;
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-2) var(--space-3);
          cursor: pointer;
          transition: var(--transition);
          min-width: 120px;
          text-align: left;
          flex-shrink: 0;
        }

        .frequency-tab:hover {
          background: var(--bg-card-hover);
          transform: translateY(-1px);
        }

        .frequency-tab.active {
          background: var(--bg-card-hover);
          border-color: var(--tab-color, var(--accent-primary));
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        .tab-frequency {
          font-weight: 600;
          font-size: var(--font-size-sm);
          margin-bottom: 2px;
        }

        .tab-count {
          font-size: var(--font-size-xs);
          color: var(--text-secondary);
        }

        .tab-indicator {
          position: absolute;
          bottom: 0;
          left: 0;
          right: 0;
          height: 3px;
          border-radius: 0 0 var(--radius) var(--radius);
          opacity: 0;
          transition: var(--transition);
        }

        .frequency-tab.active .tab-indicator {
          opacity: 1;
        }

        .frequency-content {
          margin-top: var(--space-4);
        }

        .frequency-content-header {
          margin-bottom: var(--space-3);
          padding: var(--space-3);
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius-lg);
        }

        .frequency-content-header h4 {
          color: var(--text-primary);
          display: flex;
          align-items: center;
          gap: var(--space-2);
        }

        .call-count {
          color: var(--text-secondary);
          font-weight: 400;
          font-size: var(--font-size-sm);
        }

        .calls-list {
          display: flex;
          flex-direction: column;
          gap: var(--space-2);
        }

        .frequency-tabs-error {
          text-align: center;
          padding: var(--space-4);
          color: var(--text-secondary);
        }

        .frequency-tabs-error .error-icon {
          font-size: 32px;
          margin-bottom: var(--space-2);
        }

        .retry-btn {
          margin-top: var(--space-2);
          background: var(--accent-primary);
          color: white;
          border: none;
          padding: var(--space-2) var(--space-3);
          border-radius: var(--radius);
          cursor: pointer;
          transition: var(--transition);
        }

        .retry-btn:hover {
          background: var(--accent-primary-hover);
        }

        .frequency-tabs-empty {
          text-align: center;
          padding: var(--space-4);
          color: var(--text-secondary);
        }

        .frequency-tabs-empty .empty-icon {
          font-size: 32px;
          margin-bottom: var(--space-2);
        }

        /* Mobile responsiveness */
        @media (max-width: 768px) {
          .frequency-tab {
            min-width: 100px;
            padding: var(--space-1) var(--space-2);
          }

          .tab-frequency {
            font-size: var(--font-size-xs);
          }
        }
      `}</style>
    </div>
  )
}
