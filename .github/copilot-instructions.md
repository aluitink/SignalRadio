<!-- SignalRadio Project Instructions -->

## Project Overview
SignalRadio is a trunk-recorder integration system that:
- Receives audio recordings from trunk-recorder via callbacks
- Handles both raw wave (.wav) and compressed (.m4a) audio files
- Stores recordings in Azure Blob Storage
- Provides a REST API for upload handling

## Architecture
- Multi-service architecture using docker-compose
- Separate containers for trunk-recorder and SignalRadio API
- ASP.NET Core Web API for handling uploads
- Azure Blob Storage for file persistence
- Trunk-recorder provides both raw and compressed audio formats

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

✅ **Phase 2: Dual File Handling** - COMPLETE
- Support for both WAV and M4A file uploads
- Enhanced logging for tracking both file types
- No audio processing needed (trunk-recorder provides both formats)

🚧 **Phase 3: Azure Storage** - NEXT
- Azure Blob Storage integration
- Metadata storage and retrieval
- File organization strategies

⏳ **Phase 4: Advanced Features** - PLANNED
- Background processing queues
- Monitoring and alerting
- Advanced audio analysis
