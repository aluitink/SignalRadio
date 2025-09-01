#!/bin/bash

# SignalRadio Upload Callback Script for trunk-recorder
# This script is called by trunk-recorder when a recording is complete

# Input parameters from trunk-recorder
AUDIO_FILE="$1"
JSON_FILE="$2"
M4A_FILE="$3"

# Configuration
# POST a RecordingUploadRequest (multipart form) to the API upload action. Server will fill StorageLocation.
# The controller expects a multipart/form-data POST to /api/recordings/upload with fields:
# - file: the audio file
# - metadata: JSON string matching RecordingUploadRequest
API_ENDPOINT="${API_ENDPOINT:-http://signalradio-api:8080/api/recordings/upload}"
# Ensure we post the multipart/form-data to the upload route. If API_ENDPOINT was set without '/upload', append it.
UPLOAD_ENDPOINT="$API_ENDPOINT"
case "$UPLOAD_ENDPOINT" in
    */upload) ;; # already ends with /upload
    */) UPLOAD_ENDPOINT="${UPLOAD_ENDPOINT%/}/upload" ;;
    *) UPLOAD_ENDPOINT="${UPLOAD_ENDPOINT}/upload" ;;
esac
LOG_FILE="/app/logs/upload.log"

# Create log directory if it doesn't exist
mkdir -p "$(dirname "$LOG_FILE")"

# Log function
log() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log "Upload callback triggered for: $AUDIO_FILE"
log "JSON metadata file: $JSON_FILE"
if [ -n "$M4A_FILE" ] && [ -f "$M4A_FILE" ]; then
    log "M4A file available: $M4A_FILE (will NOT be uploaded)"
else
    log "M4A file not available or not found"
fi

# Extract metadata from JSON file
if [ ! -f "$JSON_FILE" ]; then
    log "ERROR: JSON metadata file not found: $JSON_FILE"
    exit 1
fi

# Parse JSON metadata using simple grep/sed (since jq might not be available)
TALKGROUP=$(grep -o '"talkgroup":[[:space:]]*[0-9]*' "$JSON_FILE" | sed 's/.*://g' | tr -d ' ')
FREQUENCY=$(grep -o '"freq":[[:space:]]*[0-9.]*' "$JSON_FILE" | head -1 | sed 's/.*://g' | tr -d ' ')
START_TIME=$(grep -o '"start_time":[[:space:]]*[0-9]*' "$JSON_FILE" | sed 's/.*://g' | tr -d ' ')
STOP_TIME=$(grep -o '"stop_time":[[:space:]]*[0-9]*' "$JSON_FILE" | sed 's/.*://g' | tr -d ' ')
CALL_LENGTH=$(grep -o '"call_length":[[:space:]]*[0-9.]*' "$JSON_FILE" | sed 's/.*://g' | tr -d ' ')
SYSTEM_NAME=$(grep -o '"short_name":[[:space:]]*"[^"]*"' "$JSON_FILE" | sed 's/.*"//g' | sed 's/".*//g')

# Convert Unix timestamp to ISO 8601 format
if [ -n "$START_TIME" ] && [ "$START_TIME" != "" ]; then
    TIMESTAMP=$(date -d "@$START_TIME" -Iseconds 2>/dev/null || echo "")
else
    TIMESTAMP=""
fi

# Fallback values if parsing failed
TALKGROUP="${TALKGROUP:-unknown}"
FREQUENCY="${FREQUENCY:-0}"
SYSTEM_NAME="${SYSTEM_NAME:-DaneCom}"
TIMESTAMP="${TIMESTAMP:-$(date -Iseconds)}"
CALL_LENGTH="${CALL_LENGTH:-}"
STOP_TIME="${STOP_TIME:-}"

log "Parsed metadata - Talkgroup: $TALKGROUP, Frequency: $FREQUENCY, System: $SYSTEM_NAME, Timestamp: $TIMESTAMP, Duration: ${CALL_LENGTH}s, Stop: $STOP_TIME"

