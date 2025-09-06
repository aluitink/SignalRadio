# SignalRadio

A trunk-recorder integration system that receives, processes, and stores radio recordings with Azure Blob Storage integration.

## Overview

SignalRadio is a multi-service architecture that:

## Quick Start

### Prerequisites


### 1. Clone and Setup

```bash
git clone <repository-url>
cd SignalRadio
```
 - WebClient: React-based web client built from `src/SignalRadio.WebClient` and served by nginx on port 3001 (docker-compose service name `webclient`).
### 2. Quick Setup (Recommended)

Run the setup script for guided configuration:

```bash
./setup.sh
```

This script will:
- Check for Docker/Docker Compose
- Create configuration files from templates
- Guide you through the essential settings
- Start the services automatically

### 3. Manual Setup (Alternative)

If you prefer manual setup:

Copy the template configuration and customize for your radio system:

```bash
# Copy template configuration
cp config/trunk-recorder-template.json config/trunk-recorder.json

# Edit configuration for your local system
nano config/trunk-recorder.json
```

**Key configuration changes needed:**
- Update `control_channels` with your system's control frequencies
- Set `center` frequency for your SDR coverage area
- Adjust `gain` settings based on your antenna/location
- Update `shortName` to identify your system
- Configure `talkgroupsFile` if you have one

### 4. Environment Configuration

Create environment file for Azure storage (optional for testing):

```bash
# Create .env file (optional - uses local storage if not set)
echo "AZURE_STORAGE_CONNECTION_STRING=your_connection_string_here" > .env
```

### 5. Docker Override Configuration (Optional)

For custom deployments (reverse proxy, custom ports, etc.), create a docker-compose override file:

```bash
# Copy the sample override file
cp docker-compose.override.yml.sample docker-compose.override.yml

# Edit for your environment
nano docker-compose.override.yml
```

**Common override scenarios:**

**nginx-proxy + Let's Encrypt:**
```yaml
services:
  signalradio-ui:
    environment:
      VIRTUAL_HOST: radio.yourdomain.com
      VIRTUAL_PORT: 80
      LETSENCRYPT_HOST: radio.yourdomain.com
      LETSENCRYPT_EMAIL: your-email@example.com
    networks:
      - signalradio-network
      - nginx-proxy
    external_links:
      - nginx-proxy

networks:
  nginx-proxy:
    external: true
```

**Custom ports:**
```yaml
services:
  signalradio-ui:
    ports:
      - "8080:80"
  signalradio-api:
    ports:
      - "8081:8080"
```

