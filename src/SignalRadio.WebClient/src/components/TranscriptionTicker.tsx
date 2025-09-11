import React, { useEffect, useState, useRef } from 'react'
import { Link } from 'react-router-dom'
import { apiGet } from '../api'
import { audioPlayerService } from '../services/AudioPlayerService'
import type { TranscriptSummaryDto, TalkGroupDto, CallDto, TalkGroupStats, NotableIncidentDto } from '../types/dtos'

interface TickerItem {
  id: string
  talkGroupId: number
  talkGroupName: string
  talkGroupDescription?: string
  incidentDescription: string
  timestamp: number
  callIds: number[]
}

const getTimeAgo = (timestamp: number): string => {
  const now = Date.now()
  const diffInSeconds = Math.floor((now - timestamp) / 1000)
  
  if (diffInSeconds < 60) {
    return `${diffInSeconds}s ago`
  } else if (diffInSeconds < 3600) {
    const minutes = Math.floor(diffInSeconds / 60)
    return `${minutes}m ago`
  } else if (diffInSeconds < 86400) {
    const hours = Math.floor(diffInSeconds / 3600)
    return `${hours}h ago`
  } else {
    const days = Math.floor(diffInSeconds / 86400)
    return `${days}d ago`
  }
}

export default function TranscriptionTicker() {
  const [tickerItems, setTickerItems] = useState<TickerItem[]>([])
  const [isPaused, setIsPaused] = useState(false)
  const [serviceAvailable, setServiceAvailable] = useState<boolean | null>(null)
  const [isExpanded, setIsExpanded] = useState(false)
  const [dropdownTop, setDropdownTop] = useState(0)
  const tickerRef = useRef<HTMLDivElement>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Check if AI summary service is available
  useEffect(() => {
    checkServiceAvailability()
  }, [])

  // Load summaries every 2 minutes
  useEffect(() => {
    if (serviceAvailable) {
      loadRecentSummaries()
      const interval = setInterval(loadRecentSummaries, 2 * 60 * 1000) // 2 minutes
      return () => clearInterval(interval)
    }
  }, [serviceAvailable])

  const checkServiceAvailability = async () => {
    try {
      const status = await apiGet<{ available: boolean }>('/transcriptsummary/status')
      setServiceAvailable(status?.available || false)
    } catch (err) {
      console.error('Failed to check summary service status:', err)
      setServiceAvailable(false)
    }
  }

    const loadRecentSummaries = async () => {
    if (!serviceAvailable) return

    try {
      // Get talkgroups that have transcripts in the last 15 minutes
      const talkGroupsWithTranscripts = await apiGet<number[]>('/calls/transcripts-available?windowMinutes=15')
      
      if (!talkGroupsWithTranscripts || talkGroupsWithTranscripts.length === 0) {
        setTickerItems([])
        return
      }

      // Limit to top 10 most recently active talkgroups
      const limitedTalkGroupIds = talkGroupsWithTranscripts.slice(0, 10)

      // Get summaries for talkgroups with transcripts and collect notable incidents
      const allIncidents: TickerItem[] = []
      
      const summaryPromises = limitedTalkGroupIds.map(async (talkGroupId) => {
        try {
          const summary = await apiGet<TranscriptSummaryDto>(`/talkgroups/${talkGroupId}/summary?windowMinutes=15`)
          
          // Also fetch the talkgroup details to get description
          let talkGroupDetails: TalkGroupDto | null = null
          try {
            talkGroupDetails = await apiGet<TalkGroupDto>(`/talkgroups/${talkGroupId}`)
          } catch (err) {
            console.warn(`Failed to fetch talkgroup details for ${talkGroupId}:`, err)
          }
          
          // Extract notable incidents if they exist
          if (summary && summary.notableIncidentsWithCallIds && summary.notableIncidentsWithCallIds.length > 0) {
            summary.notableIncidentsWithCallIds.forEach((incident, index) => {
              allIncidents.push({
                id: `incident-${talkGroupId}-${summary.generatedAt}-${index}`,
                talkGroupId,
                talkGroupName: summary.talkGroupName,
                talkGroupDescription: talkGroupDetails?.description,
                incidentDescription: incident.description,
                timestamp: new Date(summary.generatedAt).getTime(),
                callIds: incident.callIds
              } as TickerItem)
            })
          }
        } catch (err) {
          console.warn(`Failed to get summary for talkgroup ${talkGroupId}:`, err)
        }
      })

      await Promise.all(summaryPromises)
      
      // Sort by timestamp (most recent first) and limit to 20 items
      allIncidents.sort((a, b) => b.timestamp - a.timestamp)
      setTickerItems(allIncidents.slice(0, 20))
    } catch (err) {
      console.error('Failed to load recent summaries:', err)
    }
  }

  const handleTickerItemClick = async (item: TickerItem) => {
    try {
      // Close dropdown when item is clicked
      setIsExpanded(false)
      
      // If there are call IDs for this incident, play those calls
      if (item.callIds && item.callIds.length > 0) {
        // Fetch and add calls to the front of the queue (in reverse order so first call plays first)
        const callsToAdd = []
        for (const callId of item.callIds.slice(0, 5)) { // Limit to first 5 calls
          try {
            const call = await apiGet<CallDto>(`/calls/${callId}`)
            if (call) {
              callsToAdd.push(call)
            }
          } catch (err) {
            console.warn(`Failed to fetch call ${callId}:`, err)
          }
        }
        
        // Add calls to front of queue in reverse order so they play in correct order
        for (let i = callsToAdd.length - 1; i >= 0; i--) {
          audioPlayerService.addToFront(callsToAdd[i])
        }
        
        // Start playing if not already playing
        if (audioPlayerService.getState() === 'stopped') {
          audioPlayerService.play().catch(error => {
            console.error('Failed to start audio player:', error)
          })
        }
      }
    } catch (error) {
      console.error('Failed to handle ticker item click:', error)
    }
  }

  const handleTickerClick = () => {
    // Calculate dropdown position before toggling
    if (!isExpanded && dropdownRef.current) {
      const rect = dropdownRef.current.getBoundingClientRect()
      setDropdownTop(rect.bottom)
    }
    
    setIsExpanded(!isExpanded)
    // Resume scrolling when closing dropdown, pause when opening
    setIsPaused(!isExpanded)
  }

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsExpanded(false)
        setIsPaused(false)
      }
    }

    if (isExpanded) {
      document.addEventListener('mousedown', handleClickOutside)
      setIsPaused(true) // Pause scrolling when dropdown is open
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isExpanded])

  // Auto-scroll the ticker
  useEffect(() => {
    if (!tickerRef.current || isPaused || tickerItems.length === 0) return

    const ticker = tickerRef.current
    let animationId: number
    let lastTimestamp = 0
    const scrollSpeed = 30 // pixels per second

    const scroll = (timestamp: number) => {
      if (lastTimestamp === 0) lastTimestamp = timestamp
      const deltaTime = timestamp - lastTimestamp
      
      if (ticker.scrollLeft >= ticker.scrollWidth / 2) {
        // Reset to beginning for seamless loop
        ticker.scrollLeft = 0
      } else {
        ticker.scrollLeft += (scrollSpeed * deltaTime) / 1000
      }
      
      lastTimestamp = timestamp
      animationId = requestAnimationFrame(scroll)
    }

    animationId = requestAnimationFrame(scroll)

    return () => {
      if (animationId) {
        cancelAnimationFrame(animationId)
      }
    }
  }, [isPaused, tickerItems])

  // Don't show ticker if service is not available or no items
  if (serviceAvailable === false || tickerItems.length === 0) {
    return null
  }

  return (
    <div className="transcription-ticker" ref={dropdownRef}>
      <div 
        className="ticker-content"
        ref={tickerRef}
        onClick={handleTickerClick}
        style={{ cursor: 'pointer' }}
      >
        <div className="ticker-items">
          {tickerItems.map((item, index) => (
            <div
              key={`ticker-${item.id}-${index}`}
              className="ticker-item"
            >
              <div className="ticker-link">
                <span className="ticker-talkgroup">{item.talkGroupDescription || item.talkGroupName}:</span>
                <span className="ticker-text">{item.incidentDescription}</span>
                <span className="ticker-calls">
                  ({item.callIds.length} call{item.callIds.length !== 1 ? 's' : ''})
                </span>
                <span className="ticker-time">{getTimeAgo(item.timestamp)}</span>
              </div>
            </div>
          ))}
          {/* Duplicate items for seamless scrolling */}
          {tickerItems.map((item, index) => (
            <div
              key={`ticker-dup-${item.id}-${index}`}
              className="ticker-item"
            >
              <div className="ticker-link">
                <span className="ticker-talkgroup">{item.talkGroupDescription || item.talkGroupName}:</span>
                <span className="ticker-text">{item.incidentDescription}</span>
                <span className="ticker-calls">
                  ({item.callIds.length} call{item.callIds.length !== 1 ? 's' : ''})
                </span>
                <span className="ticker-time">{getTimeAgo(item.timestamp)}</span>
              </div>
            </div>
          ))}
        </div>
        <div className="ticker-expand-hint">
          <span>{isExpanded ? '▲' : '▼'} Click to {isExpanded ? 'hide' : 'view all'}</span>
        </div>
      </div>

      {/* Dropdown with all items */}
      {isExpanded && (
        <div 
          className="ticker-dropdown"
          style={{ top: `${dropdownTop}px` }}
        >
          {tickerItems.map((item, index) => (
            <div
              key={`dropdown-${item.id}-${index}`}
              className="dropdown-item"
            >
              <Link
                to={`/talkgroups/${item.talkGroupId}`}
                className="dropdown-link"
                onClick={(e) => {
                  e.preventDefault()
                  handleTickerItemClick(item)
                }}
              >
                <div className="dropdown-item-header">
                  <span className="dropdown-talkgroup">{item.talkGroupDescription || item.talkGroupName}</span>
                  <span className="dropdown-time">{getTimeAgo(item.timestamp)}</span>
                </div>
                <div className="dropdown-item-content">
                  <span className="dropdown-text">{item.incidentDescription}</span>
                  <span className="dropdown-calls">
                    {item.callIds.length} call{item.callIds.length !== 1 ? 's' : ''}
                  </span>
                </div>
              </Link>
            </div>
          ))}
        </div>
      )}

      <style>{`
        .transcription-ticker {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          overflow: visible;
          min-height: 56px;
          position: relative;
          margin-bottom: 0; /* Remove margin since it's in header */
        }

        .ticker-content {
          width: 100%;
          overflow: hidden;
          min-height: 56px;
          position: relative;
          display: flex;
          align-items: center;
          justify-content: space-between;
          transition: all 0.2s ease;
        }

        .ticker-content:hover {
          background: var(--bg-hover);
        }

        .ticker-items {
          display: flex;
          align-items: center;
          height: 100%;
          white-space: nowrap;
          flex: 1;
          animation: none; /* We'll control scrolling via JS for better performance */
        }

        .ticker-expand-hint {
          padding: var(--space-2);
          font-size: 12px;
          color: var(--text-muted);
          flex-shrink: 0;
          border-left: 1px solid var(--border);
          background: rgba(255, 255, 255, 0.02);
        }

        .ticker-item {
          display: inline-flex;
          align-items: center;
          color: var(--text-primary);
          white-space: nowrap;
          border-right: 1px solid var(--border);
          min-width: max-content;
          font-size: 14px;
        }

        .ticker-link {
          display: flex;
          align-items: center;
          padding: var(--space-2) var(--space-3);
          color: inherit;
          width: 100%;
          gap: var(--space-1);
        }

        .ticker-talkgroup {
          font-weight: 600;
          color: var(--text-accent);
          margin-right: var(--space-1);
          flex-shrink: 0;
        }

        .ticker-text {
          color: var(--text-secondary);
          margin-right: var(--space-2);
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }

        .ticker-calls {
          color: var(--text-accent);
          font-size: 12px;
          font-weight: 500;
          margin-right: var(--space-2);
          flex-shrink: 0;
          padding: 2px 6px;
          background: var(--bg-accent-muted);
          border-radius: 4px;
        }

        .ticker-time {
          color: var(--text-muted);
          font-size: 12px;
          flex-shrink: 0;
          margin-left: auto;
        }

        /* Dropdown styles */
        .ticker-dropdown {
          position: fixed;
          left: 0;
          right: 0;
          width: 100vw;
          background: var(--bg-secondary);
          backdrop-filter: blur(8px);
          border: 1px solid var(--border);
          border-top: none;
          border-radius: 0 0 var(--radius) var(--radius);
          max-height: 400px;
          overflow-y: auto;
          z-index: 1000;
          box-shadow: 0 8px 24px rgba(0, 0, 0, 0.3);
        }

        .dropdown-item {
          border-bottom: 1px solid var(--border);
          cursor: pointer;
          transition: var(--transition);
        }

        .dropdown-item:hover {
          background: var(--bg-card-hover);
        }

        .dropdown-item:last-child {
          border-bottom: none;
        }

        .dropdown-link {
          display: block;
          padding: var(--space-3);
          color: inherit;
          text-decoration: none;
        }

        .dropdown-item-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: var(--space-1);
        }

        .dropdown-talkgroup {
          font-weight: 600;
          color: var(--accent-primary);
          font-size: 14px;
        }

        .dropdown-time {
          color: var(--text-secondary);
          font-size: 12px;
        }

        .dropdown-item-content {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          gap: var(--space-2);
        }

        .dropdown-text {
          color: var(--text-primary);
          flex: 1;
          line-height: 1.4;
          word-wrap: break-word;
        }

        .dropdown-calls {
          color: var(--accent-primary);
          font-size: 12px;
          font-weight: 500;
          flex-shrink: 0;
          padding: 2px 6px;
          background: var(--bg-accent-muted);
          border-radius: 4px;
        }

        @media (max-width: 767px) {
          .transcription-ticker {
            min-height: 48px;
            margin-bottom: 0; /* Remove margin for mobile too */
          }

          .ticker-content {
            min-height: 48px;
          }

          .ticker-link {
            padding: var(--space-1-5) var(--space-2);
            font-size: 13px;
          }

          .ticker-expand-hint {
            padding: var(--space-1-5);
            font-size: 11px;
          }

          .ticker-calls {
            font-size: 11px;
            padding: 1px 4px;
          }

          .ticker-time {
            font-size: 11px;
          }

          .ticker-dropdown {
            position: fixed;
            left: 0;
            right: 0;
            width: 100vw;
            background: var(--bg-secondary);
            backdrop-filter: blur(12px);
          }

          .dropdown-link {
            padding: var(--space-2);
          }

          .dropdown-talkgroup {
            font-size: 13px;
          }

          .dropdown-text {
            font-size: 13px;
          }

          .dropdown-calls {
            font-size: 11px;
          }
        }
      `}</style>
    </div>
  )
}
