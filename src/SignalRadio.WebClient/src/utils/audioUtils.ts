// Audio URL utilities for handling recording URLs
// Normalizes URLs to be relative to current origin and handles various URL formats

export function normalizeAudioUrl(url: string): string {
  if (!url) return ''
  
  try {
    // If it's already a relative URL, use it as-is
    if (url.startsWith('/')) {
      return url
    }
    
    // If it's a full URL, extract the path portion
    if (url.startsWith('http://') || url.startsWith('https://')) {
      const urlObj = new URL(url)
      return urlObj.pathname + urlObj.search + urlObj.hash
    }
    
    // If it's just a path without leading slash, add it
    if (!url.startsWith('/')) {
      return `/${url}`
    }
    
    return url
  } catch (error) {
    console.warn('Failed to normalize audio URL:', url, error)
    return url
  }
}

export function buildRecordingUrl(recordingId: string): string {
  return `/api/recordings/${recordingId}/file`
}

export function validateAudioUrl(url: string): boolean {
  if (!url) return false
  
  try {
    const normalizedUrl = normalizeAudioUrl(url)
    
    // Check if it's a valid path
    if (normalizedUrl.startsWith('/api/recordings/') && normalizedUrl.endsWith('/file')) {
      return true
    }
    
    // Allow other audio file extensions
    const audioExtensions = ['.wav', '.mp3', '.m4a', '.aac', '.ogg', '.flac']
    return audioExtensions.some(ext => normalizedUrl.toLowerCase().includes(ext))
  } catch {
    return false
  }
}
