import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'
import { NavLink } from 'react-router-dom'
import SummaryTicker from './SummaryTicker'
import { useConnectionStatus } from '../contexts/ConnectionStatusContext'

const links = [
  { to: '/', label: 'Live', end: true },
  { to: '/talkgroups', label: 'Talk Groups' },
  { to: '/search', label: 'Search' },
  { to: '/investigations', label: 'Investigations' },
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
    <>
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

        {/* Mobile hamburger (hidden while the menu is open — the menu's close button takes its place) */}
        <button
          className={`nav-burger${open ? ' hidden' : ''}`}
          aria-label="Open menu"
          aria-expanded={open}
          onClick={() => setOpen(true)}
        >
          ☰
        </button>
      </div>

    </header>
    {open &&
      createPortal(
        <div className="nav-scrim" onClick={() => setOpen(false)}>
          <nav className="nav-menu" aria-label="Primary menu">
            {/* Menu header: brand name + close button (overlays the hamburger's spot on mobile) */}
            <div className="nav-menu-header">
              <span className="nav-menu-brand">SignalRadio</span>
              <button
                type="button"
                className="nav-menu-close"
                aria-label="Close menu"
                onClick={() => setOpen(false)}
              >
                ✕
              </button>
            </div>
            {/* Spacer keeps items below the close button */}
            <div className="nav-menu-spacer" aria-hidden="true" />
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
        </div>,
        document.body,
      )}
    </>
  )
}
