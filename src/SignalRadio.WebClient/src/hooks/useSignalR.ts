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
    try {
    // Debug log so we can observe when the server confirms the subscription.
    // This should help trace the "No client method with the name 'allcallsstreamsubscribed' found" warning.
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
  const onSubscriptionConfirmed = (_talkGroupId: number) => {
    try {
      // eslint-disable-next-line no-console
      console.info('[signalr] SubscriptionConfirmed', { hub: hubPath, talkGroupId: _talkGroupId, time: new Date().toISOString() })
    } catch {}
  }
  const onUnsubscriptionConfirmed = (_talkGroupId: number) => {
    try {
      // eslint-disable-next-line no-console
      console.info('[signalr] UnsubscriptionConfirmed', { hub: hubPath, talkGroupId: _talkGroupId, time: new Date().toISOString() })
    } catch {}
  }

  // Lowercase versions to handle SignalR case conversion
  const onAllCallsStreamSubscribedLowercase = () => {
    try {
      console.debug('[signalr] allcallsstreamsubscribed (no-op)', { hub: hubPath, time: new Date().toISOString() })
    } catch {}
  }
  const onAllCallsStreamUnsubscribedLowercase = () => {
    try {
      console.debug('[signalr] allcallsstreamunsubscribed (no-op)', { hub: hubPath, time: new Date().toISOString() })
    } catch {}
  }
  const onSubscriptionConfirmedLowercase = (_talkGroupId: number) => {
    try {
      console.debug('[signalr] subscriptionconfirmed (no-op)', { hub: hubPath, talkGroupId: _talkGroupId, time: new Date().toISOString() })
    } catch {}
  }
  const onUnsubscriptionConfirmedLowercase = (_talkGroupId: number) => {
    try {
      console.debug('[signalr] unsubscriptionconfirmed (no-op)', { hub: hubPath, talkGroupId: _talkGroupId, time: new Date().toISOString() })
    } catch {}
  }
  const onCallUpdated = (_callData: any) => {
    try {
      // No-op handler to prevent "No client method with the name 'callupdated' found" warnings
      // Components register their own handlers for actual functionality
      console.debug('[signalr] CallUpdated (no-op)', { hub: hubPath, time: new Date().toISOString() })
    } catch {}
  }

  // SignalR converts method names to lowercase on the client side
  const onCallUpdatedLowercase = (_callData: any) => {
    try {
      // No-op handler to prevent "No client method with the name 'callupdated' found" warnings
      console.debug('[signalr] callupdated (no-op)', { hub: hubPath, time: new Date().toISOString() })
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
        c.on('CallUpdated', onCallUpdated)
        c.on('callupdated', onCallUpdatedLowercase)
        c.on('allcallsstreamsubscribed', onAllCallsStreamSubscribedLowercase)
        c.on('allcallsstreamunsubscribed', onAllCallsStreamUnsubscribedLowercase)
        c.on('subscriptionconfirmed', onSubscriptionConfirmedLowercase)
        c.on('unsubscriptionconfirmed', onUnsubscriptionConfirmedLowercase)
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
          c.off('CallUpdated', onCallUpdated as any)
          c.off('callupdated', onCallUpdatedLowercase as any)
          c.off('allcallsstreamsubscribed', onAllCallsStreamSubscribedLowercase as any)
          c.off('allcallsstreamunsubscribed', onAllCallsStreamUnsubscribedLowercase as any)
          c.off('subscriptionconfirmed', onSubscriptionConfirmedLowercase as any)
          c.off('unsubscriptionconfirmed', onUnsubscriptionConfirmedLowercase as any)
        }
      } catch {}

      releaseConnection(hubPath)
      connRef.current = null
      setConnected(false)
    }
  }, [hubPath])

  return { connection: connRef.current, connected }
}
