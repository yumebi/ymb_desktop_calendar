using System.IO;
using System.Text.Json;

namespace KabeCale.App.Services;

public class MemoService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private Dictionary<string, string> _memos = new();
    private bool _loaded;

    public string Get(DateOnly date)
    {
        EnsureLoaded();
        return _memos.TryGetValue(Key(date), out var text) ? text : string.Empty;
    }

    public void Set(DateOnly date, string text)
    {
        EnsureLoaded();
        var key = Key(date);
        if (string.IsNullOrWhiteSpace(text))
            _memos.Remove(key);
        else
            _memos[key] = text;
        Save();
    }

    private static string Key(DateOnly date) => date.ToString("yyyy-MM-dd");

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        AppPaths.EnsureRootDir();
        if (!File.Exists(AppPaths.MemosFile)) return;

        try
        {
            var json = File.ReadAllText(AppPaths.MemosFile);
            _memos = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch
        {
            _memos = new();
        }
    }

    private void Save()
    {
        AppPaths.EnsureRootDir();
        var json = JsonSerializer.Serialize(_memos, JsonOptions);
        File.WriteAllText(AppPaths.MemosFile, json);
    }
}
