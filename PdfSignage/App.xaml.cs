using System.Windows;
using PdfSignage.Services;
using PdfSignage.Views;

namespace PdfSignage;

public partial class App : Application
{
  private void Application_Startup(object sender, StartupEventArgs e)
  {
    ApplicationContext.Initialize();

    var mainWindow = new MainWindow();
    mainWindow.Show();
  }
}
