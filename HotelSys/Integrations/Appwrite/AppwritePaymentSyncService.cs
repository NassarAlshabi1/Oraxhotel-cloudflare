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
/// مزامنة الدفعات الفعلية من BillsTable المرتبطة بحجز فقط.
/// الفاتورة ذات PayAmount غير الموجب لا تُنشر إلى payments.
/// </summary>
public sealed class AppwritePaymentSyncService
{
    private readonly AppwriteRestClient _client;
    private readonly HotelAlkheerDB _db;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwritePaymentSyncService> _logger;

    public AppwritePaymentSyncService(
        AppwriteRestClient client,
        HotelAlkheerDB db,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwritePaymentSyncService> logger)
    {
        _client = client;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppwriteSyncResult> SyncPaymentsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return AppwriteSyncResult.Disabled("payments", "Appwrite integration is disabled or incomplete.");
        }

        var remoteDocuments = (await _client.ListDocumentsAsync(_options.PaymentsCollectionId, cancellationToken)).Documents.ToList();
        var serverDocuments = remoteDocuments.Where(AppwriteSyncPrimitives.IsServerOwned).ToList();
        var remoteByServerId = AppwriteSyncPrimitives.UniqueByLong(serverDocuments, "serverPaymentId", out var ambiguousServerIds);
        var compositeCandidates = remoteDocuments
            .Where(document => AppwriteSyncPrimitives.ReadInt64(document.Data, "serverPaymentId") is null)
            .Where(document => AppwriteSyncPrimitives.IsServerOwned(document)
                || (string.IsNullOrWhiteSpace(AppwriteSyncPrimitives.ReadString(document.Data, "origin"))
                    && string.IsNullOrWhiteSpace(AppwriteSyncPrimitives.ReadString(document.Data, "sync_origin"))))
            .ToList();
        var remoteByComposite = BuildUniquePaymentMap(compositeCandidates, out var ambiguousComposites);
        var receptions = _db.RecetionTables.ToList().ToDictionary(reception => reception.Id);
        var rooms = _db.RoomsTables.ToList().ToDictionary(room => room.Id);
        var bills = _db.BillsTables
            .ToList()
            .Where(bill => bill.IdReception.HasValue && bill.PayAmount.GetValueOrDefault() > 0)
            .ToList();
        var result = new AppwriteSyncResult { Entity = "payments", RemoteBeforeSync = remoteDocuments.Count, SourceRecords = bills.Count };

        foreach (var bill in bills)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var receptionId = bill.IdReception!.Value;
            if (!receptions.TryGetValue(receptionId, out var reception))
            {
                result.Skipped++;
                _logger.LogWarning("Payment sync skipped BillsTable {BillId}: reception {ReceptionId} was not found.", bill.Id, receptionId);
                continue;
            }

            var roomNumber = rooms.TryGetValue(reception.IdRoom, out var room)
                ? AppwriteSyncPrimitives.Text(room.NameR, 50)
                : string.Empty;
            var paymentDate = bill.Date;
            var composite = PaymentCompositeKey(roomNumber, paymentDate, bill.PayAmount.GetValueOrDefault());

            if (ambiguousServerIds.Contains(bill.Id))
            {
                result.Conflicts++;
                _logger.LogWarning("Payment sync skipped BillsTable {BillId}: duplicate remote serverPaymentId.", bill.Id);
                continue;
            }

            remoteByServerId.TryGetValue(bill.Id, out var remote);
            if (remote is null && composite.Length > 0)
            {
                if (ambiguousComposites.Contains(composite))
                {
                    result.Conflicts++;
                    _logger.LogWarning("Payment sync skipped BillsTable {BillId}: duplicate remote payment identity {Composite}.", bill.Id, composite);
                    continue;
                }

                remoteByComposite.TryGetValue(composite, out remote);
            }

            var documentId = remote?.Id ?? $"orax-payment-{bill.Id}";
            var now = DateTimeOffset.UtcNow;
            var nowSeconds = now.ToUnixTimeSeconds();
            var payload = new Dictionary<string, object?>
            {
                ["localUuid"] = AppwriteSyncPrimitives.DeterministicUuid("payment", bill.Id),
                ["serverPaymentId"] = bill.Id,
                ["serverId"] = bill.Id,
                ["serverBookingId"] = receptionId,
                ["bookingUuidCache"] = AppwriteSyncPrimitives.DeterministicUuid("booking", receptionId),
                ["roomNumber"] = roomNumber,
                ["amount"] = bill.PayAmount.GetValueOrDefault(),
                ["paymentDate"] = AppwriteSyncPrimitives.Iso(paymentDate),
                ["paymentMethod"] = AppwriteSyncPrimitives.Text(bill.TypePay, 255),
                ["revenueType"] = "room",
                ["referenceNumber"] = AppwriteSyncPrimitives.Text(bill.NumReference, 100),
                ["notes"] = AppwriteSyncPrimitives.Text(bill.Note, 500),
                ["hotelDayKey"] = AppwriteSyncPrimitives.HotelDay(paymentDate),
                ["isPendingBalance"] = bill.RestAmount.GetValueOrDefault() > 0,
                ["isVoided"] = false,
                ["isImmutable"] = true,
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
                ["idempotencyKey"] = $"orax:payments:{bill.Id}:{nowSeconds}"
            };

            try
            {
                await _client.UpsertDocumentAsync(_options.PaymentsCollectionId, documentId, payload, cancellationToken);
                if (remote is null) result.Created++; else result.Updated++;
            }
            catch (Exception exception)
            {
                result.Failed++;
                _logger.LogError(exception, "Appwrite payment sync failed for BillsTable {BillId}.", bill.Id);
            }
        }

        return result;
    }

    private static Dictionary<string, AppwriteDocument> BuildUniquePaymentMap(
        IEnumerable<AppwriteDocument> documents,
        out HashSet<string> ambiguous)
    {
        var grouped = documents
            .Select(document => new
            {
                Document = document,
                Key = PaymentCompositeKey(
                    AppwriteSyncPrimitives.ReadString(document.Data, "roomNumber"),
                    ParseDate(AppwriteSyncPrimitives.ReadDateText(document.Data, "paymentDate")),
                    AppwriteSyncPrimitives.ReadDouble(document.Data, "amount") ?? 0d)
            })
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var duplicateKeys = grouped.Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        ambiguous = duplicateKeys;
        return grouped
            .Where(group => !duplicateKeys.Contains(group.Key))
            .ToDictionary(group => group.Key, group => group.First().Document, StringComparer.OrdinalIgnoreCase);
    }

    private static string PaymentCompositeKey(string? roomNumber, DateTime paymentDate, double amount)
    {
        if (string.IsNullOrWhiteSpace(roomNumber) || paymentDate == DateTime.MinValue) return string.Empty;
        return $"{AppwriteSyncPrimitives.Text(roomNumber, 50).ToUpperInvariant()}|{paymentDate:yyyy-MM-ddTHH:mm:ss}|{amount.ToString("G17", CultureInfo.InvariantCulture)}";
    }

    private static DateTime ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed
            : DateTime.MinValue;
}
