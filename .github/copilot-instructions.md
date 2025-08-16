<!-- SignalRadio Project Instructions -->

## Project Overview
SignalRadio is a trunk-recorder integration system that:
- Receives audio recordings from trunk-recorder via callbacks
- Processes and compresses audio files for optimal storage
- Stores recordings in Azure Blob Storage
- Provides a REST API for upload handling

## Architecture
- Multi-service architecture using docker-compose
- Separate containers for trunk-recorder and SignalRadio API
- ASP.NET Core Web API for handling uploads
- Azure Blob Storage for file persistence
- FFmpeg for audio processing and compression

## Development Guidelines
- Build incrementally, one layer at a time
- Focus on testable components
- Use docker-compose for service orchestration
- Implement proper error handling and logging
- Follow separation of concerns principles
- **After successful implementation of features or significant changes, offer to commit changes to git**

## Phase Status
✅ **Phase 1: Foundation Setup** - COMPLETE
- Basic API structure with RecordingController
- Docker-compose configuration
- Upload callback script for trunk-recorder
- Project structure and build system
- Health check and basic upload endpoints

🚧 **Phase 2: Audio Processing** - NEXT
- FFmpeg integration for audio compression
- Support for multiple audio formats (Opus, AAC, MP3)
- Audio quality optimization

⏳ **Phase 3: Azure Storage** - PLANNED
- Azure Blob Storage integration
- Metadata storage and retrieval
- File organization strategies

⏳ **Phase 4: Advanced Features** - PLANNED
- Background processing queues
- Monitoring and alerting
- Advanced audio analysis
