// Typed API client for the SignalRadio backend.
// Base URL defaults to "/api" (proxied by nginx in prod, Vite dev server in dev).
// Override with VITE_API_BASE if the API is served from a different origin.

import type {
  CallDto,
  CallVolumeByTalkGroupDto,
  CallVolumeDto,
  CursorPagedResult,
  RecordingDto,
  SearchResultsDto,
  TalkGroupDto,
  TranscriptionDto,
  TranscriptSummaryDto,
} from './types/dtos'

const DEFAULT_BASE = '/api'
export const API_BASE: string = (import.meta.env.VITE_API_BASE as string) ?? DEFAULT_BASE

async function handleResponse<T>(res: Response): Promise<T> {
  const contentType = res.headers.get('content-type') || ''
  const isJson = contentType.includes('application/json')
  const text = await res.text()

  if (!res.ok) {
    let payload: unknown = text
    if (isJson && text) {
      try { payload = JSON.parse(text) } catch { /* keep raw text */ }
    }
    const err = new Error(`API error ${res.status}: ${res.statusText}`) as Error & {
      status: number
      payload: unknown
    }
    err.status = res.status
    err.payload = payload
    throw err
  }

  if (!text) return undefined as unknown as T
  return isJson ? (JSON.parse(text) as T) : (text as unknown as T)
}

function qs(params: Record<string, string | number | boolean | undefined>): string {
  const parts = Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== '')
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
  return parts.length ? `?${parts.join('&')}` : ''
}

// ---- Calls ----
export function getRecentCalls(count = 30) {
  return apiGet<CallDto[]>(`/calls/recent${qs({ count })}`)
}
// Unified cursor-paged call list. Omit talkGroupId for the global stream;
// pass it to scope the list to a single talk group. `before` is the cursor
// (time + id of the oldest loaded call) for scrolling back in time.
export function getCallsCursor(
  pageSize = 20,
  before?: { time: string; id: number } | null,
  talkGroupId?: number,
  talkGroupIds?: number[],
) {
  const params: Record<string, string | number | boolean | undefined> = { pageSize }
  if (before) {
    params.beforeTime = before.time
    params.beforeId = before.id
  }
  if (talkGroupId != null) params.talkGroupId = talkGroupId
  let query = qs(params)
  // Use repeated params (talkGroupIds=1&talkGroupIds=2) — ASP.NET Core binds
  // these to string[] reliably, whereas a single comma-joined value is not
  // split for array binding.
  if (talkGroupIds && talkGroupIds.length > 0) {
    const extra = talkGroupIds.map((id) => `talkGroupIds=${id}`).join('&')
    query += (query ? '&' : '?') + extra
  }
  return apiGet<CursorPagedResult<CallDto>>(`/calls/cursor${query}`)
}
export function getCall(id: number) {
  return apiGet<CallDto>(`/calls/${id}`)
}
export function getCallVolume(from?: string, to?: string, talkGroupId?: number, bucketMinutes?: number) {
  return apiGet<CallVolumeDto[]>(`/calls/volume${qs({ from, to, talkGroupId, bucketMinutes })}`)
}
export function getCallVolumeByTalkGroup(from?: string, to?: string, bucketMinutes?: number) {
  return apiGet<CallVolumeByTalkGroupDto[]>(`/calls/volume-by-talkgroup${qs({ from, to, bucketMinutes })}`)
}

// ---- Recordings ----
export function getRecording(id: number) {
  return apiGet<RecordingDto>(`/recordings/${id}`)
}
// Audio stream URLs (used directly by <audio> / fetch)
export function recordingDownloadUrl(id: number): string {
  return `${API_BASE}/recordings/${id}/download`
}
export function recordingDownloadUrlByCall(callId: number): string {
  return `${API_BASE}/recordings/call/${callId}/download`
}
export function recordingFileUrl(fileName: string): string {
  return `${API_BASE}/recordings/file/${encodeURIComponent(fileName)}`
}

// ---- Transcriptions ----
export function getTranscriptionsForRecording(recordingId: number) {
  return apiGet<TranscriptionDto[]>(`/transcriptions/recording/${recordingId}`)
}
export function getFinalTranscription(recordingId: number) {
  return apiGet<TranscriptionDto>(`/transcriptions/recording/${recordingId}/final`)
}

// ---- Talk Groups ----
export function getTalkGroups() {
  return apiGet<TalkGroupDto[]>('/talkgroups')
}
export function getTalkGroup(id: number) {
  return apiGet<TalkGroupDto>(`/talkgroups/${id}`)
}

// ---- Search ----
export function search(q: string, page = 1, pageSize = 20, semantic = false) {
  return apiGet<SearchResultsDto>(`/search${qs({ q, page, pageSize, semantic })}`)
}

// ---- Transcript Summaries ----
export function getRecentSummaries(count = 10) {
  return apiGet<TranscriptSummaryDto[]>(`/transcript-summaries/recent${qs({ count })}`)
}
export function getSummariesForTalkGroup(talkGroupId: number, startTime?: string, endTime?: string) {
  return apiGet<TranscriptSummaryDto[]>(
    `/transcript-summaries/talkgroup/${talkGroupId}${qs({ startTime, endTime })}`,
  )
}
export function getSummary(id: number) {
  return apiGet<TranscriptSummaryDto>(`/transcript-summaries/${id}`)
}

// ---- Low-level helpers ----
export async function apiGet<T = unknown>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { method: 'GET', ...init })
  return handleResponse<T>(res)
}
export async function apiPost<T = unknown>(path: string, body?: unknown, init?: RequestInit): Promise<T> {
  const headers = { 'Content-Type': 'application/json', ...(init?.headers as Record<string, string> ?? {}) }
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    ...init,
  })
  return handleResponse<T>(res)
}
export async function apiPut<T = unknown>(path: string, body?: unknown, init?: RequestInit): Promise<T> {
  const headers = { 'Content-Type': 'application/json', ...(init?.headers as Record<string, string> ?? {}) }
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    ...init,
  })
  return handleResponse<T>(res)
}
export async function apiDelete<T = unknown>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { method: 'DELETE', ...init })
  return handleResponse<T>(res)
}
