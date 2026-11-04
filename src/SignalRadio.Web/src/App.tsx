import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AudioPlayerProvider } from './contexts/AudioPlayerContext'
import Navigation from './components/Navigation'
import AudioPlayer from './components/AudioPlayer'
import CallStreamPage from './pages/CallStreamPage'
import TalkGroupsPage from './pages/TalkGroupsPage'
import TalkGroupPage from './pages/TalkGroupPage'
import CallDetailPage from './pages/CallDetailPage'
import SearchPage from './pages/SearchPage'
import NotFoundPage from './pages/NotFoundPage'

export default function App() {
  return (
    <AudioPlayerProvider>
      <BrowserRouter>
        <div className="app">
          <Navigation />
          <main className="main-content">
            <div className="container">
              <Routes>
                <Route path="/" element={<CallStreamPage />} />
                <Route path="/talkgroups" element={<TalkGroupsPage />} />
                <Route path="/talkgroup/:id" element={<TalkGroupPage />} />
                <Route path="/call/:id" element={<CallDetailPage />} />
                <Route path="/search" element={<SearchPage />} />
                <Route path="*" element={<NotFoundPage />} />
              </Routes>
            </div>
          </main>
          <AudioPlayer />
        </div>
      </BrowserRouter>
    </AudioPlayerProvider>
  )
}
