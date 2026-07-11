using System.Runtime.InteropServices;

namespace KabeCale.App.Native;

/// <summary>
/// 常駐ウィジェットとしてタスクマネージャー上の見た目のメモリ使用量を抑えるための
/// ワーキングセット・トリム処理。GC.Collectで未使用オブジェクトを回収した上で
/// SetProcessWorkingSetSize(-1, -1)を呼ぶと、確保済みだが使っていない物理メモリを
/// OSに返却できる(EmptyWorkingSetと同等の効果)。
/// </summary>
public static class MemoryTrimmer
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, nint minSize, nint maxSize);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    public static void TrimNow()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        SetProcessWorkingSetSize(GetCurrentProcess(), -1, -1);
    }
}
