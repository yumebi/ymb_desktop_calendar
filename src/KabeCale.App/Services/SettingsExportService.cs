using System.IO;
using System.Text.Json;

namespace KabeCale.App.Services;

/// <summary>
/// settings.json / memos.json / events.json を1つのJSONファイルにまとめて
/// エクスポート/インポートする。
/// </summary>
public static class SettingsExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private class ExportBundle
    {
        public string? Settings { get; set; }
        public string? Memos { get; set; }
        public string? Events { get; set; }
    }

    public static void Export(string filePath)
    {
        var bundle = new ExportBundle
        {
            Settings = ReadIfExists(AppPaths.SettingsFile),
            Memos = ReadIfExists(AppPaths.MemosFile),
            Events = ReadIfExists(AppPaths.EventsFile),
        };

        var json = JsonSerializer.Serialize(bundle, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static void Import(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var bundle = JsonSerializer.Deserialize<ExportBundle>(json)
            ?? throw new InvalidDataException("不正なファイル形式です。");

        AppPaths.EnsureRootDir();

        if (bundle.Settings is not null)
            File.WriteAllText(AppPaths.SettingsFile, bundle.Settings);
        if (bundle.Memos is not null)
            File.WriteAllText(AppPaths.MemosFile, bundle.Memos);
        if (bundle.Events is not null)
            File.WriteAllText(AppPaths.EventsFile, bundle.Events);
    }

    private static string? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
}
