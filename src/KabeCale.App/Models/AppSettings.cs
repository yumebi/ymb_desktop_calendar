namespace KabeCale.App.Models;

public class AppSettings
{
    public string ThemeName { get; set; } = "Light";
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 420;
    public double WindowHeight { get; set; } = 460;
    public bool PinToDesktop { get; set; } = false;
    public bool ClickThrough { get; set; } = false;
    public int MonthCount { get; set; } = 1;
    public string MonthLayoutDirection { get; set; } = "Horizontal";
    public bool ShowWeekNumbers { get; set; } = false;
    public string FirstDayOfWeek { get; set; } = "Sunday";
    public double BackgroundOpacity { get; set; } = 1.0;
    public double FontScale { get; set; } = 1.0;

    /// <summary>曜日×週序数で指定する休日(公休日/私休日)のルール一覧。</summary>
    public List<RestDayRule> RestDayRules { get; set; } = new();
}
