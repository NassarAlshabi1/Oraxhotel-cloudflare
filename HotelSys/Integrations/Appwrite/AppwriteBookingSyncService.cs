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
/// مزامنة الحجوزات من recetion_table إلى Appwrite bookings.
/// Orax/SQL Server هو مصدر الحقيقة في اتجاه المزامنة هذا.
/// </summary>
public sealed class AppwriteBookingSyncService
{
    private readonly AppwriteRestClient _client;
    private readonly HotelAlkheerDB _db;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwriteBookingSyncService> _logger;

    public AppwriteBookingSyncService(
        AppwriteRestClient client,
        HotelAlkheerDB db,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwriteBookingSyncService> logger)
    {
        _client = client;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppwriteSyncResult> SyncBookingsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return AppwriteSyncResult.Disabled("bookings", "Appwrite integration is disabled or incomplete.");
        }

        var remoteDocuments = (await _client.ListDocumentsAsync(_options.BookingsCollectionId, cancellationToken)).Documents.ToList();
        var serverDocuments = remoteDocuments.Where(AppwriteSyncPrimitives.IsServerOwned).ToList();
        var remoteByServerId = AppwriteSyncPrimitives.UniqueByLong(serverDocuments, "serverBookingId", out var ambiguousServerIds);
        // المستندات الحالية في Appwrite قديمة: تحمل origin=server لكن serverBookingId=null.
        // نطابقها بالغرفة وتاريخ الدخول فقط إذا كانت server-owned، أو legacy بلا origin.
        // مستندات origin=local لا تدخل في المطابقة حتى لا يكتب Orax فوق حجز أنشأه الهاتف.
        var compositeCandidates = remoteDocuments
            .Where(document => AppwriteSyncPrimitives.ReadInt64(document.Data, "serverBookingId") is null)
            .Where(document => AppwriteSyncPrimitives.IsServerOwned(document)
                || (string.IsNullOrWhiteSpace(AppwriteSyncPrimitives.ReadString(document.Data, "origin"))
                    && string.IsNullOrWhiteSpace(AppwriteSyncPrimitives.ReadString(document.Data, "sync_origin"))))
            .ToList();
        var remoteByComposite = BuildUniqueCompositeMap(compositeCandidates, out var ambiguousComposites);

        var rooms = _db.RoomsTables.ToList().ToDictionary(room => room.Id);
        var myCustomers = _db.MyCustomers.ToList().ToDictionary(customer => customer.Id);
        var customers = _db.CustomerTables.ToList().ToDictionary(customer => customer.Id);
        var receptions = _db.RecetionTables.ToList();
        var result = new AppwriteSyncResult { Entity = "bookings", RemoteBeforeSync = remoteDocuments.Count, SourceRecords = receptions.Count };

