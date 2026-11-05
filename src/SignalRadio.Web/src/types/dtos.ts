// DTO types matching the SignalRadio.Api records exactly (camelCase JSON).

export interface CallDto {
  id: number
  talkGroupId: number
  talkGroupNumber: string
  talkGroupName?: string | null
  recordingTime: string // ISO
  frequencyHz: number
  durationSeconds: number
  createdAt: string // ISO
  recordingCount: number
  latestRecordingId?: number | null
}

export interface CallVolumeDto {
  bucketStart: string // ISO
  callCount: number
  totalDurationSeconds: number
}

export interface TalkGroupVolumeDto {
  talkGroupId: number
  talkGroupNumber: string
  talkGroupName?: string | null
  callCount: number
  totalDurationSeconds: number
}

export interface CallVolumeByTalkGroupDto {
  bucketStart: string // ISO
  talkGroups: TalkGroupVolumeDto[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface TalkGroupDto {
  id: number
  number: string
  name?: string | null
  alphaTag?: string | null
  tag?: string | null
  description?: string | null
  category?: string | null
  priority?: number | null
  createdAt: string // ISO
}

export interface RecordingDto {
  id: number
  callId: number
  storageLocationId: number
  fileName: string
  sizeBytes: number
  receivedAt: string // ISO
  isProcessed: boolean
  downloadUrl?: string | null
}

export interface TranscriptionDto {
  id: number
  recordingId: number
  service: string
  language?: string | null
  fullText: string
  confidence?: number | null
  additionalDataJson?: string | null
  createdAt: string // ISO
  isFinal: boolean
}

export interface TopicDto {
  id: number
  name: string
  category?: string | null
}

export interface NotableIncidentDto {
  id: number
  description: string
  importanceScore?: number | null
  createdAt: string // ISO
}

export interface TranscriptSummaryDto {
  id: number
  talkGroupId: number
  talkGroupNumber: string
  talkGroupName?: string | null
  startTime: string // ISO
  endTime: string // ISO
  transcriptCount: number
  totalDurationSeconds: number
  summary: string
  generatedAt: string // ISO
  topics: TopicDto[]
  notableIncidents: NotableIncidentDto[]
}

export interface TranscriptionSearchResultDto {
  transcriptionId: number
  recordingId: number
  callId: number
  talkGroupId: number
  talkGroupNumber: string
  talkGroupName?: string | null
  recordingTime: string // ISO
  fullText: string
  confidence?: number | null
  service: string
  relevanceScore?: number | null
}

export interface SearchResultsDto {
  transcriptions: TranscriptionSearchResultDto[]
  summaries: TranscriptSummaryDto[]
  incidents: NotableIncidentDto[]
  totalCount: number
  page: number
  pageSize: number
}
