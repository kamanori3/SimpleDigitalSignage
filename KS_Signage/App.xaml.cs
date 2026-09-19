using System.Windows;
using KS_Signage.Services;
using KS_Signage.Views;

namespace KS_Signage;

public partial class App : Application
{
  public App()
  {
    Exit += OnApplicationExit;
    SessionEnding += (_, _) => TaskbarController.ForceRestore();
    AppDomain.CurrentDomain.ProcessExit += (_, _) => TaskbarController.ForceRestore();
  }

  private void OnApplicationExit(object sender, EventArgs e)
  {
    TaskbarController.ForceRestore();
    AppSelfRestartService.ReleaseSingleInstance();
  }

  private void Application_Startup(object sender, StartupEventArgs e)
  {
    if (!AppSelfRestartService.TryAcquireSingleInstance())
    {
      Shutdown();
      return;
    }

    ApplicationContext.Initialize();
    var logger = ApplicationContext.Current!.Logger;

    if (AppSelfRestartService.IsSelfRestartProcess())
    {
      logger.Info("自己再起動後のプロセス起動を検知しました。");
    }

    AppSelfRestartService.RegisterExceptionHandlers(logger);

    var mainWindow = new MainWindow();
    mainWindow.Show();
  }
}
