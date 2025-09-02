import React, { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'

export default function Navigation() {
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const location = useLocation()

  const navItems = [
    { path: '/', label: 'Live Stream', icon: '📡' },
    { path: '/search', label: 'Search', icon: '🔍' },
    { path: '/subscriptions', label: 'Subscriptions', icon: '⭐' },
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
        </Link>

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

        <div className={`nav-menu ${isMenuOpen ? 'open' : ''}`}>
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
      </div>

      <style>{`
        .navigation {
          background: var(--bg-secondary);
          border-bottom: 1px solid var(--border);
          position: sticky;
          top: 0;
          z-index: 100;
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
          left: 0;
          right: 0;
          background: var(--bg-secondary);
          border-bottom: 1px solid var(--border);
          padding: var(--space-2);
          transform: translateY(-100%);
          opacity: 0;
          visibility: hidden;
          transition: var(--transition);
        }

        .nav-menu.open {
          transform: translateY(0);
          opacity: 1;
          visibility: visible;
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

        @media (min-width: 768px) {
          .nav-container {
            padding: 0 var(--space-4);
          }

          .nav-toggle {
            display: none;
          }

          .nav-menu {
            position: static;
            transform: none;
            opacity: 1;
            visibility: visible;
            background: none;
            border: none;
            padding: 0;
            display: flex;
            align-items: center;
            gap: var(--space-1);
          }

          .nav-link {
            padding: var(--space-1) var(--space-2);
          }
        }
      `}</style>
    </nav>
  )
}
