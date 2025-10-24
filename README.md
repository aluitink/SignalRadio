# SignalRadio

A trunk-recorder integration system that receives, processes, and stores radio recordings with Azure Blob Storage integration.

## Overview

SignalRadio is a complete radio recording and management system that integrates with trunk-recorder to:
- Capture radio traffic from software-defined radio (SDR) hardware
- Upload and process recordings with metadata
- Store recordings in Azure Blob Storage
- Provide a web interface to browse and search recordings
- Generate AI-powered summaries of transcripts (optional)

The system uses Docker for easy deployment and includes everything needed to get started.

## Quick Start

### Prerequisites


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

**Optional**: Configure Azure Blob Storage. If not configured, the system uses local storage:

```bash
# Create .env file for Azure storage (optional)
echo "AZURE_STORAGE_CONNECTION_STRING=your_connection_string_here" > .env
```

If the `.env` file is not created or the connection string is not set, recordings will be stored locally.

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

## HTTPS with nginx-proxy & Let's Encrypt (Optional)

SignalRadio includes a pre-configured `docker-compose.nginx.yml` for setting up HTTPS with nginx-proxy and Let's Encrypt certificates.

### How to use

1. **Edit your domain and email**
   - Open `docker-compose.nginx.yml` and set your domain and email in the `DEFAULT_EMAIL` environment variable for the `acme-companion` service.

2. **Start nginx-proxy and companion**
   - Run:
     ```bash
     docker-compose -f docker-compose.nginx.yml up -d
     ```
   - This will start the proxy and certificate manager containers.

3. **Configure SignalRadio UI for HTTPS**
   - In your `docker-compose.override.yml`, set the following environment variables for the UI service:
     ```yaml
     VIRTUAL_HOST: radio.yourdomain.com
     VIRTUAL_PORT: 80
     LETSENCRYPT_HOST: radio.yourdomain.com
     LETSENCRYPT_EMAIL: your@email.com
     ```
   - Make sure the UI service is on the same Docker network as nginx-proxy (see example above).

4. **Point your DNS A record to your server's public IP.**

5. **Start SignalRadio services**
   - Run:
     ```bash
     docker-compose up -d
     ```

6. **Access your site securely**
   - Visit `https://radio.yourdomain.com` to verify HTTPS is working.

