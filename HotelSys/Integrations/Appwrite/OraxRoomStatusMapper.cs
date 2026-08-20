#nullable enable

namespace HotelSys.Integrations.Appwrite;

/// <summary>
/// تحويل حالات Orax الرقمية إلى القيم المعيارية التي يستخدمها Flutter.
/// مصدر الرموز هو Status_RoomsName.listStatus في Orax:
/// 1 فارغة، 2 تنضيف، 3 صيانة، 4 حجز بدون تسجيل دخول، 5 مشغولة.
/// </summary>
public static class OraxRoomStatusMapper
{
    public static string ToFlutterStatus(string? oraxStatus)
    {
        return Normalize(oraxStatus) switch
        {
            "1" => "شاغرة",
            "2" => "cleaning",
            "3" => "maintenance",
            "4" => "مؤقت",
            "5" => "محجوزة",
            "فارغة" => "شاغرة",
            "تنضيف" => "cleaning",
            "تنظيف" => "cleaning",
            "صيانة" => "maintenance",
            "حجز بدون تسجيل دخول" => "مؤقت",
            "شاغرة" => "شاغرة",
            "مشغولة" => "محجوزة",
            "محجوزة" => "محجوزة",
            _ => "شاغرة"
        };
    }

    public static bool RequiresMaintenance(string? oraxStatus, bool? existingValue)
    {
        var normalized = Normalize(oraxStatus);
        return normalized switch
        {
            "3" or "صيانة" => true,
            "1" or "2" or "4" or "5" or "فارغة" or "تنضيف" or "تنظيف" or "حجز بدون تسجيل دخول" or "شاغرة" or "مشغولة" or "محجوزة" => false,
            _ => existingValue ?? false
        };
    }

    public static string ResolveCleaningStatus(string? oraxStatus, string? existingValue)
    {
        return Normalize(oraxStatus) switch
        {
            "2" or "تنضيف" or "تنظيف" => "cleaning",
            _ => string.IsNullOrWhiteSpace(existingValue) ? "clean" : existingValue
        };
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();
}
