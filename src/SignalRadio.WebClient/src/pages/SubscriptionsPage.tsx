import React from 'react'
import { useSubscriptions } from '../hooks/useSubscriptions'

export default function SubscriptionsPage() {
  const { 
    subscriptions, 
    getSubscriptionsList, 
    toggle, 
    subscriptionCount 
  } = useSubscriptions()

  const subscriptionsList = getSubscriptionsList()

  return (
    <section className="subscriptions-page">
      <header className="subscriptions-header">
        <h1>Subscriptions</h1>
        <p className="text-secondary">
          {subscriptionCount === 0 
            ? 'No talkgroup subscriptions yet'
            : `${subscriptionCount} talkgroup${subscriptionCount !== 1 ? 's' : ''} subscribed`
          }
        </p>
      </header>

      {subscriptionCount === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">⭐</div>
          <h3>No Subscriptions</h3>
          <p className="text-muted">
            Subscribe to talkgroups from the live stream or talkgroup pages to get auto-playback of new calls.
          </p>
          <div className="help-text">
            <p>💡 <strong>How to subscribe:</strong></p>
            <ul>
              <li>Click the star (☆) button on any call card</li>
              <li>New calls from subscribed talkgroups will automatically play</li>
              <li>Manage your subscriptions here</li>
            </ul>
          </div>
        </div>
      ) : (
        <div className="subscriptions-list">
          <div className="subscriptions-grid">
            {subscriptionsList.map(talkGroupId => (
              <SubscriptionCard 
                key={talkGroupId}
                talkGroupId={talkGroupId}
                onUnsubscribe={() => toggle(talkGroupId)}
              />
            ))}
          </div>
        </div>
      )}

      <style>{`
        .subscriptions-page {
          min-height: 60vh;
        }

        .subscriptions-header {
          margin-bottom: var(--space-4);
        }

        .subscriptions-header h1 {
          margin-bottom: var(--space-1);
        }

        .empty-state {
          text-align: center;
          padding: var(--space-6) var(--space-2);
          color: var(--text-secondary);
        }

        .empty-icon {
          font-size: 48px;
          margin-bottom: var(--space-2);
        }

        .empty-state h3 {
          color: var(--text-primary);
          margin-bottom: var(--space-2);
        }

        .help-text {
          background: var(--bg-card);
          border-radius: var(--radius);
          padding: var(--space-3);
          margin-top: var(--space-4);
          text-align: left;
          max-width: 500px;
          margin-left: auto;
          margin-right: auto;
        }

        .help-text p {
          margin: 0 0 var(--space-2) 0;
          color: var(--text-primary);
        }

        .help-text ul {
          margin: 0;
          padding-left: var(--space-3);
          color: var(--text-secondary);
        }

        .help-text li {
          margin-bottom: var(--space-1);
        }

        .subscriptions-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
          gap: var(--space-3);
        }

        @media (max-width: 767px) {
          .subscriptions-grid {
            grid-template-columns: 1fr;
          }
        }
      `}</style>
    </section>
  )
}

function SubscriptionCard({ 
  talkGroupId, 
  onUnsubscribe 
}: { 
  talkGroupId: number
  onUnsubscribe: () => void 
}) {
  return (
    <article className="subscription-card">
      <div className="subscription-header">
        <div className="talkgroup-info">
          <h3>TalkGroup {talkGroupId}</h3>
          <p className="text-secondary">Auto-play enabled</p>
        </div>
        <button
          className="unsubscribe-btn"
          onClick={onUnsubscribe}
          title="Unsubscribe"
        >
          ✕
        </button>
      </div>
      
      <div className="subscription-status">
        <span className="status-badge">
          ⭐ Subscribed
        </span>
      </div>

      <style>{`
        .subscription-card {
          background: var(--bg-card);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          padding: var(--space-3);
          transition: var(--transition);
        }

        .subscription-card:hover {
          background: var(--bg-card-hover);
          border-color: var(--accent-primary);
        }

        .subscription-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: var(--space-2);
        }

        .talkgroup-info h3 {
          margin: 0 0 var(--space-1) 0;
          color: var(--text-primary);
          font-size: var(--font-size-lg);
        }

        .talkgroup-info p {
          margin: 0;
          font-size: var(--font-size-sm);
        }

        .unsubscribe-btn {
          background: none;
          border: 1px solid var(--border);
          color: var(--text-muted);
          width: 32px;
          height: 32px;
          border-radius: var(--radius-sm);
          cursor: pointer;
          transition: var(--transition);
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .unsubscribe-btn:hover {
          background: #ef4444;
          border-color: #ef4444;
          color: white;
        }

        .status-badge {
          background: var(--accent-primary);
          color: white;
          padding: var(--space-1) var(--space-2);
          border-radius: var(--radius-sm);
          font-size: var(--font-size-sm);
          font-weight: 500;
        }
      `}</style>
    </article>
  )
}
