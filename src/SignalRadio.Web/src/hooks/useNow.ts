import { useEffect, useState } from 'react'

/**
 * Returns the current time, updated on an interval.
 * Used to keep relative timestamps ("5m ago") fresh without a full re-fetch.
 */
export function useNow(intervalMs = 30000): Date {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), intervalMs)
    return () => clearInterval(id)
  }, [intervalMs])

  return now
}
