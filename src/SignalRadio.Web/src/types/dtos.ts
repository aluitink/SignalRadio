// DTO types matching the SignalRadio.Api records exactly (camelCase JSON).

export interface CallDto {
  id: number
  talkGroupId: number
  talkGroupNumber: string
  talkGroupName?: string | null
  talkGroupDescription?: string | null
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

export interface CursorPagedResult<T> {
  items: T[]
  nextCursorTime?: string | null // ISO
  nextCursorId?: number | null
  hasMore: boolean
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
  talkGroupDescription?: string | null
  startTime: string // ISO
  endTime: string // ISO
  transcriptCount: number
  totalDurationSeconds: number
  summary: string
  severity?: number | null
  generatedAt: string // ISO
  topics: TopicDto[]
  notableIncidents: NotableIncidentDto[]
  callIds: number[]
}

// A call that was folded into a transcript summary — enough metadata to
// render a call card and play the audio.
export interface SummaryCallDto {
  callId: number
  talkGroupId: number
  talkGroupNumber: string
  talkGroupName?: string | null
  talkGroupDescription?: string | null
  recordingTime: string // ISO
  frequencyHz: number
  durationSeconds: number
  latestRecordingId?: number | null
}

// Cursor-paged summary search results (newest first).
export interface PagedSummarySearchResult {
  summaries: TranscriptSummaryDto[]
  nextCursorTime?: string | null // ISO
  nextCursorId?: number | null
  hasMore: boolean
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
  // Summary result paging (cursor over EndTime/Id, newest first).
  summaryTotalCount: number
  summaryNextCursorTime?: string | null // ISO
  summaryNextCursorId?: number | null
  summaryHasMore: boolean
}

// ---- Investigation ----
export interface EntityMention {
  name: string
  kind: string // 'unit' | 'location' | 'person' | 'call-type' | 'other'
  mentionCount: number
}

export interface TimelineEvent {
  time: string // ISO 8601 or empty
  description: string
  talkGroupNumber: string
}

export interface RelatedCall {
  callId: number
  talkGroupNumber: string
  talkGroupName: string
  recordingTime: string // ISO
  relevance: string
  transcript: string
}

export interface InvestigationReport {
  narrative: string
  relatedCalls?: RelatedCall[]
  entities?: EntityMention[]
  timeline?: TimelineEvent[]
  followUpQuestions?: string[]
  significance?: number // 0-10
}

// ---- Background Investigations ----
export interface InvestigationDto {
  id: number
  callId: number
  callTalkGroupNumber: string
  callTalkGroupName?: string | null
  callRecordingTime: string // ISO
  windowMinutes: number
  prompt: string
  parentInvestigationId?: number | null
  status: 'Pending' | 'Running' | 'Completed' | 'Failed'
  createdAt: string // ISO
  startedAt?: string | null
  completedAt?: string | null
  resultJson?: string | null
  error?: string | null
  isViewed: boolean
}

/** A single event streamed from the investigation agent during a chat turn. */
export interface InvestigationChatEvent {
  type: 'text' | 'tool_call' | 'tool_result' | 'run_finished' | 'run_failed'
  text?: string | null
  toolName?: string | null
  toolArgs?: string | null
  toolResult?: string | null
  reportJson?: string | null
  error?: string | null
}

/** A rendered chat message in the investigation chat UI. */
export interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'tool'
  text?: string
  toolName?: string
  toolArgs?: string
  toolResult?: string
  reportJson?: string
  error?: string
  streaming?: boolean
}
