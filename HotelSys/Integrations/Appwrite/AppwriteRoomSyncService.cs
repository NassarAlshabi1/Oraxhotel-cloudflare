#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DataModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelSys.Integrations.Appwrite;

/// <summary>
/// مزامنة كتالوج الغرف من SQL Server/Orax إلى Appwrite Cloud.
/// Flutter يقرأ collection rooms الحالية عبر AppwriteSyncManager الموجود مسبقاً.
/// </summary>
public sealed class AppwriteRoomSyncService
{
    private readonly HttpClient _httpClient;
    private readonly HotelAlkheerDB _db;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwriteRoomSyncService> _logger;

    public AppwriteRoomSyncService(
        HttpClient httpClient,
        HotelAlkheerDB db,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwriteRoomSyncService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppwriteRoomSyncResult> SyncRoomsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return AppwriteRoomSyncResult.Disabled("Appwrite integration is disabled or incomplete.");
        }

        var remoteDocuments = await ListRemoteRoomsAsync(cancellationToken);
        var remoteByServerId = remoteDocuments
            .Where(document => TryReadInt(document.Data, "serverId", out _))
            .GroupBy(document => ReadInt(document.Data, "serverId"))
            .ToDictionary(group => group.Key, group => group.First());
        var remoteByRoomNumber = remoteDocuments
            .Select(document => new { Document = document, RoomNumber = ReadString(document.Data, "roomNumber") })
            .Where(item => !string.IsNullOrWhiteSpace(item.RoomNumber))
            .GroupBy(item => item.RoomNumber!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Document, StringComparer.OrdinalIgnoreCase);

