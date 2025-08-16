# SignalRadio

A trunk-recorder integration system that receives, processes, and stores radio recordings with Azure Blob Storage integration.

## Overview

SignalRadio is a multi-service architecture that:
- Receives audio recordings from trunk-recorder via callbacks
- Processes and compresses audio files using FFmpeg
- Stores recordings in Azure Blob Storage
- Provides a REST API for upload handling

## Quick Start

### Prerequisites

- Docker and Docker Compose
- RTL-SDR or compatible SDR hardware
- Azure Storage Account (for production storage)

### 1. Clone and Setup

```bash
git clone <repository-url>
cd SignalRadio
```

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

### 5. Start the System

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Check service status
docker-compose ps
```

### 6. Verify Operation

- API Health Check: http://localhost:5000/health
- Check logs: `docker-compose logs signalradio-api`
- Monitor uploads: `docker-compose logs trunk-recorder`

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
4. **FFmpeg processing** compresses audio (Phase 2)
5. **Azure Storage** stores final recordings with metadata

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
curl http://localhost:5000/health
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

## Contributing

This project follows a phased development approach. Focus on completing one phase before moving to the next to ensure stability and maintainability.

## License

MIT License
