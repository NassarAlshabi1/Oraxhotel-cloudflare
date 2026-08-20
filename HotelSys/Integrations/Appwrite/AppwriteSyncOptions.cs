#nullable enable

using System;

namespace HotelSys.Integrations.Appwrite;

/// <summary>
/// إعدادات اتصال Orax Hotel بـ Appwrite Cloud.
/// تُحمّل من القسم Appwrite في appsettings.json ويمكن تجاوزها بمتغيرات البيئة.
/// </summary>
public sealed class AppwriteSyncOptions
{
    public bool Enabled { get; set; }
    public bool AutoSyncRooms { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string RoomsCollectionId { get; set; } = "rooms";
    public int SyncIntervalMinutes { get; set; } = 15;

    public bool IsConfigured =>
        Enabled &&
        Uri.TryCreate(Endpoint, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(ProjectId) &&
        !string.IsNullOrWhiteSpace(DatabaseId) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(RoomsCollectionId);
}
