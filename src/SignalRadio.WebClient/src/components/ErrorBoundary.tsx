import React, { Component, ErrorInfo, ReactNode } from 'react'
import { Link } from 'react-router-dom'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
  error?: Error
  errorInfo?: ErrorInfo
}

export default class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Error boundary caught an error:', error, errorInfo)
    this.setState({ error, errorInfo })
  }

  handleRetry = () => {
    this.setState({ hasError: false, error: undefined, errorInfo: undefined })
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="error-boundary">
          <div className="error-content">
            <div className="error-icon">⚠️</div>
            <h1>Something went wrong</h1>
            <p className="text-secondary">
              An unexpected error occurred while loading this page.
            </p>
            
            <div className="error-actions">
              <button onClick={this.handleRetry} className="btn-primary">
                Try Again
              </button>
              <Link to="/" className="btn-secondary">
                ← Back to Home
              </Link>
            </div>

            {import.meta.env.DEV && this.state.error && (
              <details className="error-details">
                <summary>Error Details (Development)</summary>
                <pre className="error-stack">
                  {this.state.error.toString()}
                  {this.state.errorInfo?.componentStack}
                </pre>
              </details>
            )}
          </div>

          <style>{`
            .error-boundary {
              min-height: 60vh;
              display: flex;
              align-items: center;
              justify-content: center;
              padding: var(--space-4);
            }

            .error-content {
              text-align: center;
              max-width: 500px;
              width: 100%;
            }

            .error-icon {
              font-size: 72px;
              margin-bottom: var(--space-3);
              opacity: 0.7;
            }

            .error-boundary h1 {
              margin-bottom: var(--space-2);
              color: var(--text-primary);
            }

            .error-boundary p {
              margin-bottom: var(--space-4);
              font-size: var(--font-size-lg);
            }

            .error-actions {
              display: flex;
              gap: var(--space-2);
              justify-content: center;
              margin-bottom: var(--space-4);
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
              cursor: pointer;
              font-family: inherit;
              font-size: inherit;
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

            .error-details {
              background: var(--bg-card);
              border: 1px solid var(--border);
              border-radius: var(--radius);
              padding: var(--space-3);
              text-align: left;
              margin-top: var(--space-4);
            }

            .error-details summary {
              cursor: pointer;
              font-weight: 600;
              color: var(--text-primary);
              margin-bottom: var(--space-2);
            }

            .error-stack {
              background: var(--bg-primary);
              border: 1px solid var(--border);
              border-radius: var(--radius-sm);
              padding: var(--space-2);
              color: var(--text-secondary);
              font-size: var(--font-size-sm);
              overflow-x: auto;
              white-space: pre-wrap;
              margin: 0;
            }

            @media (max-width: 767px) {
              .error-actions {
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
        </div>
      )
    }

    return this.props.children
  }
}
