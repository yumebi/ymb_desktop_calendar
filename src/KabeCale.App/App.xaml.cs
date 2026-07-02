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
    private const string SingleInstanceMutexName = "YmbDesktopCalendar_SingleInstance";
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

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
            _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}

