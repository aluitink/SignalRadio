import React, { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useSignalR } from '../hooks/useSignalR'
import WakeLockIndicator from './WakeLockIndicator'
import NightModeToggle from './NightModeToggle'

export default function Navigation() {
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const location = useLocation()
  const { connected } = useSignalR('/hubs/talkgroup')

  const navItems = [
    { path: '/', label: 'Live Stream', icon: '📡' },
    { path: '/search', label: 'Search', icon: '🔍' },
    { path: '/talkgroups', label: 'Talk Groups', icon: '📻' },
    { path: '/radio-codes', label: 'Radio Codes', icon: '�' },
  ]

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  return (
    <nav className="navigation">
      <div className="nav-container">
        <Link to="/" className="nav-brand">
          <span className="nav-brand-icon">📻</span>
          <span className="nav-brand-text">SignalRadio</span>
          <span className={`nav-connection-status ${connected ? 'connected' : 'disconnected'}`}>
            <span className="connection-indicator" />
          </span>
        </Link>

        <div className="nav-actions">
          <div className="nav-desktop-controls">
            <NightModeToggle className="nav-desktop-control" showLabel={false} />
            <WakeLockIndicator className="nav-desktop-control" showLabel={false} />
          </div>
          <button 
            className="nav-toggle"
            onClick={() => setIsMenuOpen(!isMenuOpen)}
            aria-label="Toggle navigation menu"
          >
            <span className={`hamburger ${isMenuOpen ? 'open' : ''}`}>
              <span></span>
              <span></span>
              <span></span>
            </span>
          </button>
        </div>

        <div className={`nav-menu ${isMenuOpen ? 'open' : ''}`}>
          <div className="nav-menu-items">
            {navItems.map(item => (
              <Link
                key={item.path}
                to={item.path}
                className={`nav-link ${isActive(item.path) ? 'active' : ''}`}
                onClick={() => setIsMenuOpen(false)}
              >
                <span className="nav-link-icon">{item.icon}</span>
                <span className="nav-link-text">{item.label}</span>
              </Link>
            ))}
          </div>
          <div className="nav-menu-footer">
            <NightModeToggle className="nav-footer-control" showLabel={true} />
            <WakeLockIndicator className="nav-footer-control" showLabel={true} />
          </div>
        </div>
      </div>

      <style>{`
        .navigation {
          background: var(--bg-secondary);
          border-bottom: 1px solid var(--border);
          position: sticky;
          top: 0;
          z-index: 100;
        }

        /* Ensure proper positioning in fullscreen */
        @media (max-width: 767px) {
          .navigation {
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            z-index: 1000;
          }
        }

        .nav-container {
          max-width: 1200px;
          margin: 0 auto;
          padding: 0 var(--space-2);
          display: flex;
          align-items: center;
          justify-content: space-between;
          height: 64px;
        }

        .nav-brand {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          text-decoration: none;
          color: var(--text-primary);
          font-weight: 600;
          font-size: var(--font-size-lg);
        }

        .nav-brand-icon {
          font-size: var(--font-size-xl);
        }

        .nav-connection-status {
          display: flex;
          align-items: center;
          margin-left: var(--space-1);
        }

        .connection-indicator {
          width: 8px;
          height: 8px;
          border-radius: 50%;
          transition: var(--transition);
        }

        .nav-connection-status.connected .connection-indicator {
          background: var(--success-color, #22c55e);
          animation: pulse 2s ease-in-out infinite;
        }

        .nav-connection-status.disconnected .connection-indicator {
          background: var(--error-color, #ef4444);
        }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }

        .nav-actions {
          display: flex;
          align-items: center;
          gap: var(--space-2);
          position: relative;
          z-index: 101; /* Ensure it stays above other elements in fullscreen */
        }

        .nav-desktop-controls {
          display: none;
          align-items: center;
          gap: var(--space-1);
        }

        /* Show desktop controls on larger screens */
        @media (min-width: 768px) {
          .nav-desktop-controls {
            display: flex;
          }
        }

        .nav-desktop-control {
          padding: var(--space-1) !important;
          border-radius: var(--radius-sm) !important;
        }

        .nav-toggle {
          display: flex;
          align-items: center;
          justify-content: center;
          background: none;
          border: none;
          color: var(--text-primary);
          cursor: pointer;
          padding: var(--space-1);
          border-radius: var(--radius-sm);
          transition: var(--transition);
        }

        .nav-toggle:hover {
          background: var(--bg-card-hover);
        }

        .hamburger {
          width: 24px;
          height: 18px;
          position: relative;
          transform: rotate(0deg);
          transition: var(--transition);
        }

        .hamburger span {
          display: block;
          position: absolute;
          height: 2px;
          width: 100%;
          background: var(--text-primary);
          border-radius: 1px;
          opacity: 1;
          left: 0;
          transform: rotate(0deg);
          transition: var(--transition);
        }

        .hamburger span:nth-child(1) {
          top: 0;
        }

        .hamburger span:nth-child(2) {
          top: 8px;
        }

        .hamburger span:nth-child(3) {
          top: 16px;
        }

        .hamburger.open span:nth-child(1) {
          top: 8px;
          transform: rotate(135deg);
        }

        .hamburger.open span:nth-child(2) {
          opacity: 0;
          left: -60px;
        }

        .hamburger.open span:nth-child(3) {
          top: 8px;
          transform: rotate(-135deg);
        }

        .nav-menu {
          position: fixed;
          top: 64px;
          right: 0;
          bottom: 0;
          width: 280px;
          background: var(--bg-secondary);
          border-left: 1px solid var(--border);
          padding: 0;
          padding-bottom: 120px; /* Space for audio player */
          overflow-y: auto;
          transform: translateX(100%);
          opacity: 0;
          visibility: hidden;
          transition: var(--transition);
          z-index: 50; /* Behind audio player */
          display: flex;
          flex-direction: column;
        }

        .nav-menu.open {
          transform: translateX(0);
          opacity: 1;
          visibility: visible;
        }

        .nav-menu-items {
          flex: 1;
          padding: var(--space-2);
        }

        .nav-menu-footer {
          display: flex;
          flex-direction: column;
          gap: 0;
          padding: var(--space-2);
          border-top: 1px solid var(--border);
          margin-top: auto;
        }

        .nav-footer-control {
          width: 100%;
          justify-content: flex-start !important;
          margin: 0 !important;
          border: none !important;
          background: transparent !important;
          padding: var(--space-2) !important;
          border-radius: var(--radius) !important;
          font-size: var(--font-size-base) !important;
          font-weight: 500 !important;
          color: var(--text-secondary) !important;
          transition: var(--transition) !important;
          gap: var(--space-2) !important;
        }

        .nav-footer-control:hover {
          background: var(--bg-card-hover) !important;
          color: var(--text-primary) !important;
        }

        /* Style the icons to match nav link icons */
        .nav-footer-control .toggle-icon,
        .nav-footer-control .wake-lock-toggle {
          font-size: var(--font-size-lg) !important;
        }

        /* Ensure labels are visible and styled properly */
        .nav-footer-control .toggle-label,
        .nav-footer-control .wake-lock-label {
          display: block !important;
          color: inherit !important;
          font-size: var(--font-size-base) !important;
          font-weight: 500 !important;
          white-space: nowrap !important;
        }

        /* Override mobile hidden labels */
        @media (max-width: 767px) {
          .nav-footer-control .toggle-label {
            display: block !important;
          }
        }

        .nav-link {
          display: flex;
          align-items: center;
          gap: var(--space-2);
          padding: var(--space-2);
          text-decoration: none;
          color: var(--text-secondary);
          border-radius: var(--radius);
          transition: var(--transition);
          font-weight: 500;
        }

        .nav-link:hover {
          background: var(--bg-card-hover);
          color: var(--text-primary);
        }

        .nav-link.active {
          background: var(--accent-primary);
          color: white;
        }

        .nav-link-icon {
          font-size: var(--font-size-lg);
        }
      `}</style>
    </nav>
  )
}
