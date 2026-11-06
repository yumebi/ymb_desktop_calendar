using System.IO;
using Microsoft.Win32;

namespace KabeCale.App.Services;

/// <summary>
/// Windows起動時の自動起動を、スタートアップフォルダのショートカットで管理する。
/// Runキーに登録した値だけがExplorerに無視されログオン時に起動しない環境があったため、
/// 確実に処理されるスタートアップフォルダ方式にしている。
/// </summary>
public class StartupService
{
    private const string LegacyRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string LegacyValueName = "YmbDesktopCalendar";
    private const string ShortcutName = "YMBデスクトップカレンダー.lnk";

    private static string ShortcutPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ShortcutName);

    public bool IsEnabled() => File.Exists(ShortcutPath);

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            DeleteShortcut();
            DeleteLegacyRunValue();
            return;
        }

        // 開発ビルド(bin\Debug等)から登録すると、以後ずっとそのビルドが自動起動してしまうので登録しない。
        var exePath = Environment.ProcessPath;
        if (exePath is null || IsDevBuildPath(exePath))
            return;

        CreateShortcut(exePath);
        DeleteLegacyRunValue();
    }

    /// <summary>
    /// 旧方式(Runキー)で登録済みの場合、スタートアップフォルダ方式へ移行する。
    /// 登録先が開発ビルドのパスだった場合は、ショートカットを作らず古い値だけ消す。
    /// </summary>
    public void MigrateLegacyRunEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(LegacyRunKeyPath, writable: false);
        var value = key?.GetValue(LegacyValueName) as string;
        if (string.IsNullOrWhiteSpace(value))
            return;

        var target = value.Trim().Trim('"');
        if (!IsDevBuildPath(target) && File.Exists(target) && !File.Exists(ShortcutPath))
            CreateShortcut(target);

        DeleteLegacyRunValue();
    }

    private static bool IsDevBuildPath(string path) =>
        path.Contains(@"\bin\Debug\", StringComparison.OrdinalIgnoreCase)
        || path.Contains(@"\bin\Release\", StringComparison.OrdinalIgnoreCase);

    private static void CreateShortcut(string exePath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
            return;

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic link = shell.CreateShortcut(ShortcutPath);
        link.TargetPath = exePath;
        link.WorkingDirectory = Path.GetDirectoryName(exePath);
        link.Description = "YMBデスクトップカレンダー";
        link.Save();
    }

    private static void DeleteShortcut()
    {
        if (File.Exists(ShortcutPath))
            File.Delete(ShortcutPath);
    }

    private static void DeleteLegacyRunValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(LegacyRunKeyPath, writable: true);
        key?.DeleteValue(LegacyValueName, throwOnMissingValue: false);
    }
}
