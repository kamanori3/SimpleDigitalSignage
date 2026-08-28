using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PdfSignage.Models;
using PdfSignage.Services;

namespace PdfSignage.ViewModels;

/// <summary>
/// メイン画面の ViewModel。スライドショーの進行を制御する。
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
  private const int TransitionTargetMilliseconds = 3000;
  private readonly ApplicationContext _context;
  private readonly PdfRenderer _pdfRenderer;
  private PlaylistBuilder _playlistBuilder;
  private readonly SlidePreloader _preloader;
  private ContentFolderWatcher _folderWatcher;
  private readonly DispatcherTimer _timer;

  private readonly int _renderWidth;
  private readonly int _renderHeight;

  private List<Slide> _slides = [];
  private int _currentIndex = 0;
  private bool _playlistReloadPending;

  private ImageSource? _currentImage;
  private Uri? _videoSource;
  private bool _isVideoVisible;
  private bool _hasSlides;
  private string _emptyMessage = "";

  public MainViewModel(ApplicationContext context)
  {
    _context = context;
    (_renderWidth, _renderHeight) = DisplayRenderHelper.GetPrimaryScreenSize();

    _pdfRenderer = new PdfRenderer(_context.Logger, _renderWidth, _renderHeight);
    _playlistBuilder = new PlaylistBuilder(
      _pdfRenderer,
      _context.Logger,
      context.Settings.DefaultDisplaySeconds);
    _preloader = new SlidePreloader();

    _folderWatcher = new ContentFolderWatcher(_context.ResolvedWatchFolder, _context.Logger);
    _folderWatcher.ContentChanged += OnFolderContentChanged;

    _timer = new DispatcherTimer();
    _timer.Tick += OnTimerTick;

    ReloadPlaylist();
    RestartTimerForCurrentSlide();
  }

  public ImageSource? CurrentImage
  {
    get => _currentImage;
    private set => SetProperty(ref _currentImage, value);
  }

  public Uri? VideoSource
  {
    get => _videoSource;
    private set => SetProperty(ref _videoSource, value);
  }

  public bool IsVideoVisible
  {
    get => _isVideoVisible;
    private set
    {
      if (SetProperty(ref _isVideoVisible, value))
      {
        OnPropertyChanged(nameof(ShowImage));
      }
    }
  }

  public bool HasSlides
  {
    get => _hasSlides;
    private set
    {
      if (SetProperty(ref _hasSlides, value))
      {
        OnPropertyChanged(nameof(ShowEmptyMessage));
        OnPropertyChanged(nameof(ShowImage));
      }
    }
  }

  public bool ShowImage => HasSlides && !IsVideoVisible;

  public bool ShowEmptyMessage => !HasSlides;

  public string EmptyMessage
  {
    get => _emptyMessage;
    private set => SetProperty(ref _emptyMessage, value);
  }

  /// <summary>
  /// 動画再生完了時に View から呼び出す。
  /// </summary>
  public void OnVideoEnded()
  {
    if (!IsVideoVisible || _slides.Count == 0)
    {
      return;
    }

    AdvanceToNextSlide();
  }

  private void OnFolderContentChanged()
  {
    Application.Current.Dispatcher.BeginInvoke(() =>
    {
      _playlistReloadPending = true;
      _context.Logger.Info("プレイリスト更新を予約しました（現在のスライド完了後に反映）。");
    });
  }

  private void ReloadPlaylist()
  {
    ApplyPlaylistReload(preferredNextSlide: null, startIndex: 0);
  }

  private void ApplyPlaylistReload(Slide? preferredNextSlide, int? startIndex = null)
  {
    _preloader.Cancel();
    _pdfRenderer.ClearCache();
    ClearVideoState();

    var oldSlides = _slides;
    _slides = _playlistBuilder
      .Build(_context.ResolvedWatchFolder, _renderWidth, _renderHeight)
      .ToList();

    if (_slides.Count == 0)
    {
      _currentIndex = 0;
      HasSlides = false;
      CurrentImage = null;
      EmptyMessage = "表示するコンテンツがありません\n\n" +
                     $"フォルダ: {_context.ResolvedWatchFolder}";
      _context.Logger.Info("表示対象のコンテンツが見つかりませんでした。");
      return;
    }

    _currentIndex = startIndex
                    ?? ResolveIndexAfterReload(oldSlides, _slides, preferredNextSlide);

    _context.Logger.Info(
      $"プレイリストを更新しました（スライド {_slides.Count} 件、表示: {_slides[_currentIndex].GetDisplayName()}）。");
    ShowCurrentSlideOrSkip();
  }

  private static int ResolveIndexAfterReload(
    IReadOnlyList<Slide> oldSlides,
    IReadOnlyList<Slide> newSlides,
    Slide? preferredNextSlide)
  {
    if (preferredNextSlide is not null)
    {
      var preferredIndex = FindSlideIndex(
        newSlides,
        preferredNextSlide.FilePath,
        preferredNextSlide.PageIndex);
      if (preferredIndex >= 0)
      {
        return preferredIndex;
      }

      var oldPreferredIndex = FindSlideIndex(
        oldSlides,
        preferredNextSlide.FilePath,
        preferredNextSlide.PageIndex);
      if (oldPreferredIndex >= 0)
      {
        for (var i = oldPreferredIndex; i < oldSlides.Count; i++)
        {
          var candidateIndex = FindSlideIndex(
            newSlides,
            oldSlides[i].FilePath,
            oldSlides[i].PageIndex);
          if (candidateIndex >= 0)
          {
            return candidateIndex;
          }
        }

        for (var i = 0; i < oldPreferredIndex; i++)
        {
          var candidateIndex = FindSlideIndex(
            newSlides,
            oldSlides[i].FilePath,
            oldSlides[i].PageIndex);
          if (candidateIndex >= 0)
          {
            return candidateIndex;
          }
        }
      }
    }

    return 0;
  }

  private static int FindSlideIndex(IReadOnlyList<Slide> slides, string filePath, int pageIndex)
  {
    for (var i = 0; i < slides.Count; i++)
    {
      var slide = slides[i];
      if (slide.PageIndex == pageIndex &&
          slide.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
      {
        return i;
      }
    }

    return -1;
  }

  private void RestartTimerForCurrentSlide()
  {
    if (_slides.Count == 0)
    {
      _timer.Interval = TimeSpan.FromSeconds(_context.Settings.DefaultDisplaySeconds);
      _timer.Stop();
      _timer.Start();
      return;
    }

    var slide = _slides[_currentIndex];
    if (slide.ContentType == SlideContentType.Video)
    {
      _timer.Stop();
      return;
    }

    _timer.Stop();
    _timer.Interval = TimeSpan.FromSeconds(slide.DisplaySeconds);
    _timer.Start();
  }

  private void OnTimerTick(object? sender, EventArgs e)
  {
    if (_slides.Count == 0)
    {
      _playlistReloadPending = false;
      ReloadPlaylist();
      RestartTimerForCurrentSlide();
      return;
    }

    AdvanceToNextSlide();
  }

  private void AdvanceToNextSlide()
  {
    if (_slides.Count == 0)
    {
      return;
    }

    if (_playlistReloadPending)
    {
      var nextIndex = (_currentIndex + 1) % _slides.Count;
      var preferredNextSlide = _slides[nextIndex];
      _playlistReloadPending = false;
      ApplyPlaylistReload(preferredNextSlide);
      RestartTimerForCurrentSlide();
      return;
    }

    _currentIndex = (_currentIndex + 1) % _slides.Count;
    ShowCurrentSlideOrSkip();
    RestartTimerForCurrentSlide();
  }

  private void ShowCurrentSlideOrSkip()
  {
    if (_slides.Count == 0)
    {
      HasSlides = false;
      ClearVideoState();
      CurrentImage = null;
      return;
    }

    var transitionStarted = Stopwatch.GetTimestamp();
    var attempts = 0;
    while (attempts < _slides.Count)
    {
      var slide = _slides[_currentIndex];
      try
      {
        if (slide.ContentType == SlideContentType.Video)
        {
          ShowVideoSlide(slide, transitionStarted);
          return;
        }

        ShowImageSlide(slide, transitionStarted);
        return;
      }
      catch (Exception ex)
      {
        _context.Logger.Error($"スライド読込失敗: {slide.GetDisplayName()} - {ex.Message}");
        ClearVideoState();
        CurrentImage = null;
        _currentIndex = (_currentIndex + 1) % _slides.Count;
        attempts++;
      }
    }

    ClearVideoState();
    HasSlides = false;
    CurrentImage = null;
    EmptyMessage = _context.Settings.RecoveryMessage;
    _context.Logger.Error("すべてのスライドの読み込みに失敗しました。");
  }

  private void ShowVideoSlide(Slide slide, long transitionStarted)
  {
    if (!File.Exists(slide.FilePath))
    {
      throw new FileNotFoundException("動画ファイルが見つかりません。", slide.FilePath);
    }

    _timer.Stop();
    _preloader.Cancel();

    CurrentImage = null;
    VideoSource = new Uri(slide.FilePath, UriKind.Absolute);
    IsVideoVisible = true;
    HasSlides = true;
    LogTransition(slide, transitionStarted);
    _context.Logger.Info($"表示: {slide.GetDisplayName()}");
  }

  private void ShowImageSlide(Slide slide, long transitionStarted)
  {
    ClearVideoState();

    var preloaded = _preloader.TryTakePreloaded(_currentIndex);
    CurrentImage = preloaded ?? LoadSlideContent(slide);
    HasSlides = true;
    LogTransition(slide, transitionStarted);
    _context.Logger.Info($"表示: {slide.GetDisplayName()} ({slide.DisplaySeconds} 秒)");

    QueuePreloadNextSlide();
  }

  private void LogTransition(Slide slide, long transitionStarted)
  {
    var elapsedMs = Stopwatch.GetElapsedTime(transitionStarted).TotalMilliseconds;
    var label = $"{slide.GetDisplayName()} ({elapsedMs:F0}ms)";

    if (elapsedMs > TransitionTargetMilliseconds)
    {
      _context.Logger.Warn($"スライド切替が遅延（目標 {TransitionTargetMilliseconds / 1000} 秒以内）: {label}");
      return;
    }

    _context.Logger.Info($"スライド切替: {label}");
  }

  private void ClearVideoState()
  {
    IsVideoVisible = false;
    VideoSource = null;
  }

  /// <summary>
  /// 次スライドをバックグラウンドで先読みする（動画は対象外）。
  /// </summary>
  private void QueuePreloadNextSlide()
  {
    if (_slides.Count <= 1)
    {
      return;
    }

    var nextIndex = (_currentIndex + 1) % _slides.Count;
    if (_slides[nextIndex].ContentType != SlideContentType.Image &&
        _slides[nextIndex].ContentType != SlideContentType.PdfPage)
    {
      return;
    }

    _preloader.Preload(nextIndex, _slides, LoadSlideContent);
  }

  /// <summary>
  /// スライド種別に応じて ImageSource を生成する（UI スレッド・バックグラウンド両方から呼ばれる）。
  /// </summary>
  private ImageSource LoadSlideContent(Slide slide)
  {
    switch (slide.ContentType)
    {
      case SlideContentType.Image:
        return ImageLoader.Load(slide.FilePath);

      case SlideContentType.PdfPage:
        return _pdfRenderer.RenderPage(slide.FilePath, slide.PageIndex);

      default:
        throw new InvalidOperationException($"未対応のスライド種別: {slide.ContentType}");
    }
  }

  /// <summary>
  /// 管理画面から保存された設定を実行中のスライドショーへ反映する。
  /// </summary>
  public void ApplySettings()
  {
    _playlistBuilder = new PlaylistBuilder(
      _pdfRenderer,
      _context.Logger,
      _context.Settings.DefaultDisplaySeconds);

    _folderWatcher.ContentChanged -= OnFolderContentChanged;
    _folderWatcher.Dispose();
    _folderWatcher = new ContentFolderWatcher(_context.ResolvedWatchFolder, _context.Logger);
    _folderWatcher.ContentChanged += OnFolderContentChanged;

    _playlistReloadPending = false;
    ApplyPlaylistReload(preferredNextSlide: null, startIndex: 0);
    RestartTimerForCurrentSlide();
    _context.Logger.Info("スライドショーへ設定を反映しました。");
  }

  public void Dispose()
  {
    _folderWatcher.ContentChanged -= OnFolderContentChanged;
    _folderWatcher.Dispose();
    _timer.Stop();
    _timer.Tick -= OnTimerTick;
    ClearVideoState();
    _preloader.Dispose();
    _pdfRenderer.Dispose();
  }
}