For more details, see the comments in `docker-compose.nginx.yml` and the [nginx-proxy documentation](https://github.com/nginx-proxy/nginx-proxy).

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

- API Health Check: http://localhost:5000/health
- Web UI: http://localhost:3001
- Check logs: `docker-compose logs signalradio-api`
- Monitor uploads: `docker-compose logs trunk-recorder`
- Test upload: Use the sample files or `curl` commands in Development section

## Storage Configuration

SignalRadio supports two storage backends: **local file storage** (for development and small deployments) and **Azure Blob Storage** (for production and scalable deployments).

### Local Storage (Default)

By default, recordings are stored locally on the container's filesystem. This is ideal for:
- Development and testing
- Small deployments
- Quick testing without Azure credentials

No configuration required - recordings are automatically stored in the container's volume mount.

### Azure Blob Storage

For production deployments, recordings can be stored in Azure Blob Storage. This provides:
- Scalable cloud storage
- Automatic backups
- Easy access from anywhere
- Integration with Azure services

#### Configuration

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

### API Reference

**Health & Status**
- `GET /health` - Service health check (returns status and timestamp)

**Calls Management**
- `GET /api/calls` - List all calls (pagination: `page`, `pageSize`; sorting: `sortBy`, `sortDir`)
- `GET /api/calls/{id}` - Get call details by ID
- `GET /api/calls/transcripts-available` - Get talkgroups with transcripts in time window
- `POST /api/calls` - Create new call record
- `PUT /api/calls/{id}` - Update call metadata
- `DELETE /api/calls/{id}` - Delete call record

**Recordings Management**
- `GET /api/recordings` - List all recordings (pagination: `page`, `pageSize`)
- `GET /api/recordings/{id}` - Get recording details by ID
- `GET /api/recordings/{id}/file` - Download audio file (streams WAV/MP3/M4A)
- `POST /api/recordings` - Create recording record
- `POST /api/recordings/upload` - Upload new recording with metadata (multipart/form-data)
  - Form fields: `file` (audio), `metadata` (JSON: RecordingUploadRequest)
  - Supports: WAV, MP3, M4A formats
  - Max size: 500MB (configurable)
- `PUT /api/recordings/{id}` - Update recording metadata
- `DELETE /api/recordings/{id}` - Delete recording and associated file

**TalkGroups Management**
- `GET /api/talkgroups` - List all talkgroups (pagination: `page`, `pageSize`)
- `GET /api/talkgroups/{id}` - Get talkgroup details by ID
- `GET /api/talkgroups/{id}/calls` - Get calls for talkgroup (pagination, sorting)
- `GET /api/talkgroups/{id}/calls-by-frequency` - Group calls by frequency within talkgroup
- `GET /api/talkgroups/{id}/summary` - Generate AI summary (window: `windowMinutes` 5-1440)
- `GET /api/talkgroups/stats` - Get stats across all talkgroups
- `POST /api/talkgroups` - Create new talkgroup
- `POST /api/talkgroups/import` - Bulk import from CSV file (form field: `file`)
- `PUT /api/talkgroups/{id}` - Update talkgroup metadata
- `DELETE /api/talkgroups/{id}` - Delete talkgroup

**Transcriptions**
- `GET /api/transcriptions` - List all transcriptions (pagination: `page`, `pageSize`)
- `GET /api/transcriptions/{id}` - Get transcription by ID
- `GET /api/transcriptions/search?q=term` - Search transcriptions by text
- `POST /api/transcriptions` - Create transcription record
- `PUT /api/transcriptions/{id}` - Update transcription
- `DELETE /api/transcriptions/{id}` - Delete transcription

**Full-Text Search**
- `GET /api/search?q=term` - Search across all content (summaries, incidents, topics)
  - Query params: `q` (required), `types` (Summary,Incident,Topic), `page`, `pageSize`
- `GET /api/search/summaries?q=term` - Search transcript summaries with filters
  - Filters: `talkGroupId`, `startDate`, `endDate`, `maxResults`
- `GET /api/search/incidents?q=term` - Search notable incidents
  - Filters: `minImportanceScore`, `maxResults`
- `GET /api/search/topics?q=term` - Search topics by category
  - Filters: `category`, `maxResults`

**Real-time Communication (SignalR)**
- Hub URL: `/hubs/talkgroup`
- Connected clients receive live call notifications and can subscribe to talkgroups

### Storage Features

- **Automatic container creation** - Creates storage containers if they don't exist
- **Comprehensive metadata** - Stores talkgroup, system, frequency, and timing information
- **Path sanitization** - Ensures valid blob names for Azure Storage
- **Error handling** - Graceful handling of storage failures with proper logging

## AI Transcript Summarization (Optional)

SignalRadio includes AI-powered transcript summarization using Microsoft Semantic Kernel and Azure OpenAI. This feature analyzes radio communication transcripts over configurable time windows and provides intelligent summaries.

### Features

- **Intelligent Analysis**: Context-aware summaries of radio communications
- **Key Topic Extraction**: Identifies important themes and subjects
- **Notable Incident Detection**: Highlights significant events and emergencies
- **Configurable Time Windows**: 15 minutes to 24 hours
- **Caching**: Reduces API costs with intelligent caching (30-minute default)
- **Mobile-Responsive UI**: Clean interface integrated into TalkGroup pages

### Configuration

1. **Copy environment template**:
```bash
cp .env.sample .env
```

2. **Enable AI summarization** in your `.env` file:
```bash
# Enable AI Transcript Summarization
SEMANTIC_KERNEL_ENABLED=true
AZURE_OPENAI_ENDPOINT=https://your-openai-resource.openai.azure.com/
AZURE_OPENAI_KEY=your-azure-openai-api-key-here
AZURE_OPENAI_DEPLOYMENT=gpt-4
```

3. **Optional fine-tuning** (advanced):
```bash
# Optional: Adjust AI behavior
SEMANTIC_KERNEL_MAX_TOKENS=1500
SEMANTIC_KERNEL_TEMPERATURE=0.3
SEMANTIC_KERNEL_CACHE_DURATION=30
SEMANTIC_KERNEL_DEFAULT_WINDOW=60
```

4. **Restart services**:
```bash
docker-compose up -d
```

### Azure OpenAI Setup

1. **Create Azure OpenAI Resource**: Go to Azure Portal → Create Resource → Azure OpenAI
2. **Deploy a Model**: Deploy GPT-4 or GPT-3.5-turbo in your resource
3. **Get Credentials**: Copy the endpoint URL and API key
4. **Configure**: Add credentials to your `.env` file

### Usage

Once configured, AI summaries appear automatically on TalkGroup pages when transcripts are available. Users can:

- Select different time windows (15 minutes to 24 hours)
- Force refresh cached summaries
- View key topics as tags
- See notable incidents in a bulleted list
- Expand/collapse long summaries

### API Endpoints

- `GET /api/talkgroups/{id}/summary` - Generate summary for last hour
- `POST /api/transcriptsummary/custom` - Custom time range summary
- `GET /api/transcriptsummary/status` - Check service availability
- `DELETE /api/transcriptsummary/cache` - Clear cached summaries

### Privacy & Security

- All processing via Azure OpenAI (GDPR compliant)
- No transcript data stored by OpenAI
- Configurable token limits prevent excessive usage
- Service can be completely disabled via configuration

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

### Multi-Layer Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           CLIENT LAYER                                           │
├─────────────────────────────────────────────────────────────────────────────────┤
│  React Web UI (Vite)    │  Audio Playback    │  Real-time Updates (SignalR)   │
│  - Call Browser         │  - HTML5 <audio>   │  - Live talkgroup feeds        │
│  - Search Interface     │  - Stream loading  │  - Call notifications           │
│  - TalkGroup Stats      │  - Transcript view │  - Subscription management      │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        ↓ HTTP/REST/WebSocket
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            API LAYER (.NET Core)                                 │
├─────────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│  │   Calls      │  │ Recordings   │  │  TalkGroups  │  │   Search     │        │
│  │  Controller  │  │  Controller  │  │  Controller  │  │  Controller  │        │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                          │
│  │Transcriptions│  │  Transcript  │  │ StorageLocations                        │
│  │  Controller  │  │  Summary     │  │  Controller  │                          │
│  └──────────────┘  └──────────────┘  └──────────────┘                          │
│         ↓                ↓                  ↓                                     │
│  ┌──────────────────────────────────────────────────────────────────┐           │
│  │           SERVICE LAYER (Business Logic)                         │           │
│  ├──────────────────────────────────────────────────────────────────┤           │
│  │ • CallsService        • RecordingsService    • TalkGroupsService │           │
│  │ • TranscriptService   • TranscriptSummaryService               │           │
│  │ • StorageService (Azure/Local)    • AsrService (Whisper/Azure) │           │
│  │ • CallNotifier (SignalR Hub)       • LocalFileCacheService    │           │
│  └──────────────────────────────────────────────────────────────────┘           │
│         ↓                ↓                  ↓                                     │
│  ┌──────────────────────────────────────────────────────────────────┐           │
│  │          DATA ACCESS LAYER (Entity Framework Core)               │           │
│  └──────────────────────────────────────────────────────────────────┘           │
└─────────────────────────────────────────────────────────────────────────────────┘
             ↓ SQL Queries              ↓ File Upload/Download
┌──────────────────────────┐  ┌──────────────────────────────────────┐
│    SQL SERVER            │  │   STORAGE BACKEND                    │
├──────────────────────────┤  ├──────────────────────────────────────┤
│  • Calls Table           │  │  Azure Blob Storage (Production)     │
│  • Recordings Table      │  │  - {SystemName}/                    │
│  • TalkGroups Table      │  │    {TalkgroupId}/                   │
│  • Transcriptions Table  │  │    {Year}/{Month}/{Day}/...         │
│  • Transcripts Table     │  │                                       │
│  • FullText Indexes      │  │  Local Disk Storage (Development)   │
│                          │  │  - /data/recordings/...              │
└──────────────────────────┘  └──────────────────────────────────────┘
        ↑                              ↑
        └──────────────────────────────┘
             SQL Connection       File I/O
```

### Data Flow Sequence

```
trunk-recorder       SignalRadio API      SignalRadio DB     Storage
      │                   │                     │               │
      │  1. Record audio  │                     │               │
      │  and metadata     │                     │               │
      │◄──────────────────┤                     │               │
      │                   │                     │               │
      │  2. Upload via    │                     │               │
      │  POST /upload    ─────────────────────►│               │
      │  (multipart/form) │                     │               │
      │                   │  3. Store metadata  │               │
      │                   │  to database    ────────────────────│
      │                   │                     │               │
      │                   │  4. Stream file ───────────────────►│
      │                   │  to storage backend │               │
      │                   │                     │               │
      │  5. 201 Created   │◄────────────────────────────────────│
      │◄──────────────────│                     │               │
      │                   │                     │               │
      │  6. Optional: Transcribe (ASR)         │               │
      │                   │  POST to Whisper   │               │
      │                   │  or Azure Speech    │               │
      │                   │                     │               │
      │                   │  7. Store transcript               │
      │                   │◄────────────────────────────────────│
      │                   │                     │               │
      │                   │  8. Optional: Generate Summary      │
      │                   │  (Semantic Kernel)  │               │
      │                   │                     │               │
```

### Technology Stack

**Backend:**
- **Framework**: ASP.NET Core 8.0 (C#)
- **Database**: SQL Server Express with Full-Text Search indexes
- **ORM**: Entity Framework Core with code-first migrations
- **Real-time**: SignalR for live updates and subscriptions
- **API**: RESTful endpoints with OpenAPI documentation
- **Audio**: WAV and M4A format support

**Frontend:**
- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite (fast development server and production bundler)
- **Styling**: CSS with responsive design
- **Client Library**: @microsoft/signalr for real-time communication
- **Routing**: React Router v7

**Infrastructure:**
- **Containerization**: Docker & Docker Compose
- **Audio Processing**: Whisper ASR (OpenAI) for speech-to-text transcription
- **AI Summarization**: Azure OpenAI with Semantic Kernel (optional)
- **Storage**: Azure Blob Storage or Local Disk (configurable)
- **Radio Capture**: trunk-recorder with RTL-SDR support

## Directory Structure

```
SignalRadio/
├── config/                             # Configuration files
│   ├── trunk-recorder.json             # Main trunk-recorder config (runtime)
│   ├── trunk-recorder-template.json    # Template for new setups
│   ├── trunk-recorder-danecom.json     # Example: Dane County system config
│   └── danecom-talkgroups.csv          # Example: Talkgroup definitions
│
├── docker/                             # Docker build definitions
│   ├── Dockerfile.api                  # ASP.NET Core API container
│   ├── Dockerfile.mssql                # SQL Server container
│   └── Dockerfile.webclient            # React/Nginx web UI container
│
├── scripts/                            # Callback and utility scripts
│   ├── upload_callback.sh              # trunk-recorder completion hook
│   └── test_phase1.sh                  # Testing utilities
│
├── src/                                # Application source code
│   ├── SignalRadio.Api/                # ASP.NET Core API service
│   │   ├── Controllers/                # REST API endpoints
│   │   │   ├── CallsController.cs
│   │   │   ├── RecordingsController.cs
│   │   │   ├── TalkGroupsController.cs
│   │   │   ├── TranscriptionsController.cs
│   │   │   ├── SearchController.cs
│   │   │   ├── StorageLocationsController.cs
│   │   │   └── TranscriptSummariesController.cs
│   │   ├── Services/                   # Business logic services
│   │   ├── Hubs/                       # SignalR real-time hubs
│   │   ├── Migrations/                 # EF Core database migrations
│   │   ├── Dtos/                       # Data transfer objects
│   │   ├── Program.cs                  # Startup configuration
│   │   └── appsettings*.json           # Configuration files
│   │
│   ├── SignalRadio.Core/               # Shared models and interfaces
│   │   ├── Models/                     # Domain models
│   │   ├── Interfaces/                 # Service contracts
│   │   └── Services/                   # Core business services
│   │
│   ├── SignalRadio.DataAccess/         # Data access layer (EF Core)
│   │   ├── SignalRadioDbContext.cs     # EF DbContext
│   │   ├── Services/                   # Data access services
│   │   ├── Models/                     # Database entity models
│   │   └── Interfaces/                 # Repository patterns
│   │
│   └── SignalRadio.WebClient/          # React TypeScript web UI
│       ├── src/
│       │   ├── components/             # Reusable UI components
│       │   ├── pages/                  # Page-level components
│       │   ├── services/               # API client and utilities
│       │   ├── contexts/               # React context providers
│       │   ├── hooks/                  # Custom React hooks
│       │   ├── types/                  # TypeScript type definitions
│       │   ├── App.tsx                 # Main app component
│       │   └── main.tsx                # React entry point
│       ├── public/                     # Static assets
│       ├── package.json                # NPM dependencies
│       ├── vite.config.ts              # Vite build configuration
│       └── nginx.conf                  # Nginx web server config
│
├── docker-compose.yml                  # Multi-container orchestration
├── docker-compose.override.yml.sample  # Override template for custom deployments
├── SignalRadio.sln                     # Visual Studio solution file
├── setup.sh                            # Quick setup script
└── README.md                           # This file
```

### Key Project Files

**Configuration**
- `appsettings.json` - Production API configuration
- `appsettings.Development.json` - Development API configuration
- `trunk-recorder.json` - Radio system and SDR configuration
- `.env` - Environment variables (create from template)

**Volumes (Docker)**
- `api-recordings` - Local recording storage (production fallback)
- `sqlserver-data` - SQL Server database files
- `temp` - Temporary processing directory
- `logs` - Application and trunk-recorder logs
- `whisper-cache` - Cached ASR models

## Development Commands

### Docker Operations

```bash
# Start all services in background
docker-compose up -d

# Start with real-time log output
docker-compose up

# Rebuild specific service after code changes
docker-compose up --build signalradio-api
docker-compose up --build signalradio-webclient

# View real-time logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f signalradio-api
docker-compose logs -f sqlserver
docker-compose logs -f trunk-recorder
docker-compose logs -f whisper-asr

# Check service status and resource usage
docker-compose ps

# Restart specific service (preserves volumes/data)
docker-compose restart signalradio-api
docker-compose restart sqlserver

# Stop all services (preserves volumes/data)
docker-compose stop

# Remove all containers but keep volumes
docker-compose down

# Full cleanup (WARNING: removes all data including recordings!)
docker-compose down -v

# Execute command in running container
docker-compose exec signalradio-api bash
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "SignalRadio123!"

# View container resource usage
docker stats

# Inspect network and volumes
docker network ls
docker volume ls
```

### Database Operations

```bash
# Run database migrations
docker-compose exec signalradio-api dotnet ef migrations add MigrationName
docker-compose exec signalradio-api dotnet ef database update

# View SQL Server error logs
docker-compose logs sqlserver | grep -i error

# Connect to SQL Server directly (from container)
docker-compose exec sqlserver sqlcmd -S localhost -U sa -P "SignalRadio123!"
# Then run: SELECT * FROM [SignalRadio].[dbo].[Calls];

# Backup database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "SignalRadio123!" \
  -Q "BACKUP DATABASE SignalRadio TO DISK = '/var/opt/mssql/backup/SignalRadio.bak'"
```

### Local Development (without Docker)

```bash
# Build .NET solution
dotnet build SignalRadio.sln

# Run API service
cd src/SignalRadio.Api
dotnet run

# Build React frontend
cd src/SignalRadio.WebClient
npm install
npm run dev

# Build for production
npm run build
npm run preview
```

### Testing

```bash
# Run unit tests (if configured)
dotnet test SignalRadio.sln

# Test API endpoints
curl http://localhost:5000/health
curl http://localhost:5000/api/calls
curl http://localhost:5000/api/talkgroups

# Test upload endpoint
curl -X POST http://localhost:5000/api/recordings/upload \
  -F "file=@test-files/sample.wav" \
  -F 'metadata={"TalkgroupId":12345,"Frequency":"851.0125","SystemName":"TestSystem","Timestamp":"2025-08-16T21:00:00Z"}'
```

## Docker Container Architecture

### Service Topology

```
┌─────────────────────────────────────────────────────────────────┐
│                    Docker Network: signalradio-network          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │  trunk-recorder  │  │  signalradio-api │  │ sqlserver    │  │
│  │  (robotastic/    │  │  (ASP.NET Core)  │  │ (MSSQL)      │  │
│  │   trunk-         │  │  Port: 5000→8080 │  │ Port: 1433   │  │
│  │   recorder)      │  │  Memory: ∞       │  │ Memory: 3GB  │  │
│  │                  │  │                  │  │              │  │
│  │  RTL-SDR: /dev   │  │  Env: Dev/Prod   │  │  EULA: Y     │  │
│  │  Privileged: true│  │                  │  │              │  │
│  └──────────────────┘  └──────────────────┘  └──────────────┘  │
│         ↓ Upload              ↓ HTTP                ↓ SQL Query  │
│         └──────────────────────┴────────────────────┴──────────┘ │
│                                                                   │
│  ┌──────────────────┐  ┌──────────────────────────────────────┐  │
│  │  whisper-asr     │  │  signalradio-webclient              │  │
│  │  (onerahmet/     │  │  (React + Nginx)                    │  │
│  │   openai-        │  │  Port: 3001→80                      │  │
│  │   whisper-asr)   │  │  Memory: ∞                          │  │
│  │  Port: 9000      │  │                                      │  │
│  │  Memory: 2GB     │  │  Dependencies:                      │  │
│  │                  │  │  - api (network)                    │  │
│  │  Model: small.en │  │  - sqlserver (transitively)         │  │
│  └──────────────────┘  └──────────────────────────────────────┘  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘

External Interfaces:
  • HTTP:    localhost:5000 (API), localhost:3001 (Web UI)
  • WebSocket: /hubs/talkgroup (SignalR real-time)
  • ASR:     http://whisper-asr:9000 (internal)
  • USB:     /dev/bus/usb (trunk-recorder hardware)
```

### Volume Mounts

**Named Volumes** (persist across container restarts)
- `api-recordings:/data/recordings` - Local fallback recording storage
- `trunk-recordings:/app/audio` - trunk-recorder audio staging area
- `temp:/app/temp` - Temporary processing files
- `logs:/app/logs` - Application and service logs
- `sqlserver-data:/var/opt/mssql` - SQL Server database files
- `whisper-cache:/root/.cache` - Cached ASR models (faster startup)

**Bind Mounts** (host filesystem)
- `./config:/app/config:ro` - Configuration files (read-only)
- `./scripts:/app/scripts:ro` - Callback scripts (read-only)
- `/var/run/dbus` - D-Bus for audio system (trunk-recorder)

### Service Dependencies

```
trunk-recorder ──────┐
                     ├─► api ──► sqlserver
whisper-asr ─────────┤
                     └─► webclient
```

- **API** depends on **sqlserver** (database migrations run on startup)
- **webclient** depends on **api** (frontend proxies to backend)
- **trunk-recorder** depends on **api** (sends uploads)
- **whisper-asr** has no dependencies (optional standalone service)

### Environment Configuration

See `docker-compose.yml` for full environment variable reference. Key variables:

**API Service**
```
ASPNETCORE_ENVIRONMENT=Development
StorageType=Local
ASR_PROVIDER=whisper
SEMANTIC_KERNEL_ENABLED=false
```

**Whisper ASR**
```
ASR_MODEL=small.en
ASR_ENGINE=openai_whisper
ASR_DEVICE=cpu
```

**Database**
```
ACCEPT_EULA=Y
SA_PASSWORD=SignalRadio123!
MSSQL_PID=Express
```

## Troubleshooting

### Connection Issues

**API not reachable from webclient**
```bash
# Test API connectivity from webclient container
docker-compose exec signalradio-webclient curl http://signalradio-api:8080/health

# Check container network
docker-compose exec signalradio-api ping signalradio-sqlserver

# Verify service is listening
docker-compose exec signalradio-api netstat -an | grep 8080
```

**Database connection timeout**
```bash
# Check if SQL Server is ready
docker-compose logs sqlserver | grep -i "ready"

# SQL Server startup can take 30-60 seconds
docker-compose logs --tail=50 sqlserver

# Manually test connection
docker-compose exec signalradio-api dotnet \
  /app/SignalRadio.Api.dll --help
```

### SDR Issues

```bash
# Check if RTL-SDR is detected by trunk-recorder
docker-compose exec trunk-recorder rtl_test -t

# View trunk-recorder logs for frequency errors
docker-compose logs trunk-recorder | grep -i "error\|control\|signal\|frequency"

# Check for USB device permissions
docker-compose logs trunk-recorder | grep -i "usb\|device\|permission"

# Verify device is available on host
lsusb | grep -i RTL
```

### Upload Issues

```bash
# Check upload callback logs
docker-compose logs trunk-recorder | grep -i "upload\|callback"

# Test API upload manually
curl -v -X POST http://localhost:5000/api/recordings/upload \
  -F "file=@test.wav" \
  -F 'metadata={"TalkgroupId":1,"Frequency":"851.0125","SystemName":"Test","Timestamp":"2025-08-16T21:00:00Z"}'

# Check file permissions in trunk-recorder
docker-compose exec trunk-recorder ls -la /app/scripts/
docker-compose exec trunk-recorder cat /app/scripts/upload_callback.sh
```

### Transcription Issues

```bash
# Check if Whisper service is running
curl http://localhost:9000/status

# Check Whisper logs for model errors
docker-compose logs whisper-asr | grep -i "model\|error\|cuda"

# Verify ASR configuration in API
docker-compose logs signalradio-api | grep -i "asr\|whisper"

# Test Whisper endpoint directly
curl -X POST http://localhost:9000/asr \
  -F "audio_file=@test.wav"
```

### Storage Issues

**Local storage not persisting**
```bash
# Check volume mounts
docker-compose exec signalradio-api df -h

# Check disk space
docker-compose exec signalradio-api du -sh /data/recordings

# Verify volume exists
docker volume ls | grep signalradio
docker volume inspect signalradio_api-recordings
```

**Azure Storage connection failure**
```bash
# Check connection string in environment
docker-compose exec signalradio-api env | grep AZURE

# Verify credentials are loaded
docker-compose logs signalradio-api | grep -i "azure\|storage"

# Test connection manually (in API container)
# Use Azure Storage Explorer or run: az storage account list
```

### Performance Issues

**API slow responses**
```bash
# Check database query performance
docker-compose exec signalradio-api dotnet ef dbcontext info

# Monitor API resource usage
docker stats signalradio-api

# Check for long-running operations
docker-compose logs signalradio-api | grep -i "timeout\|slow\|elapsed"

# Monitor SQL Server performance
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "SignalRadio123!" \
  -Q "SELECT * FROM sys.dm_exec_requests WHERE status = 'running'"
```

**High memory usage**
```bash
# Check Whisper memory (ASR models can use 1-2GB)
docker stats whisper-asr

# Check API memory
docker stats signalradio-api

# Restart service to free memory
docker-compose restart whisper-asr
```

### Common Problems & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| No recordings captured | Wrong control channel frequency | Update `trunk-recorder.json` control_channels with your system's frequency |
| Recordings captured but not uploaded | Upload callback failing | Check script permissions: `chmod +x scripts/upload_callback.sh` |
| Permission denied errors | Container running as wrong user | Ensure /app/scripts/ is readable by container user |
| Database already exists error | Race condition on startup | Safe to ignore; migrations will complete on next restart |
| Transcriptions not running | ASR service not configured | Set `ASR_ENABLED=true` and verify Whisper service is running |
| Summaries not generating | OpenAI not configured | Set `SEMANTIC_KERNEL_ENABLED=true` and add Azure OpenAI credentials |
| Web UI blank/loading forever | API CORS issues | Check browser console for 403/CORS errors; API endpoint must match request origin |

### Debug Mode

Enable detailed logging:

```bash
# Set API to Development mode
docker-compose down
docker-compose -f docker-compose.yml -e ASPNETCORE_ENVIRONMENT=Development up

# Enable verbose trunk-recorder logs
# Edit config/trunk-recorder.json and set "logLevel": "debug"

# View all API errors
docker-compose logs signalradio-api | grep -i "error\|exception\|warn"
```

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



## License

MIT License
