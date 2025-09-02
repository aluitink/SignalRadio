import React from 'react'
import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <section className="not-found-page">
      <div className="not-found-content">
        <div className="not-found-icon">📡</div>
        <h1>404 - Page Not Found</h1>
        <p className="text-secondary">
          The page you're looking for doesn't exist or has been moved.
        </p>
        
        <div className="not-found-actions">
          <Link to="/" className="btn-primary">
            ← Back to Live Stream
          </Link>
          <Link to="/search" className="btn-secondary">
            Search Calls
          </Link>
        </div>
        
        <div className="help-links">
          <h3>Quick Links</h3>
          <div className="quick-links">
            <Link to="/" className="quick-link">
              <span className="link-icon">📡</span>
              <span>Live Stream</span>
            </Link>
            <Link to="/search" className="quick-link">
              <span className="link-icon">🔍</span>
              <span>Search</span>
            </Link>
            <Link to="/subscriptions" className="quick-link">
              <span className="link-icon">⭐</span>
              <span>Subscriptions</span>
            </Link>
          </div>
        </div>
      </div>

      <style>{`
        .not-found-page {
          min-height: 60vh;
          display: flex;
          align-items: center;
          justify-content: center;
          padding: var(--space-4);
        }

        .not-found-content {
          text-align: center;
          max-width: 500px;
          width: 100%;
        }

        .not-found-icon {
          font-size: 72px;
          margin-bottom: var(--space-3);
          opacity: 0.7;
        }

        .not-found-page h1 {
          margin-bottom: var(--space-2);
          color: var(--text-primary);
        }

        .not-found-page p {
          margin-bottom: var(--space-4);
          font-size: var(--font-size-lg);
        }

        .not-found-actions {
          display: flex;
          gap: var(--space-2);
          justify-content: center;
          margin-bottom: var(--space-5);
          flex-wrap: wrap;
        }

        .btn-primary,
        .btn-secondary {
          display: inline-flex;
          align-items: center;
          padding: var(--space-2) var(--space-3);
          border-radius: var(--radius);
          text-decoration: none;
          font-weight: 500;
          transition: var(--transition);
          border: 1px solid;
        }

        .btn-primary {
          background: var(--accent-primary);
          border-color: var(--accent-primary);
          color: white;
        }

        .btn-primary:hover {
          background: var(--accent-secondary);
          border-color: var(--accent-secondary);
        }

        .btn-secondary {
          background: var(--bg-card);
          border-color: var(--border);
          color: var(--text-primary);
        }

        .btn-secondary:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .help-links {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-3);
        }

        .help-links h3 {
          margin: 0 0 var(--space-2) 0;
          color: var(--text-primary);
          font-size: var(--font-size-lg);
        }

        .quick-links {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
          gap: var(--space-2);
        }

        .quick-link {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: var(--space-1);
          padding: var(--space-2);
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          text-decoration: none;
          color: var(--text-secondary);
          transition: var(--transition);
        }

        .quick-link:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
          color: var(--text-primary);
        }

        .link-icon {
          font-size: var(--font-size-xl);
        }

        @media (max-width: 767px) {
          .not-found-actions {
            flex-direction: column;
            align-items: center;
          }

          .btn-primary,
          .btn-secondary {
            width: 100%;
            max-width: 200px;
            justify-content: center;
          }
        }
      `}</style>
    </section>
  )
}
