import type { InvestigationReport } from '../types/dtos'
import { formatDateTime } from '../utils/format'

/**
 * Renders a structured InvestigationReport (narrative, related calls, entities,
 * timeline, follow-ups). Shared by the investigations list page and the live
 * chat page so the JSON output is rendered identically in both places.
 */
export default function InvestigationReportView({ report }: { report: InvestigationReport }) {
  return (
    <>
      <p className="investigation-narrative">{report.narrative}</p>

      {report.relatedCalls && report.relatedCalls.length > 0 && (
        <div className="investigation-section">
          <h3>Related Calls ({report.relatedCalls.length})</h3>
          <div className="related-calls">
            {report.relatedCalls.map(rc => (
              <div key={rc.callId} className="related-call">
                <div className="related-call-meta">
                  <time className="timeline-time">{formatDateTime(rc.recordingTime)}</time>
                  <span className="badge tg-badge">TG {rc.talkGroupNumber}</span>
                </div>
                <p className="related-call-relevance">{rc.relevance}</p>
                <details className="related-call-transcript">
                  <summary>Transcript</summary>
                  <p className="transcript">{rc.transcript}</p>
                </details>
              </div>
            ))}
          </div>
        </div>
      )}

      {report.entities && report.entities.length > 0 && (
        <div className="investigation-section">
          <h3>Key Entities</h3>
          <div className="entity-list">
            {report.entities.map(e => (
              <span key={`${e.kind}-${e.name}`} className={`badge entity entity-${e.kind}`}>
                {e.name}
                <span className="entity-count">×{e.mentionCount}</span>
              </span>
            ))}
          </div>
        </div>
      )}

      {report.timeline && report.timeline.length > 0 && (
        <div className="investigation-section">
          <h3>Timeline</h3>
          <ol className="timeline">
            {report.timeline.map((ev, i) => (
              <li key={i} className="timeline-event">
                {ev.time && <time className="timeline-time">{formatDateTime(ev.time)}</time>}
                <span className="timeline-desc">{ev.description}</span>
              </li>
            ))}
          </ol>
        </div>
      )}

      {report.followUpQuestions && report.followUpQuestions.length > 0 && (
        <div className="investigation-section">
          <h3>Suggested Follow-ups</h3>
          <ul className="followup-list">
            {report.followUpQuestions.map((fq, i) => (
              <li key={i} className="followup-item">
                <span className="sub">{fq}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </>
  )
}
