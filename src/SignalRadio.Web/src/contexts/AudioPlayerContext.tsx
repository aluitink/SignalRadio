import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'

export interface NowPlaying {
  url: string
  title: string
  callId?: number
}

interface AudioPlayerValue {
  nowPlaying: NowPlaying | null
  isPlaying: boolean
  currentTime: number
  duration: number
  play: (np: NowPlaying) => void
  toggle: () => void
  stop: () => void
  seek: (time: number) => void
}

const AudioPlayerContext = createContext<AudioPlayerValue | null>(null)

export function AudioPlayerProvider({ children }: { children: ReactNode }) {
  const audioRef = useRef<HTMLAudioElement | null>(null)
  const [nowPlaying, setNowPlaying] = useState<NowPlaying | null>(null)
  const [isPlaying, setIsPlaying] = useState(false)
  const [currentTime, setCurrentTime] = useState(0)
  const [duration, setDuration] = useState(0)

  // Create a single persistent <audio> element.
  useEffect(() => {
    const audio = new Audio()
    audio.preload = 'auto'
    audioRef.current = audio

    const onPlay = () => setIsPlaying(true)
    const onPause = () => setIsPlaying(false)
    const onEnded = () => setIsPlaying(false)
    const onTimeUpdate = () => setCurrentTime(audio.currentTime)
    const onLoadedMetadata = () => setDuration(audio.duration || 0)

    audio.addEventListener('play', onPlay)
    audio.addEventListener('pause', onPause)
    audio.addEventListener('ended', onEnded)
    audio.addEventListener('timeupdate', onTimeUpdate)
    audio.addEventListener('loadedmetadata', onLoadedMetadata)

    return () => {
      audio.pause()
      audio.removeEventListener('play', onPlay)
      audio.removeEventListener('pause', onPause)
      audio.removeEventListener('ended', onEnded)
      audio.removeEventListener('timeupdate', onTimeUpdate)
      audio.removeEventListener('loadedmetadata', onLoadedMetadata)
      audioRef.current = null
    }
  }, [])

  const play = useCallback((np: NowPlaying) => {
    const audio = audioRef.current
    if (!audio) return
    if (nowPlaying?.url === np.url) {
      // Same track: toggle.
      if (audio.paused) void audio.play().catch(() => { /* ignore */ })
      else audio.pause()
      return
    }
    setNowPlaying(np)
    setCurrentTime(0)
    setDuration(0)
    audio.src = np.url
    void audio.play().catch(() => { /* autoplay may be blocked */ })
  }, [nowPlaying])

  const toggle = useCallback(() => {
    const audio = audioRef.current
    if (!audio || !nowPlaying) return
    if (audio.paused) void audio.play().catch(() => { /* ignore */ })
    else audio.pause()
  }, [nowPlaying])

  const stop = useCallback(() => {
    const audio = audioRef.current
    if (!audio) return
    audio.pause()
    audio.currentTime = 0
    setNowPlaying(null)
    setIsPlaying(false)
    setCurrentTime(0)
    setDuration(0)
  }, [])

  const seek = useCallback((time: number) => {
    const audio = audioRef.current
    if (!audio) return
    audio.currentTime = time
    setCurrentTime(time)
  }, [])

  const value = useMemo<AudioPlayerValue>(
    () => ({ nowPlaying, isPlaying, currentTime, duration, play, toggle, stop, seek }),
    [nowPlaying, isPlaying, currentTime, duration, play, toggle, stop, seek],
  )

  return <AudioPlayerContext.Provider value={value}>{children}</AudioPlayerContext.Provider>
}

export function useAudioPlayer(): AudioPlayerValue {
  const ctx = useContext(AudioPlayerContext)
  if (!ctx) throw new Error('useAudioPlayer must be used within AudioPlayerProvider')
  return ctx
}
