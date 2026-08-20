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
/// مزامنة دورية للحجوزات والنزلاء والمدفوعات. الغرف لها HostedService مستقل.
/// </summary>
public sealed class AppwriteCoreSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppwriteSyncOptions _options;
    private readonly ILogger<AppwriteCoreSyncHostedService> _logger;

    public AppwriteCoreSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AppwriteSyncOptions> options,
        ILogger<AppwriteCoreSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsConfigured || (!_options.AutoSyncBookings && !_options.AutoSyncGuests && !_options.AutoSyncPayments))
        {
            _logger.LogInformation("Appwrite automatic booking, guest and payment sync is disabled.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
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
                if (_options.AutoSyncBookings)
                {
                    await LogResultAsync("bookings", await scope.ServiceProvider.GetRequiredService<AppwriteBookingSyncService>().SyncBookingsAsync(stoppingToken));
                }
                if (_options.AutoSyncGuests)
                {
                    await LogResultAsync("guest_infos", await scope.ServiceProvider.GetRequiredService<AppwriteGuestInfoSyncService>().SyncGuestsAsync(stoppingToken));
                }
                if (_options.AutoSyncPayments)
                {
                    await LogResultAsync("payments", await scope.ServiceProvider.GetRequiredService<AppwritePaymentSyncService>().SyncPaymentsAsync(stoppingToken));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Appwrite automatic core sync failed; the next cycle will retry.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(_options.SyncIntervalMinutes, 1, 1440)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private Task LogResultAsync(string entity, AppwriteSyncResult result)
    {
        if (result.IsBusy)
        {
            _logger.LogWarning("Appwrite {Entity} sync skipped because another synchronization is already running.", entity);
        }
        else
        {
            _logger.LogInformation(
                "Appwrite {Entity} sync completed: source={SourceRecords}, remote={RemoteBeforeSync}, created={Created}, updated={Updated}, skipped={Skipped}, conflicts={Conflicts}, failed={Failed}",
                entity,
                result.SourceRecords,
                result.RemoteBeforeSync,
                result.Created,
                result.Updated,
                result.Skipped,
                result.Conflicts,
                result.Failed);
        }

        return Task.CompletedTask;
    }
}
