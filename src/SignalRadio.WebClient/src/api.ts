// Minimal API helper for the SignalRadio WebClient
// - Uses Vite env var VITE_API_BASE when present, otherwise falls back to "/api"
// - Provides small helpers: apiGet, apiPost, apiPut, apiDelete

const DEFAULT_BASE = '/api'
export const API_BASE: string = (import.meta.env.VITE_API_BASE as string) ?? DEFAULT_BASE

async function handleResponse<T>(res: Response): Promise<T> {
  const contentType = res.headers.get('content-type') || ''
  const isJson = contentType.includes('application/json')
  const text = await res.text()

  if (!res.ok) {
    let payload: any = text
    if (isJson && text) {
      try { payload = JSON.parse(text) } catch {}
    }
    const err: any = new Error(`API error ${res.status}: ${res.statusText}`)
    err.status = res.status
    err.payload = payload
    throw err
  }

  if (!text) return undefined as unknown as T
  return isJson ? (JSON.parse(text) as T) : (text as unknown as T)
}

export async function apiGet<T = any>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { method: 'GET', ...init })
  return handleResponse<T>(res)
}

export async function apiPost<T = any>(path: string, body?: any, init?: RequestInit): Promise<T> {
  const headers = { 'Content-Type': 'application/json', ...(init?.headers as Record<string,string> ?? {}) }
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    ...init,
  })
  return handleResponse<T>(res)
}

export async function apiPut<T = any>(path: string, body?: any, init?: RequestInit): Promise<T> {
  const headers = { 'Content-Type': 'application/json', ...(init?.headers as Record<string,string> ?? {}) }
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    ...init,
  })
  return handleResponse<T>(res)
}

export async function apiDelete<T = any>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { method: 'DELETE', ...init })
  return handleResponse<T>(res)
}

// Usage example:
// import { apiGet } from './api'
// const recordings = await apiGet('/recordings')
