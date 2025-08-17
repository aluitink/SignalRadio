# SignalRadio UI - Modular Architecture

The SignalRadio UI has been refactored from a single large JavaScript file into a modular architecture for better maintainability, testability, and organization.

## File Structure

```
js/
├── app.js                     # Main application entry point
├── app-original.js           # Backup of original monolithic app.js
└── modules/
    ├── connection-manager.js  # SignalR connection management
    ├── audio-manager.js      # Audio playback and queue management
    ├── data-manager.js       # API calls and data caching
    ├── settings-manager.js   # User preferences and local storage
    ├── ui-manager.js         # DOM manipulation and UI updates
    └── utils.js              # Utility functions and formatters
```

## Module Responsibilities

### ConnectionManager (`connection-manager.js`)
- Manages SignalR connection lifecycle
- Handles connection events (connect, disconnect, reconnect)
- Manages SignalR event subscriptions
- Handles talk group subscriptions/unsubscriptions

### AudioManager (`audio-manager.js`)
- Audio playback functionality
- Queue management for multiple calls
- User interaction detection for auto-play
- Audio controls and progress tracking

### DataManager (`data-manager.js`)
- API calls to backend services
- Talk group data caching
- Recent calls loading
- Data fetching and caching strategies

### SettingsManager (`settings-manager.js`)
- Local storage operations
- User preference management
- Settings persistence and loading

### UIManager (`ui-manager.js`)
- DOM manipulation
- Call card creation and updates
- Toast notifications
- UI state management
- Event listener setup

### Utils (`utils.js`)
- Date/time formatting
- Frequency formatting
- Priority classification
- Recording quality assessment
- General utility functions

## Benefits of Modular Architecture

1. **Separation of Concerns**: Each module has a clear, focused responsibility
2. **Maintainability**: Easier to locate and modify specific functionality
3. **Testability**: Individual modules can be unit tested in isolation
4. **Reusability**: Modules can potentially be reused in other contexts
5. **Scalability**: New features can be added without affecting existing modules
6. **Code Organization**: Logical grouping makes the codebase easier to navigate

## Migration Notes

- The original `app.js` has been backed up as `app-original.js`
- The new modular structure maintains the same external API
- All existing onclick handlers continue to work
- ES6 modules are used with `import`/`export` statements
- The HTML now loads the script with `type="module"`

## Usage

The main `SignalRadioApp` class instantiates all managers and coordinates between them. The app instance is still available globally as `window.app` for onclick handlers.

```javascript
// App initialization happens automatically on DOMContentLoaded
// Global app instance is available for onclick handlers
window.app.playCall(callData);
window.app.toggleSubscription(talkGroupId, buttonElement);
```

## Development

When adding new features:

1. Identify which module the feature belongs to
2. Add the functionality to the appropriate module
3. Update the main app class if coordination between modules is needed
4. Consider creating new modules for entirely new feature areas

## Testing

Each module can be tested independently:

```javascript
import { Utils } from './modules/utils.js';
const utils = new Utils();
// Test utility functions...
```
