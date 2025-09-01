import React, { useState } from 'react'
import { API_BASE } from '../api'

export default function Admin() {
  const [password, setPassword] = useState('')
  const [authed, setAuthed] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const expected = (import.meta.env.VITE_ADMIN_PASSWORD as string) ?? ''

  function login(e: React.FormEvent) {
    e.preventDefault()
    setAuthed(password === expected)
    setMessage(password === expected ? 'Authenticated' : 'Invalid password')
  }

  async function upload(e: React.FormEvent) {
    e.preventDefault()
    if (!file) { setMessage('Select a file first'); return }
    const fd = new FormData()
    fd.append('file', file)
    try {
      const res = await fetch(`${API_BASE}/talkgroups/import`, {
        method: 'POST',
        body: fd,
      })
      if (!res.ok) throw new Error(await res.text())
      setMessage('Import successful')
    } catch (err: any) {
      setMessage(`Import failed: ${err?.message ?? err}`)
    }
  }

  return (
    <div className="admin-page">
      <h2>Admin - Talkgroup Import</h2>
      {!authed && (
        <form onSubmit={login}>
          <label>
            Admin password:
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} />
          </label>
          <button type="submit">Login</button>
        </form>
      )}

      {authed && (
        <form onSubmit={upload}>
          <label>
            Talkgroup CSV file:
            <input type="file" accept=".csv,text/csv" onChange={e => setFile(e.target.files?.[0] ?? null)} />
          </label>
          <div style={{ marginTop: 8 }}>
            <button type="submit">Upload & Import</button>
          </div>
        </form>
      )}

      {message && <div style={{ marginTop: 12 }}>{message}</div>}
    </div>
  )
}
