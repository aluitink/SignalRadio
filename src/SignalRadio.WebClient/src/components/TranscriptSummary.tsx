import React, { useState, useEffect } from 'react'
import { TranscriptSummaryDto, NotableIncidentDto, CallDto } from '../types/dtos'
import { apiGet } from '../api'
import { audioPlayerService } from '../services/AudioPlayerService'

interface TranscriptSummaryProps {
  talkGroupId: number
  talkGroupName: string
}

export default function TranscriptSummary({ talkGroupId, talkGroupName }: TranscriptSummaryProps) {
  const [summary, setSummary] = useState<TranscriptSummaryDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [windowMinutes, setWindowMinutes] = useState(60)
  const [isExpanded, setIsExpanded] = useState(false)
  const [serviceAvailable, setServiceAvailable] = useState<boolean | null>(null)
  const [showingIncident, setShowingIncident] = useState<NotableIncidentDto | null>(null)
  const [incidentCalls, setIncidentCalls] = useState<any[]>([])
  const [loadingCalls, setLoadingCalls] = useState(false)

  useEffect(() => {
    checkServiceAvailability()
  }, [])

  const checkServiceAvailability = async () => {
    try {
      const status = await apiGet<{ available: boolean }>('/transcriptsummary/status')
      setServiceAvailable(status?.available || false)
    } catch (err) {
      console.error('Failed to check summary service status:', err)
      setServiceAvailable(false)
    }
  }

  const loadSummary = async (forceRefresh = false) => {
    if (!serviceAvailable) return

    setLoading(true)
    setError(null)
    
    try {
      const params = new URLSearchParams({
        windowMinutes: windowMinutes.toString(),
        forceRefresh: forceRefresh.toString()
      })
      
      const result = await apiGet<TranscriptSummaryDto>(`/talkgroups/${talkGroupId}/summary?${params}`)
      setSummary(result)
    } catch (err) {
      console.error('Failed to load transcript summary:', err)
      setError('Failed to load summary. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const handleRefresh = () => {
    loadSummary(true)
  }

  const formatDuration = (seconds: number) => {
    const hours = Math.floor(seconds / 3600)
    const minutes = Math.floor((seconds % 3600) / 60)
    const remainingSeconds = seconds % 60
    
    if (hours > 0) {
      return `${hours}h ${minutes}m ${remainingSeconds}s`
    } else if (minutes > 0) {
      return `${minutes}m ${remainingSeconds}s`
    } else {
      return `${remainingSeconds}s`
    }
  }

  const formatTimeAgo = (dateString: string) => {
    const date = new Date(dateString)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMinutes = Math.floor(diffMs / (1000 * 60))
    
    if (diffMinutes < 1) return 'just now'
    if (diffMinutes < 60) return `${diffMinutes} minutes ago`
    
    const diffHours = Math.floor(diffMinutes / 60)
    if (diffHours < 24) return `${diffHours} hours ago`
    
    const diffDays = Math.floor(diffHours / 24)
    return `${diffDays} days ago`
  }

  const handleShowIncidentCalls = async (incident: NotableIncidentDto) => {
    setShowingIncident(incident)
    setLoadingCalls(true)
    setIncidentCalls([])

    try {
      // Fetch calls one by one
      const calls = []
      for (const callId of incident.callIds) {
        try {
          const call = await apiGet(`/calls/${callId}`)
          calls.push(call)
        } catch (err) {
          console.error(`Failed to fetch call ${callId}:`, err)
        }
      }
      setIncidentCalls(calls)
    } catch (err) {
      console.error('Failed to fetch incident calls:', err)
    } finally {
      setLoadingCalls(false)
    }
  }

  const closeIncidentModal = () => {
    setShowingIncident(null)
    setIncidentCalls([])
  }

  const handleCallLinkClick = async (callId: number) => {
    try {
      // Fetch the call data
      const call = await apiGet<CallDto>(`/calls/${callId}`)
      
      // Clear existing queue and add this call
      audioPlayerService.clearQueue()
      audioPlayerService.addToQueue(call)
      
      // Start playing if not already playing
      if (audioPlayerService.getState() === 'stopped') {
        audioPlayerService.play().catch(error => {
          console.error('Failed to start audio player:', error)
        })
      }
    } catch (error) {
      console.error(`Failed to load call ${callId}:`, error)
    }
  }

  const handleIncidentCallsClick = async (incident: NotableIncidentDto) => {
    try {
      // Clear existing queue
      audioPlayerService.clearQueue()
      
      // Fetch all calls for this incident and add them to queue
      for (const callId of incident.callIds) {
        try {
          const call = await apiGet<CallDto>(`/calls/${callId}`)
          audioPlayerService.addToQueue(call)
        } catch (err) {
          console.error(`Failed to fetch call ${callId}:`, err)
        }
      }
      
      // Start playing if not already playing
      if (audioPlayerService.getState() === 'stopped') {
        audioPlayerService.play().catch(error => {
          console.error('Failed to start audio player:', error)
        })
      }
    } catch (error) {
      console.error('Failed to load incident calls:', error)
    }
    
    // Also show the modal for call details
    handleShowIncidentCalls(incident)
  }

  if (serviceAvailable === false) {
    return (
      <div className="transcript-summary-card disabled">
        <div className="summary-header">
          <h3>AI Summary</h3>
          <div className="status-badge unavailable">Service Unavailable</div>
        </div>
        <p className="no-summary">
          AI transcript summary service is not configured or available.
        </p>
        
        <style>{`
          .transcript-summary-card.disabled {
            background: var(--bg-card);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: var(--space-4);
            margin: var(--space-4) 0;
            opacity: 0.6;
          }
          
          .status-badge.unavailable {
            background: var(--color-error-bg);
            color: var(--color-error);
            padding: 2px 8px;
            border-radius: 4px;
            font-size: 12px;
            font-weight: 500;
          }
        `}</style>
      </div>
    )
  }

  if (serviceAvailable === null) {
    return <div className="transcript-summary-loading">Checking AI service...</div>
  }

  return (
    <div className="transcript-summary-card">
      <div className="summary-header">
        <h3>AI Summary</h3>
        <div className="summary-controls">
          <select 
            value={windowMinutes} 
            onChange={(e) => setWindowMinutes(parseInt(e.target.value))}
            disabled={loading}
          >
            <option value={15}>Last 15 minutes</option>
            <option value={30}>Last 30 minutes</option>
            <option value={60}>Last hour</option>
            <option value={180}>Last 3 hours</option>
            <option value={360}>Last 6 hours</option>
            <option value={720}>Last 12 hours</option>
            <option value={1440}>Last 24 hours</option>
          </select>
          <button 
            onClick={() => loadSummary(false)} 
            disabled={loading}
            className="btn-secondary"
          >
            {loading ? 'Loading...' : 'Generate'}
          </button>
        </div>
      </div>

      {error && (
        <div className="error-message">
          {error}
          <button onClick={handleRefresh} className="btn-link">Retry</button>
        </div>
      )}

      {summary && (
        <div className="summary-content">
          <div className="summary-metadata">
            <span className="transcript-count">
              {summary.transcriptCount} calls analyzed
            </span>
            <span className="duration">
              {formatDuration(summary.totalDurationSeconds)} total
            </span>
            <span className="generated-time">
              Generated {formatTimeAgo(summary.generatedAt)}
              {summary.fromCache && <span className="cached-badge">Cached</span>}
            </span>
            {summary.transcriptCount > 0 && (
              <button 
                onClick={handleRefresh} 
                disabled={loading}
                className="refresh-btn"
                title="Refresh summary"
              >
                ↻
              </button>
            )}
          </div>

          {summary.transcriptCount === 0 ? (
            <p className="no-activity">
              No radio communications recorded in the last {windowMinutes} minutes.
            </p>
          ) : (
            <>
              {/* Show Notable Incidents First */}
              {(summary.notableIncidents.length > 0 || summary.notableIncidentsWithCallIds?.length > 0) && (
                <div className="notable-incidents">
                  <h4>Notable Incidents</h4>
                  <ul>
                    {/* Show incidents with call IDs first (with links) */}
                    {summary.notableIncidentsWithCallIds?.map((incident, index) => (
                      <li key={`with-id-${index}`}>
                        {incident.description}
                        {incident.callIds && incident.callIds.length > 0 && (
                          <>
                            {' '}
                            {incident.callIds.length === 1 ? (
                              <button
                                className="call-link"
                                onClick={() => handleCallLinkClick(incident.callIds[0])}
                                title={`Play Call ${incident.callIds[0]}`}
                              >
                                [Call #{incident.callIds[0]}]
                              </button>
                            ) : (
                              <button
                                className="calls-link"
                                onClick={() => handleIncidentCallsClick(incident)}
                                title={`Play ${incident.callIds.length} related calls`}
                              >
                                [Play {incident.callIds.length} calls]
                              </button>
                            )}
                          </>
                        )}
                      </li>
                    ))}
                    
                    {/* Show legacy incidents without call IDs */}
                    {summary.notableIncidents.map((incident, index) => (
                      <li key={`legacy-${index}`}>{incident}</li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Show Key Topics (Categories) Second */}
              {summary.keyTopics.length > 0 && (
                <div className="key-topics">
                  <h4>Categories</h4>
                  <div className="topics-list">
                    {summary.keyTopics.map((topic, index) => (
                      <span key={index} className="topic-tag">{topic}</span>
                    ))}
                  </div>
                </div>
              )}

              {/* Show Summary Text Last */}
              <div className="summary-text">
                {isExpanded ? (
                  <p>{summary.summary}</p>
                ) : (
                  <p>
                    {summary.summary.length > 200 
                      ? `${summary.summary.substring(0, 200)}...` 
                      : summary.summary
                    }
                  </p>
                )}
                {summary.summary.length > 200 && (
                  <button 
                    onClick={() => setIsExpanded(!isExpanded)}
                    className="expand-btn"
                  >
                    {isExpanded ? 'Show less' : 'Show more'}
                  </button>
                )}
              </div>
            </>
          )}
        </div>
      )}

      {/* Incident Calls Modal */}
      {showingIncident && (
        <div className="incident-modal-overlay" onClick={closeIncidentModal}>
          <div className="incident-modal" onClick={(e) => e.stopPropagation()}>
            <div className="incident-modal-header">
              <h3>Incident Details</h3>
              <button 
                className="close-btn" 
                onClick={closeIncidentModal}
                aria-label="Close modal"
              >
                ×
              </button>
            </div>
            
            <div className="incident-description">
              <p>{showingIncident.description}</p>
            </div>

            <div className="incident-calls">
              <h4>Related Calls ({showingIncident.callIds.length})</h4>
              
              {loadingCalls && (
                <div className="loading">Loading calls...</div>
              )}
              
              {!loadingCalls && incidentCalls.length > 0 && (
                <div className="calls-list">
                  {incidentCalls.map((call, index) => (
                    <div key={call.id || index} className="call-card">
                      <div className="call-header">
                        <span className="call-id">Call #{call.id}</span>
                        <span className="call-time">
                          {new Date(call.recordingTime || call.createdAt).toLocaleString()}
                        </span>
                      </div>
                      
                      {call.recordings?.some((r: any) => r.hasTranscription) && (
                        <div className="call-transcript">
                          <strong>Transcript:</strong>
                          <p>{call.recordings.find((r: any) => r.hasTranscription)?.transcriptionText}</p>
                        </div>
                      )}
                      
                      <div className="call-details">
                        <span>Duration: {call.duration || 'N/A'}</span>
                        <span>Frequency: {call.frequency || 'N/A'}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      <style>{`
        .transcript-summary-card {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: 8px;
          padding: var(--space-4);
          margin: var(--space-4) 0;
        }

        .summary-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: var(--space-3);
        }

        .summary-header h3 {
          margin: 0;
          color: var(--color-text-primary);
        }

        .summary-controls {
          display: flex;
          gap: var(--space-2);
          align-items: center;
        }

        .summary-controls select {
          background: var(--bg-input);
          border: 1px solid var(--border);
          border-radius: 4px;
          padding: 4px 8px;
          color: var(--color-text-primary);
          font-size: 14px;
        }

        .btn-secondary {
          background: var(--bg-secondary);
          color: var(--color-text-secondary);
          border: 1px solid var(--border);
          padding: 4px 12px;
          border-radius: 4px;
          cursor: pointer;
          font-size: 14px;
          transition: all 0.2s;
        }

        .btn-secondary:hover:not(:disabled) {
          background: var(--bg-hover);
        }

        .btn-secondary:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }

        .error-message {
          background: var(--color-error-bg);
          color: var(--color-error);
          padding: var(--space-2);
          border-radius: 4px;
          margin-bottom: var(--space-3);
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .btn-link {
          background: none;
          border: none;
          color: var(--color-error);
          text-decoration: underline;
          cursor: pointer;
          font-size: 14px;
        }

        .summary-metadata {
          display: flex;
          gap: var(--space-3);
          align-items: center;
          margin-bottom: var(--space-3);
          font-size: 14px;
          color: var(--color-text-secondary);
          flex-wrap: wrap;
        }

        .cached-badge {
          background: var(--bg-secondary);
          color: var(--color-text-secondary);
          padding: 1px 4px;
          border-radius: 3px;
          font-size: 11px;
          margin-left: 4px;
        }

        .refresh-btn {
          background: none;
          border: none;
          color: var(--color-text-secondary);
          cursor: pointer;
          font-size: 16px;
          padding: 2px;
          border-radius: 2px;
          transition: background-color 0.2s;
        }

        .refresh-btn:hover:not(:disabled) {
          background: var(--bg-hover);
        }

        .no-activity {
          color: var(--color-text-secondary);
          font-style: italic;
          text-align: center;
          padding: var(--space-4);
        }

        .summary-text {
          margin-bottom: var(--space-3);
          line-height: 1.6;
        }

        .expand-btn {
          background: none;
          border: none;
          color: var(--color-primary);
          cursor: pointer;
          font-size: 14px;
          text-decoration: underline;
          padding: 0;
          margin-left: var(--space-2);
        }

        .key-topics {
          margin-bottom: var(--space-3);
        }

        .key-topics h4 {
          margin: 0 0 var(--space-2) 0;
          font-size: 16px;
          color: var(--color-text-primary);
        }

        .topics-list {
          display: flex;
          gap: var(--space-2);
          flex-wrap: wrap;
        }

        .topic-tag {
          background: var(--bg-secondary);
          color: var(--color-text-primary);
          padding: 4px 8px;
          border-radius: 16px;
          font-size: 12px;
          font-weight: 500;
        }

        .notable-incidents {
          margin-bottom: var(--space-2);
        }

        .notable-incidents h4 {
          margin: 0 0 var(--space-2) 0;
          font-size: 16px;
          color: var(--color-text-primary);
        }

        .notable-incidents ul {
          margin: 0;
          padding-left: var(--space-4);
        }

        .notable-incidents li {
          margin-bottom: var(--space-1);
          line-height: 1.5;
        }

        .call-link {
          background: none;
          border: none;
          color: var(--color-primary);
          text-decoration: none;
          font-weight: 500;
          font-size: 0.9em;
          padding: 2px 4px;
          border-radius: 3px;
          transition: background-color 0.2s;
          cursor: pointer;
          font-family: inherit;
        }

        .call-link:hover {
          background: var(--bg-hover);
          text-decoration: underline;
        }

          .transcript-summary-loading {
          text-align: center;
          color: var(--color-text-secondary);
          font-style: italic;
          padding: var(--space-4);
        }

        .calls-link {
          background: none;
          border: none;
          color: var(--color-primary);
          cursor: pointer;
          font-size: 12px;
          font-weight: 500;
          text-decoration: underline;
          padding: 0;
        }

        .calls-link:hover {
          color: var(--color-primary-hover);
        }

        .incident-modal-overlay {
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: rgba(0, 0, 0, 0.5);
          display: flex;
          align-items: center;
          justify-content: center;
          z-index: 1000;
          padding: var(--space-4);
        }

        .incident-modal {
          background: var(--bg-primary);
          border-radius: 8px;
          max-width: 600px;
          width: 100%;
          max-height: 80vh;
          overflow: hidden;
          display: flex;
          flex-direction: column;
          box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
        }

        .incident-modal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: var(--space-4);
          border-bottom: 1px solid var(--border);
        }

        .incident-modal-header h3 {
          margin: 0;
          color: var(--color-text-primary);
        }

        .close-btn {
          background: none;
          border: none;
          font-size: 24px;
          cursor: pointer;
          color: var(--color-text-secondary);
          padding: 0;
          width: 32px;
          height: 32px;
          display: flex;
          align-items: center;
          justify-content: center;
          border-radius: 4px;
        }

        .close-btn:hover {
          background: var(--bg-hover);
        }

        .incident-description {
          padding: var(--space-4);
          border-bottom: 1px solid var(--border);
        }

        .incident-description p {
          margin: 0;
          line-height: 1.6;
          color: var(--color-text-primary);
        }

        .incident-calls {
          padding: var(--space-4);
          overflow-y: auto;
          flex: 1;
        }

        .incident-calls h4 {
          margin: 0 0 var(--space-3) 0;
          color: var(--color-text-primary);
        }

        .loading {
          text-align: center;
          color: var(--color-text-secondary);
          font-style: italic;
          padding: var(--space-4);
        }

        .calls-list {
          display: flex;
          flex-direction: column;
          gap: var(--space-3);
        }

        .call-card {
          background: var(--bg-secondary);
          border: 1px solid var(--border);
          border-radius: 6px;
          padding: var(--space-3);
        }

        .call-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: var(--space-2);
        }

        .call-id {
          font-weight: 600;
          color: var(--color-primary);
        }

        .call-time {
          color: var(--color-text-secondary);
          font-size: 12px;
        }

        .call-transcript {
          margin-bottom: var(--space-2);
        }

        .call-transcript strong {
          color: var(--color-text-primary);
          display: block;
          margin-bottom: var(--space-1);
        }

        .call-transcript p {
          margin: 0;
          background: var(--bg-card);
          padding: var(--space-2);
          border-radius: 4px;
          border-left: 3px solid var(--color-primary);
          line-height: 1.5;
          font-size: 14px;
        }

        .call-details {
          display: flex;
          gap: var(--space-3);
          font-size: 12px;
          color: var(--color-text-secondary);
        }        @media (max-width: 768px) {
          .summary-header {
            flex-direction: column;
            gap: var(--space-2);
            align-items: stretch;
          }

          .summary-controls {
            justify-content: space-between;
          }

          .summary-metadata {
            flex-direction: column;
            align-items: flex-start;
            gap: var(--space-1);
          }

          .topics-list {
            justify-content: flex-start;
          }
        }
      `}</style>
    </div>
  )
}
