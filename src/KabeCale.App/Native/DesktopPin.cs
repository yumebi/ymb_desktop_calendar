using System.Runtime.InteropServices;

namespace KabeCale.App.Native;

/// <summary>
/// デスクトップアイコン層にウィンドウを固定する(Rainmeter等と同じWorkerWトリック)。
/// Progmanへ0x052Cを送るとWorkerWが生成されるので、そこに自ウィンドウをSetParentする。
/// </summary>
public static class DesktopPin
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// 指定ウィンドウをデスクトップ壁紙の直上(WorkerW)に貼り付ける。
    /// WorkerWが見つからない場合は何もしない(Progmanへの貼り付けは壁紙より下になり
    /// ウィンドウが完全に見えなくなるため、フォールバックとして行わない)。
    /// </summary>
    /// <returns>貼り付けに成功したらtrue。</returns>
    public static bool Pin(IntPtr windowHandle)
    {
        var progman = FindWindow("Progman", null);
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

        IntPtr workerW = IntPtr.Zero;
        for (var attempt = 0; attempt < 10 && workerW == IntPtr.Zero; attempt++)
        {
            if (attempt > 0)
                Thread.Sleep(50);

            EnumWindows((hWnd, _) =>
            {
                var shellView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                    workerW = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                return true;
            }, IntPtr.Zero);
        }

        if (workerW == IntPtr.Zero)
            return false;

        SetParent(windowHandle, workerW);
        return true;
    }

    /// <summary>クリックをそのまま背後(デスクトップ)へ透過させる。</summary>
    public static void SetClickThrough(IntPtr windowHandle, bool enabled)
    {
        var exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
        exStyle = enabled
            ? exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED
            : exStyle & ~(WS_EX_TRANSPARENT | WS_EX_LAYERED);
        SetWindowLong(windowHandle, GWL_EXSTYLE, exStyle);
    }
}
