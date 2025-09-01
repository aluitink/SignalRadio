export interface Recording {
  id: string
  url: string
  durationSeconds: number
}

export interface Transcription {
  id: string
  text: string
}

export interface Call {
  id: string
  talkGroupId: string
  talkGroupDescription?: string
  priority?: number
  recordings: Recording[]
  transcriptions?: Transcription[]
  startedAt: string // ISO
  endedAt?: string // ISO
}

export type PagedResult<T> = {
  items: T[]
  totalCount?: number
  page?: number
  pageSize?: number
}
