#!/bin/bash

FILENAME="$1"
BASENAME="${FILENAME%.*}"
JSON="${BASENAME}.json"

sudo -Eu liquidsoap /app/liquid-bridge/app/SignalRadio.LiquidBridge config:/app/liquid-bridge/config/config.json mode:client wav:${FILENAME} json:${JSON} >&1