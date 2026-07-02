using System.IO;
using System.Text.Json;
using KabeCale.App.Models;

namespace KabeCale.App.Services;

/// <summary>
/// 日付ごとの予定一覧を events.json に永続化するサービス。
/// メモ(MemoService)とは別データとして共存する。
/// </summary>
public class EventService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private Dictionary<string, List<CalendarEvent>> _events = new();
    private bool _loaded;

    /// <summary>指定日の予定一覧を返す(なければ空)。</summary>
    public IReadOnlyList<CalendarEvent> Get(DateOnly date)
    {
        EnsureLoaded();
        return _events.TryGetValue(Key(date), out var list)
            ? list
            : Array.Empty<CalendarEvent>();
    }

    /// <summary>指定日の予定一覧を置き換えて保存する(空なら削除)。</summary>
    public void Set(DateOnly date, List<CalendarEvent> events)
    {
        EnsureLoaded();
        var key = Key(date);
        if (events.Count == 0)
            _events.Remove(key);
        else
            _events[key] = events;
        Save();
    }

    private static string Key(DateOnly date) => date.ToString("yyyy-MM-dd");

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        AppPaths.EnsureRootDir();
        if (!File.Exists(AppPaths.EventsFile)) return;

        try
        {
            var json = File.ReadAllText(AppPaths.EventsFile);
            _events = JsonSerializer.Deserialize<Dictionary<string, List<CalendarEvent>>>(json) ?? new();
        }
        catch
        {
            _events = new();
        }
    }

    private void Save()
    {
        AppPaths.EnsureRootDir();
        var json = JsonSerializer.Serialize(_events, JsonOptions);
        File.WriteAllText(AppPaths.EventsFile, json);
    }
}
