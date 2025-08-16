#!/bin/bash

# SignalRadio UI Development Server

echo "🎙️ Starting SignalRadio UI Development Server..."
echo ""

# Check if Python 3 is available
if command -v python3 &> /dev/null; then
    echo "✅ Python 3 found"
    echo "🌐 Starting server on http://localhost:3000"
    echo "📡 API should be running at https://localhost:7080"
    echo ""
    echo "Press Ctrl+C to stop the server"
    echo ""
    
    cd "$(dirname "$0")"
    python3 -m http.server 3000
else
    echo "❌ Python 3 not found"
    echo ""
    echo "Please install Python 3 or use Node.js:"
    echo "  npm install -g http-server"
    echo "  npx http-server -p 3000 -c-1 --cors"
    exit 1
fi
