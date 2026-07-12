using System.Reflection;
using System.Windows;
using KabeCale.App.Models;
using KabeCale.App.Services;
using Microsoft.Win32;

namespace KabeCale.App.Views;

public partial class SettingsWindow : Window
{
    private record LayoutOption(string Label, string Value);

    private static readonly LayoutOption[] LayoutOptions =
    {
        new("横並び", "Horizontal"),
        new("縦並び", "Vertical"),
    };

    private record FirstDayOption(string Label, string Value);

    private static readonly FirstDayOption[] FirstDayOptions =
    {
        new("日曜始まり", "Sunday"),
        new("月曜始まり", "Monday"),
    };

    private record FontScaleOption(string Label, double Value);

    private static readonly FontScaleOption[] FontScaleOptions =
    {
        new("小", 0.85),
        new("標準", 1.0),
        new("大", 1.15),
        new("特大", 1.3),
    };

    public AppSettings ResultSettings { get; private set; }

    private readonly AppSettings _original;
    private readonly StartupService _startupService = new();
    private List<RestDayRule> _restDayRules;

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();
        _original = current;

        ThemeComboBox.ItemsSource = ThemeService.AvailableThemes;
        ThemeComboBox.SelectedItem = current.ThemeName;

        MonthCountComboBox.ItemsSource = new[] { 1, 2, 3, 6, 12 };
        MonthCountComboBox.SelectedItem = current.MonthCount;

        LayoutDirectionComboBox.ItemsSource = LayoutOptions;
        LayoutDirectionComboBox.DisplayMemberPath = "Label";
        LayoutDirectionComboBox.SelectedValuePath = "Value";
        LayoutDirectionComboBox.SelectedValue = current.MonthLayoutDirection;

        FirstDayOfWeekComboBox.ItemsSource = FirstDayOptions;
        FirstDayOfWeekComboBox.DisplayMemberPath = "Label";
        FirstDayOfWeekComboBox.SelectedValuePath = "Value";
        FirstDayOfWeekComboBox.SelectedValue = current.FirstDayOfWeek;

        FontScaleComboBox.ItemsSource = FontScaleOptions;
        FontScaleComboBox.DisplayMemberPath = "Label";
        FontScaleComboBox.SelectedValuePath = "Value";
        FontScaleComboBox.SelectedValue = current.FontScale;

        BackgroundOpacitySlider.Value = current.BackgroundOpacity;
        UpdateBackgroundOpacityValueText(current.BackgroundOpacity);

        ShowWeekNumbersCheckBox.IsChecked = current.ShowWeekNumbers;
        PinToDesktopCheckBox.IsChecked = current.PinToDesktop;
        ClickThroughCheckBox.IsChecked = current.ClickThrough;
        LaunchAtStartupCheckBox.IsChecked = _startupService.IsEnabled();

        _restDayRules = current.RestDayRules
            .Select(r => new RestDayRule(r.DayOfWeek, r.WeekOfMonth, r.Type))
            .ToList();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? string.Empty : $"v{version.Major}.{version.Minor}.{version.Build}";

        ResultSettings = Clone(current);
    }

    private void RestDaySettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RestDaySettingsDialog(_restDayRules) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _restDayRules = dialog.ResultRules;
        }
    }

    private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateBackgroundOpacityValueText(e.NewValue);
    }

    private void UpdateBackgroundOpacityValueText(double value)
    {
        if (BackgroundOpacityValueText is not null)
            BackgroundOpacityValueText.Text = $"{value * 100:0}%";
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        ResultSettings = new AppSettings
        {
            ThemeName = ThemeComboBox.SelectedItem as string ?? _original.ThemeName,
            WindowLeft = _original.WindowLeft,
            WindowTop = _original.WindowTop,
            WindowWidth = _original.WindowWidth,
            WindowHeight = _original.WindowHeight,
            PinToDesktop = PinToDesktopCheckBox.IsChecked ?? true,
            ClickThrough = ClickThroughCheckBox.IsChecked ?? false,
            MonthCount = MonthCountComboBox.SelectedItem as int? ?? 1,
            MonthLayoutDirection = LayoutDirectionComboBox.SelectedValue as string ?? _original.MonthLayoutDirection,
            ShowWeekNumbers = ShowWeekNumbersCheckBox.IsChecked ?? false,
            FirstDayOfWeek = FirstDayOfWeekComboBox.SelectedValue as string ?? _original.FirstDayOfWeek,
            BackgroundOpacity = BackgroundOpacitySlider.Value,
            FontScale = FontScaleComboBox.SelectedValue as double? ?? _original.FontScale,
            RestDayRules = _restDayRules,
        };

        _startupService.SetEnabled(LaunchAtStartupCheckBox.IsChecked ?? false);

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "設定のエクスポート",
            Filter = "JSONファイル (*.json)|*.json",
            FileName = $"YmbDesktopCalendar_backup_{DateTime.Now:yyyyMMdd}.json",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            SettingsExportService.Export(dialog.FileName);
            MessageBox.Show(this, "エクスポートが完了しました。",
                "YMBデスクトップカレンダー", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"エクスポートに失敗しました。\n{ex.Message}",
                "YMBデスクトップカレンダー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "設定のインポート",
            Filter = "JSONファイル (*.json)|*.json",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        var confirm = MessageBox.Show(this,
            "現在の設定・メモ・予定データが上書きされます。よろしいですか?",
            "YMBデスクトップカレンダー", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            SettingsExportService.Import(dialog.FileName);
            MessageBox.Show(this,
                "インポートが完了しました。変更内容はアプリの再起動後に反映されます。",
                "YMBデスクトップカレンダー", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"インポートに失敗しました。\n{ex.Message}",
                "YMBデスクトップカレンダー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static AppSettings Clone(AppSettings source) => new()
    {
        ThemeName = source.ThemeName,
        WindowLeft = source.WindowLeft,
        WindowTop = source.WindowTop,
        WindowWidth = source.WindowWidth,
        WindowHeight = source.WindowHeight,
        PinToDesktop = source.PinToDesktop,
        ClickThrough = source.ClickThrough,
        MonthCount = source.MonthCount,
        MonthLayoutDirection = source.MonthLayoutDirection,
        ShowWeekNumbers = source.ShowWeekNumbers,
        FirstDayOfWeek = source.FirstDayOfWeek,
        BackgroundOpacity = source.BackgroundOpacity,
        FontScale = source.FontScale,
        RestDayRules = source.RestDayRules
            .Select(r => new RestDayRule(r.DayOfWeek, r.WeekOfMonth, r.Type))
            .ToList(),
    };
}
