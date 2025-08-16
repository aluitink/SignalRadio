#!/bin/bash

# SignalRadio Setup Script
# This script helps users get started quickly with SignalRadio

set -e

echo "🎵 SignalRadio Setup Script"
echo "=================================="

# Check if Docker is available
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker and Docker Compose first."
    echo "   Visit: https://docs.docker.com/get-docker/"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    echo "   Visit: https://docs.docker.com/compose/install/"
    exit 1
fi

echo "✅ Docker and Docker Compose are available"

# Check if configuration file exists
if [ ! -f "config/trunk-recorder.json" ]; then
    echo "📋 Creating trunk-recorder configuration from template..."
    cp config/trunk-recorder-template.json config/trunk-recorder.json
    echo "✅ Configuration file created: config/trunk-recorder.json"
    echo ""
    echo "⚠️  IMPORTANT: You need to edit config/trunk-recorder.json with your local settings:"
    echo "   - control_channels: Your system's control channel frequencies"
    echo "   - center: SDR center frequency for your coverage area"
    echo "   - gain: Adjust based on your antenna and location"
    echo "   - shortName: A name for your system"
    echo ""
    echo "   Use Radio Reference (https://www.radioreference.com) to find your system info"
    echo ""
    read -p "Press Enter to continue once you've configured trunk-recorder.json..."
else
    echo "✅ Configuration file already exists: config/trunk-recorder.json"
fi

# Check for .env file
if [ ! -f ".env" ]; then
    echo "🔧 Creating .env file for Azure Storage (optional)..."
    cp .env.example .env
    echo "✅ Created .env file from template (Azure Storage is optional for testing)"
    echo "   Edit .env to add your Azure Storage connection string for production"
fi

# Make scripts executable
chmod +x scripts/*.sh

echo ""
echo "🚀 Setup complete! Starting SignalRadio..."
echo ""

# Start the services
if docker-compose up --build -d; then
    echo ""
    echo "✅ SignalRadio is starting up!"
    echo ""
    echo "📊 Monitor the startup:"
    echo "   docker-compose logs -f"
    echo ""
    echo "🔍 Check service status:"
    echo "   docker-compose ps"
    echo ""
    echo "🏥 API Health Check:"
    echo "   http://localhost:5000/health"
    echo ""
    echo "🛑 To stop the services:"
    echo "   docker-compose down"
    echo ""
    echo "📚 For troubleshooting, see the README.md file"
else
    echo ""
    echo "❌ Failed to start Docker services."
    echo "   This is often due to Docker permission issues."
    echo ""
    echo "💡 Try running:"
    echo "   sudo docker-compose up --build -d"
    echo ""
    echo "   Or add your user to the docker group:"
    echo "   sudo usermod -aG docker $USER"
    echo "   newgrp docker"
    echo ""
    echo "📚 For more help, see the README.md file"
fi
