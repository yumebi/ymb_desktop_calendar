using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
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
    private const double MonthPanelHeight = 380;

    private readonly SettingsService _settingsService = new();
    private readonly HolidayService _holidayService = new();
    private readonly MemoService _memoService = new();
    private readonly EventService _eventService = new();
    private readonly ThemeService _themeService = new();
    private readonly UpdateService _updateService = new();
    private readonly BackupService _backupService = new();

    private AppSettings _settings = new();
    private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private NotifyIcon? _trayIcon;
    private System.Windows.Forms.ToolStripMenuItem? _clickThroughMenuItem;
    private DispatcherTimer? _savePositionTimer;
    private readonly DispatcherTimer _memoryTrimTimer = new() { Interval = TimeSpan.FromMinutes(15) };
    private DispatcherTimer? _renderIdleTrimTimer;
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
        EnsureWindowIsOnScreen();

        _themeService.Apply(_settings.ThemeName);
        ApplyBackgroundOpacity();

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
    /// 保存された位置が現在のモニタ構成では画面外になる場合(外付けモニタを外した等)、
    /// プライマリモニタ内に位置を戻す。
    /// </summary>
    private void EnsureWindowIsOnScreen()
    {
        var windowRect = new System.Drawing.Rectangle(
            (int)Left, (int)Top, (int)Math.Max(Width, 50), (int)Math.Max(Height, 50));

        var isVisible = System.Windows.Forms.Screen.AllScreens.Any(screen =>
        {
            var overlap = System.Drawing.Rectangle.Intersect(screen.WorkingArea, windowRect);
            return overlap.Width > 50 && overlap.Height > 50;
        });

        if (isVisible)
            return;

        var workArea = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        Left = workArea.Left + 40;
        Top = workArea.Top + 40;
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

        // Loaded直後はウィンドウがまだ初回レイアウト/描画パスを終えておらず、新規追加した
        // MonthPanel(UserControl)からFindResourceでテーマブラシを解決できずに失敗することが
        // あるため、その完了を待つDispatcherPriority.Loadedまで初回描画を遅延させる。
        Dispatcher.BeginInvoke(new Action(() => { _ = RenderInitialMonthsAndTrimAsync(); }), DispatcherPriority.Loaded);

        _ = CheckForUpdateOnStartupAsync();
        _ = Task.Run(() => _backupService.RunBackup());

        LocationChanged += (_, _) => ScheduleSaveWindowPosition();
        SizeChanged += (_, _) => ScheduleSaveWindowPosition();

        // 常駐アプリなので、一定時間ごとにアイドル時のメモリ使用量を抑える。
        _memoryTrimTimer.Tick += (_, _) => MemoryTrimmer.TrimNow();
        _memoryTrimTimer.Start();
    }

    private async Task RenderInitialMonthsAndTrimAsync()
    {
        await RenderMonthsAsync();
        // 起動時の描画で確保したガーベジを早めに回収し、アイドル時のワーキングセットを抑える。
        _ = Dispatcher.BeginInvoke(new Action(MemoryTrimmer.TrimNow), DispatcherPriority.ApplicationIdle);
    }

    /// <summary>
    /// 背景ブラシのOpacityだけを変える(ウィンドウ全体のOpacityにすると文字まで
    /// 薄くなってしまうため)。テーマ変更時は現在のテーマブラシを取り直して再適用する。
    /// </summary>
    private void ApplyBackgroundOpacity()
    {
        if (FindResource("CalBackgroundBrush") is Brush brush)
        {
            var background = brush.Clone();
            background.Opacity = _settings.BackgroundOpacity;
            RootBorder.Background = background;
        }
    }

    /// <summary>
    /// 強制終了やクラッシュでも位置が失われないよう、移動/リサイズの都度(少し
    /// デバウンスして)保存する。クリーンな終了時の保存だけに頼らない。
    /// </summary>
    private void ScheduleSaveWindowPosition()
    {
        if (_savePositionTimer is null)
        {
            _savePositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _savePositionTimer.Tick += (_, _) =>
            {
                _savePositionTimer!.Stop();
                SaveWindowPosition();
            };
        }

        _savePositionTimer.Stop();
        _savePositionTimer.Start();
    }

    private void SaveWindowPosition()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settingsService.Save(_settings);
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

    /// <summary>
    /// 表示月数と並び方向から月パネルの行数・列数を決める。6ヶ月/12ヶ月は2次元グリッド
    /// (横並び時2x3・3x4、縦並び時はそれを転置した3x2・4x3)、それ以外は1行または1列。
    /// </summary>
    private (int Rows, int Columns) GetMonthGridDimensions()
    {
        var vertical = _settings.MonthLayoutDirection == "Vertical";

        if (_settings.MonthCount is 6 or 12)
        {
            var (baseRows, baseColumns) = _settings.MonthCount == 6 ? (2, 3) : (3, 4);
            return vertical ? (baseColumns, baseRows) : (baseRows, baseColumns);
        }

        return vertical ? (_settings.MonthCount, 1) : (1, _settings.MonthCount);
    }

    private void ApplyMonthsHostLayout()
    {
        var (rows, columns) = GetMonthGridDimensions();
        MonthsHost.Rows = rows;
        MonthsHost.Columns = columns;
    }

    private void UpdateWindowSizeForLayout()
    {
        var (rows, columns) = GetMonthGridDimensions();
        Width = 60 + MonthPanelWidth * columns;
        Height = 60 + MonthPanelHeight * rows;
    }

    private async Task RenderMonthsAsync()
    {
        ApplyMonthsHostLayout();

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
            renderTasks.Add(panel.RenderAsync(month, _holidayService, _memoService, _eventService, OnDayCellClicked, _settings));
        }
        await Task.WhenAll(renderTasks);

        ScheduleIdleTrimAfterRender();
    }

    /// <summary>
    /// 月移動や設定変更で発生したガーベジを、操作が落ち着いた数秒後にまとめて回収する。
    /// 毎回律儀にトリムするとページフォルトでCPU負荷が上がるため、連続操作中はデバウンスする。
    /// </summary>
    private void ScheduleIdleTrimAfterRender()
    {
        if (_renderIdleTrimTimer is null)
        {
            _renderIdleTrimTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _renderIdleTrimTimer.Tick += (_, _) =>
            {
                _renderIdleTrimTimer!.Stop();
                MemoryTrimmer.TrimNow();
            };
        }

        _renderIdleTrimTimer.Stop();
        _renderIdleTrimTimer.Start();
    }

    private void OnDayCellClicked(DateTime date)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var dialog = new DayDetailDialog(date, _memoService.Get(dateOnly), _eventService.Get(dateOnly)) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _memoService.Set(dateOnly, dialog.MemoText);
            _eventService.Set(dateOnly, dialog.Events);
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

    /// <summary>マウスホイールで前後の月に移動する(上スクロールで前月、下スクロールで翌月)。</summary>
    private void MonthsHost_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(e.Delta > 0 ? -1 : 1);
        _ = RenderMonthsAsync();
        e.Handled = true;
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        GoToToday();
    }

    private void GoToToday()
    {
        _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _ = RenderMonthsAsync();
    }

    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // 枠なしウィンドウはスナップ等で最大化されると戻す手段がないため、
            // 最大化中のダブルクリックは今日ジャンプではなく元のサイズへの復帰にする。
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                GoToToday();
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            // 最大化中にヘッダーを掴んだら、カーソル位置を基準に元サイズへ戻してそのままドラッグ継続。
            var cursorInWindow = e.GetPosition(this);
            var cursorOnScreen = PointToScreen(cursorInWindow);
            var ratioX = cursorInWindow.X / Math.Max(ActualWidth, 1);

            WindowState = WindowState.Normal;

            Left = cursorOnScreen.X - Width * ratioX;
            Top = cursorOnScreen.Y - 12;
        }

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
        var layoutChanged = updated.MonthLayoutDirection != _settings.MonthLayoutDirection;
        var weekNumbersChanged = updated.ShowWeekNumbers != _settings.ShowWeekNumbers;
        var firstDayOfWeekChanged = updated.FirstDayOfWeek != _settings.FirstDayOfWeek;
        var fontScaleChanged = updated.FontScale != _settings.FontScale;
        var restDayRulesChanged = !RestDayRulesEqual(updated.RestDayRules, _settings.RestDayRules);

        _settings = updated;
        _settingsService.Save(_settings);

        if (themeChanged)
        {
            _themeService.Apply(_settings.ThemeName);
        }
        ApplyBackgroundOpacity();

        if (monthCountChanged || layoutChanged)
        {
            UpdateWindowSizeForLayout();
        }

        if (themeChanged || monthCountChanged || layoutChanged || weekNumbersChanged || firstDayOfWeekChanged || fontScaleChanged || restDayRulesChanged)
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

    /// <summary>休日ルール一覧が(順序を問わず)同じ内容かどうかを比較する。</summary>
    private static bool RestDayRulesEqual(List<RestDayRule> a, List<RestDayRule> b)
    {
        if (a.Count != b.Count)
            return false;

        var setA = a.Select(r => (r.DayOfWeek, r.WeekOfMonth, r.Type)).OrderBy(t => t).ToList();
        var setB = b.Select(r => (r.DayOfWeek, r.WeekOfMonth, r.Type)).OrderBy(t => t).ToList();
        return setA.SequenceEqual(setB);
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
        {
            Hide();
            MemoryTrimmer.TrimNow();
        }
        else
        {
            Show();
        }
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

        SaveWindowPosition();

        _trayIcon?.Dispose();
    }
}
