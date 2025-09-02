import React from 'react'
import { Link, useLocation } from 'react-router-dom'
import { getDynamicBreadcrumbTitle } from '../hooks/usePageTitle'

interface BreadcrumbItem {
  label: string
  path?: string
  icon?: string
}

interface BreadcrumbProps {
  items?: BreadcrumbItem[]
}

export default function Breadcrumb({ items }: BreadcrumbProps) {
  const location = useLocation()
  
  // Auto-generate breadcrumbs if not provided
  const breadcrumbItems = items || generateBreadcrumbs(location.pathname)

  if (breadcrumbItems.length <= 1) {
    return null
  }

  return (
    <nav className="breadcrumb" aria-label="Breadcrumb">
      <ol className="breadcrumb-list">
        {breadcrumbItems.map((item, index) => {
          const isLast = index === breadcrumbItems.length - 1
          
          return (
            <li key={index} className="breadcrumb-item">
              {item.path && !isLast ? (
                <Link to={item.path} className="breadcrumb-link">
                  {item.icon && <span className="breadcrumb-icon">{item.icon}</span>}
                  <span>{item.label}</span>
                </Link>
              ) : (
                <span className="breadcrumb-current">
                  {item.icon && <span className="breadcrumb-icon">{item.icon}</span>}
                  <span>{item.label}</span>
                </span>
              )}
              
              {!isLast && <span className="breadcrumb-separator">›</span>}
            </li>
          )
        })}
      </ol>

      <style>{`
        .breadcrumb {
          margin-bottom: var(--space-3);
        }

        .breadcrumb-list {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          margin: 0;
          padding: 0;
          list-style: none;
          flex-wrap: wrap;
        }

        .breadcrumb-item {
          display: flex;
          align-items: center;
          gap: var(--space-1);
        }

        .breadcrumb-link {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          color: var(--accent-primary);
          text-decoration: none;
          font-size: var(--font-size-sm);
          transition: var(--transition);
          padding: 2px 4px;
          border-radius: var(--radius-sm);
        }

        .breadcrumb-link:hover {
          background: var(--bg-card-hover);
          text-decoration: underline;
        }

        .breadcrumb-current {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          color: var(--text-secondary);
          font-size: var(--font-size-sm);
          font-weight: 500;
        }

        .breadcrumb-separator {
          color: var(--text-muted);
          font-size: var(--font-size-sm);
          user-select: none;
        }

        .breadcrumb-icon {
          font-size: var(--font-size-sm);
        }

        @media (max-width: 767px) {
          .breadcrumb-link,
          .breadcrumb-current {
            font-size: var(--font-size-xs);
          }
          
          .breadcrumb-list {
            gap: 4px;
          }
        }
      `}</style>
    </nav>
  )
}

function generateBreadcrumbs(pathname: string): BreadcrumbItem[] {
  const items: BreadcrumbItem[] = [
    { label: 'Live Stream', path: '/', icon: '📡' }
  ]

  if (pathname === '/') {
    return [{ label: 'Live Stream', icon: '📡' }]
  }

  if (pathname === '/search') {
    items.push({ label: 'Search', icon: '🔍' })
  } else if (pathname === '/subscriptions') {
    items.push({ label: 'Subscriptions', icon: '⭐' })
  } else if (pathname.startsWith('/talkgroup/')) {
    const talkGroupId = pathname.split('/')[2]
    const dynamicTitle = getDynamicBreadcrumbTitle()
    items.push({ 
      label: dynamicTitle || `TalkGroup ${talkGroupId}`, 
      path: `/talkgroup/${talkGroupId}`,
      icon: '📻' 
    })
  } else if (pathname.startsWith('/call/')) {
    const callId = pathname.split('/')[2]
    items.push({ 
      label: `Call ${callId}`, 
      icon: '🎵' 
    })
  } else if (pathname === '/admin') {
    items.push({ label: 'Admin', icon: '⚙️' })
  }

  return items
}
