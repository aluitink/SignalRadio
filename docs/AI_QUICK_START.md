# SignalRadio AI Configuration Quick Reference

## Enable AI Transcript Summarization

### Step 1: Environment Setup
```bash
# Copy template and edit
cp .env.sample .env
nano .env
```

### Step 2: Required Variables
```bash
# Enable the feature
SEMANTIC_KERNEL_ENABLED=true

# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-openai-resource.openai.azure.com/
AZURE_OPENAI_KEY=your-api-key-here
AZURE_OPENAI_DEPLOYMENT=gpt-4  # or your deployment name
```

### Step 3: Restart Services
```bash
docker-compose up -d
```

### Step 4: Test
1. Navigate to any TalkGroup page in the web UI
2. Look for the "AI Summary" section
3. Click "Generate" to create a summary
4. Try different time windows (15min - 24hr)

## Optional Tuning Parameters
```bash
SEMANTIC_KERNEL_MAX_TOKENS=1500      # Response length limit
SEMANTIC_KERNEL_TEMPERATURE=0.3       # AI creativity (0.0-1.0)
SEMANTIC_KERNEL_CACHE_DURATION=30     # Cache minutes
SEMANTIC_KERNEL_DEFAULT_WINDOW=60     # Default time window
```

## Azure OpenAI Resource Setup
1. Go to portal.azure.com
2. Create → Azure OpenAI Service
3. Deploy a model (gpt-4 or gpt-35-turbo)
4. Keys and Endpoint → Copy endpoint and key
5. Add to your .env file

## Troubleshooting
- Service status: `GET /api/transcriptsummary/status`
- Check logs: `docker-compose logs api`
- Verify transcripts exist for the talkgroup
- Ensure Azure OpenAI deployment name matches exactly
