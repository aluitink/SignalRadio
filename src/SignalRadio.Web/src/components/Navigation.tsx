import { useState } from 'react'
import { NavLink } from 'react-router-dom'
import SummaryTicker from './SummaryTicker'
import { useSubscriptions } from '../contexts/SubscriptionsContext'

const links = [
  { to: '/', label: 'Live', end: true },
  { to: '/talkgroups', label: 'Talk Groups' },
  { to: '/search', label: 'Search' },
]

export default function Navigation() {
  const [open, setOpen] = useState(false)
  const { queue, current, subscribedIds } = useSubscriptions()
  const pending = queue.length + (current ? 1 : 0)

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `nav-link${isActive ? ' active' : ''}`

  return (
    <header className={`nav${open ? ' open' : ''}`} role="banner">
      <div className="nav-inner">
        <NavLink to="/" className="brand" aria-label="SignalRadio home">
          <span className="brand-dot" aria-hidden="true" />
          SignalRadio
        </NavLink>

        {/* Centered AI summary ticker */}
        <div className="nav-ticker">
          <SummaryTicker />
        </div>

        {/* Desktop tab bar */}
        <nav className="nav-tabs" aria-label="Primary">
          {links.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end} className={linkClass}>
              {l.label}
            </NavLink>
          ))}
          {subscribedIds.length > 0 && (
            <span
              className={`nav-queue-badge${pending > 0 ? ' active' : ''}`}
              title={`${subscribedIds.length} subscribed talk group${subscribedIds.length === 1 ? '' : 's'} · ${pending} in auto-play queue`}
            >
              <span className="nav-queue-icon" aria-hidden="true">🔔</span>
              {pending > 0 && <span className="nav-queue-count">{pending}</span>}
            </span>
          )}
        </nav>

        {/* Mobile hamburger */}
        <button
          className="nav-burger"
          aria-label={open ? 'Close menu' : 'Open menu'}
          aria-expanded={open}
          onClick={() => setOpen((v) => !v)}
        >
          {open ? '✕' : '☰'}
        </button>
      </div>

      {open && (
        <nav className="nav-menu" aria-label="Primary menu">
          {links.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              end={l.end}
              className={linkClass}
              onClick={() => setOpen(false)}
            >
              {l.label}
            </NavLink>
          ))}
        </nav>
      )}
    </header>
  )
}