### 6. Start the System

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Check service status
docker-compose ps
```

### 7. Verify Operation

- API Health Check: http://localhost:5210/health
- Check logs: `docker-compose logs signalradio-api`
- Monitor uploads: `docker-compose logs trunk-recorder`
- Test upload: Use the sample files in `test-files/` directory

## Azure Storage Integration

### Configuration

The API supports both development and production Azure Storage configurations:

**Production** (`appsettings.json`):
```json
{
  "AzureStorage": {
    "ConnectionString": "your_azure_storage_connection_string",
    "ContainerName": "recordings",
    "DefaultPathPattern": "{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}"
  }
}
```

**Development** (`appsettings.Development.json`):
```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "recordings-dev",
    "DefaultPathPattern": "{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}"
  }
}
```

### File Organization

Recordings are automatically organized using a configurable path pattern:
- Default: `{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}`
- Example: `DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.wav`

### API Endpoints

- `POST /api/recording/upload` - Upload new recording (supports both WAV and M4A)
- `GET /api/recording/list` - List recordings with optional filtering
- `GET /api/recording/download/{blobName}` - Download a recording
- `DELETE /api/recording/delete/{blobName}` - Delete a recording
- `GET /health` - Service health check

### Storage Features

- **Automatic container creation** - Creates storage containers if they don't exist
- **Comprehensive metadata** - Stores talkgroup, system, frequency, and timing information
- **Path sanitization** - Ensures valid blob names for Azure Storage
- **Error handling** - Graceful handling of storage failures with proper logging

## Configuration Guide

### Trunk-Recorder Configuration

The main configuration file is `config/trunk-recorder.json`. Here's a basic template:

```json
{
  "ver": 2,
  "sources": [{
    "center": 857000000,
    "rate": 2048000,
    "error": 0,
    "gain": 40,
    "digitalRecorders": 4,
    "driver": "rtl",
    "device": "rtl=0"
  }],
  "systems": [{
    "shortName": "LOCAL",
    "type": "p25",
    "control_channels": [855462500],
    "modulation": "qpsk",
    "squelch": -50,
    "uploadScript": "/app/scripts/upload_callback.sh",
    "compressWav": false,
    "audioArchive": true,
    "callLog": true,
    "recordUnknown": true,
    "minDuration": 2.0
  }],
  "captureDir": "/app/audio",
  "tempDir": "/app/temp",
  "callTimeout": 5,
  "logLevel": "info"
}
```

### Finding Your System Information

Use [Radio Reference](https://www.radioreference.com) to find:
- Control channel frequencies
- System type (P25, SmartNet, etc.)
- Talk group information
- Frequency ranges

### SDR Configuration

1. **Find your SDR**: `rtl_test -t` (if using RTL-SDR)
2. **Test reception**: Use GQRX to verify you can receive the control channel
3. **Calculate center frequency**: Ensure your SDR bandwidth covers all needed frequencies
4. **Adjust gain**: Start with moderate gain (20-40) and adjust based on signal quality

## System Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Trunk-        │    │   SignalRadio   │    │   Azure Blob    │
│   Recorder      ├────┤   API           ├────┤   Storage       │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Data Flow

1. **trunk-recorder** captures radio traffic from SDR hardware
2. **Upload callback** triggered when recording completes
3. **SignalRadio API** receives file and metadata via HTTP POST
4. **Azure Storage** stores recordings with comprehensive metadata
5. **REST API** provides full CRUD operations for managing recordings

## Directory Structure

```
SignalRadio/
├── config/                     # Configuration files
│   ├── trunk-recorder.json     # Main trunk-recorder config
│   ├── trunk-recorder-template.json  # Template for new setups
│   └── danecom-talkgroups.csv  # Example talkgroups
├── scripts/                    # Upload callback scripts
│   ├── upload_callback.sh      # Main callback script
│   └── test_phase1.sh         # Testing script
├── docker/                     # Docker configurations
│   └── Dockerfile.api         # API container definition
├── src/                       # Source code
│   ├── SignalRadio.Api/       # ASP.NET Core API
│   └── SignalRadio.Core/      # Core library
├── docker-compose.yml         # Service orchestration
├── setup.sh                   # Quick setup script
└── README.md                  # This file
```

## Development Commands

```bash
# View real-time logs
docker-compose logs -f signalradio-api
docker-compose logs -f trunk-recorder

# Restart specific service
docker-compose restart signalradio-api

# Rebuild after code changes
docker-compose up --build signalradio-api

# Shell into containers
docker-compose exec signalradio-api bash
docker-compose exec trunk-recorder bash

# Stop all services
docker-compose down

# Clean up volumes (WARNING: removes recordings)
docker-compose down -v
```

## Troubleshooting

### SDR Issues

```bash
# Check if SDR is detected
docker-compose exec trunk-recorder rtl_test -t

# View trunk-recorder logs for frequency/gain issues
docker-compose logs trunk-recorder | grep -i "error\|control\|signal"
```

### Upload Issues

```bash
# Check upload callback logs
docker-compose exec trunk-recorder cat /app/logs/upload.log

# Test API connectivity
curl http://localhost:5210/health

# Test upload with sample files
curl -X POST http://localhost:5210/api/recording/upload \
  -F "TalkgroupId=12345" \
  -F "Frequency=851.0125" \
  -F "Timestamp=2025-08-16T21:00:00Z" \
  -F "SystemName=TestSystem" \
  -F "audioFile=@test-files/sample.wav"
