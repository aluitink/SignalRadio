Resource .env if missing connection string

Keep your final response concise and short.

## UI/UX Design Guidelines

### Design Principles
- **Mobile-first responsive design** - All components must work seamlessly on mobile and desktop
- **Uniform interaction patterns** - Clicking call cards plays audio across all pages
- **Minimal cognitive load** - Clean, focused interfaces with essential information only
- **Consistent visual hierarchy** - Use typography, spacing, and color systematically

### Visual Design
- **Color Palette**: Dark theme with blue gradient background, subtle card backgrounds
- **Typography**: Inter font family, clear size hierarchy (h1: 24px, h2: 20px, body: 16px)
- **Spacing**: 8px base unit system (8, 16, 24, 32px)
- **Cards**: Subtle backgrounds (rgba(255,255,255,0.02)), 8px border radius, 16px padding

### Component Standards
- **Call Cards**: Clickable for audio playback, show talkgroup (linkable), duration, time, frequency, transcript
- **Navigation**: Hamburger menu on mobile, tab bar on desktop
- **Loading States**: Skeleton loading for call cards, connection status indicators
- **Audio Playback**: Visual feedback when playing, auto-play for subscribed talkgroups

### User Experience Patterns
- **Subscription Model**: Users can subscribe to talkgroups for auto-play of new calls
- **Progressive Enhancement**: Core functionality works without JavaScript
- **Accessibility**: ARIA labels, keyboard navigation, screen reader support
- **Performance**: Lazy loading, virtualized lists for large datasets
