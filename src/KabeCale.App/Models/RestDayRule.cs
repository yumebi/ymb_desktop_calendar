namespace KabeCale.App.Models;

/// <summary>休日ルールの種別。</summary>
public enum RestDayType
{
    /// <summary>公休日。</summary>
    Public,

    /// <summary>私休日。</summary>
    Private,
}

/// <summary>
/// 曜日×週序数(第1〜第5週、または毎週)の組み合わせで指定する休日ルール。
/// 例: 「毎週土曜日」「第2・第4水曜日」など、壁カレ4の休日設定を再現する。
/// </summary>
public class RestDayRule
{
    /// <summary>「毎週」を表す週序数の値。</summary>
    public const int EveryWeek = 0;

    /// <summary>対象の曜日。</summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>週序数(1〜5)。<see cref="EveryWeek"/>(0)は毎週を表す。</summary>
    public int WeekOfMonth { get; set; }

    /// <summary>公休日/私休日の種別。</summary>
    public RestDayType Type { get; set; } = RestDayType.Public;

    public RestDayRule()
    {
    }

    public RestDayRule(DayOfWeek dayOfWeek, int weekOfMonth, RestDayType type)
    {
        DayOfWeek = dayOfWeek;
        WeekOfMonth = weekOfMonth;
        Type = type;
    }
}
