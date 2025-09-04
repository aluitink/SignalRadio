import React, { useState, useMemo } from 'react'
import { usePageTitle } from '../hooks/usePageTitle'

interface RadioCode {
  code: string
  meaning: string
  category: string
}

const radioCodes: RadioCode[] = [
  // 10 Codes
  { code: '10-1', meaning: 'Unable to copy, change location', category: '10 Codes' },
  { code: '10-2', meaning: 'Signal good', category: '10 Codes' },
  { code: '10-3', meaning: 'Stop transmitting', category: '10 Codes' },
  { code: '10-4', meaning: 'Affirmative/OK', category: '10 Codes' },
  { code: '10-5', meaning: 'Relay message', category: '10 Codes' },
  { code: '10-6', meaning: 'Busy, stand by unless urgent', category: '10 Codes' },
  { code: '10-7', meaning: 'Out of service', category: '10 Codes' },
  { code: '10-8', meaning: 'In service', category: '10 Codes' },
  { code: '10-9', meaning: 'Repeat message', category: '10 Codes' },
  { code: '10-10', meaning: 'Fight in progress', category: '10 Codes' },
  { code: '10-11', meaning: 'Dog case', category: '10 Codes' },
  { code: '10-12', meaning: 'Standby', category: '10 Codes' },
  { code: '10-13', meaning: 'Weather and road conditions', category: '10 Codes' },
  { code: '10-14', meaning: 'Prowler report', category: '10 Codes' },
  { code: '10-15', meaning: 'Civil disturbance', category: '10 Codes' },
  { code: '10-16', meaning: 'Domestic problem', category: '10 Codes' },
  { code: '10-17', meaning: 'Meet complainant', category: '10 Codes' },
  { code: '10-18', meaning: 'Complete assignment quickly', category: '10 Codes' },
  { code: '10-19', meaning: 'Nothing for you, return to station', category: '10 Codes' },
  { code: '10-20', meaning: 'My location is...', category: '10 Codes' },
  { code: '10-21', meaning: 'Call by telephone', category: '10 Codes' },
  { code: '10-22', meaning: 'Report in person to...', category: '10 Codes' },
  { code: '10-23', meaning: 'Stand by', category: '10 Codes' },
  { code: '10-24', meaning: 'Completed last assignment', category: '10 Codes' },
  { code: '10-25', meaning: 'Can you contact...', category: '10 Codes' },
  { code: '10-26', meaning: 'Detaining subject, expedite', category: '10 Codes' },
  { code: '10-27', meaning: 'Drivers license check', category: '10 Codes' },
  { code: '10-28', meaning: 'Vehicle registration information', category: '10 Codes' },
  { code: '10-29', meaning: 'Check for wanted', category: '10 Codes' },
  { code: '10-30', meaning: 'Illegal use of radio', category: '10 Codes' },
  { code: '10-31', meaning: 'Crime in progress', category: '10 Codes' },
  { code: '10-32', meaning: 'Person with gun', category: '10 Codes' },
  { code: '10-33', meaning: 'Emergency', category: '10 Codes' },
  { code: '10-34', meaning: 'Riot', category: '10 Codes' },
  { code: '10-35', meaning: 'Major crime alert', category: '10 Codes' },
  { code: '10-36', meaning: 'Correct time', category: '10 Codes' },
  { code: '10-37', meaning: 'Investigate suspicious vehicle', category: '10 Codes' },
  { code: '10-38', meaning: 'Stopping suspicious vehicle', category: '10 Codes' },
  { code: '10-39', meaning: 'Urgent, use light, siren', category: '10 Codes' },
  { code: '10-40', meaning: 'Silent run, no light, siren', category: '10 Codes' },

  // Common Police Codes
  { code: '10-50', meaning: 'Accident (fatal)', category: 'Police Codes' },
  { code: '10-51', meaning: 'Wrecker needed', category: 'Police Codes' },
  { code: '10-52', meaning: 'Ambulance needed', category: 'Police Codes' },
  { code: '10-53', meaning: 'Road blocked', category: 'Police Codes' },
  { code: '10-54', meaning: 'Livestock on highway', category: 'Police Codes' },
  { code: '10-55', meaning: 'Intoxicated driver', category: 'Police Codes' },
  { code: '10-56', meaning: 'Intoxicated pedestrian', category: 'Police Codes' },
  { code: '10-57', meaning: 'Hit and run (fatal)', category: 'Police Codes' },
  { code: '10-58', meaning: 'Direct traffic', category: 'Police Codes' },
  { code: '10-59', meaning: 'Convoy or escort duty', category: 'Police Codes' },
  { code: '10-60', meaning: 'Squad in vicinity', category: 'Police Codes' },
  { code: '10-70', meaning: 'Fire', category: 'Police Codes' },
  { code: '10-71', meaning: 'Advise nature of fire', category: 'Police Codes' },
  { code: '10-72', meaning: 'Report progress on fire', category: 'Police Codes' },
  { code: '10-73', meaning: 'Smoke report', category: 'Police Codes' },
  { code: '10-74', meaning: 'Negative', category: 'Police Codes' },
  { code: '10-75', meaning: 'In contact with...', category: 'Police Codes' },
  { code: '10-76', meaning: 'Estimated time of arrival', category: 'Police Codes' },
  { code: '10-77', meaning: 'Estimated time available', category: 'Police Codes' },
  { code: '10-78', meaning: 'Need assistance', category: 'Police Codes' },
  { code: '10-79', meaning: 'Notify coroner', category: 'Police Codes' },
  { code: '10-80', meaning: 'Chase in progress', category: 'Police Codes' },
  { code: '10-90', meaning: 'Bank alarm at...', category: 'Police Codes' },
  { code: '10-91', meaning: 'Pick up prisoner/subject', category: 'Police Codes' },
  { code: '10-92', meaning: 'Improperly parked vehicle', category: 'Police Codes' },
  { code: '10-93', meaning: 'Blockade', category: 'Police Codes' },
  { code: '10-94', meaning: 'Drag racing', category: 'Police Codes' },
  { code: '10-95', meaning: 'Prisoner/subject in custody', category: 'Police Codes' },
  { code: '10-96', meaning: 'Mental subject', category: 'Police Codes' },
  { code: '10-97', meaning: 'Check (test) signal', category: 'Police Codes' },
  { code: '10-98', meaning: 'Prison/jail break', category: 'Police Codes' },
  { code: '10-99', meaning: 'Wanted/stolen indicated', category: 'Police Codes' },

  // Fire/EMS Codes
  { code: 'Code 1', meaning: 'Routine response', category: 'Fire/EMS' },
  { code: 'Code 2', meaning: 'Urgent response', category: 'Fire/EMS' },
  { code: 'Code 3', meaning: 'Emergency response (lights & siren)', category: 'Fire/EMS' },
  { code: 'Code 4', meaning: 'No further assistance needed', category: 'Fire/EMS' },
  { code: 'Code 5', meaning: 'Stakeout', category: 'Fire/EMS' },
  { code: 'Code 6', meaning: 'Responding from a long distance', category: 'Fire/EMS' },
  { code: 'Code 7', meaning: 'Mealtime', category: 'Fire/EMS' },
  { code: 'Code 8', meaning: 'Request cover/backup', category: 'Fire/EMS' },
  { code: 'Code 9', meaning: 'Set up a roadblock', category: 'Fire/EMS' },

  // Phonetic Alphabet
  { code: 'Alpha', meaning: 'A', category: 'Phonetic Alphabet' },
  { code: 'Bravo', meaning: 'B', category: 'Phonetic Alphabet' },
  { code: 'Charlie', meaning: 'C', category: 'Phonetic Alphabet' },
  { code: 'Delta', meaning: 'D', category: 'Phonetic Alphabet' },
  { code: 'Echo', meaning: 'E', category: 'Phonetic Alphabet' },
  { code: 'Foxtrot', meaning: 'F', category: 'Phonetic Alphabet' },
  { code: 'Golf', meaning: 'G', category: 'Phonetic Alphabet' },
  { code: 'Hotel', meaning: 'H', category: 'Phonetic Alphabet' },
  { code: 'India', meaning: 'I', category: 'Phonetic Alphabet' },
  { code: 'Juliet', meaning: 'J', category: 'Phonetic Alphabet' },
  { code: 'Kilo', meaning: 'K', category: 'Phonetic Alphabet' },
  { code: 'Lima', meaning: 'L', category: 'Phonetic Alphabet' },
  { code: 'Mike', meaning: 'M', category: 'Phonetic Alphabet' },
  { code: 'November', meaning: 'N', category: 'Phonetic Alphabet' },
  { code: 'Oscar', meaning: 'O', category: 'Phonetic Alphabet' },
  { code: 'Papa', meaning: 'P', category: 'Phonetic Alphabet' },
  { code: 'Quebec', meaning: 'Q', category: 'Phonetic Alphabet' },
  { code: 'Romeo', meaning: 'R', category: 'Phonetic Alphabet' },
  { code: 'Sierra', meaning: 'S', category: 'Phonetic Alphabet' },
  { code: 'Tango', meaning: 'T', category: 'Phonetic Alphabet' },
  { code: 'Uniform', meaning: 'U', category: 'Phonetic Alphabet' },
  { code: 'Victor', meaning: 'V', category: 'Phonetic Alphabet' },
  { code: 'Whiskey', meaning: 'W', category: 'Phonetic Alphabet' },
  { code: 'X-ray', meaning: 'X', category: 'Phonetic Alphabet' },
  { code: 'Yankee', meaning: 'Y', category: 'Phonetic Alphabet' },
  { code: 'Zulu', meaning: 'Z', category: 'Phonetic Alphabet' },

  // Common Radio Terms
  { code: 'Copy', meaning: 'I understand/I hear you', category: 'Radio Terms' },
  { code: 'Go ahead', meaning: 'Proceed with your message', category: 'Radio Terms' },
  { code: 'Say again', meaning: 'Please repeat your message', category: 'Radio Terms' },
  { code: 'Stand by', meaning: 'Wait/Hold on', category: 'Radio Terms' },
  { code: 'Roger', meaning: 'Message received and understood', category: 'Radio Terms' },
  { code: 'Wilco', meaning: 'Will comply/Will do as instructed', category: 'Radio Terms' },
  { code: 'Negative', meaning: 'No', category: 'Radio Terms' },
  { code: 'Affirmative', meaning: 'Yes', category: 'Radio Terms' },
  { code: 'Break', meaning: 'Pause in transmission', category: 'Radio Terms' },
  { code: 'Over', meaning: 'End of transmission, expecting reply', category: 'Radio Terms' },
  { code: 'Out', meaning: 'End of transmission, no reply expected', category: 'Radio Terms' },
  { code: 'Clear', meaning: 'Finished/Complete', category: 'Radio Terms' },
  { code: 'ETA', meaning: 'Estimated Time of Arrival', category: 'Radio Terms' },
  { code: 'QSL', meaning: 'Acknowledge receipt', category: 'Radio Terms' },
  { code: '73', meaning: 'Best wishes/Goodbye', category: 'Radio Terms' },
  { code: '88', meaning: 'Love and kisses', category: 'Radio Terms' },
]

