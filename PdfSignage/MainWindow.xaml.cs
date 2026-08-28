using System.Windows;

namespace PdfSignage;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();

    // Phase 0: 設定読込・パス算出の確認用表示（Phase 1 以降で非表示化）
    StatusText.Text =
      $"Phase 0 基盤構築\n\n" +
      $"監視フォルダ: {App.ResolvedWatchFolder}\n" +
      $"ログフォルダ: {App.LogDirectory}\n" +
      $"デフォルト表示秒数: {App.Settings.DefaultDisplaySeconds} 秒";
  }
}
