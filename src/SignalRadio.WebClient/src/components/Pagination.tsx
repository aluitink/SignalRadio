import React from 'react'

interface PaginationProps {
  currentPage: number
  totalPages: number
  totalItems: number
  itemsPerPage: number
  onPageChange: (page: number) => void
  loading?: boolean
}

export default function Pagination({
  currentPage,
  totalPages,
  totalItems,
  itemsPerPage,
  onPageChange,
  loading = false
}: PaginationProps) {
  if (totalPages <= 1) {
    return null
  }

  const getVisiblePages = () => {
    const delta = 2
    const range = []
    const rangeWithDots = []

    for (let i = Math.max(2, currentPage - delta); i <= Math.min(totalPages - 1, currentPage + delta); i++) {
      range.push(i)
    }

    if (currentPage - delta > 2) {
      rangeWithDots.push(1, '...')
    } else {
      rangeWithDots.push(1)
    }

    rangeWithDots.push(...range)

    if (currentPage + delta < totalPages - 1) {
      rangeWithDots.push('...', totalPages)
    } else {
      rangeWithDots.push(totalPages)
    }

    return rangeWithDots
  }

  const startItem = (currentPage - 1) * itemsPerPage + 1
  const endItem = Math.min(currentPage * itemsPerPage, totalItems)

  return (
    <div className="pagination">
      <div className="pagination-info">
        <span className="pagination-text">
          Showing {startItem}-{endItem} of {totalItems.toLocaleString()} items
        </span>
      </div>
      
      <nav className="pagination-nav" aria-label="Pagination">
        <button
          className="pagination-btn pagination-btn--prev"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1 || loading}
          aria-label="Previous page"
        >
          <span className="pagination-icon">‹</span>
          <span className="pagination-label">Previous</span>
        </button>

        <div className="pagination-pages">
          {getVisiblePages().map((page, index) => (
            <React.Fragment key={index}>
              {typeof page === 'number' ? (
                <button
                  className={`pagination-page ${page === currentPage ? 'pagination-page--current' : ''}`}
                  onClick={() => onPageChange(page)}
                  disabled={loading}
                  aria-label={`Page ${page}`}
                  aria-current={page === currentPage ? 'page' : undefined}
                >
                  {page}
                </button>
              ) : (
                <span className="pagination-dots" aria-hidden="true">
                  {page}
                </span>
              )}
            </React.Fragment>
          ))}
        </div>

        <button
          className="pagination-btn pagination-btn--next"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages || loading}
          aria-label="Next page"
        >
          <span className="pagination-label">Next</span>
          <span className="pagination-icon">›</span>
        </button>
      </nav>

      <style>{`
        .pagination {
          display: flex;
          flex-direction: column;
          gap: var(--space-3);
          align-items: center;
          margin-top: var(--space-4);
          padding: var(--space-3);
        }

        .pagination-info {
          text-align: center;
        }

        .pagination-text {
          font-size: var(--font-size-sm);
          color: var(--text-secondary);
        }

        .pagination-nav {
          display: flex;
          align-items: center;
          gap: var(--space-2);
        }

        .pagination-btn {
          display: flex;
          align-items: center;
          gap: var(--space-1);
          padding: var(--space-2) var(--space-3);
          border: 1px solid var(--border);
          background: var(--bg-card);
          color: var(--text-primary);
          text-decoration: none;
          border-radius: var(--radius);
          font-size: var(--font-size-sm);
          font-weight: 500;
          transition: var(--transition);
          cursor: pointer;
        }

        .pagination-btn:hover:not(:disabled) {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .pagination-btn:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .pagination-pages {
          display: flex;
          align-items: center;
          gap: var(--space-1);
        }

        .pagination-page {
          display: flex;
          align-items: center;
          justify-content: center;
          width: 40px;
          height: 40px;
          border: 1px solid var(--border);
          background: var(--bg-card);
          color: var(--text-primary);
          border-radius: var(--radius);
          font-size: var(--font-size-sm);
          font-weight: 500;
          transition: var(--transition);
          cursor: pointer;
        }

        .pagination-page:hover:not(:disabled) {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .pagination-page--current {
          background: var(--accent-primary);
          color: var(--accent-contrast);
          border-color: var(--accent-primary);
        }

        .pagination-page:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }

        .pagination-dots {
          display: flex;
          align-items: center;
          justify-content: center;
          width: 40px;
          height: 40px;
          color: var(--text-muted);
          font-weight: bold;
        }

        .pagination-icon {
          font-size: 18px;
          font-weight: bold;
        }

        .pagination-label {
          display: none;
        }

        @media (min-width: 768px) {
          .pagination {
            flex-direction: row;
            justify-content: space-between;
          }

          .pagination-label {
            display: block;
          }

          .pagination-nav {
            order: 0;
          }
        }

        @media (max-width: 640px) {
          .pagination-pages {
            display: none;
          }

          .pagination-nav {
            justify-content: space-between;
            width: 100%;
          }
        }
      `}</style>
    </div>
  )
}
