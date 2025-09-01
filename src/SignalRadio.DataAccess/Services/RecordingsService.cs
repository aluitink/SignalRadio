using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public class RecordingsService : IRecordingsService
{
    private readonly SignalRadioDbContext _db;
    private readonly IConfiguration _config;
    private readonly AzureStorageOptions _azureOptions;
    private readonly LocalStorageOptions _localOptions;
    private readonly SignalRadio.Core.Services.IStorageService? _storageService;
    private readonly SignalRadio.Core.Services.ICallNotifier? _callNotifier;

    public RecordingsService(
        SignalRadioDbContext db,
        IConfiguration config,
        IOptions<AzureStorageOptions> azureOptions,
        IOptions<LocalStorageOptions> localOptions,
        SignalRadio.Core.Services.IStorageService? storageService = null,
        SignalRadio.Core.Services.ICallNotifier? callNotifier = null)
    {
        _db = db;
        _config = config;
        _azureOptions = azureOptions?.Value ?? new AzureStorageOptions();
        _localOptions = localOptions?.Value ?? new LocalStorageOptions();
        _storageService = storageService;
        _callNotifier = callNotifier;
    }

    public async Task<PagedResult<Recording>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.Recordings.Include(r => r.Transcriptions).AsNoTracking().OrderByDescending(r => r.ReceivedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Recording>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<Recording?> GetByIdAsync(int id)
    {
        // Include Call navigation so callers (controllers/services) can access call/talkgroup info when needed
        return await _db.Recordings
            .Include(r => r.Transcriptions)
            .Include(r => r.Call)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Recording> CreateAsync(Recording model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // If the incoming model contains a nested Call with a nested TalkGroup.Number,
        // try to find an existing TalkGroup (by Number) and reuse it instead of inserting a new one.
        if (model?.Call?.TalkGroup != null)
        {
            var tgNumber = model.Call.TalkGroup.Number;
            if (tgNumber != 0)
            {
                var existing = await _db.TalkGroups.FirstOrDefaultAsync(tg => tg.Number == tgNumber);
                if (existing != null)
                {
                    // Associate existing TalkGroup by Id and clear the navigation property to avoid duplicate insert
                    model.Call.TalkGroupId = existing.Id;
                    model.Call.TalkGroup = null;
                }
            }
        }

        // Ensure StorageLocation is set based on configured storage type.
        // If caller already provided a StorageLocationId, leave it alone.
        if (model!.StorageLocationId == 0)
        {
            var storageType = _config["StorageType"] ?? _config["StorageType"] ?? "Azure";
            StorageKind kind;
            switch (storageType.Trim().ToLowerInvariant())
            {
                case "local":
                case "localfile":
                    kind = StorageKind.LocalFile;
                    break;
                case "azure":
                case "azureblob":
                    kind = StorageKind.AzureBlob;
                    break;
                case "s3":
                    kind = StorageKind.S3;
                    break;
                default:
                    kind = StorageKind.Other;
                    break;
            }

            // Try to find an existing StorageLocation for this kind
            var existingLoc = await _db.StorageLocations.FirstOrDefaultAsync(s => s.Kind == kind);
            if (existingLoc != null)
            {
                model.StorageLocationId = existingLoc.Id;
                model.StorageLocation = null;
            }
            else
            {
                // Determine a reasonable LocationUri from configuration when available
                string locationUri = string.Empty;
                if (kind == StorageKind.LocalFile)
                {
                    locationUri = _localOptions.BasePath ?? string.Empty;
                }
                else if (kind == StorageKind.AzureBlob)
                {
                    locationUri = _azureOptions.ContainerName ?? string.Empty;
                }
                else
                {
                    locationUri = _config.GetValue<string>("StorageLocationUri") ?? string.Empty;
                }

                var newLoc = new StorageLocation
                {
                    Kind = kind,
                    LocationUri = locationUri,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.StorageLocations.Add(newLoc);
                // Save now to obtain Id for the recording
                await _db.SaveChangesAsync();

                model.StorageLocationId = newLoc.Id;
                model.StorageLocation = null;
            }
        }

        _db.Recordings.Add(model);
        await _db.SaveChangesAsync();
        // Best-effort notify subscribers about the updated/created call.
        try
        {
            // If the recording is associated with a Call, notify by CallId
            if (_callNotifier != null)
            {
                if (model.CallId != 0)
                {
                    await _callNotifier.NotifyCallUpdatedAsync(model.CallId);
                }
                else if (model.Call != null && model.Call.Id != 0)
                {
                    await _callNotifier.NotifyCallUpdatedAsync(model.Call.Id);
                }
            }
        }
        catch { /* swallow - notification is best-effort */ }

        return model;
    }

    public async Task<Recording> CreateWithFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            SignalRadio.Core.Models.RecordingUploadRequest? uploadRequest = null,
            CancellationToken cancellationToken = default)
    {
        // Build Recording model from incoming metadata DTO
        var recording = new Recording
        {
            FileName = fileName,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = false,
            SizeBytes = fileStream.CanSeek ? fileStream.Length : 0
        };
        if (uploadRequest != null)
        {
            recording.Call = new Call
            {
                RecordingTime = uploadRequest.Timestamp.ToUniversalTime(),
                FrequencyHz = double.TryParse(uploadRequest.Frequency, out var f2) ? f2 : 0,
                DurationSeconds = uploadRequest.Duration.HasValue ? (int)Math.Round(uploadRequest.Duration.Value) : 0,
                CreatedAt = DateTimeOffset.UtcNow
            };

            if (int.TryParse(uploadRequest.TalkgroupId, out var tg2) && tg2 != 0)
            {
                // Set TalkGroup on the Call; CreateAsync will handle deduplication/reuse
                recording.Call.TalkGroup = new TalkGroup { Number = tg2, Name = uploadRequest.SystemName };
            }
        }

        // Upload file via storage service if available
        if (_storageService != null)
        {
            var recordingMeta = new SignalRadio.Core.Models.RecordingMetadata
            {
                TalkgroupId = uploadRequest?.TalkgroupId ?? string.Empty,
                SystemName = uploadRequest?.SystemName ?? string.Empty,
                RecordingTime = uploadRequest?.Timestamp ?? DateTime.UtcNow,
                Frequency = uploadRequest?.Frequency ?? string.Empty,
                FileName = fileName,
                OriginalFormat = Path.GetExtension(fileName),
                OriginalSize = fileStream.CanSeek ? fileStream.Length : 0
            };

            // Ensure FileName/OriginalSize are set on the metadata
            recordingMeta.FileName = recordingMeta.FileName == string.Empty ? fileName : recordingMeta.FileName;
            recordingMeta.OriginalSize = recordingMeta.OriginalSize == 0 ? (fileStream.CanSeek ? fileStream.Length : 0) : recordingMeta.OriginalSize;

            var storageResult = await _storageService.UploadRecordingAsync(fileStream, fileName, contentType ?? "application/octet-stream", recordingMeta);
            if (!storageResult.IsSuccess)
            {
                throw new InvalidOperationException($"Storage upload failed: {storageResult.ErrorMessage}");
            }

            // set the uploaded bytes; CreateAsync will handle StorageLocation resolution
            // Persist the storage blob name (path) returned by the storage service so
            // the DownloadFile API can locate the file. Fall back to the original
            // fileName if BlobName is not provided.
            recording.FileName = string.IsNullOrEmpty(storageResult.BlobName) ? fileName : storageResult.BlobName;
            recording.SizeBytes = storageResult.UploadedBytes;
        }

        // Delegate DB persistence (and deduplication) to existing CreateAsync
        var created = await CreateAsync(recording);
        return created;
    }

    public async Task<bool> UpdateAsync(int id, Recording model)
    {
        if (id != model.Id) return false;
        var exists = await _db.Recordings.AnyAsync(r => r.Id == id);
        if (!exists) return false;
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Recordings.FindAsync(id);
        if (item == null) return false;
        _db.Recordings.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Recording>> GetRecordingsNeedingTranscriptionAsync(int limit = 10)
    {
        // DataAccess models: Call.TalkGroupId (int) and TalkGroup.Id (int)
        // Priority: lower numeric value is higher priority. Null means lowest priority.
        // Use AsNoTracking to avoid leaving tracked Recording instances in the DbContext
        // which can cause identity conflicts when callers later pass detached instances
        // back into UpdateAsync.
        // NOTE: do not filter by StorageLocationId here — use whatever storage location is defined
        var query = from r in _db.Recordings.Include(r => r.Call).AsNoTracking()
                    join tg in _db.TalkGroups on r.Call!.TalkGroupId equals tg.Id into tgJoin
                    from tg in tgJoin.DefaultIfEmpty()
                    where !r.Transcriptions.Any(t => t.IsFinal)
                    select new { Recording = r, TalkGroupPriority = (int?)tg.Priority };

        var ordered = query
            .OrderBy(x => x.TalkGroupPriority.HasValue ? x.TalkGroupPriority.Value : int.MaxValue)
            .ThenByDescending(x => x.Recording.ReceivedAt)
            .Take(limit)
            .Select(x => x.Recording);

        return await ordered.ToListAsync();
    }
}