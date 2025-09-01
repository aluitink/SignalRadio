import { HubConnection } from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import { acquireConnection, releaseConnection, getConnectionIfStarted } from './signalRManager'

export function useSignalR(hubPath: string) {
  const connRef = useRef<HubConnection | null>(null)
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    const apiBase = (import.meta.env.VITE_API_BASE as string) ?? ''

    let mounted = true

    // Try to reuse an already-started connection immediately
    const existing = getConnectionIfStarted(hubPath)
    if (existing) {
      connRef.current = existing
      setConnected(true)
    }

    // Acquire (start if needed) the shared connection
    acquireConnection(hubPath, apiBase).then(conn => {
      if (!mounted) return
      connRef.current = conn
      setConnected(true)
    }).catch(err => {
      if (!mounted) return
      console.error('SignalR acquire failed', { error: err })
    })

  const onReconnecting = () => setConnected(false)
  const onReconnected = () => setConnected(true)
  const onClose = () => setConnected(false)

  // No-op handlers for server-invoked client methods so the server
  // doesn't log warnings when it calls them. Components may also
  // override these by registering their own handlers on the connection.
  const onAllCallsStreamSubscribed = () => {
    // Debug log so we can observe when the server confirms the subscription.
    // This should help trace the "No client method with the name 'allcallsstreamsubscribed' found" warning.
    try {
      // eslint-disable-next-line no-console
      console.info('[signalr] AllCallsStreamSubscribed', { hub: hubPath, time: new Date().toISOString() })
    } catch {}
  }
  const onAllCallsStreamUnsubscribed = () => {
    try {
      // eslint-disable-next-line no-console
      console.info('[signalr] AllCallsStreamUnsubscribed', { hub: hubPath, time: new Date().toISOString() })
    } catch {}
  }
  const onSubscriptionConfirmed = (_talkGroupId: string) => {
    try {
      // eslint-disable-next-line no-console
      console.info('[signalr] SubscriptionConfirmed', { hub: hubPath, talkGroupId: _talkGroupId, time: new Date().toISOString() })
    } catch {}
  }
  const onUnsubscriptionConfirmed = (_talkGroupId: string) => {
    try {
      // eslint-disable-next-line no-console
      console.info('[signalr] UnsubscriptionConfirmed', { hub: hubPath, talkGroupId: _talkGroupId, time: new Date().toISOString() })
    } catch {}
  }

    // Attach handlers to any live connection
    const attachHandlers = (c: HubConnection | null) => {
      if (!c) return
      try {
        c.onreconnecting(onReconnecting)
        c.onreconnected(onReconnected)
        c.onclose(onClose)
  c.on('AllCallsStreamSubscribed', onAllCallsStreamSubscribed)
  c.on('AllCallsStreamUnsubscribed', onAllCallsStreamUnsubscribed)
  c.on('SubscriptionConfirmed', onSubscriptionConfirmed)
  c.on('UnsubscriptionConfirmed', onUnsubscriptionConfirmed)
      } catch {}
    }

    attachHandlers(connRef.current)

  // Intentionally do not subscribe to any server groups here. The
  // connection is established and client handlers are attached, but
  // subscription is left to UI components or future logic.

    return () => {
      mounted = false
      // Detach handlers
      try {
        const c = connRef.current
        if (c) {
          c.off('reconnecting', onReconnecting as any)
          c.off('reconnected', onReconnected as any)
          c.off('close', onClose as any)
          c.off('AllCallsStreamSubscribed', onAllCallsStreamSubscribed as any)
          c.off('AllCallsStreamUnsubscribed', onAllCallsStreamUnsubscribed as any)
          c.off('SubscriptionConfirmed', onSubscriptionConfirmed as any)
          c.off('UnsubscriptionConfirmed', onUnsubscriptionConfirmed as any)
        }
      } catch {}

      releaseConnection(hubPath)
      connRef.current = null
      setConnected(false)
    }
  }, [hubPath])

  return { connection: connRef.current, connected }
}