        var rooms = _db.RoomsTables.ToList();
        var types = _db.TypeRoomsTables.ToList().ToDictionary(type => type.Id);
        var prices = _db.PriceRoomsTables
            .ToList()
            .Where(price => price.Price.HasValue)
            .GroupBy(price => price.IdRoom)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(price => price.Id).First().Price!.Value);
        var statuses = _db.StatusCurrentTables
            .ToList()
            .GroupBy(status => status.IdRoom)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(status => status.Id).First().Status);

        var result = new AppwriteRoomSyncResult();
        foreach (var room in rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var roomNumber = room.NameR?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomNumber))
            {
                result.Skipped++;
                continue;
            }

            remoteByServerId.TryGetValue(room.Id, out var remote);
            if (remote is null)
            {
                remoteByRoomNumber.TryGetValue(roomNumber, out remote);
            }

            var documentId = remote?.Id ?? $"orax-room-{room.Id}";
            var now = DateTimeOffset.UtcNow;
            var nowSeconds = now.ToUnixTimeSeconds();
            var typeName = types.TryGetValue(room.IdType, out var type) ? type.NameT : string.Empty;
            var status = statuses.TryGetValue(room.Id, out var roomStatus) && !string.IsNullOrWhiteSpace(roomStatus)
                ? roomStatus
                : "شاغرة";
            var payload = new Dictionary<string, object?>
            {
                ["localUuid"] = ReadString(remote?.Data, "localUuid") ?? Guid.NewGuid().ToString(),
                ["serverId"] = room.Id,
                ["roomNumber"] = roomNumber,
                ["type"] = typeName,
                ["price"] = prices.TryGetValue(room.Id, out var price) ? price : 0d,
                ["status"] = status,
                ["cleaningStatus"] = ReadString(remote?.Data, "cleaningStatus") ?? "clean",
                ["requiresMaintenance"] = ReadBool(remote?.Data, "requiresMaintenance"),
                ["origin"] = "server",
                ["sync_origin"] = "orax",
                ["deviceId"] = "orax-server",
                ["vectorClock"] = ReadString(remote?.Data, "vectorClock") ?? "{}",
                ["version"] = Math.Max(1L, ReadLong(remote?.Data, "version") + 1),
                ["updatedAtIso"] = now.UtcDateTime.ToString("O"),
                ["updatedAt"] = nowSeconds,
                ["lastModified"] = nowSeconds,
                ["lastModifiedEpoch"] = nowSeconds,
                ["syncTimestamp"] = now.ToUnixTimeMilliseconds(),
                ["idempotencyKey"] = $"orax:rooms:{room.Id}:{nowSeconds}"
            };

            try
            {
                if (remote is null)
                {
                    await CreateDocumentAsync(documentId, payload, cancellationToken);
                    result.Created++;
                }
                else
                {
                    await UpdateDocumentAsync(documentId, payload, cancellationToken);
                    result.Updated++;
                }
            }
            catch (Exception exception)
            {
                result.Failed++;
                _logger.LogError(exception, "Appwrite room sync failed for Orax room {RoomId} ({RoomNumber})", room.Id, roomNumber);
            }
        }

        result.RemoteBeforeSync = remoteDocuments.Count;
        return result;
    }

    private async Task<IReadOnlyList<AppwriteDocument>> ListRemoteRoomsAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, DocumentCollectionUrl());
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Appwrite list rooms failed ({(int)response.StatusCode}): {TrimError(body)}");
        }

        var result = JsonSerializer.Deserialize<AppwriteDocumentList>(body, JsonOptions) ?? new AppwriteDocumentList();
        if (result.Total > result.Documents.Count)
        {
            throw new InvalidOperationException($"Appwrite rooms collection has {result.Total} documents but the first response returned only {result.Documents.Count}. Pagination must be configured before syncing.");
        }

        return result.Documents;
    }

    private async Task CreateDocumentAsync(string documentId, IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new { documentId, data }, JsonOptions);
        using var request = CreateRequest(HttpMethod.Post, DocumentCollectionUrl());
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        await SendEnsuringSuccessAsync(request, cancellationToken, "create room");
    }

    private async Task UpdateDocumentAsync(string documentId, IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new { data }, JsonOptions);
        using var request = CreateRequest(HttpMethod.Put, DocumentUrl(documentId));
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        await SendEnsuringSuccessAsync(request, cancellationToken, "update room");
    }

    private async Task SendEnsuringSuccessAsync(HttpRequestMessage request, CancellationToken cancellationToken, string operation)
    {
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Appwrite {operation} failed ({(int)response.StatusCode}): {TrimError(body)}");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Appwrite-Project", _options.ProjectId);
        request.Headers.Add("X-Appwrite-Key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private string DocumentCollectionUrl() =>
        $"{_options.Endpoint.TrimEnd('/')}/databases/{Uri.EscapeDataString(_options.DatabaseId)}/collections/{Uri.EscapeDataString(_options.RoomsCollectionId)}/documents";

    private string DocumentUrl(string documentId) =>
        $"{DocumentCollectionUrl()}/{Uri.EscapeDataString(documentId)}";

    private static string TrimError(string body) => body.Length <= 500 ? body : body[..500];

    private static bool TryReadInt(IReadOnlyDictionary<string, JsonElement> data, string key, out int value)
    {
        if (data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value)) return true;
        value = 0;
        return false;
    }

    private static bool TryReadString(IReadOnlyDictionary<string, JsonElement> data, string key, out string? value)
    {
        value = ReadString(data, key);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static int ReadInt(IReadOnlyDictionary<string, JsonElement> data, string key) =>
        TryReadInt(data, key, out var value) ? value : 0;

    private static long ReadLong(IReadOnlyDictionary<string, JsonElement>? data, string key)
    {
        if (data is not null && data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var value)) return value;
        return 0;
    }

    private static string? ReadString(IReadOnlyDictionary<string, JsonElement>? data, string key)
    {
        if (data is not null && data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String) return element.GetString();
        return null;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, JsonElement>? data, string key)
    {
        if (data is not null && data.TryGetValue(key, out var element) && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)) return element.GetBoolean();
        return false;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class AppwriteDocumentList
    {
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("documents")] public List<AppwriteDocument> Documents { get; set; } = new();
    }

    private sealed class AppwriteDocument
    {
        [JsonPropertyName("$id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("data")] public Dictionary<string, JsonElement> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class AppwriteRoomSyncResult
{
    public int RemoteBeforeSync { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public bool IsDisabled { get; private set; }
    public string? Message { get; private set; }

    public static AppwriteRoomSyncResult Disabled(string message) => new()
    {
        IsDisabled = true,
        Message = message
    };
}
