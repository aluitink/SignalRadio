# Static Audio Files

This folder contains static audio files used by the application.

## Files

- `silence.mp3` - A 0.5-second silence file used for autoplay keep-alive functionality

## Usage

These files are served as static assets and can be referenced in the application code using:
```typescript
const silenceUrl = '/static/silence.mp3'
```

## Note

The silence.mp3 file should be a very short (0.5 seconds) silent audio file that helps maintain audio context for background playback without interfering with user experience.
