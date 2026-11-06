import { useEffect, useState } from 'react'
import { NavLink } from 'react-router-dom'
import SummaryTicker from './SummaryTicker'
import { useConnectionStatus } from '../contexts/ConnectionStatusContext'

const links = [
  { to: '/', label: 'Live', end: true },
  { to: '/talkgroups', label: 'Talk Groups' },
  { to: '/search', label: 'Search' },
]

export default function Navigation() {
  const [open, setOpen] = useState(false)
  const status = useConnectionStatus()

  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open])

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `nav-link${isActive ? ' active' : ''}`

  return (
    <header className={`nav${open ? ' open' : ''}`} role="banner">
      <div className="nav-inner">
        <NavLink to="/" className="brand" aria-label="SignalRadio home">
          <span
            className={`brand-dot status-dot ${status}`}
            role="status"
            aria-label={status === 'connected' ? 'Live' : status === 'connecting' ? 'Connecting' : 'Offline'}
          />
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
        <div className="nav-overlay" onClick={() => setOpen(false)} />
      )}
      <nav className={`nav-menu${open ? ' open' : ''}`} aria-label="Primary menu" aria-hidden={!open}>
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
    </header>
  )
}