# Check if audio file exists
if [ ! -f "$AUDIO_FILE" ]; then
    log "ERROR: Audio file not found: $AUDIO_FILE"
    exit 1
fi

# Get file size for logging
FILE_SIZE=$(stat -c%s "$AUDIO_FILE" 2>/dev/null || echo "unknown")
log "WAV file size: $FILE_SIZE bytes"

# Check M4A file if provided
M4A_SIZE="0"
if [ -n "$M4A_FILE" ] && [ -f "$M4A_FILE" ]; then
    M4A_SIZE=$(stat -c%s "$M4A_FILE" 2>/dev/null || echo "0")
    log "M4A file size: $M4A_SIZE bytes"
fi

# Upload to SignalRadio API (JSON)
log "Posting Recording JSON to: $API_ENDPOINT"

# Normalize numeric fields
if [[ "$TALKGROUP" =~ ^[0-9]+$ ]]; then
    TG_NUM=$TALKGROUP
else
    TG_NUM=0
fi

if [[ -z "$FREQUENCY" ]]; then
    FREQUENCY=0
fi

# Duration in seconds (int)
if [[ -n "$CALL_LENGTH" ]]; then
    # round to nearest int
    DURATION_SECONDS=$(printf "%.0f" "$CALL_LENGTH" 2>/dev/null || echo 0)
else
    DURATION_SECONDS=0
fi

# Determine recording time and receivedAt
if [ -n "$TIMESTAMP" ] && [ "$TIMESTAMP" != "" ]; then
    RECORDING_TIME="$TIMESTAMP"
    RECEIVED_AT="$TIMESTAMP"
else
    RECORDING_TIME=$(date -Iseconds)
    RECEIVED_AT="$RECORDING_TIME"
fi

NOW=$(date -Iseconds)

# We will upload ONLY the WAV audio file. Do not send metadata in the multipart form.
# Confirm the provided audio file is a WAV file (basic check by extension).
if [[ "${AUDIO_FILE,,}" != *.wav ]]; then
    log "ERROR: Upload callback is configured to send only WAV files. Provided file is not .wav: $AUDIO_FILE"
    exit 1
fi

# Determine mime type for the audio file if possible
MIME_TYPE=$(file --brief --mime-type "$AUDIO_FILE" 2>/dev/null || echo "audio/wav")

log "Building RecordingUploadRequest metadata"

# Build RecordingUploadRequest JSON
METADATA_JSON=$(cat <<JSON
{
    "TalkgroupId": "${TALKGROUP}",
    "Frequency": "${FREQUENCY}",
    "Timestamp": "${TIMESTAMP}",
    "SystemName": "${SYSTEM_NAME}",
    "Duration": ${CALL_LENGTH:-null},
    "StopTime": ${STOP_TIME:-null}
}
JSON
)

log "Metadata JSON: $METADATA_JSON"

# Create a temp file for metadata to reliably send as a form field
TMP_META=$(mktemp /tmp/recording_meta.XXXXXX.json)
echo "$METADATA_JSON" > "$TMP_META"

log "Posting multipart/form-data (file + metadata) to: $UPLOAD_ENDPOINT"

# Perform multipart upload: send audio as file and metadata as a string form field
RESPONSE=$(curl -s -w "HTTPSTATUS:%{http_code}" -X POST "$UPLOAD_ENDPOINT" \
    -F "file=@${AUDIO_FILE};type=${MIME_TYPE}" \
    -F "metadata=$(<${TMP_META})" 2>&1 || true)

# Clean up temp metadata file
rm -f "$TMP_META"

# Extract HTTP status and body
HTTP_STATUS=$(echo "$RESPONSE" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
BODY=$(echo "$RESPONSE" | sed -e 's/HTTPSTATUS:.*//g')

if [ "$HTTP_STATUS" -eq 200 ] || [ "$HTTP_STATUS" -eq 201 ]; then
        log "SUCCESS: Upload completed successfully"
        log "Response: $BODY"
else
        log "ERROR: Upload failed with status $HTTP_STATUS"
        log "Response: $BODY"
        exit 1
fi

log "Upload callback completed for: $AUDIO_FILE"
