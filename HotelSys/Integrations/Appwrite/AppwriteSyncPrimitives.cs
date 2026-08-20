#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HotelSys.Integrations.Appwrite;

internal static class AppwriteSyncPrimitives
{
    public static string DeterministicUuid(string entity, long id)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes($"orax:{entity}:{id}"));
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x30);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes).ToString();
    }

    public static string? Iso(DateTime? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    public static string HotelDay(DateTime value)
    {
        var cutoff = new DateTime(value.Year, value.Month, value.Day, 14, 1, 0, DateTimeKind.Unspecified);
        var day = value < cutoff ? value.Date.AddDays(-1) : value.Date;
        return day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static int CalculateNights(DateTime checkin, DateTime checkout)
    {
        var nights = (checkout.Date - checkin.Date).Days;
        if (checkout.TimeOfDay > new TimeSpan(14, 0, 0))
        {
            nights++;
        }

        return Math.Max(1, nights);
    }

    public static string Text(string? value, int maxLength = 0)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return maxLength > 0 && normalized.Length > maxLength
            ? normalized[..maxLength]
            : normalized;
    }

    public static string? NullableText(string? value, int maxLength = 0)
    {
        var normalized = Text(value, maxLength);
        return normalized.Length == 0 ? null : normalized;
    }

    public static long? ReadInt64(IReadOnlyDictionary<string, JsonElement>? data, string key)
    {
        if (data is null || !data.TryGetValue(key, out var element)) return null;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number)) return number;
        if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return number;
        return null;
    }

    public static string? ReadString(IReadOnlyDictionary<string, JsonElement>? data, string key)
    {
        if (data is null || !data.TryGetValue(key, out var element)) return null;
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    public static double? ReadDouble(IReadOnlyDictionary<string, JsonElement>? data, string key)
    {
        if (data is null || !data.TryGetValue(key, out var element)) return null;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number)) return number;
        if (element.ValueKind == JsonValueKind.String
            && double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return number;
        return null;
    }

    public static bool IsServerOwned(AppwriteDocument document)
    {
        var origin = ReadString(document.Data, "origin");
        var syncOrigin = ReadString(document.Data, "sync_origin");
        return string.Equals(origin, "server", StringComparison.OrdinalIgnoreCase)
            || string.Equals(origin, "orax", StringComparison.OrdinalIgnoreCase)
            || string.Equals(syncOrigin, "server", StringComparison.OrdinalIgnoreCase)
            || string.Equals(syncOrigin, "orax", StringComparison.OrdinalIgnoreCase);
    }

    public static Dictionary<long, AppwriteDocument> UniqueByLong(
        IEnumerable<AppwriteDocument> documents,
        string field,
        out HashSet<long> ambiguous)
    {
        var grouped = documents
            .Select(document => new { Document = document, Value = ReadInt64(document.Data, field) })
            .Where(item => item.Value.HasValue)
            .GroupBy(item => item.Value!.Value)
            .ToList();
        var duplicateKeys = grouped.Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        ambiguous = duplicateKeys;
        return grouped
            .Where(group => !duplicateKeys.Contains(group.Key))
            .ToDictionary(group => group.Key, group => group.First().Document);
    }

    public static Dictionary<string, AppwriteDocument> UniqueByString(
        IEnumerable<AppwriteDocument> documents,
        string field,
        StringComparer comparer,
        out HashSet<string> ambiguous)
    {
        var grouped = documents
            .Select(document => new { Document = document, Value = ReadString(document.Data, field) })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .GroupBy(item => item.Value!, comparer)
            .ToList();
        var duplicateKeys = grouped.Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(comparer);
        ambiguous = duplicateKeys;
        return grouped
            .Where(group => !duplicateKeys.Contains(group.Key))
            .ToDictionary(group => group.Key, group => group.First().Document, comparer);
    }

    public static string? ReadDateText(IReadOnlyDictionary<string, JsonElement>? data, string key) =>
        NullableText(ReadString(data, key), 64);
}

public sealed class AppwriteSyncResult
{
    public string Entity { get; init; } = string.Empty;
    public int RemoteBeforeSync { get; set; }
    public int SourceRecords { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int Conflicts { get; set; }
    public bool IsDisabled { get; private set; }
    public bool IsBusy { get; private set; }
    public string? Message { get; private set; }

    public static AppwriteSyncResult Disabled(string entity, string message) => new()
    {
        Entity = entity,
        IsDisabled = true,
        Message = message
    };

    public static AppwriteSyncResult Busy(string entity, string message) => new()
    {
        Entity = entity,
        IsBusy = true,
        Message = message
    };
}
