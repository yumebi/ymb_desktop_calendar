using System.IO;

namespace KabeCale.App.Services;

/// <summary>
/// 起動時にsettings.json/memos.json/events.jsonを日付フォルダへコピーしておく
/// 簡易バックアップ。同日中は再実行しても上書きしない(1日1回)。
/// 古い世代(既定7世代)を超えたバックアップフォルダは削除する。
/// 失敗しても本体動作に影響させないよう、例外は握りつぶす。
/// </summary>
public class BackupService
{
    private const int MaxGenerations = 7;

    private static readonly string[] TargetFileNames = { "settings.json", "memos.json", "events.json" };

    public void RunBackup()
    {
        try
        {
            var backupRoot = Path.Combine(AppPaths.RootDir, "backup");
            Directory.CreateDirectory(backupRoot);

            var todayDir = Path.Combine(backupRoot, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(todayDir))
            {
                Directory.CreateDirectory(todayDir);
                foreach (var fileName in TargetFileNames)
                {
                    var source = Path.Combine(AppPaths.RootDir, fileName);
                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(todayDir, fileName), overwrite: true);
                }
            }

            PruneOldBackups(backupRoot);
        }
        catch
        {
            // バックアップの失敗はアプリ本体の動作に影響させない。
        }
    }

    private static void PruneOldBackups(string backupRoot)
    {
        var directories = Directory.GetDirectories(backupRoot)
            .OrderByDescending(Path.GetFileName)
            .ToList();

        foreach (var dir in directories.Skip(MaxGenerations))
            Directory.Delete(dir, recursive: true);
    }
}
