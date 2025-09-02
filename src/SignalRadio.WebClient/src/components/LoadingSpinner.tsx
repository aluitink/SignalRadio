import React from 'react'

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  message?: string
}

export default function LoadingSpinner({ size = 'md', message }: LoadingSpinnerProps) {
  const sizeClasses = {
    sm: 'spinner-sm',
    md: 'spinner-md', 
    lg: 'spinner-lg'
  }

  return (
    <div className="loading-spinner">
      <div className={`spinner ${sizeClasses[size]}`}>
        <div className="spinner-inner">
          <div></div>
          <div></div>
          <div></div>
          <div></div>
        </div>
      </div>
      {message && <p className="loading-message">{message}</p>}

      <style>{`
        .loading-spinner {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          gap: var(--space-2);
          padding: var(--space-4);
        }

        .spinner {
          position: relative;
        }

        .spinner-inner {
          display: inline-block;
          position: relative;
        }

        .spinner-inner div {
          position: absolute;
          border: 2px solid var(--accent-primary);
          opacity: 1;
          border-radius: 50%;
          animation: spinner-ripple 1s cubic-bezier(0, 0.2, 0.8, 1) infinite;
        }

        .spinner-inner div:nth-child(2) {
          animation-delay: -0.5s;
        }

        .spinner-sm .spinner-inner {
          width: 24px;
          height: 24px;
        }

        .spinner-sm .spinner-inner div {
          width: 20px;
          height: 20px;
          margin: 2px;
          border-width: 1px;
        }

        .spinner-md .spinner-inner {
          width: 40px;
          height: 40px;
        }

        .spinner-md .spinner-inner div {
          width: 32px;
          height: 32px;
          margin: 4px;
        }

        .spinner-lg .spinner-inner {
          width: 64px;
          height: 64px;
        }

        .spinner-lg .spinner-inner div {
          width: 52px;
          height: 52px;
          margin: 6px;
          border-width: 3px;
        }

        .loading-message {
          color: var(--text-secondary);
          font-size: var(--font-size-sm);
          margin: 0;
          text-align: center;
        }

        @keyframes spinner-ripple {
          0% {
            top: 50%;
            left: 50%;
            width: 0;
            height: 0;
            opacity: 1;
            transform: translate(-50%, -50%);
          }
          100% {
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            opacity: 0;
            transform: translate(0, 0);
          }
        }
      `}</style>
    </div>
  )
}

// Skeleton loading component for call cards
export function CallCardSkeleton() {
  return (
    <div className="call-card-skeleton">
      <div className="skeleton-header">
        <div className="skeleton-title"></div>
        <div className="skeleton-actions"></div>
      </div>
      <div className="skeleton-meta"></div>
      <div className="skeleton-content"></div>

      <style>{`
        .call-card-skeleton {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-2);
          margin-bottom: var(--space-2);
        }

        .skeleton-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--space-2);
        }

        .skeleton-title {
          width: 200px;
          height: 20px;
          background: var(--bg-card-hover);
          border-radius: var(--radius-sm);
          animation: pulse 1.5s ease-in-out infinite;
        }

        .skeleton-actions {
          width: 80px;
          height: 32px;
          background: var(--bg-card-hover);
          border-radius: var(--radius-sm);
          animation: pulse 1.5s ease-in-out infinite;
        }

        .skeleton-meta {
          display: flex;
          gap: var(--space-2);
          margin-bottom: var(--space-2);
        }

        .skeleton-meta::before,
        .skeleton-meta::after {
          content: '';
          width: 80px;
          height: 16px;
          background: var(--bg-card-hover);
          border-radius: var(--radius-sm);
          animation: pulse 1.5s ease-in-out infinite;
        }

        .skeleton-content {
          width: 100%;
          height: 60px;
          background: var(--bg-card-hover);
          border-radius: var(--radius-sm);
          animation: pulse 1.5s ease-in-out infinite;
        }

        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
      `}</style>
    </div>
  )
}
