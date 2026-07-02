using System.Globalization;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace KabeCale.App.Models;

/// <summary>
/// 1日に複数登録できる予定1件分。
/// </summary>
public class CalendarEvent
{
    /// <summary>件名(必須)。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>開始時刻("HH:mm" 形式。未指定は null)。</summary>
    public string? StartTime { get; set; }

    /// <summary>表示色(EventColorPalette のキー)。</summary>
    public string Color { get; set; } = EventColorPalette.DefaultKey;

    /// <summary>備考(任意・複数行可)。</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>「9:00 会議」形式の表示文字列。時刻がなければ件名のみ。</summary>
    [JsonIgnore]
    public string DisplayText =>
        TimeOnly.TryParse(StartTime, CultureInfo.InvariantCulture, out var time)
            ? $"{time.ToString("H:mm", CultureInfo.InvariantCulture)} {Title}"
            : Title;

    public CalendarEvent Clone() => new()
    {
        Title = Title,
        StartTime = StartTime,
        Color = Color,
        Note = Note,
    };
}

/// <summary>
/// 予定に使える定義済みカラーパレット。
/// </summary>
public static class EventColorPalette
{
    public record ColorOption(string Key, string Label, string Hex);

    public const string DefaultKey = "Blue";

    public static readonly IReadOnlyList<ColorOption> Options = new ColorOption[]
    {
        new("Red", "赤", "#E53935"),
        new("Orange", "橙", "#FB8C00"),
        new("Green", "緑", "#43A047"),
        new("Blue", "青", "#1E88E5"),
        new("Purple", "紫", "#8E24AA"),
        new("Gray", "グレー", "#757575"),
    };

    private static readonly Dictionary<string, SolidColorBrush> BrushCache = new();

    /// <summary>色キーに対応するBrushを返す。未知のキーはグレー扱い。</summary>
    public static SolidColorBrush GetBrush(string key)
    {
        if (BrushCache.TryGetValue(key, out var cached))
            return cached;

        var option = Options.FirstOrDefault(o => o.Key == key)
                     ?? Options.First(o => o.Key == "Gray");
        var brush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(option.Hex));
        brush.Freeze();
        BrushCache[key] = brush;
        return brush;
    }
}
