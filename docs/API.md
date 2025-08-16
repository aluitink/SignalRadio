# SignalRadio API Documentation

## Overview

The SignalRadio API provides REST endpoints for managing radio recordings with Azure Blob Storage integration. The API handles uploads from trunk-recorder and provides full CRUD operations for recordings.

## Base URL

- Development: `http://localhost:5210`
- Production: Configure as needed

## Authentication

Currently, no authentication is required. This may be added in future phases.

## Endpoints

### Health Check

#### GET `/health`

Check the health status of the API service.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2025-08-16T21:00:00Z"
}
```

### Upload Recording

#### POST `/api/recording/upload`

Upload a new radio recording. Supports both WAV and M4A files.

**Content-Type:** `multipart/form-data`

**Form Parameters:**
- `TalkgroupId` (required): The talkgroup identifier
- `Frequency` (required): Recording frequency (e.g., "851.0125")
- `Timestamp` (required): Recording timestamp in ISO 8601 format
- `SystemName` (required): Radio system name
- `audioFile` (optional): WAV audio file
- `m4aFile` (optional): M4A audio file

**Note:** At least one audio file (audioFile or m4aFile) must be provided.

**Example Request:**
```bash
curl -X POST http://localhost:5210/api/recording/upload \
  -F "TalkgroupId=12345" \
  -F "Frequency=851.0125" \
  -F "Timestamp=2025-08-16T21:00:00Z" \
  -F "SystemName=DanecomSystem" \
  -F "audioFile=@recording.wav" \
  -F "m4aFile=@recording.m4a"
```

**Success Response (200):**
```json
{
  "message": "Upload successful",
  "wavResult": {
    "isSuccess": true,
    "blobUri": "https://storage.blob.core.windows.net/recordings/DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.wav",
    "blobName": "DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.wav",
    "uploadedBytes": 1048576
  },
  "m4aResult": {
    "isSuccess": true,
    "blobUri": "https://storage.blob.core.windows.net/recordings/DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.m4a",
    "blobName": "DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.m4a",
    "uploadedBytes": 524288
  }
}
```

**Error Response (400):**
```json
{
  "error": "No audio file provided"
}
```

### List Recordings

#### GET `/api/recording/list`

Retrieve a list of recordings with optional filtering.

**Query Parameters:**
- `systemName` (optional): Filter by system name
- `talkgroupId` (optional): Filter by talkgroup ID
- `fromDate` (optional): Filter recordings from this date (ISO 8601)
- `toDate` (optional): Filter recordings to this date (ISO 8601)

**Example Request:**
```bash
curl "http://localhost:5210/api/recording/list?systemName=DanecomSystem&talkgroupId=12345&fromDate=2025-08-16T00:00:00Z"
```

**Success Response (200):**
```json
[
  {
    "talkgroupId": "12345",
    "systemName": "DanecomSystem",
    "recordingTime": "2025-08-16T21:00:00Z",
    "frequency": "851.0125",
    "fileName": "20250816-210000-851.0125Hz.wav",
    "originalFormat": "wav",
    "originalSize": 1048576,
    "blobName": "DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.wav",
    "blobUri": "https://storage.blob.core.windows.net/recordings/DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.wav"
  }
]
```

### Download Recording

#### GET `/api/recording/download/{blobName}`

Download a recording file by its blob name.

**Path Parameters:**
- `blobName`: The full blob path (URL-encoded if necessary)

**Example Request:**
```bash
curl "http://localhost:5210/api/recording/download/DanecomSystem%2F12345%2F2025%2F08%2F16%2F20250816-210000-851.0125Hz.wav" \
  -o recording.wav
```

**Success Response (200):**
- Content-Type: `audio/wav` or `audio/mp4` (based on file type)
- Binary audio data

**Error Response (404):**
```json
{
  "error": "Recording not found"
}
```

### Delete Recording

#### DELETE `/api/recording/delete/{blobName}`

Delete a recording by its blob name.

**Path Parameters:**
- `blobName`: The full blob path (URL-encoded if necessary)

**Example Request:**
```bash
curl -X DELETE "http://localhost:5210/api/recording/delete/DanecomSystem%2F12345%2F2025%2F08%2F16%2F20250816-210000-851.0125Hz.wav"
```

**Success Response (200):**
```json
{
  "message": "Recording deleted successfully"
}
```

**Error Response (404):**
```json
{
  "error": "Recording not found"
}
```

## Data Models

### RecordingMetadata

```json
{
  "talkgroupId": "string",
  "systemName": "string", 
  "recordingTime": "datetime (ISO 8601)",
  "frequency": "string",
  "fileName": "string",
  "originalFormat": "string",
  "originalSize": "number",
  "blobName": "string",
  "blobUri": "string"
}
```

### StorageResult

```json
{
  "isSuccess": "boolean",
  "blobUri": "string",
  "blobName": "string",
  "errorMessage": "string (optional)",
  "uploadedBytes": "number"
}
```

## Error Handling

The API uses standard HTTP status codes:

- `200` - Success
- `400` - Bad Request (invalid parameters)
- `404` - Not Found (recording doesn't exist)
- `500` - Internal Server Error

Error responses include a JSON object with an `error` field describing the issue.

## File Organization

Recordings are stored using the pattern:
```
{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}/{Timestamp}-{Frequency}Hz.{extension}
```

Example:
```
DanecomSystem/12345/2025/08/16/20250816-210000-851.0125Hz.wav
```

## Configuration

See the main README.md for Azure Storage configuration details.

## Development Testing

Use the sample files in the `test-files/` directory for testing:

```bash
# Test WAV upload
curl -X POST http://localhost:5210/api/recording/upload \
  -F "TalkgroupId=12345" \
  -F "Frequency=851.0125" \
  -F "Timestamp=2025-08-16T21:00:00Z" \
  -F "SystemName=TestSystem" \
  -F "audioFile=@test-files/sample.wav"

# Test M4A upload  
curl -X POST http://localhost:5210/api/recording/upload \
  -F "TalkgroupId=67890" \
  -F "Frequency=852.2375" \
  -F "Timestamp=2025-08-16T21:05:00Z" \
  -F "SystemName=TestSystem" \
  -F "m4aFile=@test-files/sample.m4a"
```
