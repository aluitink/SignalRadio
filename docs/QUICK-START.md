# SignalRadio Quick Start Guide

## Prerequisites

- Docker and Docker Compose installed
- RTL-SDR or compatible SDR hardware (for radio reception)
- Azure Storage Account (optional - uses development storage emulator by default)

## Quick Setup

### 1. Clone and Configure

```bash
git clone <your-repo-url>
cd SignalRadio

# Run the setup script for guided configuration
./setup.sh
```

### 2. Start the System

```bash
# Start all services
docker-compose up -d

# Check service status
docker-compose ps

# View logs
docker-compose logs -f
```

### 3. Verify Installation

```bash
# Check API health
curl http://localhost:5210/health

# Test upload with sample file
curl -X POST http://localhost:5210/api/recording/upload \
  -F "TalkgroupId=12345" \
  -F "Frequency=851.0125" \
  -F "Timestamp=2025-08-16T21:00:00Z" \
  -F "SystemName=TestSystem" \
  -F "audioFile=@test-files/sample.wav"

# List recordings
curl http://localhost:5210/api/recording/list
```

## Azure Storage Configuration

### Development (Default)

The system uses Azure Storage Emulator by default for development:

```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "recordings-dev"
  }
}
```

No additional setup required - the system will automatically create the development storage container.

### Production

For production, update `appsettings.json` or set environment variables:

```bash
# Option 1: Update appsettings.json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=your_account;AccountKey=your_key;EndpointSuffix=core.windows.net",
    "ContainerName": "recordings"
  }
}

# Option 2: Environment variable
export AZURE_STORAGE_CONNECTION_STRING="your_connection_string"
```

## Radio System Configuration

### 1. Find Your System Information

Use [Radio Reference](https://www.radioreference.com) to find:
- Control channel frequencies
- System type (P25, SmartNet, etc.)
- Talk group information

### 2. Update Configuration

Edit `config/trunk-recorder.json`:

```json
{
  "systems": [{
    "shortName": "YOUR_SYSTEM",
    "type": "p25",
    "control_channels": [your_control_frequency],
    "uploadScript": "/app/scripts/upload_callback.sh"
  }]
}
```

### 3. Test Radio Reception

```bash
# Check if SDR is detected
docker-compose exec trunk-recorder rtl_test -t

# Monitor trunk-recorder logs
docker-compose logs -f trunk-recorder
```

## Available Features

✅ **Dual File Upload**: Supports both WAV and M4A files  
✅ **Azure Storage**: Persistent storage with metadata  
✅ **File Organization**: Automatic hierarchical organization  
✅ **REST API**: Full CRUD operations for recordings  
✅ **Development Tools**: Sample files and test endpoints  

## API Endpoints

- `GET /health` - Service health check
- `POST /api/recording/upload` - Upload recordings
- `GET /api/recording/list` - List recordings with filtering
- `GET /api/recording/download/{blobName}` - Download recordings
- `DELETE /api/recording/delete/{blobName}` - Delete recordings

See `docs/API.md` for complete API documentation.

## Troubleshooting

### Common Issues

1. **API not responding**: Check if container is running: `docker-compose ps`
2. **Upload failures**: Verify trunk-recorder can reach API: `docker-compose logs trunk-recorder`
3. **Storage errors**: Check Azure Storage configuration and connectivity
4. **No recordings**: Verify SDR hardware and control channel frequency

### Useful Commands

```bash
# View real-time logs
docker-compose logs -f signalradio-api

# Restart specific service
docker-compose restart signalradio-api

# Shell into API container
docker-compose exec signalradio-api bash

# Clean restart
docker-compose down && docker-compose up -d
```

## File Structure

Recordings are organized as:
```
{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}/
├── 20250816-210000-851.0125Hz.wav
├── 20250816-210030-851.0125Hz.m4a
└── ...
```

## Next Steps

With the basic system running, you can:

1. Configure your specific radio system frequencies
2. Set up production Azure Storage if needed
3. Monitor recordings in the Azure Storage container
4. Use the API to build custom applications

For advanced features and Phase 4 development, see the main README.md.
