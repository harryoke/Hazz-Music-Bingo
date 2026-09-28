using System.Windows;
using Application = System.Windows.Application;

namespace HazzMusicBingo;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length > 0 && e.Args[0] == "--self-test")
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            base.OnStartup(e);
            var result = await Services.ReleaseSelfTest.RunAsync(e.Args.ElementAtOrDefault(1));
            Shutdown(result);
            return;
        }
        base.OnStartup(e);
        MainWindow = new Views.MainWindow();
        MainWindow.Show();
    }
}
