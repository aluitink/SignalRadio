import React, { useEffect } from 'react'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import ErrorBoundary from './components/ErrorBoundary'
import Navigation from './components/Navigation'
import Breadcrumb from './components/Breadcrumb'
import PageTransition from './components/PageTransition'
import CallStreamPage from './pages/CallStreamPage'
import Admin from './pages/Admin'
import SearchPage from './pages/SearchPage'
import TalkGroupPage from './pages/TalkGroupPage'
import CallDetailPage from './pages/CallDetailPage'
import SubscriptionsPage from './pages/SubscriptionsPage'
import NotFoundPage from './pages/NotFoundPage'
import { createApiTester } from './utils/ApiTester'
import { SubscriptionProvider } from './contexts/SubscriptionContext'

export default function App() {
  // Initialize API tester for development
  useEffect(() => {
    if (import.meta.env.DEV) {
      createApiTester()
      console.log('🔧 Development mode: API tester available. Run runApiTests() in console to test all endpoints.')
    }
  }, [])

  return (
    <Router>
      <SubscriptionProvider>
        <ErrorBoundary>
          <div className="app">
            <Navigation />
            
            <main className="main-content">
              <div className="container">
                <Breadcrumb />
                <ErrorBoundary>
                  <PageTransition>
                    <Routes>
                      <Route path="/" element={<CallStreamPage />} />
                      <Route path="/search" element={<SearchPage />} />
                      <Route path="/subscriptions" element={<SubscriptionsPage />} />
                      <Route path="/talkgroup/:id" element={<TalkGroupPage />} />
                      <Route path="/call/:id" element={<CallDetailPage />} />
                      <Route path="/admin" element={<Admin />} />
                      <Route path="*" element={<NotFoundPage />} />
                    </Routes>
                  </PageTransition>
                </ErrorBoundary>
              </div>
            </main>
          </div>
        </ErrorBoundary>
      </SubscriptionProvider>

      <style>{`
        .main-content {
          flex: 1;
          padding: var(--space-4) 0;
          padding-bottom: calc(var(--space-4) + 80px); /* Account for fixed audio player */
        }

        @media (max-width: 767px) {
          .main-content {
            padding: var(--space-2) 0;
            padding-bottom: calc(var(--space-2) + 70px); /* Account for fixed audio player on mobile */
          }
        }
      `}</style>
    </Router>
  )
}
