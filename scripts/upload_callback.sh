#!/bin/bash

# SignalRadio Upload Callback Script for trunk-recorder
# This script is called by trunk-recorder when a recording is complete

# Input parameters from trunk-recorder
AUDIO_FILE="$1"
JSON_FILE="$2"
M4A_FILE="$3"

# Configuration
API_ENDPOINT="${API_ENDPOINT:-http://signalradio-api:8080/api/recording/upload}"
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
    log "M4A file available: $M4A_FILE"
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
FREQUENCY=$(grep -o '"freq":[[:space:]]*[0-9.]*' "$JSON_FILE" | sed 's/.*://g' | tr -d ' ')
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

# Upload to SignalRadio API
log "Uploading to: $API_ENDPOINT"

# Build curl command with available files
CURL_ARGS=(
    -s -w "HTTPSTATUS:%{http_code}"
    -X POST "$API_ENDPOINT"
    -F "talkgroupId=${TALKGROUP}"
    -F "frequency=${FREQUENCY}"
    -F "timestamp=${TIMESTAMP}"
    -F "systemName=${SYSTEM_NAME}"
    -F "audioFile=@${AUDIO_FILE}"
)

# Add duration if available
if [ -n "$CALL_LENGTH" ] && [ "$CALL_LENGTH" != "" ]; then
    CURL_ARGS+=(-F "duration=${CALL_LENGTH}")
    log "Including call duration: ${CALL_LENGTH} seconds"
fi

# Add stop time if available
if [ -n "$STOP_TIME" ] && [ "$STOP_TIME" != "" ]; then
    CURL_ARGS+=(-F "stopTime=${STOP_TIME}")
    log "Including stop time: ${STOP_TIME}"
fi

# Add M4A file if available
if [ -n "$M4A_FILE" ] && [ -f "$M4A_FILE" ]; then
    CURL_ARGS+=(-F "m4aFile=@${M4A_FILE}")
    log "Including both WAV and M4A files in upload"
else
    log "Uploading WAV file only"
fi

RESPONSE=$(curl "${CURL_ARGS[@]}" 2>&1)

# Extract HTTP status and body
HTTP_STATUS=$(echo "$RESPONSE" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
BODY=$(echo "$RESPONSE" | sed -e 's/HTTPSTATUS:.*//g')

if [ "$HTTP_STATUS" -eq 200 ]; then
    log "SUCCESS: Upload completed successfully"
    log "Response: $BODY"
else
    log "ERROR: Upload failed with status $HTTP_STATUS"
    log "Response: $BODY"
    exit 1
fi

log "Upload callback completed for: $AUDIO_FILE"
