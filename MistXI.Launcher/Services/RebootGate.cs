using System.Management;

namespace MistXI.Launcher.Services;

public sealed class RebootGate
{
    public DateTimeOffset? GetLastBootTimeUtc()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem WHERE Primary='true'");
            foreach (var mo in searcher.Get())
            {
                var raw = mo["LastBootUpTime"]?.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var dtLocal = ManagementDateTimeConverter.ToDateTime(raw);
                    var dto = new DateTimeOffset(DateTime.SpecifyKind(dtLocal, DateTimeKind.Local));
                    return dto.ToUniversalTime();
                }
            }
        }
        catch { }
        return null;
    }

    public bool HasRebootedSince(DateTimeOffset? markerUtc)
    {
        if (markerUtc is null) return true;
        var boot = GetLastBootTimeUtc();
        if (boot is null) return false;
        return boot > markerUtc.Value;
    }
}
