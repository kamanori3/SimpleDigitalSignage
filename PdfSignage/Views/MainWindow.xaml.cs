using System.Windows;
using PdfSignage.Services;
using PdfSignage.ViewModels;

namespace PdfSignage.Views;

public partial class MainWindow : Window
{
  private readonly MainViewModel _viewModel;

  public MainWindow()
  {
    InitializeComponent();
    _viewModel = new MainViewModel(ApplicationContext.Current!);
    DataContext = _viewModel;
    Closed += (_, _) => _viewModel.Dispose();
  }
}
