import React, { useEffect, useState, useRef } from 'react'
import { Link } from 'react-router-dom'
import { callStreamService } from '../services/CallStreamService'
import type { CallDto } from '../types/dtos'

interface TickerItem {
  id: string
  callId: number
  text: string
  timestamp: number
  talkGroupName: string
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
  const tickerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const unsubscribe = callStreamService.subscribe({
      onCallsChanged: () => {}, // Not needed for ticker
      onNewCall: (call) => {
        // Add new transcriptions from new calls
        if (call.transcriptions?.length) {
          const newItems = call.transcriptions.map(transcription => ({
            id: transcription.id,
            callId: call.id,
            text: transcription.text,
            timestamp: Date.now(),
            talkGroupName: call.talkGroup?.description || 
              call.talkGroup?.alphaTag || 
              call.talkGroup?.name || 
              `TG ${call.talkGroupId}`
          }))
          
          setTickerItems(prev => {
            // Add new items and keep only the last 50
            const combined = [...newItems, ...prev]
            return combined.slice(0, 50)
          })
        }
      },
      onCallUpdated: (call) => {
        // Handle updated calls that might have new transcriptions
        if (call.transcriptions?.length) {
          const newItems = call.transcriptions
            .filter(transcription => {
              // Only add transcriptions we haven't seen before
              return !tickerItems.some(item => 
                item.id === transcription.id || 
                (item.callId === call.id && item.text === transcription.text)
              )
            })
            .map(transcription => ({
              id: transcription.id,
              callId: call.id,
              text: transcription.text,
              timestamp: Date.now(),
              talkGroupName: call.talkGroup?.description || 
                call.talkGroup?.alphaTag || 
                call.talkGroup?.name || 
                `TG ${call.talkGroupId}`
            }))
          
          if (newItems.length > 0) {
            setTickerItems(prev => {
              const combined = [...newItems, ...prev]
              return combined.slice(0, 50)
            })
          }
        }
      }
    })

    return unsubscribe
  }, [tickerItems]) // Include tickerItems in dependency to check against existing items

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

  if (tickerItems.length === 0) {
    return null
  }

  return (
    <div className="transcription-ticker">
      <div 
        className="ticker-content"
        ref={tickerRef}
        onMouseEnter={() => setIsPaused(true)}
        onMouseLeave={() => setIsPaused(false)}
      >
        <div className="ticker-items">
          {tickerItems.map((item, index) => (
            <Link
              key={`ticker-${item.id}-${item.callId}-${index}`}
              to={`/call/${item.callId}`}
              className="ticker-item"
            >
              <span className="ticker-talkgroup">{item.talkGroupName}:</span>
              <span className="ticker-text">{item.text}</span>
              <span className="ticker-time">{getTimeAgo(item.timestamp)}</span>
            </Link>
          ))}
          {/* Duplicate items for seamless scrolling */}
          {tickerItems.map((item, index) => (
            <Link
              key={`ticker-dup-${item.id}-${item.callId}-${index}`}
              to={`/call/${item.callId}`}
              className="ticker-item"
            >
              <span className="ticker-talkgroup">{item.talkGroupName}:</span>
              <span className="ticker-text">{item.text}</span>
              <span className="ticker-time">{getTimeAgo(item.timestamp)}</span>
            </Link>
          ))}
        </div>
      </div>

      <style>{`
        .transcription-ticker {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          margin-bottom: var(--space-3);
          overflow: hidden;
          height: 56px;
        }

        .ticker-content {
          width: 100%;
          overflow: hidden;
          height: 100%;
          position: relative;
        }

        .ticker-items {
          display: flex;
          align-items: center;
          height: 100%;
          white-space: nowrap;
          animation: none; /* We'll control scrolling via JS for better performance */
        }

        .ticker-item {
          display: inline-flex;
          align-items: center;
          padding: var(--space-2) var(--space-3);
          text-decoration: none;
          color: var(--text-primary);
          transition: background-color 0.2s ease;
          white-space: nowrap;
          border-right: 1px solid var(--border);
          min-width: max-content;
          font-size: 14px; /* Slightly smaller text */
        }

        .ticker-item:hover {
          background: var(--bg-hover);
        }

        .ticker-talkgroup {
          font-weight: 600;
          color: var(--text-accent);
          margin-right: var(--space-1);
          flex-shrink: 0;
        }

        .ticker-text {
          color: var(--text-secondary);
          max-width: 800px; /* Increased from 600px */
          overflow: hidden;
          text-overflow: ellipsis;
          margin-right: var(--space-2);
        }

        .ticker-time {
          color: var(--text-muted);
          font-size: 12px;
          flex-shrink: 0;
          margin-left: auto;
        }

        @media (max-width: 767px) {
          .transcription-ticker {
            height: 48px;
            margin-bottom: var(--space-2);
          }

          .ticker-item {
            padding: var(--space-1-5) var(--space-2);
            font-size: 13px; /* Slightly smaller on mobile */
          }

          .ticker-text {
            max-width: 500px; /* Increased from 350px */
          }

          .ticker-time {
            font-size: 11px;
          }
        }

        /* Hover pause indicator */
        .ticker-content:hover::after {
          content: '⏸️';
          position: absolute;
          top: 4px;
          right: 4px;
          font-size: 12px;
          opacity: 0.7;
          z-index: 1;
        }
      `}</style>
    </div>
  )
}
