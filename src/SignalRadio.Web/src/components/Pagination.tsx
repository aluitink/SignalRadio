import './Pagination.css'

export default function Pagination({
  page,
  totalPages,
  onPageChange,
}: {
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}) {
  return (
    <nav className="pagination" aria-label="Pagination">
      <button
        className="btn ghost"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
        aria-label="Previous page"
      >
        ← Prev
      </button>
      <span className="page-info" aria-live="polite">
        Page {page} of {totalPages}
      </span>
      <button
        className="btn ghost"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
        aria-label="Next page"
      >
        Next →
      </button>
    </nav>
  )
}
