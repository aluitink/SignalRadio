import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

type Entry = {
  connection: HubConnection | null
  startPromise: Promise<HubConnection> | null
  refCount: number
  stopTimeout: ReturnType<typeof setTimeout> | null
}

const map = new Map<string, Entry>()

const STOP_GRACE_MS = 3000

function buildConnection(fullUrl: string) {
  return new HubConnectionBuilder()
    .withUrl(fullUrl, { withCredentials: true })
    .configureLogging(LogLevel.Information)
    .withAutomaticReconnect()
    .build()
}

export async function acquireConnection(hubPath: string, baseUrl: string) {
  const key = hubPath
  let entry = map.get(key)
  if (!entry) {
    entry = { connection: null, startPromise: null, refCount: 0, stopTimeout: null }
    map.set(key, entry)
  }

  entry.refCount += 1

  // Cancel any pending stop
  if (entry.stopTimeout) {
    clearTimeout(entry.stopTimeout)
    entry.stopTimeout = null
  }

  if (entry.connection) return entry.connection

  if (!entry.startPromise) {
    const cleaned = hubPath.replace(/^\/?/, '')
    // If hubPath already starts with 'hubs/', don't add another /hubs/ prefix
    const full = cleaned.startsWith('hubs/') 
      ? `${baseUrl.replace(/\/$/, '')}/${cleaned}`
      : `${baseUrl.replace(/\/$/, '')}/hubs/${cleaned}`
    const conn = buildConnection(full)
    entry.startPromise = conn.start().then(() => {
      entry!.connection = conn
      entry!.startPromise = null
      try {
        // eslint-disable-next-line no-console
        console.info('[signalr] connection started', { hub: key, url: full })
      } catch {}
  // Do NOT automatically invoke server-side subscription here. Components
  // need to attach their client handlers first, otherwise the server may
  // send the confirmation event before the client has registered a handler
  // (which causes the "No client method with the name ... found" warning).
      return conn
    })
  }

  return entry.startPromise!
}

export function releaseConnection(hubPath: string) {
  const key = hubPath
  const entry = map.get(key)
  if (!entry) return

  entry.refCount = Math.max(0, entry.refCount - 1)
  if (entry.refCount === 0) {
    // Schedule a stop to avoid churn when components mount/unmount rapidly
    entry.stopTimeout = setTimeout(async () => {
      try {
        if (entry.connection) {
          // Try to unsubscribe from the all calls stream before stopping
          try {
            await entry.connection.stop()
            // eslint-disable-next-line no-console
            console.info('[signalr] connection stopped', { hub: key })
          } catch {}
        }
      } catch {
        // ignore
      }
      map.delete(key)
    }, STOP_GRACE_MS)
  }
}

export function getConnectionIfStarted(hubPath: string) {
  const entry = map.get(hubPath)
  return entry?.connection ?? null
}

