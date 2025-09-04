import React, { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import type { CallDto } from '../types/dtos'
import { audioPlayerService } from '../services/AudioPlayerService'
import { useSubscriptions } from '../contexts/SubscriptionContext'

interface CallCardProps {
  call: CallDto
}

function formatTime(dateString: string) {
  const date = new Date(dateString)
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

function secondsToHuman(s: number) {
  if (!isFinite(s) || s <= 0) return '0s'
  const m = Math.floor(s / 60)
  const sec = Math.floor(s % 60)
  return m ? `${m}m ${sec}s` : `${sec}s`
}

function secondsToAge(s: number) {
  if (!isFinite(s) || s <= 0) return '0s'
  
  const days = Math.floor(s / 86400)
  const hours = Math.floor((s % 86400) / 3600)
  const minutes = Math.floor((s % 3600) / 60)
  const seconds = Math.floor(s % 60)
  
  const parts = []
  if (days > 0) parts.push(`${days}d`)
  if (hours > 0) parts.push(`${hours}h`)
  if (minutes > 0) parts.push(`${minutes}m`)
  if (seconds > 0 || parts.length === 0) parts.push(`${seconds}s`)
  
  return parts.join(' ')
}

export default function CallCard({ call }: CallCardProps) {
  const { isSubscribed, toggle: toggleSubscription, isPending } = useSubscriptions()
  const [currentPlayingCall, setCurrentPlayingCall] = useState<CallDto | null>(null)
  const [isPlayingState, setIsPlayingState] = useState(false)
  
  const isSubscribedToTalkGroup = isSubscribed(call.talkGroupId)
  const isSubscriptionPending = isPending(call.talkGroupId)

  // Subscribe to audio player changes to know if this call is playing
  useEffect(() => {
    const unsubscribe = audioPlayerService.subscribe({
      onStateChanged: () => {}, // We don't need this
      onCurrentCallChanged: (playingCall) => {
        setCurrentPlayingCall(playingCall)
      },
      onPlaybackChanged: (playing) => {
        setIsPlayingState(playing)
      },
      onUserInteractionChanged: () => {} // We don't need this
    })

    return unsubscribe
  }, [])

  // Check if this call is currently playing
  const isCurrentlyPlaying = currentPlayingCall?.id === call.id && isPlayingState

  // Calculate duration and age
  const duration = call.durationSeconds || call.recordings.reduce((acc, r) => acc + (r.durationSeconds || 0), 0)
  const started = new Date(call.recordingTime)
  const ageSec = Math.floor((Date.now() - started.getTime()) / 1000)

  // Get talkgroup display name
  const talkGroupDisplay = call.talkGroup?.description || 
                          call.talkGroup?.alphaTag || 
                          call.talkGroup?.name || 
                          `TG ${call.talkGroup?.number || call.talkGroupId}`

  // Get transcription text
  const transcriptionText = call.transcriptions && call.transcriptions.length > 0 
    ? call.transcriptions.map(t => t.text).join(' ')
    : null

  // Get age-based visual state
  const getAgeState = () => {
    if (ageSec < 60) return 'fresh' // Less than 1 minute
    if (ageSec < 300) return 'recent' // Less than 5 minutes  
    if (ageSec < 1800) return 'normal' // Less than 30 minutes
    if (ageSec < 3600) return 'aging' // Less than 1 hour
    return 'old' // More than 1 hour
  }

  const ageState = getAgeState()

  // Get priority-based visual intensity
  const getPriorityIntensity = () => {
    if (!call.talkGroup?.priority) return 'normal'
    if (call.talkGroup.priority <= 2) return 'critical'
    if (call.talkGroup.priority <= 5) return 'high'
    return 'normal'
  }

  const priorityIntensity = getPriorityIntensity()

  // Get category-based icon and animation class
  const getCategoryData = () => {
    const category = call.talkGroup?.category?.toLowerCase()
    switch (category) {
      case 'law':
        return { icon: '👮', animationClass: 'law' }
      case 'fire':
        return { icon: '🚒', animationClass: 'fire' }
      case 'medical':
      case 'ems':
        return { icon: '🚑', animationClass: 'medical' }
      case 'emergency':
        return { icon: '🚨', animationClass: 'emergency' }
      case 'hospital':
        return { icon: '🏥', animationClass: 'medical' }
      case 'public works':
        return { icon: '🚧', animationClass: 'public-works' }
      case 'schools':
        return { icon: '🏫', animationClass: 'schools' }
      case 'interop':
        return { icon: '📡', animationClass: 'interop' }
      case 'multi-dispatch':
      case 'multi-talk':
      case 'multi-tac':
        return { icon: '📢', animationClass: 'multi-dispatch' }
      default:
        return { icon: '📻', animationClass: 'default' }
    }
  }

  const { icon: categoryIcon, animationClass: categoryAnimationClass } = getCategoryData()

  const handleCardClick = () => {
    if (!call.recordings?.length) return
    
    // Add call to queue and start playing if not already playing
    audioPlayerService.addToQueue(call)
    
    // If player is stopped, start it
    if (audioPlayerService.getState() === 'stopped') {
      audioPlayerService.play().catch(error => {
        console.error('Failed to start audio player:', error)
      })
    }
  }

  const handleSubscribe = async (e: React.MouseEvent) => {
    e.stopPropagation()
    
    try {
      await toggleSubscription(call.talkGroupId)
    } catch (error) {
      console.error('Failed to toggle subscription:', error)
    }
  }

  const handleShare = async (e: React.MouseEvent) => {
    e.stopPropagation()
    
    const shareUrl = `${window.location.origin}/call/${call.id}`
    
    if (navigator.share) {
      try {
        await navigator.share({
          title: `${talkGroupDisplay} - SignalRadio`,
          text: transcriptionText || 'Radio call',
          url: shareUrl
        })
      } catch (error) {
        // User cancelled share
      }
    } else {
      try {
        await navigator.clipboard.writeText(shareUrl)
        // Could show a toast here
      } catch (error) {
        console.error('Copy failed:', error)
      }
    }
  }

  return (
    <article 
      className={`new-call-card age-${ageState} priority-${priorityIntensity} ${isCurrentlyPlaying ? `playing playing-${categoryAnimationClass}` : ''} ${isSubscribedToTalkGroup ? 'subscribed-tg' : ''}`}
      onClick={handleCardClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          handleCardClick()
        }
      }}
      aria-label={`Play call from ${talkGroupDisplay}`}
    >
      <div className="call-header">
        <div className="call-talkgroup">
          <Link 
            to={`/talkgroup/${call.talkGroupId}`}
            className={`talkgroup-link ${isCurrentlyPlaying ? 'playing' : ''}`}
            onClick={(e) => e.stopPropagation()}
          >
            {categoryIcon} {talkGroupDisplay}
          </Link>
          <div className="badges">
            {call.talkGroup?.priority && (
              <span className={`badge priority-badge priority-${priorityIntensity}`}>P{call.talkGroup.priority}</span>
            )}
            {call.talkGroup?.tag && (
              <span className="badge tag-badge">{call.talkGroup.tag}</span>
            )}
            {call.talkGroup?.category && (
              <span className="badge category-badge">{call.talkGroup.category}</span>
            )}
            {call.talkGroup?.alphaTag && call.talkGroup.alphaTag !== call.talkGroup.tag && (
              <span className="badge alpha-badge">{call.talkGroup.alphaTag}</span>
            )}
          </div>
        </div>
        
        <div className="call-actions">
          <button
            className={`subscribe-btn ${isSubscribedToTalkGroup ? 'subscribed' : ''} ${isSubscriptionPending ? 'pending' : ''}`}
            onClick={handleSubscribe}
            disabled={isSubscriptionPending}
            aria-label={isSubscribedToTalkGroup ? 'Unsubscribe from talkgroup' : 'Subscribe to talkgroup'}
            title={isSubscribedToTalkGroup ? 'Unsubscribe from talkgroup' : 'Subscribe to talkgroup'}
          >
            {isSubscriptionPending ? '⏳' : (isSubscribedToTalkGroup ? '⭐' : '☆')}
          </button>
          
          <button
            className="share-btn"
            onClick={handleShare}
            aria-label="Share call"
            title="Share call"
          >
            📤
          </button>
        </div>
      </div>

      <div className="call-meta">
        <span className={`call-time ${isCurrentlyPlaying ? 'playing' : ''}`} title={started.toLocaleString()}>
          {isCurrentlyPlaying && '▶️ '}
          {formatTime(call.recordingTime)}
        </span>
        <span className="call-duration">{secondsToHuman(duration)}</span>
        <span className="call-frequency">
          {(call.frequencyHz / 1000000).toFixed(3)} MHz
        </span>
        <span className={`call-age age-${ageState}`}>{secondsToAge(ageSec)} ago</span>
      </div>

      {transcriptionText && (
        <div className="call-transcript">
          <p>{transcriptionText}</p>
        </div>
      )}

      {!transcriptionText && (
        <div className="call-transcript">
          <p className="no-transcript">No transcription available</p>
        </div>
      )}

      <style>{`
        .new-call-card {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-2);
          cursor: pointer;
          transition: all 0.3s ease;
          position: relative;
          overflow: hidden;
          transform: translateZ(0); /* Enable GPU acceleration */
        }

        /* Age-based color variations */
        .new-call-card.age-fresh {
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(34, 197, 94, 0.08) 100%);
          border-color: rgba(34, 197, 94, 0.3);
          box-shadow: 0 0 8px rgba(34, 197, 94, 0.1);
        }

        .new-call-card.age-recent {
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.06) 100%);
          border-color: rgba(59, 130, 246, 0.2);
        }

        .new-call-card.age-normal {
          background: var(--bg-card);
          border-color: var(--border);
        }

        .new-call-card.age-aging {
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(245, 158, 11, 0.05) 100%);
          border-color: rgba(245, 158, 11, 0.2);
        }

        .new-call-card.age-old {
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(107, 114, 128, 0.05) 100%);
          border-color: rgba(107, 114, 128, 0.2);
          opacity: 0.8;
        }

        /* Priority-based intensity */
        .new-call-card.priority-critical {
          box-shadow: 0 0 12px rgba(239, 68, 68, 0.2);
        }

        .new-call-card.priority-critical.age-fresh {
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(239, 68, 68, 0.1) 100%);
          border-color: rgba(239, 68, 68, 0.4);
          box-shadow: 0 0 16px rgba(239, 68, 68, 0.25);
        }

        .new-call-card.priority-high {
          box-shadow: 0 0 8px rgba(245, 158, 11, 0.15);
        }

        .new-call-card.priority-high.age-fresh {
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(245, 158, 11, 0.08) 100%);
          border-color: rgba(245, 158, 11, 0.3);
        }

        /* Subscribed talkgroup styling */
        .new-call-card.subscribed-tg {
          border-left: 3px solid var(--accent-primary);
        }

        .new-call-card.subscribed-tg.age-fresh {
          border-left: 3px solid rgba(34, 197, 94, 0.8);
        }

        /* Playing state with category-specific animations */
        .new-call-card.playing {
          transform: scale(1.02);
          z-index: 10; /* Bring playing cards to front */
          transition: all 0.3s ease;
        }

        /* Law Enforcement - Red and Blue */
        .new-call-card.playing-law {
          animation: lawPulse 2s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(239, 68, 68, 0.15) 100%);
          border-color: rgba(220, 38, 38, 0.6);
          box-shadow: 0 0 20px rgba(220, 38, 38, 0.3);
        }

        .new-call-card.playing-law::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(45deg, 
            rgba(220, 38, 38, 0.8), 
            rgba(59, 130, 246, 0.8), 
            rgba(220, 38, 38, 0.8), 
            rgba(59, 130, 246, 0.8)
          );
          background-size: 400% 400%;
          border-radius: var(--radius);
          z-index: -1;
          animation: lawBorder 2s ease infinite;
        }

        @keyframes lawPulse {
          0%, 100% {
            box-shadow: 0 0 20px rgba(220, 38, 38, 0.3);
            border-color: rgba(220, 38, 38, 0.6);
          }
          50% {
            box-shadow: 0 0 30px rgba(59, 130, 246, 0.4);
            border-color: rgba(59, 130, 246, 0.7);
          }
        }

        @keyframes lawBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        /* Fire Department - Red and Orange */
        .new-call-card.playing-fire {
          animation: firePulse 1.5s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(239, 68, 68, 0.2) 100%);
          border-color: rgba(234, 88, 12, 0.7);
          box-shadow: 0 0 25px rgba(234, 88, 12, 0.4);
        }

        .new-call-card.playing-fire::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(45deg, 
            rgba(239, 68, 68, 0.9), 
            rgba(249, 115, 22, 0.9), 
            rgba(234, 88, 12, 0.9), 
            rgba(239, 68, 68, 0.9)
          );
          background-size: 300% 300%;
          border-radius: var(--radius);
          z-index: -1;
          animation: fireBorder 1.8s ease infinite;
        }

        @keyframes firePulse {
          0%, 100% {
            box-shadow: 0 0 25px rgba(239, 68, 68, 0.4);
            border-color: rgba(234, 88, 12, 0.7);
          }
          50% {
            box-shadow: 0 0 35px rgba(249, 115, 22, 0.6);
            border-color: rgba(249, 115, 22, 0.8);
          }
        }

        @keyframes fireBorder {
          0% { background-position: 0% 50%; }
          33% { background-position: 100% 0%; }
          66% { background-position: 0% 100%; }
          100% { background-position: 0% 50%; }
        }

        /* Medical/EMS - Red and White */
        .new-call-card.playing-medical {
          animation: medicalPulse 2.5s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(239, 68, 68, 0.12) 100%);
          border-color: rgba(220, 38, 38, 0.5);
          box-shadow: 0 0 18px rgba(220, 38, 38, 0.3);
        }

        .new-call-card.playing-medical::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(90deg, 
            rgba(220, 38, 38, 0.8), 
            rgba(255, 255, 255, 0.6), 
            rgba(220, 38, 38, 0.8), 
            rgba(255, 255, 255, 0.6)
          );
          background-size: 200% 100%;
          border-radius: var(--radius);
          z-index: -1;
          animation: medicalBorder 3s linear infinite;
        }

        @keyframes medicalPulse {
          0%, 100% {
            box-shadow: 0 0 18px rgba(220, 38, 38, 0.3);
          }
          50% {
            box-shadow: 0 0 25px rgba(220, 38, 38, 0.5);
          }
        }

        @keyframes medicalBorder {
          0% { background-position: 0% 0%; }
          100% { background-position: 200% 0%; }
        }

        /* Emergency - Rapid Red Flash */
        .new-call-card.playing-emergency {
          animation: emergencyFlash 0.8s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(239, 68, 68, 0.25) 100%);
          border-color: rgba(220, 38, 38, 0.8);
          box-shadow: 0 0 30px rgba(220, 38, 38, 0.5);
        }

        @keyframes emergencyFlash {
          0%, 100% {
            background: linear-gradient(135deg, var(--bg-card) 0%, rgba(239, 68, 68, 0.25) 100%);
            box-shadow: 0 0 30px rgba(220, 38, 38, 0.5);
          }
          50% {
            background: linear-gradient(135deg, var(--bg-card) 0%, rgba(239, 68, 68, 0.05) 100%);
            box-shadow: 0 0 10px rgba(220, 38, 38, 0.2);
          }
        }

        /* Public Works - Yellow and Black */
        .new-call-card.playing-public-works {
          animation: publicWorksPulse 2s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(245, 158, 11, 0.15) 100%);
          border-color: rgba(245, 158, 11, 0.6);
          box-shadow: 0 0 20px rgba(245, 158, 11, 0.3);
        }

        .new-call-card.playing-public-works::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: repeating-linear-gradient(
            45deg,
            rgba(245, 158, 11, 0.8) 0px,
            rgba(245, 158, 11, 0.8) 8px,
            rgba(0, 0, 0, 0.8) 8px,
            rgba(0, 0, 0, 0.8) 16px
          );
          border-radius: var(--radius);
          z-index: -1;
          animation: publicWorksStripes 2s linear infinite;
        }

        @keyframes publicWorksPulse {
          0%, 100% {
            box-shadow: 0 0 20px rgba(245, 158, 11, 0.3);
            border-color: rgba(245, 158, 11, 0.6);
          }
          50% {
            box-shadow: 0 0 30px rgba(245, 158, 11, 0.5);
            border-color: rgba(245, 158, 11, 0.8);
          }
        }

        @keyframes publicWorksStripes {
          0% { background-position: 0px 0px; }
          100% { background-position: 32px 0px; }
        }

        /* Schools - Green and Blue */
        .new-call-card.playing-schools {
          animation: schoolsPulse 3s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(34, 197, 94, 0.12) 100%);
          border-color: rgba(34, 197, 94, 0.5);
          box-shadow: 0 0 15px rgba(34, 197, 94, 0.3);
        }

        .new-call-card.playing-schools::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(45deg, 
            rgba(34, 197, 94, 0.7), 
            rgba(59, 130, 246, 0.7), 
            rgba(34, 197, 94, 0.7), 
            rgba(59, 130, 246, 0.7)
          );
          background-size: 400% 400%;
          border-radius: var(--radius);
          z-index: -1;
          animation: schoolsBorder 4s ease infinite;
        }

        @keyframes schoolsPulse {
          0%, 100% {
            box-shadow: 0 0 15px rgba(34, 197, 94, 0.3);
            border-color: rgba(34, 197, 94, 0.5);
          }
          50% {
            box-shadow: 0 0 20px rgba(59, 130, 246, 0.4);
            border-color: rgba(59, 130, 246, 0.6);
          }
        }

        @keyframes schoolsBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        /* Interop - Purple and Cyan */
        .new-call-card.playing-interop {
          animation: interopPulse 2.2s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(147, 51, 234, 0.15) 100%);
          border-color: rgba(147, 51, 234, 0.6);
          box-shadow: 0 0 22px rgba(147, 51, 234, 0.3);
        }

        .new-call-card.playing-interop::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(45deg, 
            rgba(147, 51, 234, 0.8), 
            rgba(6, 182, 212, 0.8), 
            rgba(147, 51, 234, 0.8), 
            rgba(6, 182, 212, 0.8)
          );
          background-size: 400% 400%;
          border-radius: var(--radius);
          z-index: -1;
          animation: interopBorder 3s ease infinite;
        }

        @keyframes interopPulse {
          0%, 100% {
            box-shadow: 0 0 22px rgba(147, 51, 234, 0.3);
            border-color: rgba(147, 51, 234, 0.6);
          }
          50% {
            box-shadow: 0 0 28px rgba(6, 182, 212, 0.4);
            border-color: rgba(6, 182, 212, 0.7);
          }
        }

        @keyframes interopBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        /* Multi-Dispatch - Rainbow effect */
        .new-call-card.playing-multi-dispatch {
          animation: multiDispatchPulse 1.8s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(168, 85, 247, 0.15) 100%);
          border-color: rgba(168, 85, 247, 0.6);
          box-shadow: 0 0 25px rgba(168, 85, 247, 0.3);
        }

        .new-call-card.playing-multi-dispatch::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(45deg, 
            rgba(239, 68, 68, 0.8), 
            rgba(245, 158, 11, 0.8), 
            rgba(34, 197, 94, 0.8), 
            rgba(59, 130, 246, 0.8),
            rgba(168, 85, 247, 0.8),
            rgba(239, 68, 68, 0.8)
          );
          background-size: 600% 600%;
          border-radius: var(--radius);
          z-index: -1;
          animation: multiDispatchBorder 3s ease infinite;
        }

        @keyframes multiDispatchPulse {
          0%, 100% {
            box-shadow: 0 0 25px rgba(168, 85, 247, 0.3);
          }
          25% {
            box-shadow: 0 0 30px rgba(239, 68, 68, 0.4);
          }
          50% {
            box-shadow: 0 0 30px rgba(34, 197, 94, 0.4);
          }
          75% {
            box-shadow: 0 0 30px rgba(59, 130, 246, 0.4);
          }
        }

        @keyframes multiDispatchBorder {
          0% { background-position: 0% 50%; }
          100% { background-position: 100% 50%; }
        }

        /* Default - Original blue/red */
        .new-call-card.playing-default {
          animation: defaultPulse 2s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(220, 38, 38, 0.15) 100%);
          border-color: rgba(220, 38, 38, 0.6);
          box-shadow: 0 0 20px rgba(220, 38, 38, 0.3);
        }

        .new-call-card.playing-default::before {
          content: '';
          position: absolute;
          top: -2px;
          left: -2px;
          right: -2px;
          bottom: -2px;
          background: linear-gradient(45deg, 
            rgba(220, 38, 38, 0.8), 
            rgba(59, 130, 246, 0.8), 
            rgba(220, 38, 38, 0.8), 
            rgba(59, 130, 246, 0.8)
          );
          background-size: 400% 400%;
          border-radius: var(--radius);
          z-index: -1;
          animation: defaultBorder 3s ease infinite;
        }

        @keyframes defaultPulse {
          0%, 100% {
            box-shadow: 0 0 20px rgba(220, 38, 38, 0.3);
            border-color: rgba(220, 38, 38, 0.6);
          }
          50% {
            box-shadow: 0 0 30px rgba(59, 130, 246, 0.4);
            border-color: rgba(59, 130, 246, 0.7);
          }
        }

        @keyframes defaultBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        /* Night Mode - Reduced contrast animations */
        .night-mode .new-call-card.playing-law {
          animation: nightLawPulse 3s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.08) 100%);
          border-color: rgba(59, 130, 246, 0.3);
          box-shadow: 0 0 8px rgba(59, 130, 246, 0.15);
        }

        .night-mode .new-call-card.playing-law::before {
          background: linear-gradient(45deg, 
            rgba(59, 130, 246, 0.4), 
            rgba(99, 102, 241, 0.4), 
            rgba(59, 130, 246, 0.4), 
            rgba(99, 102, 241, 0.4)
          );
          animation: nightLawBorder 4s ease infinite;
        }

        @keyframes nightLawPulse {
          0%, 100% {
            box-shadow: 0 0 8px rgba(59, 130, 246, 0.15);
            border-color: rgba(59, 130, 246, 0.3);
          }
          50% {
            box-shadow: 0 0 12px rgba(99, 102, 241, 0.2);
            border-color: rgba(99, 102, 241, 0.4);
          }
        }

        @keyframes nightLawBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        .night-mode .new-call-card.playing-fire {
          animation: nightFirePulse 2.5s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(245, 158, 11, 0.08) 100%);
          border-color: rgba(245, 158, 11, 0.3);
          box-shadow: 0 0 8px rgba(245, 158, 11, 0.15);
        }

        .night-mode .new-call-card.playing-fire::before {
          background: linear-gradient(45deg, 
            rgba(245, 158, 11, 0.4), 
            rgba(217, 119, 6, 0.4), 
            rgba(245, 158, 11, 0.4), 
            rgba(217, 119, 6, 0.4)
          );
          animation: nightFireBorder 3s ease infinite;
        }

        @keyframes nightFirePulse {
          0%, 100% {
            box-shadow: 0 0 8px rgba(245, 158, 11, 0.15);
          }
          50% {
            box-shadow: 0 0 12px rgba(217, 119, 6, 0.2);
          }
        }

        @keyframes nightFireBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        .night-mode .new-call-card.playing-medical {
          animation: nightMedicalPulse 3.5s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(220, 38, 38, 0.06) 100%);
          border-color: rgba(220, 38, 38, 0.25);
          box-shadow: 0 0 6px rgba(220, 38, 38, 0.12);
        }

        .night-mode .new-call-card.playing-medical::before {
          background: linear-gradient(90deg, 
            rgba(220, 38, 38, 0.3), 
            rgba(255, 255, 255, 0.2), 
            rgba(220, 38, 38, 0.3), 
            rgba(255, 255, 255, 0.2)
          );
          animation: nightMedicalBorder 4s linear infinite;
        }

        @keyframes nightMedicalPulse {
          0%, 100% {
            box-shadow: 0 0 6px rgba(220, 38, 38, 0.12);
          }
          50% {
            box-shadow: 0 0 10px rgba(220, 38, 38, 0.18);
          }
        }

        @keyframes nightMedicalBorder {
          0% { background-position: 0% 0%; }
          100% { background-position: 200% 0%; }
        }

        .night-mode .new-call-card.playing-emergency {
          animation: nightEmergencyFlash 1.5s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(220, 38, 38, 0.1) 100%);
          border-color: rgba(220, 38, 38, 0.4);
          box-shadow: 0 0 10px rgba(220, 38, 38, 0.2);
        }

        @keyframes nightEmergencyFlash {
          0%, 100% {
            background: linear-gradient(135deg, var(--bg-card) 0%, rgba(220, 38, 38, 0.1) 100%);
            box-shadow: 0 0 10px rgba(220, 38, 38, 0.2);
          }
          50% {
            background: linear-gradient(135deg, var(--bg-card) 0%, rgba(220, 38, 38, 0.03) 100%);
            box-shadow: 0 0 4px rgba(220, 38, 38, 0.1);
          }
        }

        .night-mode .new-call-card.playing-public-works {
          animation: nightPublicWorksPulse 3s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(245, 158, 11, 0.08) 100%);
          border-color: rgba(245, 158, 11, 0.3);
          box-shadow: 0 0 8px rgba(245, 158, 11, 0.15);
        }

        .night-mode .new-call-card.playing-public-works::before {
          background: repeating-linear-gradient(
            45deg,
            rgba(245, 158, 11, 0.3) 0px,
            rgba(245, 158, 11, 0.3) 6px,
            rgba(0, 0, 0, 0.3) 6px,
            rgba(0, 0, 0, 0.3) 12px
          );
          animation: nightPublicWorksStripes 3s linear infinite;
        }

        @keyframes nightPublicWorksPulse {
          0%, 100% {
            box-shadow: 0 0 8px rgba(245, 158, 11, 0.15);
          }
          50% {
            box-shadow: 0 0 12px rgba(245, 158, 11, 0.2);
          }
        }

        @keyframes nightPublicWorksStripes {
          0% { background-position: 0px 0px; }
          100% { background-position: 24px 0px; }
        }

        .night-mode .new-call-card.playing-schools {
          animation: nightSchoolsPulse 4s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(34, 197, 94, 0.06) 100%);
          border-color: rgba(34, 197, 94, 0.25);
          box-shadow: 0 0 6px rgba(34, 197, 94, 0.12);
        }

        .night-mode .new-call-card.playing-schools::before {
          background: linear-gradient(45deg, 
            rgba(34, 197, 94, 0.3), 
            rgba(59, 130, 246, 0.3), 
            rgba(34, 197, 94, 0.3), 
            rgba(59, 130, 246, 0.3)
          );
          animation: nightSchoolsBorder 5s ease infinite;
        }

        @keyframes nightSchoolsPulse {
          0%, 100% {
            box-shadow: 0 0 6px rgba(34, 197, 94, 0.12);
          }
          50% {
            box-shadow: 0 0 8px rgba(59, 130, 246, 0.15);
          }
        }

        @keyframes nightSchoolsBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        .night-mode .new-call-card.playing-interop {
          animation: nightInteropPulse 3.5s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(147, 51, 234, 0.08) 100%);
          border-color: rgba(147, 51, 234, 0.3);
          box-shadow: 0 0 8px rgba(147, 51, 234, 0.15);
        }

        .night-mode .new-call-card.playing-interop::before {
          background: linear-gradient(45deg, 
            rgba(147, 51, 234, 0.4), 
            rgba(6, 182, 212, 0.4), 
            rgba(147, 51, 234, 0.4), 
            rgba(6, 182, 212, 0.4)
          );
          animation: nightInteropBorder 4s ease infinite;
        }

        @keyframes nightInteropPulse {
          0%, 100% {
            box-shadow: 0 0 8px rgba(147, 51, 234, 0.15);
          }
          50% {
            box-shadow: 0 0 10px rgba(6, 182, 212, 0.18);
          }
        }

        @keyframes nightInteropBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        .night-mode .new-call-card.playing-multi-dispatch {
          animation: nightMultiDispatchPulse 2.5s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(168, 85, 247, 0.08) 100%);
          border-color: rgba(168, 85, 247, 0.3);
          box-shadow: 0 0 8px rgba(168, 85, 247, 0.15);
        }

        .night-mode .new-call-card.playing-multi-dispatch::before {
          background: linear-gradient(45deg, 
            rgba(168, 85, 247, 0.3), 
            rgba(59, 130, 246, 0.3), 
            rgba(34, 197, 94, 0.3), 
            rgba(245, 158, 11, 0.3),
            rgba(168, 85, 247, 0.3)
          );
          animation: nightMultiDispatchBorder 4s ease infinite;
        }

        @keyframes nightMultiDispatchPulse {
          0%, 100% {
            box-shadow: 0 0 8px rgba(168, 85, 247, 0.15);
          }
          25% {
            box-shadow: 0 0 10px rgba(59, 130, 246, 0.18);
          }
          50% {
            box-shadow: 0 0 10px rgba(34, 197, 94, 0.18);
          }
          75% {
            box-shadow: 0 0 10px rgba(245, 158, 11, 0.18);
          }
        }

        @keyframes nightMultiDispatchBorder {
          0% { background-position: 0% 50%; }
          100% { background-position: 100% 50%; }
        }

        .night-mode .new-call-card.playing-default {
          animation: nightDefaultPulse 3s ease-in-out infinite;
          background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.08) 100%);
          border-color: rgba(59, 130, 246, 0.3);
          box-shadow: 0 0 8px rgba(59, 130, 246, 0.15);
        }

        .night-mode .new-call-card.playing-default::before {
          background: linear-gradient(45deg, 
            rgba(59, 130, 246, 0.4), 
            rgba(99, 102, 241, 0.4), 
            rgba(59, 130, 246, 0.4), 
            rgba(99, 102, 241, 0.4)
          );
          animation: nightDefaultBorder 4s ease infinite;
        }

        @keyframes nightDefaultPulse {
          0%, 100% {
            box-shadow: 0 0 8px rgba(59, 130, 246, 0.15);
          }
          50% {
            box-shadow: 0 0 12px rgba(99, 102, 241, 0.2);
          }
        }

        @keyframes nightDefaultBorder {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }

        /* Override age-based styles when playing - applies to all category animations */
        .new-call-card[class*="playing-"].age-fresh,
        .new-call-card[class*="playing-"].age-recent,
        .new-call-card[class*="playing-"].age-normal,
        .new-call-card[class*="playing-"].age-aging,
        .new-call-card[class*="playing-"].age-old {
          opacity: 1;
        }

        /* Hover effects with age consideration */
        .new-call-card:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          transform: translateY(-2px) translateZ(0);
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        }

        .new-call-card.age-fresh:hover {
          box-shadow: 0 4px 16px rgba(34, 197, 94, 0.2);
        }

        .new-call-card.age-old:hover {
          opacity: 0.9;
          transform: translateY(-1px) translateZ(0);
        }

        .new-call-card[class*="playing-"]:hover {
          transform: scale(1.03) translateY(-1px);
        }

        .new-call-card:focus {
          outline: 2px solid var(--accent-primary);
          outline-offset: 2px;
        }

        .call-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--space-2);
          gap: var(--space-2);
        }

        .call-talkgroup {
          display: flex;
          align-items: flex-start;
          gap: var(--space-2);
          flex: 1;
          min-width: 0;
          flex-direction: column;
        }

        .badges {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          flex-wrap: wrap;
          margin-top: var(--space-1);
        }

        .talkgroup-link {
          color: var(--text-primary);
          text-decoration: none;
          font-weight: 600;
          font-size: var(--font-size-lg);
          transition: all 0.3s ease;
          word-break: break-word;
        }

        .talkgroup-link:hover {
          color: var(--accent-primary);
          transform: translateX(2px);
        }

        .talkgroup-link.playing {
          color: var(--accent-primary);
          animation: playingText 1.5s ease-in-out infinite;
        }

        .badge {
          padding: 2px 6px;
          border-radius: var(--radius-sm);
          font-size: var(--font-size-xs);
          font-weight: 600;
          flex-shrink: 0;
          line-height: 1.2;
          transition: all 0.3s ease;
        }

        .priority-badge {
          background: var(--accent-secondary);
          color: white;
        }

        .priority-badge.priority-critical {
          background: linear-gradient(135deg, #ef4444, #dc2626);
          animation: criticalPulse 2s ease-in-out infinite;
          box-shadow: 0 0 8px rgba(239, 68, 68, 0.5);
        }

        .priority-badge.priority-high {
          background: linear-gradient(135deg, #f59e0b, #d97706);
          animation: highPriorityGlow 3s ease-in-out infinite;
        }

        @keyframes criticalPulse {
          0%, 100% {
            box-shadow: 0 0 8px rgba(239, 68, 68, 0.5);
          }
          50% {
            box-shadow: 0 0 16px rgba(239, 68, 68, 0.8);
          }
        }

        @keyframes highPriorityGlow {
          0%, 100% {
            box-shadow: 0 0 4px rgba(245, 158, 11, 0.3);
          }
          50% {
            box-shadow: 0 0 8px rgba(245, 158, 11, 0.6);
          }
        }

        .tag-badge {
          background: var(--accent-primary);
          color: white;
        }

        .category-badge {
          background: rgba(147, 51, 234, 0.9);
          color: white;
        }

        .alpha-badge {
          background: rgba(34, 197, 94, 0.9);
          color: white;
        }

        .call-actions {
          display: flex;
          gap: var(--space-1);
          flex-shrink: 0;
        }

        .subscribe-btn,
        .share-btn {
          background: none;
          border: 1px solid var(--border);
          color: var(--text-secondary);
          width: 32px;
          height: 32px;
          border-radius: var(--radius-sm);
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: var(--font-size-sm);
        }

        .subscribe-btn:hover,
        .share-btn:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
        }

        .subscribe-btn.subscribed {
          background: var(--accent-primary);
          border-color: var(--accent-primary);
          color: white;
        }

        .subscribe-btn.pending {
          opacity: 0.6;
          cursor: not-allowed;
        }

        .subscribe-btn.pending:hover {
          background: none;
          border-color: var(--border);
          color: var(--text-secondary);
        }

        .call-meta {
          display: flex;
          gap: var(--space-2);
          margin-bottom: var(--space-2);
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
          flex-wrap: wrap;
        }

        .call-meta span {
          white-space: nowrap;
          transition: all 0.3s ease;
        }

        .call-time.playing {
          color: rgba(220, 38, 38, 0.9);
          font-weight: 700;
          background: linear-gradient(90deg, rgba(220, 38, 38, 0.9), rgba(59, 130, 246, 0.9));
          background-clip: text;
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
          animation: playingText 1.5s ease-in-out infinite;
        }

        @keyframes playingText {
          0%, 100% {
            opacity: 1;
          }
          50% {
            opacity: 0.7;
          }
        }

        .call-age.age-fresh {
          color: rgba(34, 197, 94, 0.8);
          font-weight: 600;
        }

        .call-age.age-recent {
          color: rgba(59, 130, 246, 0.8);
        }

        .call-age.age-aging {
          color: rgba(245, 158, 11, 0.8);
        }

        .call-age.age-old {
          color: rgba(107, 114, 128, 0.6);
        }

        .call-transcript {
          margin-bottom: var(--space-1);
        }

        .call-transcript p {
          margin: 0;
          line-height: 1.5;
          color: var(--text-primary);
        }

        .call-transcript .no-transcript {
          color: var(--text-secondary);
          font-style: italic;
        }

        @media (max-width: 767px) {
          .new-call-card {
            padding: var(--space-1-5);
          }

          .call-header {
            margin-bottom: var(--space-1-5);
          }
          
          .call-talkgroup {
            flex-direction: column;
            align-items: flex-start;
            gap: var(--space-1);
          }

          .badges {
            margin-top: 0;
            gap: 4px;
          }

          .badge {
            padding: 1px 4px;
            font-size: 10px;
          }

          .talkgroup-link {
            font-size: var(--font-size-base);
            flex-shrink: 0;
          }
          
          .call-meta {
            display: grid;
            grid-template-columns: auto auto;
            gap: var(--space-1) var(--space-2);
            margin-bottom: var(--space-1-5);
            font-size: var(--font-size-xs);
          }

          .call-meta span:nth-child(1) { /* time */
            grid-column: 1;
            grid-row: 1;
          }

          .call-meta span:nth-child(2) { /* duration */
            grid-column: 2;
            grid-row: 1;
            justify-self: end;
          }

          .call-meta span:nth-child(3) { /* frequency */
            grid-column: 1;
            grid-row: 2;
          }

          .call-meta span:nth-child(4) { /* age */
            grid-column: 2;
            grid-row: 2;
            justify-self: end;
          }

          .call-transcript {
            margin-bottom: var(--space-1);
          }

          .call-transcript p {
            font-size: var(--font-size-sm);
            line-height: 1.4;
          }
        }
      `}</style>
    </article>
  )
}
