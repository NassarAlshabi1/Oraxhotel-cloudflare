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
/// نقاط تحكم إدارية لمزامنة كتالوج الغرف من Orax إلى Appwrite Cloud.
/// Flutter لا يستدعي هذه النقاط؛ بل يقرأ Appwrite عبر خدمته الحالية.
/// </summary>
[ApiController]
[Authorize]
[Route("api/appwrite")]
public sealed class AppwriteSyncController : ControllerBase
{
    private readonly AppwriteRoomSyncService _roomSyncService;
    private readonly AppwriteSyncOptions _options;

    public AppwriteSyncController(
        AppwriteRoomSyncService roomSyncService,
        IOptions<AppwriteSyncOptions> options)
    {
        _roomSyncService = roomSyncService;
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
            collection = _options.RoomsCollectionId
        });
    }

    [HttpPost("sync/rooms")]
    public async Task<IActionResult> SyncRooms(CancellationToken cancellationToken)
    {
        var result = await _roomSyncService.SyncRoomsAsync(cancellationToken);
        if (result.IsDisabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }

        return Ok(result);
    }
}
