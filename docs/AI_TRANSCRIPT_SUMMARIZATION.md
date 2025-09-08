# AI Transcript Summarization Feature

## Overview

This feature adds AI-powered transcript summarization to SignalRadio using Microsoft Semantic Kernel and Azure OpenAI. It analyzes radio communication transcripts over configurable time windows and provides intelligent summaries, key topics, and notable incidents.

## Components Added

### Backend (C#/.NET 9)

1. **Configuration Models**
   - `SemanticKernelOptions.cs` - Configuration for Azure OpenAI settings
   - `TranscriptSummaryModels.cs` - Request/response models for summary operations

2. **Service Interface & Implementation**
   - `ITranscriptSummaryService.cs` - Service contract
   - `SemanticKernelTranscriptSummaryService.cs` - Azure OpenAI integration

3. **API Controllers**
   - `TranscriptSummaryController.cs` - Dedicated summary endpoints
   - Extended `TalkGroupsController.cs` - Added `/summary` endpoint

4. **Database Extensions**
   - Extended `ITranscriptionsService` with time range queries
   - Added `GetByTalkGroupAndTimeRangeAsync` method

### Frontend (React/TypeScript)

1. **Types**
   - Added `TranscriptSummaryDto` to DTOs

2. **Components**
   - `TranscriptSummary.tsx` - React component with configurable time windows

3. **Integration**
   - Integrated summary component into `TalkGroupPage.tsx`

## Configuration

Add to your `appsettings.json`:

```json
{
  "SemanticKernel": {
    "Enabled": false,
    "AzureOpenAIEndpoint": "https://your-openai-resource.openai.azure.com/",
    "AzureOpenAIKey": "your-api-key-here",
    "ChatDeploymentName": "gpt-4",
    "MaxTokens": 1500,
    "Temperature": 0.3,
    "CacheDurationMinutes": 30,
    "DefaultTimeWindowMinutes": 60
  }
}
```

### Environment Variables (Optional)

You can also configure via environment variables:
- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_KEY`

## API Endpoints

### Check Service Status
```
GET /api/transcriptsummary/status
```

### Generate Summary for TalkGroup (Last Hour)
```
GET /api/talkgroups/{id}/summary?windowMinutes=60&forceRefresh=false
```

### Generate Custom Summary
```
POST /api/transcriptsummary/custom
Content-Type: application/json

{
  "talkGroupId": 13001,
  "startTime": "2025-09-06T10:00:00Z",
  "endTime": "2025-09-06T11:00:00Z",
  "forceRefresh": false
}
```

### Clear Cache
```
DELETE /api/transcriptsummary/cache?talkGroupId=13001
```

## Features

### Time Windows
- Configurable time windows (15 minutes to 24 hours)
- Default 1-hour analysis window
- Custom date/time range support

### Caching
- 30-minute cache duration (configurable)
- Force refresh option
- Per-talkgroup cache clearing

### AI Analysis
- Comprehensive activity summaries
- Key topic extraction
- Notable incident identification
- Context-aware radio communications analysis

### UI Features
- Service availability detection
- Real-time loading states
- Expandable summary text
- Topic tags and incident lists
- Mobile-responsive design

## Security & Privacy

- All AI processing happens via Azure OpenAI (GDPR compliant)
- No transcript data is stored by OpenAI
- Configurable token limits and temperature settings
- Service can be completely disabled via configuration

## Testing

1. **Enable the service** by setting `"Enabled": true` in configuration
2. **Configure Azure OpenAI** with valid endpoint and API key
3. **Ensure transcripts exist** for the test talkgroup
4. **Visit a TalkGroup page** in the UI to see the summary component
5. **Generate a summary** using different time windows

## Performance Considerations

- Summaries are cached to reduce API calls
- Large time windows may take longer to process
- Token limits prevent excessive API usage
- Database queries are optimized for time ranges

## Future Enhancements

- Multi-talkgroup summaries
- Trending analysis across time periods
- Alert generation for unusual activity patterns
- Export summaries to reports
- Integration with notification systems
