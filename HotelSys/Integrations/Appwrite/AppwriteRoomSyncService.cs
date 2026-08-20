#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    private readonly AppwriteRestClient _client;
    private readonly HotelAlkheerDB _db;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwriteRoomSyncService> _logger;
    private readonly AppwriteSyncCoordinator _coordinator;

    public AppwriteRoomSyncService(
        AppwriteRestClient client,
        HotelAlkheerDB db,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwriteRoomSyncService> logger,
        AppwriteSyncCoordinator coordinator)
    {
        _client = client;
        _db = db;
        _options = options.Value;
        _logger = logger;
        _coordinator = coordinator;
    }

    public async Task<AppwriteRoomSyncResult> SyncRoomsAsync(CancellationToken cancellationToken = default)
    {
        if (!_coordinator.TryEnter("rooms", out var lease))
        {
            return AppwriteRoomSyncResult.Busy("A rooms synchronization is already running.");
        }

        try
        {
            return await SyncRoomsCoreAsync(cancellationToken);
        }
        finally
        {
            lease.Dispose();
        }
    }

    private async Task<AppwriteRoomSyncResult> SyncRoomsCoreAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return AppwriteRoomSyncResult.Disabled("Appwrite integration is disabled or incomplete.");
        }

        var remoteDocuments = (await _client.ListDocumentsAsync(_options.RoomsCollectionId, cancellationToken)).Documents.ToList();
        var remoteWithServerIds = remoteDocuments
            .Where(document => TryReadInt(document.Data, "serverId", out _))
            .Select(document => new { Document = document, ServerId = ReadInt(document.Data, "serverId") })
            .ToList();
        var ambiguousServerIds = remoteWithServerIds
            .GroupBy(item => item.ServerId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        var remoteByServerId = remoteWithServerIds
            .Where(item => !ambiguousServerIds.Contains(item.ServerId))
            .ToDictionary(item => item.ServerId, item => item.Document);

        var remoteWithRoomNumbers = remoteDocuments
            .Where(IsTrustedServerDocument)
            .Select(document => new { Document = document, RoomNumber = ReadString(document.Data, "roomNumber") })
            .Where(item => !string.IsNullOrWhiteSpace(item.RoomNumber))
            .ToList();
        var ambiguousRoomNumbers = remoteWithRoomNumbers
            .GroupBy(item => item.RoomNumber!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remoteByRoomNumber = remoteWithRoomNumbers
            .Where(item => !ambiguousRoomNumbers.Contains(item.RoomNumber!))
            .ToDictionary(item => item.RoomNumber!, item => item.Document, StringComparer.OrdinalIgnoreCase);

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
                if (ambiguousRoomNumbers.Contains(roomNumber))
                {
                    result.Conflicts++;
                    _logger.LogWarning("Appwrite room sync skipped Orax room {RoomId} ({RoomNumber}) because multiple remote documents share the room number.", room.Id, roomNumber);
                    continue;
                }

                remoteByRoomNumber.TryGetValue(roomNumber, out remote);
            }

            var documentId = remote?.Id ?? $"orax-room-{room.Id}";
            var now = DateTimeOffset.UtcNow;
            var nowSeconds = now.ToUnixTimeSeconds();
            var typeName = types.TryGetValue(room.IdType, out var type) ? type.NameT : string.Empty;
            var oraxStatus = statuses.TryGetValue(room.Id, out var roomStatus) ? roomStatus : null;
            var status = OraxRoomStatusMapper.ToFlutterStatus(oraxStatus);
            var payload = new Dictionary<string, object?>
            {
                ["localUuid"] = ReadString(remote?.Data, "localUuid") ?? Guid.NewGuid().ToString(),
                ["serverId"] = room.Id,
                ["roomNumber"] = roomNumber,
                ["type"] = typeName,
                ["price"] = prices.TryGetValue(room.Id, out var price) ? price : 0d,
                ["status"] = status,
                ["cleaningStatus"] = OraxRoomStatusMapper.ResolveCleaningStatus(oraxStatus, ReadString(remote?.Data, "cleaningStatus")),
                ["requiresMaintenance"] = OraxRoomStatusMapper.RequiresMaintenance(oraxStatus, ReadBool(remote?.Data, "requiresMaintenance")),
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
                await _client.UpsertDocumentAsync(_options.RoomsCollectionId, documentId, payload, cancellationToken);
                if (remote is null)
                {
                    result.Created++;
                }
                else
                {
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

    private static bool IsTrustedServerDocument(AppwriteDocument document)
    {
        var origin = ReadString(document.Data, "origin");
        var syncOrigin = ReadString(document.Data, "sync_origin");
        return string.Equals(origin, "server", StringComparison.OrdinalIgnoreCase)
            || string.Equals(origin, "orax", StringComparison.OrdinalIgnoreCase)
            || string.Equals(syncOrigin, "server", StringComparison.OrdinalIgnoreCase)
            || string.Equals(syncOrigin, "orax", StringComparison.OrdinalIgnoreCase);
    }

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

}

public sealed class AppwriteRoomSyncResult
{
    public int RemoteBeforeSync { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int Conflicts { get; set; }
    public bool IsDisabled { get; private set; }
    public bool IsBusy { get; private set; }
    public string? Message { get; private set; }

    public static AppwriteRoomSyncResult Disabled(string message) => new()
    {
        IsDisabled = true,
        Message = message
    };

    public static AppwriteRoomSyncResult Busy(string message) => new()
    {
        IsBusy = true,
        Message = message
    };
}
