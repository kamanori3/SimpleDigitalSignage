using System.Windows;
using KS_Signage.Issuer.ViewModels;

namespace KS_Signage.Issuer.Views;

public partial class IssuerWindow : Window
{
  public IssuerWindow(IssuerViewModel viewModel)
  {
    InitializeComponent();
    DataContext = viewModel;
  }
}
