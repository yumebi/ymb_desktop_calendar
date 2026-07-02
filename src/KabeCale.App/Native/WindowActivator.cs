using System.Runtime.InteropServices;

namespace KabeCale.App.Native;

/// <summary>
/// 二重起動時に、既に起動している既存インスタンスのウィンドウを前面に呼び出すための
/// ヘルパー。既存ウィンドウはトレイに格納されて非表示(Hide)の場合もあるため、
/// 最小化解除と表示を合わせて行う。
/// </summary>
public static class WindowActivator
{
    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    /// <summary>
    /// 指定タイトルのウィンドウを探し、前面に表示する。見つからない場合は何もしない。
    /// </summary>
    public static void ActivateByTitle(string windowTitle)
    {
        var hWnd = FindWindow(null, windowTitle);
        if (hWnd == IntPtr.Zero)
            return;

        if (IsIconic(hWnd))
            ShowWindow(hWnd, SW_RESTORE);
        else
            ShowWindow(hWnd, SW_SHOW);

        SetForegroundWindow(hWnd);
    }
}
