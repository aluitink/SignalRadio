import React, { useState } from 'react'
import { useAudioManager } from '../hooks/useAudioManager'

interface AutoplayBannerProps {
  className?: string
}

export default function AutoplayBanner({ className = '' }: AutoplayBannerProps) {
  const { 
    autoplayEnabled,
    autoplayChecked, 
    playerState,
    needsUserInteraction,
    enableAutoplayAndStartPlaying,
    testAutoplayWithSilence,
    getUserHasInteracted,
    queueLength 
  } = useAudioManager()

  const [isRequesting, setIsRequesting] = useState(false)

  const userHasInteracted = getUserHasInteracted()

  // Don't show banner if:
  // - Player is already playing
  // - User hasn't interacted yet (will show on audio player instead)  
  // - No calls in queue to play
  // - User doesn't need interaction and autoplay is enabled
  if (playerState === 'playing' || !userHasInteracted || queueLength === 0 || (!needsUserInteraction && autoplayEnabled)) {
    return null
  }

  const handleEnableAutoplay = async () => {
    if (isRequesting) return
    
    setIsRequesting(true)
    try {
      // First test with silence to see if autoplay would work
      const silenceTestPassed = await testAutoplayWithSilence()
      
      if (silenceTestPassed) {
        console.log('Silence test passed, enabling autoplay and starting playback')
      } else {
        console.log('Silence test failed, but proceeding with enable attempt')
      }
      
      const enabled = await enableAutoplayAndStartPlaying()
      if (!enabled) {
        console.warn('Failed to enable autoplay after user interaction')
      }
    } catch (error) {
      console.error('Failed to enable autoplay:', error)
    } finally {
      setIsRequesting(false)
    }
  }

  return (
    <div className={`autoplay-banner ${className}`}>
      <div className="autoplay-banner-content">
        <div className="autoplay-banner-text">
          <h4>{needsUserInteraction ? '🚫 Audio Needs Permission' : '🎵 Ready to Play'}</h4>
          <p>
            {needsUserInteraction 
              ? `You have ${queueLength} call${queueLength !== 1 ? 's' : ''} queued. Click below to enable audio and start playback.`
              : `You have ${queueLength} call${queueLength !== 1 ? 's' : ''} waiting to play. Click below to start autoplay.`
            }
          </p>
        </div>
        <button
          onClick={handleEnableAutoplay}
          className="autoplay-enable-btn"
          disabled={isRequesting}
        >
          <span className="btn-icon">{isRequesting ? '⏳' : '🔊'}</span>
          {isRequesting 
            ? 'Starting...'
            : needsUserInteraction 
              ? 'Enable Audio & Play' 
              : 'Start Playing'
          }
        </button>
      </div>

      <style>{`
        .autoplay-banner {
          background: linear-gradient(135deg, var(--accent-primary), var(--accent-secondary));
          border: 1px solid rgba(255, 255, 255, 0.2);
          border-radius: var(--radius);
          padding: var(--space-3);
          margin-bottom: var(--space-3);
          color: white;
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        }

        .autoplay-banner-content {
          display: flex;
          align-items: center;
          gap: var(--space-3);
        }

        .autoplay-banner-text {
          flex: 1;
        }

        .autoplay-banner-text h4 {
          margin: 0 0 var(--space-1) 0;
          font-size: 1rem;
          font-weight: 600;
          color: white;
        }

        .autoplay-banner-text p {
          margin: 0;
          font-size: 0.9rem;
          color: rgba(255, 255, 255, 0.9);
          line-height: 1.4;
        }

        .autoplay-enable-btn {
          background: rgba(255, 255, 255, 0.2);
          border: 1px solid rgba(255, 255, 255, 0.3);
          border-radius: var(--radius);
          padding: var(--space-2) var(--space-3);
          color: white;
          font-weight: 600;
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          gap: var(--space-1);
          white-space: nowrap;
        }

        .autoplay-enable-btn:disabled {
          opacity: 0.7;
          cursor: not-allowed;
          transform: none;
        }

        .autoplay-enable-btn:hover {
          background: rgba(255, 255, 255, 0.3);
          border-color: rgba(255, 255, 255, 0.5);
          transform: translateY(-1px);
        }

        .autoplay-enable-btn:disabled:hover {
          background: rgba(255, 255, 255, 0.2);
          transform: none;
        }

        .btn-icon {
          font-size: 1.1em;
        }

        @media (max-width: 768px) {
          .autoplay-banner-content {
            flex-direction: column;
            text-align: center;
            gap: var(--space-2);
          }

          .autoplay-enable-btn {
            align-self: stretch;
            justify-content: center;
          }
        }
      `}</style>
    </div>
  )
}
