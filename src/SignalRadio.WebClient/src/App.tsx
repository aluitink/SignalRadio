import React, { useState } from 'react'
import CallStream from './pages/CallStream'
import Admin from './pages/Admin'

export default function App() {
  const [page, setPage] = useState<'stream' | 'admin'>('stream')

  return (
    <div className="app">
      <header>
        <h1>SignalRadio WebClient</h1>
        <p>Basic React UI scaffold — Vite + React + TypeScript</p>
        <nav>
          <button onClick={() => setPage('stream')}>Stream</button>
          <button onClick={() => setPage('admin')}>Admin</button>
        </nav>
      </header>
      <main>
        {page === 'stream' && <CallStream />}
        {page === 'admin' && <Admin />}
      </main>
    </div>
  )
}
