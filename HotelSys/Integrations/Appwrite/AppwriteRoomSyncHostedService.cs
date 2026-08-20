#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelSys.Integrations.Appwrite;

/// <summary>
/// يشغّل مزامنة الغرف دورياً عند تفعيل AutoSyncRooms.
/// الفشل في Appwrite لا يوقف تطبيق Orax؛ تُسجّل المشكلة وتستمر الدورة التالية.
/// </summary>
public sealed class AppwriteRoomSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwriteRoomSyncHostedService> _logger;

    public AppwriteRoomSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwriteRoomSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsConfigured || !_options.AutoSyncRooms)
        {
            _logger.LogInformation("Appwrite automatic room sync is disabled.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<AppwriteRoomSyncService>();
                var result = await syncService.SyncRoomsAsync(stoppingToken);
                if (result.IsBusy)
                {
                    _logger.LogWarning("Appwrite room sync skipped because another room sync is already running.");
                }
                else
                {
                    _logger.LogInformation(
                        "Appwrite room sync completed: created={Created}, updated={Updated}, skipped={Skipped}, failed={Failed}",
                        result.Created,
                        result.Updated,
                        result.Skipped,
                        result.Failed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Appwrite automatic room sync failed; the next cycle will retry.");
            }

            var minutes = Math.Clamp(_options.SyncIntervalMinutes, 1, 1440);
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
