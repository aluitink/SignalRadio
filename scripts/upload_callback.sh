#!/bin/bash

# SignalRadio Upload Callback Script for trunk-recorder
# This script is called by trunk-recorder when a recording is complete

# Input parameters from trunk-recorder
AUDIO_FILE="$1"
TALKGROUP="$2"
FREQUENCY="$3"
TIMESTAMP="$4"
SYSTEM_NAME="$5"

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
log "Talkgroup: $TALKGROUP, Frequency: $FREQUENCY, System: $SYSTEM_NAME"

# Check if audio file exists
if [ ! -f "$AUDIO_FILE" ]; then
    log "ERROR: Audio file not found: $AUDIO_FILE"
    exit 1
fi

# Get file size for logging
FILE_SIZE=$(stat -c%s "$AUDIO_FILE" 2>/dev/null || echo "unknown")
log "File size: $FILE_SIZE bytes"

# Upload to SignalRadio API
log "Uploading to: $API_ENDPOINT"

RESPONSE=$(curl -s -w "HTTPSTATUS:%{http_code}" -X POST "$API_ENDPOINT" \
    -F "audioFile=@${AUDIO_FILE}" \
    -F "talkgroupId=${TALKGROUP}" \
    -F "frequency=${FREQUENCY}" \
    -F "timestamp=${TIMESTAMP}" \
    -F "systemName=${SYSTEM_NAME}" \
    2>&1)

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
