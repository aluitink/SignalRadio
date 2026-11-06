using Microsoft.EntityFrameworkCore;
using SignalRadio.Data;
using SignalRadio.Data.Entities;

namespace SignalRadio.Api.Services;

/// <summary>
/// Imports talk group definitions from a CSV file (e.g. the trunk-recorder
/// <c>danecom-talkgroups.csv</c>). The trunk recorder only yields the talkgroup
/// number on upload, so importing the full table at startup ensures the UI has
/// rich info (name, description, category, priority) for every group.
///
/// Expected columns (header row required):
///   Decimal, Hex, Mode, Alpha Tag, Description, Tag, Category, Priority
///
/// Import semantics:
///   - Rows are keyed by the decimal talkgroup number.
///   - Existing rows are enriched with any missing fields (never overwritten).
///   - New rows are inserted.
///   - Rows with a non-numeric Decimal are skipped.
/// </summary>
public class TalkGroupCsvImporter
{
    public record ImportResult(int Created, int Updated, int Skipped, int TotalRows);

    public static async Task<ImportResult> ImportFromFileAsync(
        SignalRadioDbContext db, string path, ILogger logger, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            logger.LogInformation("Talk group CSV not found at {Path}; skipping import.", path);
            return new ImportResult(0, 0, 0, 0);
        }

        var lines = await File.ReadAllLinesAsync(path, ct);
        return await ImportLinesAsync(db, lines, logger, ct);
    }

    public static async Task<ImportResult> ImportLinesAsync(
        SignalRadioDbContext db, IEnumerable<string> lines, ILogger logger, CancellationToken ct = default)
    {
        var rows = ParseLines(lines);
        if (rows.Count == 0)
        {
            logger.LogInformation("Talk group CSV contained no usable rows.");
            return new ImportResult(0, 0, 0, 0);
        }

        var existing = await db.TalkGroups.ToDictionaryAsync(t => t.Number, ct);

        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in rows)
        {
            if (existing.TryGetValue(row.Number, out var tg))
            {
                var changed = false;
                if (string.IsNullOrEmpty(tg.AlphaTag) && !string.IsNullOrWhiteSpace(row.AlphaTag)) { tg.AlphaTag = row.AlphaTag; changed = true; }
                if (string.IsNullOrEmpty(tg.Description) && !string.IsNullOrWhiteSpace(row.Description)) { tg.Description = row.Description; changed = true; }
                if (string.IsNullOrEmpty(tg.Tag) && !string.IsNullOrWhiteSpace(row.Tag)) { tg.Tag = row.Tag; changed = true; }
                if (string.IsNullOrEmpty(tg.Category) && !string.IsNullOrWhiteSpace(row.Category)) { tg.Category = row.Category; changed = true; }
                if (!tg.Priority.HasValue && row.Priority.HasValue) { tg.Priority = row.Priority; changed = true; }
                // Name: prefer the human-readable Description; fall back to AlphaTag.
                // Also upgrade names that are just the system name (e.g. "DaneCom")
                // — those were set by the upload path before the CSV import ran and
                // carry no real information.
                var csvName = !string.IsNullOrWhiteSpace(row.Description) ? row.Description : row.AlphaTag;
                if (string.IsNullOrEmpty(tg.Name) || IsGenericSystemName(tg.Name))
                {
                    if (!string.IsNullOrWhiteSpace(csvName) && !string.Equals(tg.Name, csvName, StringComparison.OrdinalIgnoreCase))
                    {
                        tg.Name = csvName;
                        changed = true;
                    }
                }
                if (changed) updated++;
            }
            else
            {
                db.TalkGroups.Add(new TalkGroup
                {
                    Number = row.Number,
                    Name = !string.IsNullOrWhiteSpace(row.Description) ? row.Description : row.AlphaTag,
                    AlphaTag = row.AlphaTag,
                    Description = row.Description,
                    Tag = row.Tag,
                    Category = row.Category,
                    Priority = row.Priority,
                    CreatedAt = DateTime.UtcNow
                });
                created++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Talk group CSV import complete: {Created} created, {Updated} enriched, {Skipped} skipped ({Total} rows).",
            created, updated, skipped, rows.Count);

        return new ImportResult(created, updated, skipped, rows.Count);
    }

    /// <summary>
    /// Names that carry no real information — typically the system name stamped by
    /// the upload path (e.g. "DaneCom") before the CSV import had a chance to run.
    /// </summary>
    private static readonly HashSet<string> GenericSystemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DaneCom", "Dane County", "DaneCom System", "Unknown", "N/A", "System"
    };

    private static bool IsGenericSystemName(string? name)
        => !string.IsNullOrWhiteSpace(name) && GenericSystemNames.Contains(name.Trim());

    private record CsvRow(string Number, string? AlphaTag, string? Description, string? Tag, string? Category, int? Priority);

    private static List<CsvRow> ParseLines(IEnumerable<string> lines)
    {
        var rows = new List<CsvRow>();
        string?[]? header = null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            var fields = SplitCsvLine(line);
            if (header == null)
            {
                header = fields.Select(f => f.Trim().ToLowerInvariant()).ToArray();
                continue;
            }

            string Get(string name)
            {
                var idx = header!.Select(h => h == name).ToList().IndexOf(true);
                return idx >= 0 && idx < fields.Length ? fields[idx].Trim() : null;
            }

            var number = Get("decimal");
            if (string.IsNullOrWhiteSpace(number) || !int.TryParse(number, out _))
                continue;

            rows.Add(new CsvRow(
                number,
                Get("alpha tag"),
                Get("description"),
                Get("tag"),
                Get("category"),
                int.TryParse(Get("priority"), out var p) ? p : null));
        }

        return rows;
    }

    /// <summary>Splits a CSV line, honoring double-quoted fields (with "" escapes).</summary>
    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