        foreach (var reception in receptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roomNumber = rooms.TryGetValue(reception.IdRoom, out var room)
                ? AppwriteSyncPrimitives.Text(room.NameR, 512)
                : string.Empty;
            var customer = ResolveCustomer(reception, myCustomers, customers);
            var composite = CompositeKey(roomNumber, reception.StartDate);
            AppwriteDocument? remote = null;

            if (remoteByServerId.TryGetValue(reception.Id, out var serverDocument))
            {
                remote = serverDocument;
            }
            else if (ambiguousServerIds.Contains(reception.Id))
            {
                result.Conflicts++;
                _logger.LogWarning("Booking sync skipped Orax reception {ReceptionId}: duplicate remote serverBookingId.", reception.Id);
                continue;
            }
            else if (ambiguousComposites.Contains(composite))
            {
                result.Conflicts++;
                _logger.LogWarning("Booking sync skipped Orax reception {ReceptionId}: duplicate legacy room/date documents for {Composite}.", reception.Id, composite);
                continue;
            }
            else
            {
                remoteByComposite.TryGetValue(composite, out remote);
            }

            var documentId = remote?.Id ?? $"orax-booking-{reception.Id}";
            var now = DateTimeOffset.UtcNow;
            var nowSeconds = now.ToUnixTimeSeconds();
            var checkin = reception.StartDate;
            var checkout = reception.EndDate;
            var actualCheckout = reception.ChechoutDate;
            var payload = new Dictionary<string, object?>
            {
                ["localUuid"] = AppwriteSyncPrimitives.DeterministicUuid("booking", reception.Id),
                ["serverBookingId"] = reception.Id,
                ["serverId"] = reception.Id,
                ["roomNumber"] = roomNumber,
                ["guestName"] = AppwriteSyncPrimitives.Text(customer?.Name, 512),
                ["guestPhone"] = AppwriteSyncPrimitives.Text(customer?.PhoneWork, 512),
                ["guestNationality"] = AppwriteSyncPrimitives.Text(customer?.Nationality, 512),
                ["guestIdType"] = AppwriteSyncPrimitives.Text(customer?.TypeProof, 50),
                ["guestIdNumber"] = AppwriteSyncPrimitives.Text(customer?.NumProof, 100),
                ["guestIdIssueDate"] = AppwriteSyncPrimitives.Iso(customer?.ReleaseDate),
                ["guestIdIssuePlace"] = AppwriteSyncPrimitives.Text(customer?.LocRelease, 100),
                ["guestEmail"] = AppwriteSyncPrimitives.Text(customer?.Email, 100),
                ["guestAddress"] = null,
                ["checkinDate"] = AppwriteSyncPrimitives.Iso(checkin),
                ["checkoutDate"] = AppwriteSyncPrimitives.Iso(checkout),
                ["actualCheckout"] = AppwriteSyncPrimitives.Iso(actualCheckout),
                ["status"] = MapStatus(reception),
                ["notes"] = AppwriteSyncPrimitives.Text(reception.Note, 1000),
                ["discount"] = 0d,
                ["discountType"] = "per_night",
                ["expectedNights"] = AppwriteSyncPrimitives.CalculateNights(checkin, checkout),
                ["calculatedNights"] = AppwriteSyncPrimitives.CalculateNights(checkin, checkout),
                ["hotelDayCheckin"] = AppwriteSyncPrimitives.HotelDay(checkin),
                ["hotelDayCheckout"] = AppwriteSyncPrimitives.HotelDay(checkout),
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
                ["syncTimestamp"] = now.ToUnixTimeMilliseconds(),
                ["createdAtIso"] = now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                ["updatedAtIso"] = now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                ["idempotencyKey"] = $"orax:bookings:{reception.Id}:{nowSeconds}"
            };

            try
            {
                await _client.UpsertDocumentAsync(_options.BookingsCollectionId, documentId, payload, cancellationToken);
                if (remote is null) result.Created++; else result.Updated++;
            }
            catch (Exception exception)
            {
                result.Failed++;
                _logger.LogError(exception, "Appwrite booking sync failed for Orax reception {ReceptionId}.", reception.Id);
            }
        }

        return result;
    }

    private static CustomerTable? ResolveCustomer(
        RecetionTable reception,
        IReadOnlyDictionary<long, MyCustomer> myCustomers,
        IReadOnlyDictionary<long, CustomerTable> customers)
    {
        return reception.IdMyCustomer.HasValue
            && myCustomers.TryGetValue(reception.IdMyCustomer.Value, out var myCustomer)
            && customers.TryGetValue(myCustomer.IdCustomer, out var customer)
            ? customer
            : null;
    }

    private static string MapStatus(RecetionTable reception) => reception.Status switch
    {
        1 => "مؤقت",
        2 when reception.IsChechin == true => "checked_in",
        2 => "مؤقت",
        3 => "checked_out",
        4 => "cancelled",
        _ => "مؤقت"
    };

    private static Dictionary<string, AppwriteDocument> BuildUniqueCompositeMap(
        IEnumerable<AppwriteDocument> documents,
        out HashSet<string> ambiguous)
    {
        var grouped = documents
            .Select(document => new
            {
                Document = document,
                Key = CompositeKey(
                    AppwriteSyncPrimitives.ReadString(document.Data, "roomNumber"),
                    ParseDate(AppwriteSyncPrimitives.ReadDateText(document.Data, "checkinDate")))
            })
            .Where(item => item.Key.Length > 1)
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var duplicateKeys = grouped.Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        ambiguous = duplicateKeys;
        return grouped
            .Where(group => !duplicateKeys.Contains(group.Key))
            .ToDictionary(group => group.Key, group => group.First().Document, StringComparer.OrdinalIgnoreCase);
    }

    private static string CompositeKey(string? roomNumber, DateTime checkin) =>
        $"{AppwriteSyncPrimitives.Text(roomNumber, 512).ToUpperInvariant()}|{checkin:yyyy-MM-dd}";

    private static DateTime ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed
            : DateTime.MinValue;
}
