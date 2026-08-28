using System.Windows;
using PdfSignage.Services;
using PdfSignage.Views;

namespace PdfSignage;

public partial class App : Application
{
  public App()
  {
    Exit += (_, _) => TaskbarController.ForceRestore();
    SessionEnding += (_, _) => TaskbarController.ForceRestore();
    AppDomain.CurrentDomain.ProcessExit += (_, _) => TaskbarController.ForceRestore();
  }

  private void Application_Startup(object sender, StartupEventArgs e)
  {
    ApplicationContext.Initialize();

    var mainWindow = new MainWindow();
    mainWindow.Show();
  }
}
