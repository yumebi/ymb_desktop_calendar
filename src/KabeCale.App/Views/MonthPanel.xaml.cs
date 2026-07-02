using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KabeCale.App.Services;

namespace KabeCale.App.Views;

public partial class MonthPanel : UserControl
{
    public MonthPanel()
    {
        InitializeComponent();
    }

    public async Task RenderAsync(DateTime month, HolidayService holidayService, MemoService memoService, Action<DateTime> onDayClicked)
    {
        BuildWeekdayHeader();

        MonthTitleText.Text = month.ToString("yyyy年 M月", CultureInfo.GetCultureInfo("ja-JP"));

        var firstOfMonth = new DateTime(month.Year, month.Month, 1);
        var gridStart = firstOfMonth.AddDays(-(int)firstOfMonth.DayOfWeek);

        var years = new HashSet<int> { firstOfMonth.Year };
        for (var i = 0; i < 42; i++)
            years.Add(gridStart.AddDays(i).Year);

        var holidayMaps = new Dictionary<int, Dictionary<string, string>>();
        foreach (var year in years)
            holidayMaps[year] = await holidayService.GetHolidaysAsync(year);

        DaysGrid.Children.Clear();

        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var dateOnly = DateOnly.FromDateTime(date);
            var holidayName = holidayMaps[date.Year].TryGetValue(date.ToString("yyyy-MM-dd"), out var name)
                ? name
                : null;

            var cell = BuildDayCell(date, month.Month, holidayName, memoService.Get(dateOnly), onDayClicked);
            Grid.SetRow(cell, i / 7);
            Grid.SetColumn(cell, i % 7);
            DaysGrid.Children.Add(cell);
        }
    }

    private void BuildWeekdayHeader()
    {
        if (WeekdayHeader.Children.Count > 0)
            return;

        WeekdayHeader.Children.Clear();
        var names = new[] { "日", "月", "火", "水", "木", "金", "土" };
        for (var i = 0; i < names.Length; i++)
        {
            var brush = i == 0
                ? (Brush)FindResource("CalSundayBrush")
                : i == 6
                    ? (Brush)FindResource("CalSaturdayBrush")
                    : (Brush)FindResource("CalForegroundBrush");

            WeekdayHeader.Children.Add(new TextBlock
            {
                Text = names[i],
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Foreground = brush,
            });
        }
    }

    private Border BuildDayCell(DateTime date, int displayedMonth, string? holidayName, string memo, Action<DateTime> onDayClicked)
    {
        var isOtherMonth = date.Month != displayedMonth;
        var isToday = date.Date == DateTime.Today;

        var dayBrush = holidayName is not null
            ? (Brush)FindResource("CalHolidayBrush")
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
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        if (holidayName is not null && !isOtherMonth)
        {
            stack.Children.Add(new TextBlock
            {
                Text = holidayName,
                FontSize = 9,
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

        var border = new Border
        {
            Child = stack,
            Background = isToday ? (Brush)FindResource("CalTodayBrush") : Brushes.Transparent,
            BorderBrush = (Brush)FindResource("CalGridLineBrush"),
            BorderThickness = new Thickness(0.5),
            CornerRadius = new CornerRadius(4),
            Cursor = Cursors.Hand,
            ToolTip = string.IsNullOrEmpty(memo) ? null : memo,
        };
        border.MouseLeftButtonUp += (_, _) => onDayClicked(date);

        return border;
    }
}
