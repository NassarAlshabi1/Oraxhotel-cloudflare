#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelSys.Integrations.Appwrite;

/// <summary>
/// مزامنة ملف النزيل المسطح إلى guest_infos.
/// مصدر الهوية هو CustomerTable.Id، مع اختيار غرفة من أحدث حجز نشط إن وُجد.
/// </summary>
public sealed class AppwriteGuestInfoSyncService
{
    private readonly AppwriteRestClient _client;
    private readonly HotelAlkheerDB _db;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwriteGuestInfoSyncService> _logger;
    private readonly AppwriteSyncCoordinator _coordinator;

    public AppwriteGuestInfoSyncService(
        AppwriteRestClient client,
        HotelAlkheerDB db,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwriteGuestInfoSyncService> logger,
        AppwriteSyncCoordinator coordinator)
    {
        _client = client;
        _db = db;
        _options = options.Value;
        _logger = logger;
        _coordinator = coordinator;
    }

    public async Task<AppwriteSyncResult> SyncGuestsAsync(CancellationToken cancellationToken = default)
    {
        if (!_coordinator.TryEnter("guest_infos", out var lease))
        {
            return AppwriteSyncResult.Busy("guest_infos", "A guest_infos synchronization is already running.");
        }

        try
        {
            return await SyncGuestsCoreAsync(cancellationToken);
        }
        finally
        {
            lease.Dispose();
        }
    }

