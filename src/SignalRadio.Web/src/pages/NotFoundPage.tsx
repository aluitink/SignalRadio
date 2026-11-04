import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <div className="empty-state" style={{ textAlign: 'center', padding: '64px 16px' }}>
      <h1 style={{ fontSize: 48, margin: 0 }}>404</h1>
      <p>That page doesn't exist.</p>
      <Link to="/" className="btn primary">Back to Live</Link>
    </div>
  )
}
