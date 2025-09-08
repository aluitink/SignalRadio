// Unified DTOs for API and SignalR Hub communication
// These types match exactly what both the API endpoints and Hub will return

export interface RecordingDto {
  id: string
  fileName: string
  url: string
  durationSeconds: number
  sizeBytes: number
}

export interface TranscriptionDto {
  id: string
  text: string
  confidence?: number
  language?: string
}

export interface TalkGroupDto {
  id: number
  number: number
  name?: string
  alphaTag?: string
  description?: string
  tag?: string
  category?: string
  priority?: number
}

export interface TalkGroupStats {
  talkGroupId: number
  callCount: number
  lastActivity?: string // ISO string
  totalDurationSeconds: number
}

export interface CallDto {
  id: number
  talkGroupId: number
  talkGroup?: TalkGroupDto
  recordingTime: string // ISO string
  frequencyHz: number
  durationSeconds: number
  recordings: RecordingDto[]
  transcriptions?: TranscriptionDto[]
  createdAt: string // ISO string
}

export interface TranscriptSummaryDto {
  talkGroupId: number
  talkGroupName: string
  startTime: string // ISO string
  endTime: string // ISO string
  transcriptCount: number
  totalDurationSeconds: number
  summary: string
  keyTopics: string[]
  notableIncidents: string[]
  notableIncidentsWithCallIds: NotableIncidentDto[]
  generatedAt: string // ISO string
  fromCache: boolean
}

export interface NotableIncidentDto {
  description: string
  callIds: number[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
