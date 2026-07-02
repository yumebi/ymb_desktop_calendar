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

        var merged = Application.Current.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => d.Source?.OriginalString.StartsWith("Themes/") == true);
        if (existing is not null)
            merged.Remove(existing);

        merged.Add(dict);
    }
}
