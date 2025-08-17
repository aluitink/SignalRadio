using SignalRadio.Core.Models;
using SignalRadio.Core.Repositories;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SignalRadio.Core.Services;

public interface ITalkGroupService
{
    Task<IEnumerable<TalkGroup>> GetAllTalkGroupsAsync();
    Task<TalkGroup?> GetTalkGroupByIdAsync(string decimalId);
    Task<IEnumerable<TalkGroup>> GetTalkGroupsByCategoryAsync(string category);
    Task<IEnumerable<TalkGroup>> SearchTalkGroupsAsync(string searchTerm);
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<int> ImportFromCsvAsync(Stream csvStream);
    Task<bool> ClearAllTalkGroupsAsync();
}

public class TalkGroupService : ITalkGroupService
{
    private readonly ITalkGroupRepository _talkGroupRepository;
    private readonly ILogger<TalkGroupService> _logger;

    public TalkGroupService(ITalkGroupRepository talkGroupRepository, ILogger<TalkGroupService> logger)
    {
        _talkGroupRepository = talkGroupRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<TalkGroup>> GetAllTalkGroupsAsync()
    {
        return await _talkGroupRepository.GetAllAsync();
    }

    public async Task<TalkGroup?> GetTalkGroupByIdAsync(string decimalId)
    {
        return await _talkGroupRepository.GetByDecimalAsync(decimalId);
    }

    public async Task<IEnumerable<TalkGroup>> GetTalkGroupsByCategoryAsync(string category)
    {
        return await _talkGroupRepository.GetByCategoryAsync(category);
    }

    public async Task<IEnumerable<TalkGroup>> SearchTalkGroupsAsync(string searchTerm)
    {
        return await _talkGroupRepository.SearchAsync(searchTerm);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        var talkGroups = await _talkGroupRepository.GetAllAsync();
        return talkGroups
            .Where(tg => !string.IsNullOrEmpty(tg.Category))
            .Select(tg => tg.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public async Task<int> ImportFromCsvAsync(Stream csvStream)
    {
        var talkGroups = new List<TalkGroup>();
        
        using var reader = new StreamReader(csvStream);
        
        // Read header line
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null)
        {
            throw new InvalidOperationException("CSV file is empty");
        }

        _logger.LogInformation("CSV Header: {Header}", headerLine);

        var lineNumber = 1;
        var importedCount = 0;
        var skippedCount = 0;

        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();
            
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var talkGroup = ParseCsvLine(line);
                if (talkGroup != null)
                {
                    talkGroups.Add(talkGroup);
                    importedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse line {LineNumber}: {Line}", lineNumber, line);
                skippedCount++;
            }
        }

        _logger.LogInformation("Parsed {ImportedCount} talk groups, skipped {SkippedCount} lines", 
            importedCount, skippedCount);

        if (talkGroups.Count > 0)
        {
            // Clear existing data and insert new data
            await _talkGroupRepository.DeleteAllAsync();
            await _talkGroupRepository.BulkInsertAsync(talkGroups);
            
            _logger.LogInformation("Successfully imported {Count} talk groups", talkGroups.Count);
        }

        return talkGroups.Count;
    }

    public async Task<bool> ClearAllTalkGroupsAsync()
    {
        try
        {
            await _talkGroupRepository.DeleteAllAsync();
            _logger.LogInformation("Cleared all talk groups");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear talk groups");
            return false;
        }
    }

    private TalkGroup? ParseCsvLine(string line)
    {
        // Parse CSV line - handle quoted fields
        var fields = SplitCsvLine(line);
        
        if (fields.Length < 8)
        {
            _logger.LogWarning("Line has insufficient fields ({Count}): {Line}", fields.Length, line);
            return null;
        }

        // Skip if decimal field is empty or invalid
        if (string.IsNullOrWhiteSpace(fields[0]) || !int.TryParse(fields[0], out _))
        {
            return null;
        }

        var priority = 1; // Default priority
        if (fields.Length > 7 && int.TryParse(fields[7], out var parsedPriority))
        {
            priority = parsedPriority;
        }

        return new TalkGroup
        {
            Decimal = fields[0].Trim(),
            Hex = GetSafeField(fields, 1),
            Mode = GetSafeField(fields, 2),
            AlphaTag = fields.Length > 3 ? fields[3].Trim() : "Unknown",
            Description = GetSafeField(fields, 4),
            Tag = GetSafeField(fields, 5),
            Category = GetSafeField(fields, 6),
            Priority = priority
        };
    }

    private string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private string? GetSafeField(string[] fields, int index)
    {
        if (index >= fields.Length)
            return null;
        
        var field = fields[index].Trim();
        return string.IsNullOrWhiteSpace(field) ? null : field;
    }
}
