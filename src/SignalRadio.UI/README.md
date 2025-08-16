# SignalRadio UI

A real-time web interface for monitoring talk group communications via SignalRadio API.

## Features

- 🎙️ **Real-time Talk Group Subscriptions** - Subscribe to specific talk groups and receive live notifications
- 🔊 **Auto-play Audio** - Automatically play new call recordings as they arrive
- 📱 **Mobile Responsive** - Works great on phones, tablets, and desktops
- 🔄 **Automatic Reconnection** - Handles network interruptions gracefully
- 📈 **Live Activity Feed** - See calls as they happen in real-time
- 📊 **Recent Call History** - Browse recent calls across all talk groups

## Quick Start

### Development Server (Python)
```bash
cd src/SignalRadio.UI
python3 -m http.server 3000
```

### Development Server (Node.js)
```bash
cd src/SignalRadio.UI
npm install
npm run dev-node
```

Then open http://localhost:3000 in your browser.

## Configuration

Update the API URL in `js/app.js`:

```javascript
const API_BASE_URL = 'https://localhost:7080'; // Update to your API URL
```

## Usage

1. **Connect** - The app automatically connects to the SignalR hub when loaded
2. **Subscribe** - Enter a talk group ID (e.g., "4001") and click Subscribe
3. **Listen** - New calls for subscribed talk groups will appear in real-time
4. **Play** - Click the play button to listen to call recordings

## API Requirements

The SignalRadio API must have:
- SignalR hub at `/hubs/talkgroups`
- CORS enabled for the UI origin
- Recording stream endpoint at `/api/Recording/stream/{id}`

## Browser Compatibility

- Chrome 88+
- Firefox 85+
- Safari 14+
- Edge 88+

Audio playback requires user interaction on first play (browser security policy).
