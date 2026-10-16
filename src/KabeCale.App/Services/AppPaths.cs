using System.IO;

namespace KabeCale.App.Services;

public static class AppPaths
{
    /// <summary>開発・動作確認用。設定すると、設定フォルダをこのフォルダに切り替える。</summary>
    public const string DataDirEnvVar = "YMB_KABECALE_DATA_DIR";

    // 実際の設定に触れずに起動して確かめられるよう、環境変数で保存先を変えられる。
    public static readonly string RootDir =
        Environment.GetEnvironmentVariable(DataDirEnvVar) is { Length: > 0 } overrideDir
            ? overrideDir
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YmbDesktopCalendar");

    public static string SettingsFile => Path.Combine(RootDir, "settings.json");
    public static string MemosFile => Path.Combine(RootDir, "memos.json");
    public static string EventsFile => Path.Combine(RootDir, "events.json");
    public static string HolidayCacheFile(int year) => Path.Combine(RootDir, $"holidays_{year}.json");

    public static void EnsureRootDir()
    {
        Directory.CreateDirectory(RootDir);
    }
}
