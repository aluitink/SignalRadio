# Phase 3: Azure Storage Integration

## Overview
Phase 3 adds Azure Blob Storage integration to the SignalRadio project, enabling persistent storage of audio recordings with comprehensive metadata management.

## Features Added

### Azure Blob Storage Service
- **AzureBlobStorageService**: Complete implementation of `IStorageService` interface
- **Automatic container creation**: Creates storage container if it doesn't exist
- **Metadata preservation**: Stores recording metadata as blob metadata
- **File organization**: Hierarchical folder structure using configurable path patterns

### Storage Configuration
- **Connection string support**: Configurable Azure Storage connection
- **Development storage**: Uses Azure Storage Emulator for local development
- **Container naming**: Separate containers for production and development
- **Path patterns**: Configurable file organization using template variables

### API Enhancements
- **Upload with storage**: Records are now stored in Azure Blob Storage during upload
- **List recordings**: GET `/api/recording/list` with filtering support
- **Download recordings**: GET `/api/recording/download/{blobName}` for file retrieval
- **Delete recordings**: DELETE `/api/recording/delete/{blobName}` for file removal
- **Enhanced error handling**: Proper error responses for storage operations

### File Organization
Default path pattern: `{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}`

Example blob path:
```
DanecomSystem/12345/2024/01/15/20240115-143000-851.0125Hz.wav
```

## Configuration

### appsettings.json
```json
{
  "AzureStorage": {
    "ConnectionString": "",
    "ContainerName": "recordings",
    "DefaultPathPattern": "{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}"
  }
}
```

### appsettings.Development.json
```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "recordings-dev",
    "DefaultPathPattern": "{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}"
  }
}
```

## API Endpoints

### Upload Recording
- **POST** `/api/recording/upload`
- Supports both WAV and M4A files
- Stores files in Azure Blob Storage
- Returns blob URIs and names

### List Recordings
- **GET** `/api/recording/list`
- Query parameters:
  - `systemName`: Filter by system name
  - `talkgroupId`: Filter by talkgroup ID
  - `fromDate`: Filter recordings from date
  - `toDate`: Filter recordings until date

### Download Recording
- **GET** `/api/recording/download/{blobName}`
- Downloads recording from Azure Blob Storage
- Returns appropriate content type based on file extension

### Delete Recording
- **DELETE** `/api/recording/delete/{blobName}`
- Removes recording from Azure Blob Storage
- Returns success/failure status

## Storage Features

### Metadata Storage
Each blob includes comprehensive metadata:
- TalkgroupId
- SystemName  
- RecordingTime
- Frequency
- OriginalFileName
- OriginalFormat
- OriginalSize
- UploadedAt

### Path Sanitization
- Invalid characters are replaced with hyphens
- Ensures blob names are valid for Azure Storage
- Maintains readable file organization

### Error Handling
- Graceful handling of storage failures
- Partial upload success support
- Comprehensive logging for troubleshooting

## Dependencies Added
- **Azure.Storage.Blobs**: Azure Blob Storage client library
- **Microsoft.Extensions.Options.ConfigurationExtensions**: Configuration binding
- **Microsoft.Extensions.Logging.Abstractions**: Logging support

## Testing
- Sample test files created in `/test-files/`
- HTTP file updated with test endpoints
- Development storage emulator support

## Next Steps (Phase 4)
- Background processing queues
- Monitoring and alerting
- Advanced audio analysis
- Performance optimization
