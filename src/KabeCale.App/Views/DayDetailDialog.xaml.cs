using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using KabeCale.App.Models;

namespace KabeCale.App.Views;

/// <summary>
/// 1日分のメモと予定一覧をまとめて編集するダイアログ。
/// 予定はリスト選択で下部の入力欄に読み込み、追加/更新/削除する
/// 簡易マスター・ディテール構成。OKで呼び出し元がまとめて保存する。
/// </summary>
public partial class DayDetailDialog : Window
{
    public string MemoText { get; private set; } = string.Empty;
    public List<CalendarEvent> Events { get; private set; } = new();

    private readonly List<CalendarEvent> _events;
    private int _editingIndex = -1;
    private bool _suppressSelectionChanged;

    public DayDetailDialog(DateTime date, string currentMemo, IReadOnlyList<CalendarEvent> currentEvents)
    {
        InitializeComponent();
        DateText.Text = date.ToString("yyyy年M月d日(ddd)", CultureInfo.GetCultureInfo("ja-JP"));
        MemoTextBox.Text = currentMemo;

        // キャンセル時に元データへ影響しないよう作業用コピーを編集する
        _events = currentEvents.Select(ev => ev.Clone()).ToList();

        ColorComboBox.ItemsSource = EventColorPalette.Options;
        ColorComboBox.DisplayMemberPath = "Label";
        ColorComboBox.SelectedValuePath = "Key";

        RefreshEventList();
        SetEditTarget(-1);
    }

    /// <summary>作業用リストの内容でListBoxを再構築する。</summary>
    private void RefreshEventList()
    {
        _suppressSelectionChanged = true;
        EventListBox.Items.Clear();
        foreach (var ev in _events)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(new Rectangle
            {
                Width = 10,
                Height = 10,
                RadiusX = 2,
                RadiusY = 2,
                Fill = EventColorPalette.GetBrush(ev.Color),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            line.Children.Add(new TextBlock { Text = ev.DisplayText });
            EventListBox.Items.Add(new ListBoxItem { Content = line });
        }
        _suppressSelectionChanged = false;
    }

    /// <summary>
    /// 編集対象を切り替え、入力欄へ反映する。-1 は新規追加モード。
    /// </summary>
    private void SetEditTarget(int index)
    {
        _editingIndex = index;

        if (index < 0 || index >= _events.Count)
        {
            TitleTextBox.Text = string.Empty;
            TimeTextBox.Text = string.Empty;
            NoteTextBox.Text = string.Empty;
            ColorComboBox.SelectedValue = EventColorPalette.DefaultKey;
            ApplyButton.Content = "追加";
            DeleteButton.IsEnabled = false;
        }
        else
        {
            var ev = _events[index];
            TitleTextBox.Text = ev.Title;
            TimeTextBox.Text = ev.StartTime ?? string.Empty;
            NoteTextBox.Text = ev.Note;
            ColorComboBox.SelectedValue = EventColorPalette.Options.Any(o => o.Key == ev.Color)
                ? ev.Color
                : EventColorPalette.DefaultKey;
            ApplyButton.Content = "更新";
            DeleteButton.IsEnabled = true;
        }
    }

    private void EventListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged)
            return;
        SetEditTarget(EventListBox.SelectedIndex);
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        _suppressSelectionChanged = true;
        EventListBox.SelectedIndex = -1;
        _suppressSelectionChanged = false;
        SetEditTarget(-1);
        TitleTextBox.Focus();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text.Trim();
        if (title.Length == 0)
        {
            MessageBox.Show(this, "件名を入力してください。", "予定とメモ",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TitleTextBox.Focus();
            return;
        }

        string? startTime = null;
        var timeInput = TimeTextBox.Text.Trim();
        if (timeInput.Length > 0)
        {
            if (!TimeOnly.TryParse(timeInput, CultureInfo.InvariantCulture, out var time))
            {
                MessageBox.Show(this, "時刻は「9:00」のような形式で入力してください。", "予定とメモ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TimeTextBox.Focus();
                return;
            }
            startTime = time.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        var ev = new CalendarEvent
        {
            Title = title,
            StartTime = startTime,
            Color = ColorComboBox.SelectedValue as string ?? EventColorPalette.DefaultKey,
            Note = NoteTextBox.Text,
        };

        if (_editingIndex >= 0 && _editingIndex < _events.Count)
        {
            _events[_editingIndex] = ev;
            RefreshEventList();
            // 更新後も同じ項目を選択したままにする
            _suppressSelectionChanged = true;
            EventListBox.SelectedIndex = _editingIndex;
            _suppressSelectionChanged = false;
        }
        else
        {
            _events.Add(ev);
            RefreshEventList();
            SetEditTarget(-1);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingIndex < 0 || _editingIndex >= _events.Count)
            return;

        _events.RemoveAt(_editingIndex);
        RefreshEventList();
        SetEditTarget(-1);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        MemoText = MemoTextBox.Text;
        Events = _events;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
