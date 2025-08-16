# SignalRadio Database Setup

## Overview

SignalRadio now includes SQL Server database support for tracking calls and recordings. The system uses Entity Framework Core for data access and stores metadata about each call and its associated recordings.

## Database Schema

### Tables

#### Calls
- `Id` (Primary Key)
- `TalkgroupId` - Radio talkgroup identifier
- `SystemName` - Radio system name
- `RecordingTime` - When the call was recorded
- `Frequency` - Radio frequency
- `Duration` - Call duration (optional)
- `CreatedAt` - Record creation timestamp
- `UpdatedAt` - Record last update timestamp

#### Recordings
- `Id` (Primary Key)
- `CallId` (Foreign Key to Calls)
- `FileName` - Original filename
- `Format` - Audio format (WAV, M4A, etc.)
- `ContentType` - MIME type
- `FileSize` - File size in bytes
- `BlobUri` - Azure Storage blob URI
- `BlobName` - Azure Storage blob name
- `IsUploaded` - Upload status flag
- `UploadedAt` - Upload completion timestamp
- `CreatedAt` - Record creation timestamp
- `UpdatedAt` - Record last update timestamp

## API Endpoints

### Recording Management
- `POST /api/recording/upload` - Upload audio files (now stores in database)
- `GET /api/recording/list` - List recordings
- `GET /api/recording/download/{blobName}` - Download recording
- `DELETE /api/recording/delete/{blobName}` - Delete recording
- `GET /api/recording/health` - Service health check

### Call Management (New)
- `GET /api/calls` - Get recent calls
- `GET /api/calls/{id}` - Get specific call by ID
- `GET /api/calls/talkgroup/{talkgroupId}` - Get calls by talkgroup
- `GET /api/calls/system/{systemName}` - Get calls by system
- `GET /api/calls/stats` - Get call statistics

## Configuration

### Docker Compose
The system now includes a SQL Server container:

```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    - ACCEPT_EULA=Y
    - SA_PASSWORD=SignalRadio123!
    - MSSQL_PID=Express
  ports:
    - "1433:1433"
```

### Connection String
The API is configured with the following connection string:
```
Server=sqlserver,1433;Database=SignalRadio;User Id=sa;Password=SignalRadio123!;TrustServerCertificate=true;
```

## Development Setup

1. **Start the services:**
   ```bash
   docker-compose up -d
   ```

2. **Database migrations are applied automatically** when the API starts up.

3. **For local development** (without Docker), update the connection string in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=SignalRadio;User Id=sa;Password=SignalRadio123!;TrustServerCertificate=true;"
     }
   }
   ```

## Features

### Call Deduplication
The system automatically handles duplicate uploads for the same call. If multiple recordings (e.g., WAV and M4A) are uploaded for the same talkgroup, system, and timestamp, they are associated with the same call record.

### Recording Tracking
Each audio file upload creates a recording record linked to a call. The system tracks:
- Upload status
- Blob storage information
- File metadata
- Format information

### Statistics and Reporting
The `/api/calls/stats` endpoint provides:
- Total call counts
- Recording counts by format
- System and talkgroup breakdowns
- Most active talkgroups

## Database Operations

### Manual Migration Commands
If you need to create migrations manually:

```bash
cd src/SignalRadio.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Backup and Restore
The SQL Server data is persisted in a Docker volume (`sqlserver-data`). To backup:

```bash
docker exec signalradio-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'SignalRadio123!' -Q "BACKUP DATABASE [SignalRadio] TO DISK = N'/var/opt/mssql/backup/SignalRadio.bak'"
```

## Monitoring

### Health Checks
- API Health: `GET /api/recording/health`
- Database connection is verified during API startup
- Failed database migrations are logged

### Logging
The system logs:
- Call creation and updates
- Recording uploads and associations
- Database operation status
- Error conditions

## Security Notes

- The default SA password should be changed in production
- Consider using Azure SQL Database for production deployments
- Implement proper backup strategies
- Monitor database growth and implement retention policies
