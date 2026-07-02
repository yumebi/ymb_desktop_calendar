using System.IO;

namespace KabeCale.App.Services;

public static class AppPaths
{
    public static readonly string RootDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YmbDesktopCalendar");

    public static string SettingsFile => Path.Combine(RootDir, "settings.json");
    public static string MemosFile => Path.Combine(RootDir, "memos.json");
    public static string HolidayCacheFile(int year) => Path.Combine(RootDir, $"holidays_{year}.json");

    public static void EnsureRootDir()
    {
        Directory.CreateDirectory(RootDir);
    }
}
