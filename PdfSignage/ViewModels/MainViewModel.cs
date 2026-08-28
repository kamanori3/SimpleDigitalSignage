using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PdfSignage.Models;
using PdfSignage.Services;

namespace PdfSignage.ViewModels;

/// <summary>
/// メイン画面の ViewModel。スライドショーの進行を制御する。
/// <para>
/// Phase 1: 静止画のみ
/// Phase 2: PDF 複数ページをスライドに展開して画像と混在表示
/// Phase 4 で表示秒数の個別指定、Phase 5 でフォルダ監視を追加予定。
/// </para>
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
  private readonly ApplicationContext _context;
  private readonly PdfRenderer _pdfRenderer;
  private readonly PlaylistBuilder _playlistBuilder;
  private readonly DispatcherTimer _timer;

  /// <summary>画面レンダリング基準サイズ（PDF ラスタライズに使用）</summary>
  private readonly int _renderWidth;
  private readonly int _renderHeight;

  /// <summary>現在のプレイリスト（スライド列）</summary>
  private List<Slide> _slides = [];

  /// <summary>現在表示中のスライドインデックス</summary>
  private int _currentIndex = 0;

  private ImageSource? _currentImage;
  private bool _hasSlides;
  private string _emptyMessage = "";

  public MainViewModel(ApplicationContext context)
  {
    _context = context;
    _pdfRenderer = new PdfRenderer();
    _playlistBuilder = new PlaylistBuilder(
      _pdfRenderer,
      _context.Logger,
      context.Settings.DefaultDisplaySeconds);

    // プライマリディスプレイのサイズを PDF レンダリング解像度の基準にする
    (_renderWidth, _renderHeight) = DisplayRenderHelper.GetPrimaryScreenSize();

    _timer = new DispatcherTimer();
    _timer.Tick += OnTimerTick;

    ReloadPlaylist();
    RestartTimerForCurrentSlide();
  }

  /// <summary>現在表示中の画像（静止画・PDF ページのラスタ画像）</summary>
  public ImageSource? CurrentImage
  {
    get => _currentImage;
    private set => SetProperty(ref _currentImage, value);
  }

  /// <summary>表示可能なスライドが存在するか</summary>
  public bool HasSlides
  {
    get => _hasSlides;
    private set
    {
      if (SetProperty(ref _hasSlides, value))
      {
        OnPropertyChanged(nameof(ShowEmptyMessage));
      }
    }
  }

  /// <summary>空状態メッセージを表示するか（スライドが 0 件のとき true）</summary>
  public bool ShowEmptyMessage => !HasSlides;

  /// <summary>スライドが無い場合に表示するメッセージ</summary>
  public string EmptyMessage
  {
    get => _emptyMessage;
    private set => SetProperty(ref _emptyMessage, value);
  }

  /// <summary>
  /// 監視フォルダを再スキャンし、プレイリストを再構築する。
  /// Phase 5 ではフォルダ監視から呼び出す予定。
  /// </summary>
  private void ReloadPlaylist()
  {
    _slides = _playlistBuilder
      .Build(_context.ResolvedWatchFolder, _renderWidth, _renderHeight)
      .ToList();

    _currentIndex = 0;

    if (_slides.Count == 0)
    {
      HasSlides = false;
      CurrentImage = null;
      EmptyMessage = "表示するコンテンツがありません\n\n" +
                     $"フォルダ: {_context.ResolvedWatchFolder}";
      _context.Logger.Info("表示対象のコンテンツが見つかりませんでした。");
      return;
    }

    _context.Logger.Info($"スライド {_slides.Count} 件を構築しました。");
    ShowCurrentSlideOrSkip();
  }

  /// <summary>
  /// 現在のスライドの表示秒数に合わせてタイマーを再設定・開始する。
  /// </summary>
  private void RestartTimerForCurrentSlide()
  {
    if (_slides.Count == 0)
    {
      _timer.Interval = TimeSpan.FromSeconds(_context.Settings.DefaultDisplaySeconds);
    }
    else
    {
      var seconds = _slides[_currentIndex].DisplaySeconds;
      _timer.Interval = TimeSpan.FromSeconds(seconds);
    }

    _timer.Start();
  }

  /// <summary>
  /// タイマー満了時: 次のスライドへ進む。最後のスライドの次は先頭へ（ループ）。
  /// </summary>
  private void OnTimerTick(object? sender, EventArgs e)
  {
    if (_slides.Count == 0)
    {
      // 空のときは定期的に再スキャン（画像追加後の再起動なし検知用・Phase 5 で本格対応）
      ReloadPlaylist();
      RestartTimerForCurrentSlide();
      return;
    }

    _currentIndex = (_currentIndex + 1) % _slides.Count;
    ShowCurrentSlideOrSkip();
    RestartTimerForCurrentSlide();
  }

  /// <summary>
  /// 現在インデックスのスライドを表示する。失敗時は次スライドへスキップ。
  /// </summary>
  private void ShowCurrentSlideOrSkip()
  {
    if (_slides.Count == 0)
    {
      HasSlides = false;
      CurrentImage = null;
      return;
    }

    var attempts = 0;
    while (attempts < _slides.Count)
    {
      var slide = _slides[_currentIndex];
      try
      {
        CurrentImage = LoadSlideContent(slide);
        HasSlides = true;
        _context.Logger.Info($"表示: {slide.GetDisplayName()}");
        return;
      }
      catch (Exception ex)
      {
        _context.Logger.Error($"スライド読込失敗: {slide.GetDisplayName()} - {ex.Message}");
        _currentIndex = (_currentIndex + 1) % _slides.Count;
        attempts++;
      }
    }

    HasSlides = false;
    CurrentImage = null;
    EmptyMessage = "表示できるコンテンツがありません";
    _context.Logger.Error("すべてのスライドの読み込みに失敗しました。");
  }

  /// <summary>
  /// スライド種別に応じて ImageSource を生成する。
  /// </summary>
  private ImageSource LoadSlideContent(Slide slide)
  {
    switch (slide.ContentType)
    {
      case SlideContentType.Image:
        return ImageLoader.Load(slide.FilePath);

      case SlideContentType.PdfPage:
        return _pdfRenderer.RenderPage(
          slide.FilePath,
          slide.PageIndex,
          _renderWidth,
          _renderHeight);

      default:
        throw new InvalidOperationException($"未対応のスライド種別: {slide.ContentType}");
    }
  }

  public void Dispose()
  {
    _timer.Stop();
    _timer.Tick -= OnTimerTick;
  }
}
