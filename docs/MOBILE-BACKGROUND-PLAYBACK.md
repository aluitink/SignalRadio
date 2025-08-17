# SignalRadio Mobile Background Playback Guide

## Overview

SignalRadio now supports continuous audio playback when your mobile device goes into standby mode. This feature uses modern web technologies to provide a native app-like experience.

## Features

### 🎵 Background Audio Playback
- Audio continues playing when you switch apps or lock your phone
- Uses Media Session API for native media controls
- Appears in your device's media control center
- Supports play/pause/stop/next track controls

### 🔒 Wake Lock Support
- Optionally keeps your screen on during active listening
- Prevents accidental screen timeouts during important calls
- Can be disabled to save battery

### 📱 Progressive Web App (PWA)
- Install SignalRadio as a native app on your device
- Offline caching for faster loading
- Full-screen experience without browser UI

## Setup Instructions

### For Mobile Devices (Android/iOS)

#### 1. Install as PWA (Recommended)
**Chrome/Edge (Android):**
1. Open SignalRadio in your mobile browser
2. Tap the three-dot menu (⋮)
3. Select "Add to Home screen" or "Install app"
4. Follow the prompts to install

**Safari (iOS):**
1. Open SignalRadio in Safari
2. Tap the Share button (⎘)
3. Select "Add to Home Screen"
4. Tap "Add" to install

#### 2. Enable Background Playback
1. Open SignalRadio
2. Tap any interaction (required for audio permissions)
3. In the Controls section, ensure these are enabled:
   - ✅ **Background playback** - Continue playing when app is in background
   - ✅ **Keep screen on** - Prevent screen from turning off (optional)

#### 3. Configure Browser Permissions
**Chrome/Edge:**
- When prompted, allow "Audio" permissions
- In browser settings, ensure SignalRadio has "Sound" permission

**Safari:**
- Audio permissions are handled automatically
- Ensure "Auto-Play" is not blocked for SignalRadio

### For Desktop

The background playback features work on desktop as well:
- Audio continues when SignalRadio tab is not active
- Media Session controls appear in browser/OS media controls
- Wake lock prevents system sleep during playback

## Usage

### Basic Listening
1. Subscribe to talk groups you want to monitor
2. Enable "Auto-play subscribed calls" if desired
3. Audio will play automatically and continue in background

### Media Controls
Once audio is playing, you can control it from:
- **Lock screen** - Play/pause/stop/next controls
- **Notification panel** - Media controls
- **Browser tab** - Standard SignalRadio controls
- **OS media controls** - System-level controls

### Queue Management
- Calls are automatically queued when multiple arrive
- Use "Next track" in media controls to skip current call
- View and manage queue in the SignalRadio interface

## Browser Compatibility

### Fully Supported
- **Chrome 66+** (Android/Desktop)
- **Edge 79+** (Android/Desktop)
- **Firefox 71+** (Android/Desktop)

### Partial Support
- **Safari 14+** (iOS/macOS) - Background playback with limitations
- **Samsung Internet 7+** - Most features supported

### Features by Browser

| Feature | Chrome/Edge | Firefox | Safari |
|---------|-------------|---------|--------|
| Background Audio | ✅ | ✅ | ✅ |
| Media Session API | ✅ | ✅ | ⚠️¹ |
| Wake Lock | ✅ | ❌ | ❌ |
| PWA Install | ✅ | ✅² | ✅ |
| Service Worker | ✅ | ✅ | ✅ |

¹ Safari has limited Media Session support  
² Firefox requires manual installation via "Add to Home Screen"

## Troubleshooting

### Audio Stops in Background
1. **Check browser permissions** - Ensure audio is allowed
2. **Disable battery optimization** - For your browser app in Android settings
3. **Close other audio apps** - Some apps may interfere with background audio
4. **Re-enable background playback** - Toggle the setting off and on

### Media Controls Not Appearing
1. **Start audio playback first** - Media controls appear after audio begins
2. **Check browser version** - Update to latest version
3. **Try different browser** - Chrome/Edge have best support

### PWA Installation Issues
1. **Clear browser cache** - Force refresh the page
2. **Check connection** - Ensure stable internet connection
3. **Try incognito mode** - Test installation in private browsing

### Wake Lock Not Working
1. **Check browser support** - Only Chrome/Edge support wake lock
2. **Ensure permission granted** - Browser may prompt for permission
3. **Device settings** - Check if browser has wake lock permission

## Battery Optimization

To maximize battery life while using background playback:

1. **Disable wake lock** - If you don't need screen to stay on
2. **Use lower volume** - Reduces power consumption
3. **Close other apps** - Minimize background processes
4. **Enable power saving mode** - Most devices optimize for audio playback

## Privacy & Permissions

SignalRadio requests these permissions:
- **Audio playback** - Required for audio functionality
- **Wake lock** - Optional, only if enabled by user
- **Notifications** - For background sync updates (future feature)

All audio processing is done locally. No audio data is transmitted or stored beyond normal API calls to your SignalRadio server.

## Support

If you experience issues with background playback:

1. Check this guide for troubleshooting steps
2. Test with different browsers
3. Verify your device/browser compatibility
4. Report issues with specific device/browser information

For the best experience, we recommend:
- Installing as a PWA
- Using Chrome or Edge browsers
- Keeping your browser updated
- Enabling background playback settings

---

*This feature uses modern web APIs that continue to evolve. Functionality may vary across devices and browsers.*
