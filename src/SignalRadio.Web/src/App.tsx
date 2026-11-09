import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AudioPlayerProvider } from './contexts/AudioPlayerContext'
import { SubscriptionsProvider } from './contexts/SubscriptionsContext'
import { SignalRBusProvider } from './contexts/SignalRBusContext'
import { ConnectionStatusProvider } from './contexts/ConnectionStatusContext'
import Navigation from './components/Navigation'
import AudioPlayer from './components/AudioPlayer'
import CallStreamPage from './pages/CallStreamPage'
import TalkGroupsPage from './pages/TalkGroupsPage'
import TalkGroupPage from './pages/TalkGroupPage'
import CallDetailPage from './pages/CallDetailPage'
import SearchPage from './pages/SearchPage'
import SummaryDetailPage from './pages/SummaryDetailPage'
import InvestigationsPage from './pages/InvestigationsPage'
import InvestigationChatPage from './pages/InvestigationChatPage'
import NotFoundPage from './pages/NotFoundPage'

export default function App() {
  return (
    <AudioPlayerProvider>
      <SignalRBusProvider>
      <SubscriptionsProvider>
      <ConnectionStatusProvider>
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
                <Route path="/summary/:id" element={<SummaryDetailPage />} />
                <Route path="/search" element={<SearchPage />} />
                <Route path="/investigations" element={<InvestigationsPage />} />
                <Route path="/investigations/:id" element={<InvestigationChatPage />} />
                <Route path="*" element={<NotFoundPage />} />
              </Routes>
            </div>
          </main>
          <AudioPlayer />
        </div>
      </BrowserRouter>
      </ConnectionStatusProvider>
      </SubscriptionsProvider>
      </SignalRBusProvider>
    </AudioPlayerProvider>
  )
}
