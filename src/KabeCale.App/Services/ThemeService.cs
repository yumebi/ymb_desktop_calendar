using System.Windows;

namespace KabeCale.App.Services;

public class ThemeService
{
    public static readonly string[] AvailableThemes = { "Light", "Dark", "Pastel", "Ocean", "Forest", "Sunset" };

    public void Apply(string themeName)
    {
        var name = AvailableThemes.Contains(themeName) ? themeName : "Light";
        var uri = new Uri($"Themes/{name}.xaml", UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };

        // 差し替え対象は配色テーマの辞書だけに限定する。
        // "Themes/" で始まるものを一括で外すと、同じフォルダにある
        // YMB共通トークン(Themes/YmbTypography.xaml)まで巻き込んで消えてしまう。
        var merged = Application.Current.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d =>
            AvailableThemes.Any(t => d.Source?.OriginalString == $"Themes/{t}.xaml"));
        if (existing is not null)
            merged.Remove(existing);

        merged.Add(dict);
    }
}
