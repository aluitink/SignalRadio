using Microsoft.EntityFrameworkCore;

namespace SignalRadio.DataAccess.Extensions;

/// <summary>
/// Extension methods for full-text search functionality
/// </summary>
public static class FullTextSearchExtensions
{
    /// <summary>
    /// Performs a free-text search on TranscriptSummary.Summary field
    /// </summary>
    public static IQueryable<TranscriptSummary> WhereFreeTextContains(
        this IQueryable<TranscriptSummary> query,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(ts => EF.Functions.FreeText(ts.Summary, searchTerm));
    }

    /// <summary>
    /// Performs a free-text search on NotableIncident.Description field
    /// </summary>
    public static IQueryable<NotableIncident> WhereFreeTextContains(
        this IQueryable<NotableIncident> query,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(ni => EF.Functions.FreeText(ni.Description, searchTerm));
    }

    /// <summary>
    /// Performs a free-text search on Topic.Name field
    /// </summary>
    public static IQueryable<Topic> WhereFreeTextContains(
        this IQueryable<Topic> query,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(t => EF.Functions.FreeText(t.Name, searchTerm));
    }

    /// <summary>
    /// Performs a full-text search using CONTAINS on TranscriptSummary.Summary field
    /// </summary>
    public static IQueryable<TranscriptSummary> WhereFullTextContains(
        this IQueryable<TranscriptSummary> query,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(ts => EF.Functions.Contains(ts.Summary, searchTerm));
    }

    /// <summary>
    /// Performs a full-text search using CONTAINS on NotableIncident.Description field
    /// </summary>
    public static IQueryable<NotableIncident> WhereFullTextContains(
        this IQueryable<NotableIncident> query,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(ni => EF.Functions.Contains(ni.Description, searchTerm));
    }

    /// <summary>
    /// Performs a full-text search using CONTAINS on Topic.Name field
    /// </summary>
    public static IQueryable<Topic> WhereFullTextContains(
        this IQueryable<Topic> query,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(t => EF.Functions.Contains(t.Name, searchTerm));
    }
}