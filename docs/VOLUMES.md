# Docker Volume Strategy

## Overview
The SignalRadio project uses a hybrid approach for Docker volumes to separate configuration from container images while maintaining data persistence.

## Volume Types

### 1. Bind Mounts (Configuration & Scripts)
These map host directories to container paths, allowing easy configuration updates without rebuilding images:

- `./config` → `/app/config` (read-only, trunk-recorder)
  - Contains trunk-recorder JSON configurations
  - Contains talkgroup CSV files
  - Mounted as read-only for security

- `./scripts` → `/app/scripts` (read-only, trunk-recorder)
  - Contains upload callback scripts
  - Contains utility scripts
  - Mounted as read-only for security

- `./config` → `/app/config` (read-only, SignalRadio API)
  - Contains API configuration files if needed
  - Allows runtime configuration changes

### 2. Named Volumes (Data Persistence)
These provide persistent storage managed by Docker:

- `recordings` → `/app/audio` (trunk-recorder)
  - Stores recorded audio files
  - Persists across container restarts
  - Managed by Docker for optimal performance

- `temp` → `/app/temp` (SignalRadio API)
  - Temporary processing files
  - Shared between services if needed
  - Cleaned up by application logic

## Benefits

### ✅ Configuration Flexibility
- Update configs without rebuilding images
- Environment-specific configurations
- Version control for configurations
- Easy backup and restore

### ✅ Data Persistence
- Audio recordings survive container restarts
- Proper data isolation
- Docker-managed optimization
- Cross-platform compatibility

### ✅ Security
- Read-only mounts for configurations
- Separated concerns (config vs data)
- No sensitive data in images

## File Structure
```
SignalRadio/
├── config/
│   ├── trunk-recorder-template.json    # Template configuration
│   ├── trunk-recorder-danecom.json     # DaneCom test configuration
│   └── danecom-talkgroups.csv         # Talkgroup definitions
├── scripts/
│   ├── upload_callback.sh              # Trunk-recorder callback
│   └── test_phase1.sh                  # Testing script
└── docker-compose.yml                  # Volume definitions
```

## Usage

### Starting Services
```bash
docker-compose up -d
```

### Updating Configuration
1. Edit files in `./config/` directory
2. Restart affected services:
```bash
docker-compose restart trunk-recorder
```

### Accessing Data
```bash
# List recordings
docker run --rm -v signalradio_recordings:/data alpine ls -la /data

# Backup recordings
docker run --rm -v signalradio_recordings:/data -v $(pwd):/backup alpine tar czf /backup/recordings-$(date +%Y%m%d).tar.gz /data
```

### Development Workflow
1. Modify configurations in `./config/`
2. Update scripts in `./scripts/`
3. Test with: `docker-compose restart trunk-recorder`
4. No image rebuilding required for config changes

## Path References

### In trunk-recorder config files:
- `"talkgroupsFile": "/app/config/danecom-talkgroups.csv"`
- `"uploadScript": "/app/scripts/upload_callback.sh"`
- `"captureDir": "/app/audio"`

### In upload callback scripts:
- Configuration files: `/app/config/`
- Audio files: `/app/audio/`
- API endpoint: `http://signalradio-api:8080/api/recording/upload`
