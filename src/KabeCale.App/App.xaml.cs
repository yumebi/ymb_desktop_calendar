using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;
using KabeCale.App.Native;

namespace KabeCale.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // 保存先を環境変数で切り替えている場合(動作確認用)は、本番の起動と二重起動扱いにならないよう名前を分ける。
    private static readonly string SingleInstanceMutexName = "YmbDesktopCalendar_SingleInstance" + DataDirSuffix();
    private const string MainWindowTitle = "YMBデスクトップカレンダー";

    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;

    /// <summary>
    /// 二重起動防止。Mutexの取得に失敗したら既存インスタンスをアクティブ化して即終了する
    /// (StartupUriによるMainWindow生成はbase.OnStartupを呼ばないことで抑止する)。
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            WindowActivator.ActivateByTitle(MainWindowTitle);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    private static string DataDirSuffix()
    {
        var dir = Environment.GetEnvironmentVariable(Services.AppPaths.DataDirEnvVar);
        if (string.IsNullOrWhiteSpace(dir)) return "";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dir.ToLowerInvariant()));
        return "_" + Convert.ToHexString(hash)[..12];
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
            _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}