export default function RadioCodesPage() {
  const [searchQuery, setSearchQuery] = useState('')
  usePageTitle('Radio Codes', 'Common radio codes and their meanings')

  // Filter codes based on search query
  const filteredCodes = useMemo(() => {
    if (!searchQuery.trim()) return radioCodes
    
    const query = searchQuery.toLowerCase().trim()
    return radioCodes.filter(code => 
      code.code.toLowerCase().includes(query) ||
      code.meaning.toLowerCase().includes(query) ||
      code.category.toLowerCase().includes(query)
    )
  }, [searchQuery])

  // Group filtered codes by category
  const groupedCodes = filteredCodes.reduce((acc, code) => {
    if (!acc[code.category]) {
      acc[code.category] = []
    }
    acc[code.category].push(code)
    return acc
  }, {} as Record<string, RadioCode[]>)

  const categories = Object.keys(groupedCodes).sort()

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value)
  }

  const clearSearch = () => {
    setSearchQuery('')
  }

  return (
    <div className="radio-codes-page">
      {/* Fixed search bar at top */}
      <div className="search-bar">
        <div className="search-container">
          <div className="search-box">
            <span className="search-icon">🔍</span>
            <input
              type="text"
              placeholder="Search codes or meanings..."
              value={searchQuery}
              onChange={handleSearchChange}
              className="search-input"
            />
            {searchQuery && (
              <button onClick={clearSearch} className="clear-button" aria-label="Clear search">
                ✕
              </button>
            )}
          </div>
          {searchQuery && (
            <div className="search-results-info">
              Found {filteredCodes.length} code{filteredCodes.length !== 1 ? 's' : ''}
              {searchQuery && ` for "${searchQuery}"`}
            </div>
          )}
        </div>
      </div>

      {/* Page content */}
      <div className="page-content">
        <div className="page-header">
          <h1>📻 Radio Codes</h1>
          <p className="page-description">
            Common radio codes, signals, and terminology used in emergency services and amateur radio.
          </p>
        </div>

        <div className="codes-grid">
          {categories.length > 0 ? (
            categories.map(category => (
              <section key={category} className="codes-category">
                <h2 className="category-title">{category}</h2>
                <div className="codes-list">
                  {groupedCodes[category].map(code => (
                    <div key={`${category}-${code.code}`} className="code-item">
                      <div className="code-signal">{code.code}</div>
                      <div className="code-meaning">{code.meaning}</div>
                    </div>
                  ))}
                </div>
              </section>
            ))
          ) : (
            <div className="no-results">
              <div className="no-results-icon">🔍</div>
              <h3>No codes found</h3>
              <p>Try adjusting your search terms or <button onClick={clearSearch} className="link-button">clear the search</button></p>
            </div>
          )}
        </div>
      </div>

      <style>{`
        .radio-codes-page {
          display: flex;
          flex-direction: column;
          height: 100%;
          min-height: 0; /* Allow flex child to shrink */
        }

        /* Fixed search bar at the top */
        .search-bar {
          background: var(--bg-secondary);
          border-bottom: 1px solid var(--border);
          padding: var(--space-3);
          flex-shrink: 0; /* Don't shrink the search bar */
          z-index: 10;
        }

        .search-container {
          max-width: 1200px;
          margin: 0 auto;
        }

        .search-box {
          position: relative;
          display: flex;
          align-items: center;
          background: var(--bg-primary);
          border: 1px solid var(--border);
          border-radius: 8px;
          overflow: hidden;
          transition: border-color 0.2s ease;
          max-width: 500px;
          margin: 0 auto;
        }

        .search-box:focus-within {
          border-color: var(--accent);
        }

        .search-icon {
          padding: 0 var(--space-3);
          color: var(--text-secondary);
          font-size: 1rem;
        }

        .search-input {
          flex: 1;
          padding: var(--space-3);
          background: transparent;
          border: none;
          color: var(--text-primary);
          font-size: 1rem;
          outline: none;
        }

        .search-input::placeholder {
          color: var(--text-secondary);
        }

        .clear-button {
          padding: var(--space-2) var(--space-3);
          background: transparent;
          border: none;
          color: var(--text-secondary);
          cursor: pointer;
          font-size: 1rem;
          transition: color 0.2s ease;
        }

        .clear-button:hover {
          color: var(--text-primary);
        }

        .search-results-info {
          text-align: center;
          margin-top: var(--space-2);
          font-size: 0.9rem;
          color: var(--text-secondary);
        }

        /* Scrollable page content */
        .page-content {
          flex: 1;
          overflow-y: auto;
          padding: var(--space-4);
          min-height: 0; /* Allow flex child to shrink and create its own scroll */
        }

        .page-header {
          text-align: center;
          margin-bottom: var(--space-6);
          max-width: 1200px;
          margin-left: auto;
          margin-right: auto;
        }

        .page-header h1 {
          font-size: 2rem;
          font-weight: 700;
          color: var(--text-primary);
          margin-bottom: var(--space-2);
        }

        .page-description {
          font-size: 1.1rem;
          color: var(--text-secondary);
          max-width: 600px;
          margin: 0 auto;
          line-height: 1.6;
        }

        .codes-grid {
          display: grid;
          gap: var(--space-6);
          max-width: 1200px;
          margin: 0 auto;
        }

        .codes-category {
          background: var(--bg-secondary);
          border: 1px solid var(--border);
          border-radius: 12px;
          padding: var(--space-6);
        }

        .category-title {
          font-size: 1.25rem;
          font-weight: 600;
          color: var(--text-primary);
          margin-bottom: var(--space-4);
          padding-bottom: var(--space-2);
          border-bottom: 2px solid var(--accent);
        }

        .codes-list {
          display: grid;
          gap: var(--space-3);
        }

        .code-item {
          display: grid;
          grid-template-columns: auto 1fr;
          gap: var(--space-4);
          align-items: flex-start;
          padding: var(--space-3);
          background: rgba(255, 255, 255, 0.02);
          border: 1px solid var(--border);
          border-radius: 8px;
          transition: all 0.2s ease;
        }

        .code-item:hover {
          background: rgba(255, 255, 255, 0.04);
          border-color: var(--accent);
        }

        .code-signal {
          font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
          font-size: 0.95rem;
          font-weight: 600;
          color: var(--accent);
          background: rgba(66, 153, 225, 0.1);
          padding: var(--space-1) var(--space-2);
          border-radius: 4px;
          min-width: 80px;
          text-align: center;
          white-space: nowrap;
          flex-shrink: 0;
        }

        .code-meaning {
          color: var(--text-primary);
          font-size: 0.95rem;
          line-height: 1.5;
          word-wrap: break-word;
          min-width: 0;
        }

        .no-results {
          grid-column: 1 / -1;
          text-align: center;
          padding: var(--space-8);
          color: var(--text-secondary);
        }

        .no-results-icon {
          font-size: 3rem;
          margin-bottom: var(--space-4);
          opacity: 0.5;
        }

        .no-results h3 {
          font-size: 1.25rem;
          margin-bottom: var(--space-2);
          color: var(--text-primary);
        }

        .link-button {
          background: none;
          border: none;
          color: var(--accent);
          cursor: pointer;
          text-decoration: underline;
          font-size: inherit;
        }

        .link-button:hover {
          color: var(--accent-hover, #5a9fd4);
        }

        /* Custom scrollbar styling */
        .page-content::-webkit-scrollbar {
          width: 8px;
        }

        .page-content::-webkit-scrollbar-track {
          background: var(--bg-secondary);
          border-radius: 4px;
        }

        .page-content::-webkit-scrollbar-thumb {
          background: var(--border);
          border-radius: 4px;
        }

        .page-content::-webkit-scrollbar-thumb:hover {
          background: var(--text-secondary);
        }

        /* Responsive design */
        @media (min-width: 768px) {
          .codes-grid {
            grid-template-columns: repeat(auto-fit, minmax(600px, 1fr));
          }

          .code-item {
            grid-template-columns: 140px 1fr;
            gap: var(--space-5);
          }

          .code-signal {
            min-width: 120px;
            font-size: 1rem;
          }

          .code-meaning {
            font-size: 1rem;
            line-height: 1.6;
          }
        }

        @media (min-width: 1200px) {
          .codes-grid {
            grid-template-columns: repeat(2, 1fr);
          }

          .code-item {
            grid-template-columns: 160px 1fr;
          }
        }

        @media (max-width: 767px) {
          .search-bar {
            padding: var(--space-2);
          }
          
          .search-box {
            max-width: none;
          }

          .page-content {
            padding: var(--space-3);
          }

          .page-header h1 {
            font-size: 1.75rem;
          }

          .page-description {
            font-size: 1rem;
          }

          .codes-category {
            padding: var(--space-4);
          }

          .category-title {
            font-size: 1.125rem;
          }

          .code-item {
            grid-template-columns: 1fr;
            gap: var(--space-2);
            text-align: center;
          }

          .code-signal {
            justify-self: center;
          }
        }

        /* Special styling for phonetic alphabet - more compact grid */
        @media (min-width: 768px) {
          .codes-category:has(.code-signal:contains("Alpha")) .codes-list,
          .codes-category:has(.code-signal:contains("Bravo")) .codes-list {
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          }
        }
      `}</style>
    </div>
  )
}