```

### Common Problems

1. **No recordings**: Check control channel frequency and gain settings
2. **Upload failures**: Verify API endpoint is accessible between containers
3. **Permission errors**: Ensure script permissions are correct
4. **SDR not found**: Check USB device permissions and container privileged mode

## Configuration Examples

### RTL-SDR Single Dongle Setup
```json
{
  "sources": [{
    "center": 857000000,
    "rate": 2048000,
    "gain": 40,
    "digitalRecorders": 4,
    "driver": "rtl"
  }]
}
```

### Multiple RTL-SDR Setup
```json
{
  "sources": [
    {
      "center": 857000000,
      "rate": 2048000,
      "gain": 40,
      "digitalRecorders": 4,
      "driver": "rtl",
      "device": "rtl=0"
    },
    {
      "center": 862000000,
      "rate": 2048000,
      "gain": 40,
      "digitalRecorders": 4,
      "driver": "rtl",
      "device": "rtl=1"
    }
  ]
}
```

## Phase Development Status

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

✅ **Phase 3: Azure Storage** - COMPLETE
- Azure Blob Storage integration
- Metadata storage and retrieval
- File organization strategies
- Full CRUD API for recordings
- Development and production storage configurations

✅ **Phase 4: AI-Powered Transcription & Summarization** - COMPLETE
- Automatic Speech Recognition (ASR) with Azure Speech Services and Whisper
- AI-powered transcript summarization using Azure OpenAI
- Background processing for transcription and summarization
- Configurable AI models and parameters
- Full API endpoints for managing transcripts and summaries

⏳ **Phase 5: Advanced Features** - NEXT
- Background processing queues
- Monitoring and alerting
- Advanced audio analysis
- Performance optimization

## AI Summary Configuration

SignalRadio now includes AI-powered summarization of radio transcripts using Azure OpenAI and Semantic Kernel.

### Prerequisites

1. **Azure OpenAI Resource**: Create an Azure OpenAI resource in the Azure portal
2. **Model Deployment**: Deploy a chat completion model (e.g., `gpt-35-turbo` or `gpt-4`)
3. **API Access**: Obtain the endpoint URL and API key

### Configuration

Add the following settings to your `.env` file or `appsettings.json`:

```bash
# Enable AI Summary
AiSummary__Enabled=true
AiSummary__AutoSummarize=true

# Azure OpenAI Configuration
AiSummary__AzureOpenAiEndpoint=https://your-openai-resource.openai.azure.com
AiSummary__AzureOpenAiApiKey=your-api-key
AiSummary__ModelDeployment=gpt-35-turbo

# Optional: Customize AI behavior
AiSummary__MaxTokens=150
AiSummary__Temperature=0.3
AiSummary__TimeoutSeconds=30
AiSummary__MinTranscriptLength=50
AiSummary__ProcessingIntervalSeconds=5
```

### API Endpoints

- `GET /api/transcriptions/summaries` - Get transcriptions with summaries
- `POST /api/transcriptions/{id}/generate-summary` - Manually generate summary
- `GET /api/transcriptions/ai-summary-status` - Check AI service status

### Features

- **Automatic Processing**: Background service processes new transcriptions
- **Context-Aware**: Includes talkgroup, time, and confidence information
- **Error Handling**: Retry logic and error tracking
- **Manual Triggering**: Generate summaries on-demand via API
- **Health Monitoring**: Service status and queue monitoring

### Example Summary

For a transcript: *"Unit 23 to dispatch, traffic stop on Main Street, requesting backup"*

Generated summary: *"Police unit conducting traffic stop on Main Street and requesting backup assistance."*

## Contributing

This project follows a phased development approach. Focus on completing one phase before moving to the next to ensure stability and maintainability.

## Documentation

- [Quick Start Guide](docs/QUICK-START.md) - Get up and running quickly
- [API Documentation](docs/API.md) - Complete API reference
- [Phase 3: Azure Storage](docs/PHASE3-AZURE-STORAGE.md) - Azure Storage implementation details
- [Volume Management](docs/VOLUMES.md) - Docker volume configuration

## License

MIT License
