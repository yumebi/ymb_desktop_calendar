using System.Windows;
using KabeCale.App.Models;
using KabeCale.App.Services;

namespace KabeCale.App.Views;

public partial class SettingsWindow : Window
{
    private record LayoutOption(string Label, string Value);

    private static readonly LayoutOption[] LayoutOptions =
    {
        new("横並び", "Horizontal"),
        new("縦並び", "Vertical"),
    };

    public AppSettings ResultSettings { get; private set; }

    private readonly AppSettings _original;
    private readonly StartupService _startupService = new();

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();
        _original = current;

        ThemeComboBox.ItemsSource = ThemeService.AvailableThemes;
        ThemeComboBox.SelectedItem = current.ThemeName;

        MonthCountComboBox.ItemsSource = new[] { 1, 2, 3 };
        MonthCountComboBox.SelectedItem = current.MonthCount;

        LayoutDirectionComboBox.ItemsSource = LayoutOptions;
        LayoutDirectionComboBox.DisplayMemberPath = "Label";
        LayoutDirectionComboBox.SelectedValuePath = "Value";
        LayoutDirectionComboBox.SelectedValue = current.MonthLayoutDirection;

        PinToDesktopCheckBox.IsChecked = current.PinToDesktop;
        ClickThroughCheckBox.IsChecked = current.ClickThrough;
        LaunchAtStartupCheckBox.IsChecked = _startupService.IsEnabled();

        ResultSettings = Clone(current);
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
        };

        _startupService.SetEnabled(LaunchAtStartupCheckBox.IsChecked ?? false);

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
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
    };
}