    private async Task<AppwriteSyncResult> SyncGuestsCoreAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return AppwriteSyncResult.Disabled("guest_infos", "Appwrite integration is disabled or incomplete.");
        }

        var remoteDocuments = (await _client.ListDocumentsAsync(_options.GuestInfosCollectionId, cancellationToken)).Documents.ToList();
        var serverDocuments = remoteDocuments.Where(AppwriteSyncPrimitives.IsServerOwned).ToList();
        var remoteByServerId = AppwriteSyncPrimitives.UniqueByLong(serverDocuments, "serverId", out var ambiguousServerIds);
        var compositeCandidates = remoteDocuments
            .Where(document => AppwriteSyncPrimitives.ReadInt64(document.Data, "serverId") is null)
            .Where(document => AppwriteSyncPrimitives.IsServerOwned(document)
                || (string.IsNullOrWhiteSpace(AppwriteSyncPrimitives.ReadString(document.Data, "origin"))
                    && string.IsNullOrWhiteSpace(AppwriteSyncPrimitives.ReadString(document.Data, "sync_origin"))))
            .ToList();
        var remoteByComposite = BuildUniqueGuestMap(compositeCandidates, out var ambiguousComposites);
        var myCustomers = _db.MyCustomers.ToList();
        var customers = _db.CustomerTables.ToList().ToDictionary(customer => customer.Id);
        var receptions = _db.RecetionTables.ToList();
        var rooms = _db.RoomsTables.ToList().ToDictionary(room => room.Id);
        var activeRoomByCustomer = BuildActiveRoomMap(myCustomers, receptions, rooms);
        var result = new AppwriteSyncResult { Entity = "guest_infos", RemoteBeforeSync = remoteDocuments.Count, SourceRecords = myCustomers.Count };

        foreach (var myCustomer in myCustomers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!customers.TryGetValue(myCustomer.IdCustomer, out var customer))
            {
                result.Skipped++;
                _logger.LogWarning("Guest sync skipped MyCustomer {MyCustomerId}: CustomerTable {CustomerId} was not found.", myCustomer.Id, myCustomer.IdCustomer);
                continue;
            }

            if (ambiguousServerIds.Contains(customer.Id))
            {
                result.Conflicts++;
                _logger.LogWarning("Guest sync skipped CustomerTable {CustomerId}: duplicate remote serverId.", customer.Id);
                continue;
            }

            var composite = GuestCompositeKey(customer);
            remoteByServerId.TryGetValue(customer.Id, out var remote);
            if (remote is null && composite.Length > 0)
            {
                if (ambiguousComposites.Contains(composite))
                {
                    result.Conflicts++;
                    _logger.LogWarning("Guest sync skipped CustomerTable {CustomerId}: duplicate remote guest identity {Composite}.", customer.Id, composite);
                    continue;
                }

                remoteByComposite.TryGetValue(composite, out remote);
            }

            var documentId = remote?.Id ?? $"orax-guest-{customer.Id}";
            var now = DateTimeOffset.UtcNow;
            var nowSeconds = now.ToUnixTimeSeconds();
            activeRoomByCustomer.TryGetValue(myCustomer.Id, out var roomNumber);
            var payload = new Dictionary<string, object?>
            {
                ["localUuid"] = AppwriteSyncPrimitives.DeterministicUuid("guest", customer.Id),
                ["serverId"] = customer.Id,
                ["guestName"] = AppwriteSyncPrimitives.Text(customer.Name, 100),
                ["nationality"] = AppwriteSyncPrimitives.Text(customer.Nationality, 50),
                ["idType"] = AppwriteSyncPrimitives.Text(customer.TypeProof, 50),
                ["idNumber"] = AppwriteSyncPrimitives.Text(customer.NumProof, 100),
                ["issueDate"] = AppwriteSyncPrimitives.Iso(customer.ReleaseDate),
                ["issuePlace"] = AppwriteSyncPrimitives.Text(customer.LocRelease, 100),
                ["roomNumber"] = AppwriteSyncPrimitives.Text(roomNumber),
                ["governorate"] = string.Empty,
                ["notes"] = AppwriteSyncPrimitives.Text(customer.PublicNote, 1000),
                ["origin"] = "server",
                ["sync_origin"] = "orax",
                ["deviceId"] = "orax-server",
                ["vectorClock"] = "{}",
                ["version"] = Math.Max(1L, AppwriteSyncPrimitives.ReadInt64(remote?.Data, "version") ?? 0L) + 1L,
                ["createdAt"] = nowSeconds,
                ["updatedAt"] = nowSeconds,
                ["createdAtEpoch"] = nowSeconds,
                ["lastModified"] = nowSeconds,
                ["lastModifiedEpoch"] = nowSeconds,
                ["createdAtIso"] = now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                ["updatedAtIso"] = now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                ["idempotencyKey"] = $"orax:guests:{customer.Id}:{nowSeconds}"
            };

            try
            {
                await _client.UpsertDocumentAsync(_options.GuestInfosCollectionId, documentId, payload, cancellationToken);
                if (remote is null) result.Created++; else result.Updated++;
            }
            catch (Exception exception)
            {
                result.Failed++;
                _logger.LogError(exception, "Appwrite guest sync failed for CustomerTable {CustomerId}.", customer.Id);
            }
        }

        return result;
    }

    private static Dictionary<string, AppwriteDocument> BuildUniqueGuestMap(
        IEnumerable<AppwriteDocument> documents,
        out HashSet<string> ambiguous)
    {
        var grouped = documents
            .Select(document => new { Document = document, Key = GuestCompositeKey(document.Data) })
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var duplicateKeys = grouped.Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        ambiguous = duplicateKeys;
        return grouped
            .Where(group => !duplicateKeys.Contains(group.Key))
            .ToDictionary(group => group.Key, group => group.First().Document, StringComparer.OrdinalIgnoreCase);
    }

    private static string GuestCompositeKey(CustomerTable customer) =>
        GuestCompositeKey(customer.Name, customer.NumProof, customer.Nationality);

    private static string GuestCompositeKey(IReadOnlyDictionary<string, System.Text.Json.JsonElement> data) =>
        GuestCompositeKey(
            AppwriteSyncPrimitives.ReadString(data, "guestName"),
            AppwriteSyncPrimitives.ReadString(data, "idNumber"),
            AppwriteSyncPrimitives.ReadString(data, "nationality"));

    private static string GuestCompositeKey(string? name, string? idNumber, string? nationality)
    {
        var normalizedId = AppwriteSyncPrimitives.Text(idNumber, 100);
        if (normalizedId.Length == 0) return string.Empty;
        return $"{AppwriteSyncPrimitives.Text(name, 100).ToUpperInvariant()}|{normalizedId.ToUpperInvariant()}|{AppwriteSyncPrimitives.Text(nationality, 50).ToUpperInvariant()}";
    }

    private static Dictionary<long, string> BuildActiveRoomMap(
        IReadOnlyCollection<MyCustomer> myCustomers,
        IReadOnlyCollection<RecetionTable> receptions,
        IReadOnlyDictionary<int, RoomsTable> rooms)
    {
        var myCustomerIds = myCustomers.Select(customer => customer.Id).ToHashSet();
        return receptions
            .Where(reception => reception.IdMyCustomer.HasValue
                && myCustomerIds.Contains(reception.IdMyCustomer.Value)
                && reception.IsChechout != true
                && reception.Status != 3
                && reception.Status != 4)
            .GroupBy(reception => reception.IdMyCustomer!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(reception => reception.IsChechin == true)
                    .ThenByDescending(reception => reception.StartDate)
                    .Select(reception => rooms.TryGetValue(reception.IdRoom, out var room) ? AppwriteSyncPrimitives.Text(room.NameR, 512) : string.Empty)
                    .FirstOrDefault() ?? string.Empty);
    }
}
