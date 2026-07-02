using System.Globalization;
using System.Windows;

namespace KabeCale.App.Views;

public partial class MemoDialog : Window
{
    public string MemoText { get; private set; } = string.Empty;

    public MemoDialog(DateTime date, string currentMemo)
    {
        InitializeComponent();
        DateText.Text = date.ToString("yyyy年M月d日(ddd)", CultureInfo.GetCultureInfo("ja-JP"));
        MemoTextBox.Text = currentMemo;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        MemoText = MemoTextBox.Text;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
