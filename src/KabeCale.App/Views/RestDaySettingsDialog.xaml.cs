using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KabeCale.App.Models;

namespace KabeCale.App.Views;

/// <summary>
/// 曜日×週序数のチェックマトリクスで公休日/私休日を指定するダイアログ(壁カレ4の
/// 休日設定画面を再現)。OKでチェック状態からルール一覧を組み立てて呼び出し元に返す。
/// </summary>
public partial class RestDaySettingsDialog : Window
{
    /// <summary>マトリクスの行(週序数、表示ラベル)。0番目は「毎週」。</summary>
    private static readonly (int Week, string Label)[] WeekRows =
    {
        (RestDayRule.EveryWeek, "毎週"),
        (1, "第1週"),
        (2, "第2週"),
        (3, "第3週"),
        (4, "第4週"),
        (5, "第5週"),
    };

    /// <summary>マトリクスの列(曜日)。日曜始まりで表示する。</summary>
    private static readonly DayOfWeek[] DayColumns =
    {
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday,
    };

    private static readonly string[] DayLabels = { "日", "月", "火", "水", "木", "金", "土" };

    public List<RestDayRule> ResultRules { get; private set; } = new();

    private readonly Dictionary<(RestDayType Type, int Week, DayOfWeek Day), CheckBox> _checkBoxes = new();

    public RestDaySettingsDialog(IEnumerable<RestDayRule> currentRules)
    {
        InitializeComponent();

        BuildMatrix(PublicGrid, RestDayType.Public);
        BuildMatrix(PrivateGrid, RestDayType.Private);
        ApplyRules(currentRules);
    }

    /// <summary>曜日(列)×週序数(行)のチェックボックスマトリクスを組み立てる。</summary>
    private void BuildMatrix(Grid grid, RestDayType type)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        for (var c = 0; c < DayColumns.Length; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var r = 0; r < WeekRows.Length; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var c = 0; c < DayColumns.Length; c++)
        {
            var header = new TextBlock
            {
                Text = DayLabels[c],
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("CalForegroundBrush"),
                Margin = new Thickness(0, 0, 0, 4),
            };
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, c + 1);
            grid.Children.Add(header);
        }

        for (var r = 0; r < WeekRows.Length; r++)
        {
            var (week, label) = WeekRows[r];

            var rowLabel = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("CalForegroundBrush"),
            };
            Grid.SetRow(rowLabel, r + 1);
            Grid.SetColumn(rowLabel, 0);
            grid.Children.Add(rowLabel);

            for (var c = 0; c < DayColumns.Length; c++)
            {
                var dow = DayColumns[c];
                var checkBox = new CheckBox
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 2),
                };
                Grid.SetRow(checkBox, r + 1);
                Grid.SetColumn(checkBox, c + 1);
                grid.Children.Add(checkBox);
                _checkBoxes[(type, week, dow)] = checkBox;
            }
        }
    }

    /// <summary>現在の設定値をチェック状態に反映する。</summary>
    private void ApplyRules(IEnumerable<RestDayRule> rules)
    {
        foreach (var rule in rules)
        {
            if (_checkBoxes.TryGetValue((rule.Type, rule.WeekOfMonth, rule.DayOfWeek), out var checkBox))
                checkBox.IsChecked = true;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        ResultRules = _checkBoxes
            .Where(kvp => kvp.Value.IsChecked == true)
            .Select(kvp => new RestDayRule(kvp.Key.Day, kvp.Key.Week, kvp.Key.Type))
            .ToList();

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
