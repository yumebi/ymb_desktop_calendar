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
}
