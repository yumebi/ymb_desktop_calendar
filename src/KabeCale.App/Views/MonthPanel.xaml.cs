using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KabeCale.App.Models;
using KabeCale.App.Services;

namespace KabeCale.App.Views;

public partial class MonthPanel : UserControl
{
    /// <summary>セル内に直接表示する予定の最大件数(超過分は「+n件」表記)。</summary>
    private const int MaxEventsPerCell = 3;

    /// <summary>DayOfWeek(Sunday=0)順の曜日名。開始曜日設定に応じて並べ替えて使う。</summary>
    private static readonly string[] DayNamesByDow = { "日", "月", "火", "水", "木", "金", "土" };

    /// <summary>曜日ヘッダーを構築済みの開始曜日(変更時のみ再構築するためのキャッシュキー)。</summary>
    private DayOfWeek? _headerStartDow;

    /// <summary>直近のRenderAsyncで渡された設定(フォント倍率などをヘルパーメソッドから参照するため保持)。</summary>
    private AppSettings _settings = new();

    /// <summary>曜日×週序数ルールから休日(公休日/私休日)を判定するサービス。</summary>
    private readonly RestDayService _restDayService = new();

    public MonthPanel()
    {
        InitializeComponent();
    }

    public async Task RenderAsync(DateTime month, HolidayService holidayService, MemoService memoService, EventService eventService, Action<DateTime> onDayClicked, AppSettings settings)
    {
        _settings = settings;

        var startDow = settings.FirstDayOfWeek == "Monday" ? DayOfWeek.Monday : DayOfWeek.Sunday;
        BuildWeekdayHeader(startDow, settings.ShowWeekNumbers);

        MonthTitleText.Text = month.ToString("yyyy年 M月", CultureInfo.GetCultureInfo("ja-JP"));
        MonthTitleText.FontSize = 14 * settings.FontScale;

        var firstOfMonth = new DateTime(month.Year, month.Month, 1);
        var offset = ((int)firstOfMonth.DayOfWeek - (int)startDow + 7) % 7;
        var gridStart = firstOfMonth.AddDays(-offset);

        var years = new HashSet<int> { firstOfMonth.Year };
        for (var i = 0; i < 42; i++)
            years.Add(gridStart.AddDays(i).Year);

        var holidayMaps = new Dictionary<int, Dictionary<string, string>>();
        foreach (var year in years)
            holidayMaps[year] = await holidayService.GetHolidaysAsync(year);

        DaysGrid.Children.Clear();
        WeekNumberColumn.Width = new GridLength(settings.ShowWeekNumbers ? 24 : 0);

        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var dateOnly = DateOnly.FromDateTime(date);
            var holidayName = holidayMaps[date.Year].TryGetValue(date.ToString("yyyy-MM-dd"), out var name)
                ? name
                : null;

            var cell = BuildDayCell(date, month.Month, holidayName, memoService.Get(dateOnly), eventService.Get(dateOnly), onDayClicked);
            Grid.SetRow(cell, i / 7);
            Grid.SetColumn(cell, i % 7 + 1);
            DaysGrid.Children.Add(cell);

            if (settings.ShowWeekNumbers && i % 7 == 0)
            {
                var weekText = new TextBlock
                {
                    Text = ISOWeek.GetWeekOfYear(date).ToString(),
                    FontSize = 9 * settings.FontScale,
                    Foreground = (Brush)FindResource("CalOtherMonthBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetRow(weekText, i / 7);
                Grid.SetColumn(weekText, 0);
                DaysGrid.Children.Add(weekText);
            }
        }
    }

    /// <summary>
    /// 曜日ヘッダーを構築する。開始曜日が変わらない限り再構築しない(週番号列幅は
    /// 表示ON/OFFの都度変わりうるため、キャッシュの有無に関わらず毎回更新する)。
    /// </summary>
    private void BuildWeekdayHeader(DayOfWeek startDow, bool showWeekNumbers)
    {
        WeekNumberHeaderColumn.Width = new GridLength(showWeekNumbers ? 24 : 0);

        if (WeekdayHeader.Children.Count > 0 && _headerStartDow == startDow)
            return;

        _headerStartDow = startDow;
        WeekdayHeader.Children.Clear();

        for (var i = 0; i < 7; i++)
        {
            var dow = (DayOfWeek)(((int)startDow + i) % 7);
            var brush = dow == DayOfWeek.Sunday
                ? (Brush)FindResource("CalSundayBrush")
                : dow == DayOfWeek.Saturday
                    ? (Brush)FindResource("CalSaturdayBrush")
                    : (Brush)FindResource("CalForegroundBrush");

            var text = new TextBlock
            {
                Text = DayNamesByDow[(int)dow],
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12 * _settings.FontScale,
                Foreground = brush,
            };
            Grid.SetColumn(text, i + 1);
            WeekdayHeader.Children.Add(text);
        }
    }

    private Border BuildDayCell(DateTime date, int displayedMonth, string? holidayName, string memo, IReadOnlyList<CalendarEvent> events, Action<DateTime> onDayClicked)
    {
        var isOtherMonth = date.Month != displayedMonth;
        var isToday = date.Date == DateTime.Today;

        // 優先順位: 祝日 > 公休日 > 私休日 > 日曜 > 土曜 > 平日
        var restDayType = _restDayService.GetRestDayType(DateOnly.FromDateTime(date), _settings.RestDayRules);

        var dayBrush = holidayName is not null
            ? (Brush)FindResource("CalHolidayBrush")
            : restDayType == RestDayType.Public
                ? (Brush)FindResource("CalHolidayBrush")
                : restDayType == RestDayType.Private
                    ? (Brush)FindResource("CalPrivateRestBrush")
                    : date.DayOfWeek == DayOfWeek.Sunday
                        ? (Brush)FindResource("CalSundayBrush")
                        : date.DayOfWeek == DayOfWeek.Saturday
                            ? (Brush)FindResource("CalSaturdayBrush")
                            : (Brush)FindResource("CalForegroundBrush");

        if (isOtherMonth)
            dayBrush = (Brush)FindResource("CalOtherMonthBrush");

        var stack = new StackPanel { Margin = new Thickness(2) };
        stack.Children.Add(new TextBlock
        {
            Text = date.Day.ToString(),
            Foreground = dayBrush,
            FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
            FontSize = 12 * _settings.FontScale,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        if (holidayName is not null && !isOtherMonth)
        {
            stack.Children.Add(new TextBlock
            {
                Text = holidayName,
                FontSize = 9 * _settings.FontScale,
                Foreground = dayBrush,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
            });
        }

        if (!string.IsNullOrEmpty(memo))
        {
            stack.Children.Add(new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = (Brush)FindResource("CalForegroundBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }

        // 予定を最大 MaxEventsPerCell 件まで小さく直接表示。超過分は「+n件」。
        var sortedEvents = SortForDisplay(events);
        foreach (var ev in sortedEvents.Take(MaxEventsPerCell))
        {
            stack.Children.Add(new TextBlock
            {
                Text = ev.DisplayText,
                FontSize = 9 * _settings.FontScale,
                Foreground = EventColorPalette.GetBrush(ev.Color),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 0),
            });
        }
        if (events.Count > MaxEventsPerCell)
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"+{events.Count - MaxEventsPerCell}件",
                FontSize = 9 * _settings.FontScale,
                Foreground = (Brush)FindResource("CalForegroundBrush"),
                Margin = new Thickness(0, 1, 0, 0),
            });
        }

        var border = new Border
        {
            Child = stack,
            Background = isToday ? (Brush)FindResource("CalTodayBrush") : Brushes.Transparent,
            BorderBrush = (Brush)FindResource("CalGridLineBrush"),
            BorderThickness = new Thickness(0.5),
            CornerRadius = new CornerRadius(4),
            Cursor = Cursors.Hand,
            ToolTip = BuildToolTip(date, holidayName, restDayType, memo, sortedEvents),
        };
        border.MouseLeftButtonUp += (_, _) => onDayClicked(date);

        return border;
    }

    /// <summary>時刻付きの予定を時刻順に先へ、時刻なしはその後ろに並べる。</summary>
    private static List<CalendarEvent> SortForDisplay(IReadOnlyList<CalendarEvent> events)
        => events
            .OrderBy(ev => !TimeOnly.TryParse(ev.StartTime, CultureInfo.InvariantCulture, out _))
            .ThenBy(ev => TimeOnly.TryParse(ev.StartTime, CultureInfo.InvariantCulture, out var t) ? t : TimeOnly.MaxValue)
            .ToList();

    /// <summary>
    /// 休日区分(祝日名/公休日/私休日)+メモ+予定一覧を整形したToolTipコンテンツを組み立てる。
    /// いずれもない日は null(ToolTipなし)。
    /// </summary>
    private static object? BuildToolTip(DateTime date, string? holidayName, RestDayType? restDayType, string memo, IReadOnlyList<CalendarEvent> sortedEvents)
    {
        var hasMemo = !string.IsNullOrEmpty(memo);
        var restDayLabel = holidayName ?? (restDayType == RestDayType.Public ? "公休日" : restDayType == RestDayType.Private ? "私休日" : null);
        if (!hasMemo && sortedEvents.Count == 0 && restDayLabel is null)
            return null;

        var panel = new StackPanel { MaxWidth = 280 };
        panel.Children.Add(new TextBlock
        {
            Text = date.ToString("yyyy年M月d日(ddd)", CultureInfo.GetCultureInfo("ja-JP")),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 4),
        });

        if (restDayLabel is not null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = restDayLabel,
                Margin = new Thickness(0, 0, 0, hasMemo || sortedEvents.Count > 0 ? 4 : 0),
            });
        }

        if (hasMemo)
        {
            panel.Children.Add(new TextBlock
            {
                Text = memo,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, sortedEvents.Count > 0 ? 6 : 0),
            });
        }

        foreach (var ev in sortedEvents)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
            line.Children.Add(new Rectangle
            {
                Width = 8,
                Height = 8,
                RadiusX = 2,
                RadiusY = 2,
                Fill = EventColorPalette.GetBrush(ev.Color),
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            line.Children.Add(new TextBlock
            {
                Text = ev.DisplayText,
                TextWrapping = TextWrapping.Wrap,
            });
            panel.Children.Add(line);

            if (!string.IsNullOrWhiteSpace(ev.Note))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = ev.Note,
                    FontSize = 10,
                    Opacity = 0.75,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(13, 0, 0, 2),
                });
            }
        }

        return panel;
    }
}
