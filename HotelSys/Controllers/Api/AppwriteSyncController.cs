#nullable enable

using System.Threading;
using System.Threading.Tasks;
using HotelSys.Integrations.Appwrite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HotelSys.Controllers.Api;

/// <summary>
/// نقاط تحكم إدارية لمزامنة بيانات Orax إلى Appwrite Cloud.
/// Flutter لا يستدعي هذه النقاط؛ بل يقرأ Appwrite عبر خدمته الحالية.
/// </summary>
[ApiController]
[Authorize]
[Route("api/appwrite")]
public sealed class AppwriteSyncController : ControllerBase
{
    private readonly AppwriteRoomSyncService _roomSyncService;
    private readonly AppwriteBookingSyncService _bookingSyncService;
    private readonly AppwriteGuestInfoSyncService _guestSyncService;
    private readonly AppwritePaymentSyncService _paymentSyncService;
    private readonly AppwriteSyncOptions _options;

    public AppwriteSyncController(
        AppwriteRoomSyncService roomSyncService,
        AppwriteBookingSyncService bookingSyncService,
        AppwriteGuestInfoSyncService guestSyncService,
        AppwritePaymentSyncService paymentSyncService,
        IOptions<AppwriteSyncOptions> options)
    {
        _roomSyncService = roomSyncService;
        _bookingSyncService = bookingSyncService;
        _guestSyncService = guestSyncService;
        _paymentSyncService = paymentSyncService;
        _options = options.Value;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            enabled = _options.Enabled,
            configured = _options.IsConfigured,
            autoSyncRooms = _options.AutoSyncRooms,
            autoSyncBookings = _options.AutoSyncBookings,
            autoSyncGuests = _options.AutoSyncGuests,
            autoSyncPayments = _options.AutoSyncPayments,
            collections = new
            {
                rooms = _options.RoomsCollectionId,
                bookings = _options.BookingsCollectionId,
                guests = _options.GuestInfosCollectionId,
                payments = _options.PaymentsCollectionId
            }
        });
    }

    [HttpPost("sync/rooms")]
    public async Task<IActionResult> SyncRooms(CancellationToken cancellationToken)
    {
        var result = await _roomSyncService.SyncRoomsAsync(cancellationToken);
        return result.IsDisabled
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, result)
            : Ok(result);
    }

    [HttpPost("sync/bookings")]
    public async Task<IActionResult> SyncBookings(CancellationToken cancellationToken)
    {
        var result = await _bookingSyncService.SyncBookingsAsync(cancellationToken);
        return result.IsDisabled
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, result)
            : Ok(result);
    }

    [HttpPost("sync/guests")]
    public async Task<IActionResult> SyncGuests(CancellationToken cancellationToken)
    {
        var result = await _guestSyncService.SyncGuestsAsync(cancellationToken);
        return result.IsDisabled
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, result)
            : Ok(result);
    }

    [HttpPost("sync/payments")]
    public async Task<IActionResult> SyncPayments(CancellationToken cancellationToken)
    {
        var result = await _paymentSyncService.SyncPaymentsAsync(cancellationToken);
        return result.IsDisabled
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, result)
            : Ok(result);
    }
}
