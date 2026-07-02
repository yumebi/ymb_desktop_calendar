using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using KabeCale.App.Models;
using KabeCale.App.Native;
using KabeCale.App.Services;
using KabeCale.App.Views;
using DrawingIcon = System.Drawing.Icon;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace KabeCale.App;

public partial class MainWindow : Window
{
    private const double MonthPanelWidth = 220;

    private readonly SettingsService _settingsService = new();
    private readonly HolidayService _holidayService = new();
    private readonly MemoService _memoService = new();
    private readonly ThemeService _themeService = new();
    private readonly UpdateService _updateService = new();

    private AppSettings _settings = new();
    private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private NotifyIcon? _trayIcon;
    private System.Windows.Forms.ToolStripMenuItem? _clickThroughMenuItem;
    private string? _pendingReleaseUrl;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _settings = _settingsService.Load();
        Left = _settings.WindowLeft;
        Top = _settings.WindowTop;
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;

        _themeService.Apply(_settings.ThemeName);

        var hwnd = new WindowInteropHelper(this).Handle;
        if (_settings.PinToDesktop && !DesktopPin.Pin(hwnd))
        {
            // WorkerWが見つからずピン留めできなかった場合、通常ウィンドウとして
            // 表示させるため設定を戻す(そのままだとデスクトップ上に見えなくなるため)。
            _settings.PinToDesktop = false;
            _settingsService.Save(_settings);
        }
        if (_settings.ClickThrough)
            DesktopPin.SetClickThrough(hwnd, true);
        UpdateInteractiveControlsVisibility();
    }

    /// <summary>
    /// クリックスルー中は矢印・設定ボタン・リサイズグリップを押せないので隠す
    /// (押せないボタンが見えているとユーザーが混乱するため)。
    /// </summary>
    private void UpdateInteractiveControlsVisibility()
    {
        var visibility = _settings.ClickThrough ? Visibility.Collapsed : Visibility.Visible;
        PrevMonthButton.Visibility = visibility;
        NextMonthButton.Visibility = visibility;
        SettingsButtonElement.Visibility = visibility;
        ResizeMode = _settings.ClickThrough ? ResizeMode.NoResize : ResizeMode.CanResizeWithGrip;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SetupTrayIcon();
        _ = RenderMonthsAsync();
        _ = CheckForUpdateOnStartupAsync();
    }

    private async Task CheckForUpdateOnStartupAsync()
    {
        var result = await _updateService.CheckForUpdateAsync();
        if (!result.HasUpdate || result.ReleaseUrl is null)
            return;

        _pendingReleaseUrl = result.ReleaseUrl;
        _trayIcon?.ShowBalloonTip(
            8000,
            "YMBデスクトップカレンダー",
            $"新しいバージョン {result.LatestVersion} があります。クリックして確認。",
            System.Windows.Forms.ToolTipIcon.Info);
    }

    private async void CheckForUpdateFromTray()
    {
        var result = await _updateService.CheckForUpdateAsync();
        if (result.LatestVersion is null)
        {
            MessageBox.Show(this, "更新情報を取得できませんでした。ネット接続を確認してください。",
                "YMBデスクトップカレンダー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (result.HasUpdate)
        {
            _pendingReleaseUrl = result.ReleaseUrl;
            var openPage = MessageBox.Show(this,
                $"新しいバージョン {result.LatestVersion} があります。リリースページを開きますか?",
                "YMBデスクトップカレンダー", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (openPage == MessageBoxResult.Yes)
                OpenReleasePage();
        }
        else
        {
            MessageBox.Show(this, "最新バージョンを使用しています。",
                "YMBデスクトップカレンダー", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenReleasePage()
    {
        if (_pendingReleaseUrl is null)
            return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_pendingReleaseUrl)
        {
            UseShellExecute = true,
        });
    }

    private async Task RenderMonthsAsync()
    {
        while (MonthsHost.Children.Count < _settings.MonthCount)
            MonthsHost.Children.Add(new MonthPanel());
        while (MonthsHost.Children.Count > _settings.MonthCount)
            MonthsHost.Children.RemoveAt(MonthsHost.Children.Count - 1);

        YearHeaderText.Text = _currentMonth.ToString("yyyy年", CultureInfo.GetCultureInfo("ja-JP"));

        var renderTasks = new List<Task>();
        for (var i = 0; i < _settings.MonthCount; i++)
        {
            var panel = (MonthPanel)MonthsHost.Children[i];
            var month = _currentMonth.AddMonths(i);
            renderTasks.Add(panel.RenderAsync(month, _holidayService, _memoService, OnDayCellClicked));
        }
        await Task.WhenAll(renderTasks);
    }

    private void OnDayCellClicked(DateTime date)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var dialog = new MemoDialog(date, _memoService.Get(dateOnly)) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _memoService.Set(dateOnly, dialog.MemoText);
            _ = RenderMonthsAsync();
        }
    }

    private void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        _ = RenderMonthsAsync();
    }

    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        _ = RenderMonthsAsync();
    }

    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            return;
        DragMove();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            ApplySettings(dialog.ResultSettings);
        }
    }

    private void ApplySettings(AppSettings updated)
    {
        var themeChanged = updated.ThemeName != _settings.ThemeName;
        var pinChanged = updated.PinToDesktop != _settings.PinToDesktop;
        var clickThroughChanged = updated.ClickThrough != _settings.ClickThrough;
        var monthCountChanged = updated.MonthCount != _settings.MonthCount;

        _settings = updated;
        _settingsService.Save(_settings);

        if (themeChanged)
        {
            _themeService.Apply(_settings.ThemeName);
        }

        if (monthCountChanged)
        {
            Width = 60 + MonthPanelWidth * _settings.MonthCount;
        }

        if (themeChanged || monthCountChanged)
        {
            _ = RenderMonthsAsync();
        }

        var hwnd = new WindowInteropHelper(this).Handle;
        if (pinChanged && _settings.PinToDesktop && !DesktopPin.Pin(hwnd))
        {
            _settings.PinToDesktop = false;
            _settingsService.Save(_settings);
            MessageBox.Show(this,
                "このPCではデスクトップへの常駐に対応していませんでした。通常ウィンドウとして表示します。",
                "YMBデスクトップカレンダー", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        if (clickThroughChanged)
        {
            DesktopPin.SetClickThrough(hwnd, _settings.ClickThrough);
            if (_clickThroughMenuItem is not null)
                _clickThroughMenuItem.Checked = _settings.ClickThrough;
            UpdateInteractiveControlsVisibility();
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = DrawingIcon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Visible = true,
            Text = "YMBデスクトップカレンダー",
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("表示/非表示", null, (_, _) => ToggleVisibility());
        _clickThroughMenuItem = new System.Windows.Forms.ToolStripMenuItem(
            "クリックスルー", null, (_, _) => ToggleClickThroughFromTray())
        {
            Checked = _settings.ClickThrough,
        };
        menu.Items.Add(_clickThroughMenuItem);
        menu.Items.Add("更新を確認", null, (_, _) => CheckForUpdateFromTray());
        menu.Items.Add("終了", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ToggleVisibility();
        _trayIcon.BalloonTipClicked += (_, _) => OpenReleasePage();
    }

    private void ToggleClickThroughFromTray()
    {
        _settings.ClickThrough = !_settings.ClickThrough;
        _settingsService.Save(_settings);

        var hwnd = new WindowInteropHelper(this).Handle;
        DesktopPin.SetClickThrough(hwnd, _settings.ClickThrough);

        if (_clickThroughMenuItem is not null)
            _clickThroughMenuItem.Checked = _settings.ClickThrough;
        UpdateInteractiveControlsVisibility();
    }

    private void ToggleVisibility()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    private void ExitApplication()
    {
        _allowClose = true;
        Close();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settingsService.Save(_settings);

        _trayIcon?.Dispose();
    }
}
