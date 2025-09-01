import React, { useState } from 'react'
import CallStream from './pages/CallStream'

export default function App() {
  const [page] = useState<'stream'>('stream')

  return (
    <div className="app">
      <header>
        <h1>SignalRadio WebClient</h1>
        <p>Basic React UI scaffold — Vite + React + TypeScript</p>
      </header>
      <main>
        {page === 'stream' && <CallStream />}
      </main>
    </div>
  )
}
