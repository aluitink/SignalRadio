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
echo "� Building and starting services..."
echo ""

# Build the services first
if ! docker-compose build; then
    echo "❌ Failed to build Docker services."
    exit 1
fi

# Start database service first
echo "🗄️  Starting database service..."
if ! docker-compose up -d sqlserver; then
    echo "❌ Failed to start database service."
    exit 1
fi

# Wait for SQL Server to be ready
echo "⏳ Waiting for SQL Server to initialize (30 seconds)..."
sleep 30

# Check if dotnet EF tools are available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Database initialization skipped."
    echo "   Please install .NET SDK to enable database setup."
    echo "   Visit: https://dotnet.microsoft.com/download"
else
    echo "🗃️  Initializing database..."
    
    # Navigate to API project
    cd src/SignalRadio.Api
    
    # Install EF tools if not already installed
    if ! dotnet tool list -g | grep -q dotnet-ef; then
        echo "📦 Installing Entity Framework tools..."
        dotnet tool install --global dotnet-ef
    fi
    
    # Apply database migrations
    echo "🔄 Applying database migrations..."
    if dotnet ef database update; then
        echo "✅ Database initialized successfully!"
    else
        echo "⚠️  Database migration failed, but continuing with service startup..."
        echo "   You may need to run 'dotnet ef database update' manually later."
    fi
    
    # Return to root directory
    cd ../..
fi

echo ""
echo "🚀 Starting all services..."
echo ""

# Start the services
if docker-compose up --build -d; then
    echo ""
    echo "✅ SignalRadio is running!"
    echo "✅ Database has been initialized with migrations"
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
    echo "🗄️  Database Management:"
    echo "   cd src/SignalRadio.Api && dotnet ef migrations list"
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
