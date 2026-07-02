using KabeCale.App.Models;

namespace KabeCale.App.Services;

/// <summary>
/// 曜日×週序数で指定された休日ルールに基づき、指定日が休日(公休日/私休日)に
/// 該当するかどうかを判定するサービス。
/// </summary>
public class RestDayService
{
    /// <summary>
    /// 指定日がいずれかのルールに該当するかを判定し、該当する種別を返す(該当なしはnull)。
    /// 公休日と私休日の両方に該当する場合は公休日を優先する。
    /// </summary>
    public RestDayType? GetRestDayType(DateOnly date, IEnumerable<RestDayRule> rules)
    {
        var matchedAny = false;
        var matchedPublic = false;

        foreach (var rule in rules)
        {
            if (!Matches(date, rule))
                continue;

            matchedAny = true;
            if (rule.Type == RestDayType.Public)
                matchedPublic = true;
        }

        if (!matchedAny)
            return null;

        return matchedPublic ? RestDayType.Public : RestDayType.Private;
    }

    /// <summary>指定日がいずれかのルールに該当する休日かどうか。</summary>
    public bool IsRestDay(DateOnly date, IEnumerable<RestDayRule> rules) => GetRestDayType(date, rules) is not null;

    private static bool Matches(DateOnly date, RestDayRule rule)
    {
        if (date.DayOfWeek != rule.DayOfWeek)
            return false;

        return rule.WeekOfMonth == RestDayRule.EveryWeek || GetWeekOfMonth(date) == rule.WeekOfMonth;
    }

    /// <summary>その月における同一曜日の何回目にあたるか(第1週=1)を返す。</summary>
    private static int GetWeekOfMonth(DateOnly date) => (date.Day - 1) / 7 + 1;
}
