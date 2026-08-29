using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PdfSignage.Models;
using PdfSignage.Services;

namespace PdfSignage.ViewModels;

/// <summary>
/// メイン画面（キオスク）の ViewModel。スライドショー進行の中核。
/// <para>
/// 責務の範囲:
/// プレイリスト構築・現在スライドの表示切替・フォルダ変更の遅延反映・
/// 空フォルダ / 復帰不能の表示状態。キオスク枠やスケジュールは <c>MainWindow</c> 側。
/// </para>
/// <para>
/// 進行の起点は 2 系統:
/// 画像・PDF は <see cref="DispatcherTimer"/>、動画は View からの <see cref="OnVideoEnded"/>。
/// フォルダ変更は即リロードせず、次のスライド送りまで待つ（ADR 0003）。
/// </para>
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
  /// <summary>スライド切替の性能目標（ms）。超過すると WARN ログ。</summary>
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

  /// <summary>
  /// フォルダ変更を検知済みで、次の <see cref="AdvanceToNextSlide"/> 時に再構築するフラグ。
  /// 表示中スライドは中断しない。
  /// </summary>
  private bool _playlistReloadPending;

  private ImageSource? _currentImage;
  private Uri? _videoSource;
  private bool _isVideoVisible;
  private bool _hasSlides;

  /// <summary>
  /// true = 復帰不能画面、false かつ <see cref="HasSlides"/> が false = 空フォルダ画面。
  /// 両者は別状態（運用ガイド「画面に出るメッセージの見分け方」）。
  /// </summary>
  private bool _isRecoveryMessage;

  private string _emptyMessage = "";
  private string _recoveryMessage = "";

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

  // --- バインド用プロパティ（表示の 3 状態は HasSlides × IsRecoveryMessage で決まる） ---

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
        OnPropertyChanged(nameof(ShowRecoveryMessage));
        OnPropertyChanged(nameof(ShowEmptyFolderMessage));
      }
    }
  }

  public bool ShowImage => HasSlides && !IsVideoVisible;

  public bool ShowEmptyMessage => !HasSlides;

  public bool ShowRecoveryMessage => !HasSlides && IsRecoveryMessage;

  public bool ShowEmptyFolderMessage => !HasSlides && !IsRecoveryMessage;

  public bool IsRecoveryMessage
  {
    get => _isRecoveryMessage;
    private set
    {
      if (SetProperty(ref _isRecoveryMessage, value))
      {
        OnPropertyChanged(nameof(ShowRecoveryMessage));
        OnPropertyChanged(nameof(ShowEmptyFolderMessage));
      }
    }
  }

  public string EmptyMessage
  {
    get => _emptyMessage;
    private set => SetProperty(ref _emptyMessage, value);
  }

  public string RecoveryMessage
  {
    get => _recoveryMessage;
    private set => SetProperty(ref _recoveryMessage, value);
  }

  /// <summary>
  /// 動画再生完了（または MediaFailed）時に View から呼び出す。
  /// タイマーは動画中停止しているため、ここが次スライドへの唯一の入口。
  /// </summary>
  public void OnVideoEnded()
  {
    if (!IsVideoVisible || _slides.Count == 0)
    {
      return;
    }

    AdvanceToNextSlide();
  }

  /// <summary>
  /// 監視フォルダ変更。即リロードせず pending のみ立てる（ADR 0003）。
  /// FileSystemWatcher はワーカースレッドから来るため UI スレッドへ marshal する。
  /// </summary>
  private void OnFolderContentChanged()
  {
    Application.Current.Dispatcher.BeginInvoke(() =>
    {
      _playlistReloadPending = true;
      _context.Logger.Info("プレイリスト更新を予約しました（現在のスライド完了後に反映）。");
    });
  }

  /// <summary>起動時・空フォルダポーリング用。常に先頭から構築する。</summary>
  private void ReloadPlaylist()
  {
    ApplyPlaylistReload(preferredNextSlide: null, startIndex: 0);
  }

  /// <summary>
  /// プレイリストを再構築して表示を再開する。
  /// <paramref name="startIndex"/> 指定時はその位置、未指定時は
  /// <paramref name="preferredNextSlide"/> を手がかりに位置を復元する。
  /// </summary>
  private void ApplyPlaylistReload(Slide? preferredNextSlide, int? startIndex = null)
  {
    // 差し替えられた PDF の古いページが残らないよう、キャッシュと先読みを破棄する
    _preloader.Cancel();
    _pdfRenderer.ClearCache();
    ClearVideoState();

    var oldSlides = _slides;
    var contentFiles = ContentFolderScanner.Scan(_context.ResolvedWatchFolder);
    _slides = _playlistBuilder
      .Build(_context.ResolvedWatchFolder, _renderWidth, _renderHeight)
      .ToList();

    if (_slides.Count == 0)
    {
      _currentIndex = 0;
      HasSlides = false;
      CurrentImage = null;

      // ファイルはあるが Build で全部落ちた → 復帰不能。無いだけ → 空フォルダ（正常）
      if (contentFiles.Count > 0)
      {
        ShowRecoveryState("コンテンツファイルは存在しますが、すべて読み込みに失敗しました。");
      }
      else
      {
        IsRecoveryMessage = false;
        EmptyMessage = "表示するコンテンツがありません\n\n" +
                       $"フォルダ: {_context.ResolvedWatchFolder}";
        _context.Logger.Info("表示対象のコンテンツが見つかりませんでした。");
      }

      return;
    }

    _currentIndex = startIndex
                    ?? ResolveIndexAfterReload(oldSlides, _slides, preferredNextSlide);

    _context.Logger.Info(
      $"プレイリストを更新しました（スライド {_slides.Count} 件、表示: {_slides[_currentIndex].GetDisplayName()}）。");
    ShowCurrentSlideOrSkip();
  }

  /// <summary>
  /// 再構築後の再生位置を決める。差し替えで先頭へ巻き戻さないための処理。
  /// 1) preferred が新リストにあればそこ
  /// 2) 無ければ旧リスト上で preferred 以降→先頭側の順に、新リストに残る最初のスライド
  /// 3) どれも無ければ 0
  /// </summary>
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

  /// <summary>FilePath + PageIndex で同一スライドを探す（PDF はページ単位）。</summary>
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

  /// <summary>
  /// 現在スライド向けにタイマーを張り直す。
  /// 動画は MediaEnded 待ちなので停止。スライド 0 件時は空フォルダ復帰用のポーリング間隔にする。
  /// </summary>
  private void RestartTimerForCurrentSlide()
  {
    if (_slides.Count == 0)
    {
      // FileSystemWatcher 取りこぼしでも空→有コンテンツへ戻れるよう定期再スキャンする
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

  /// <summary>
  /// 次スライドへ進む。pending があれば「次に出るはずだったスライド」を手がかりに再構築する。
  /// </summary>
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

  /// <summary>
  /// 現在インデックスを表示する。失敗したら次へスキップし、一周全滅なら復帰不能へ。
  /// </summary>
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
        _context.Logger.Error($"スライド読込失敗: {slide.GetDisplayName()}", ex);
        ClearVideoState();
        CurrentImage = null;
        _currentIndex = (_currentIndex + 1) % _slides.Count;
        attempts++;
      }
    }

    ShowRecoveryState("すべてのスライドの読み込みに失敗しました。");
  }

  /// <summary>設定の RecoveryMessage を全面表示する異常状態。</summary>
  private void ShowRecoveryState(string logMessage)
  {
    ClearVideoState();
    HasSlides = false;
    CurrentImage = null;
    IsRecoveryMessage = true;
    RecoveryMessage = _context.Settings.RecoveryMessage;
    _context.Logger.Error(logMessage);
  }

  private void ShowVideoSlide(Slide slide, long transitionStarted)
  {
    if (!File.Exists(slide.FilePath))
    {
      throw new FileNotFoundException("動画ファイルが見つかりません。", slide.FilePath);
    }

    // 動画はタイマー進行しない。失敗時も View 側 MediaFailed → OnVideoEnded で進む
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

  /// <summary>
  /// 切替所要時間をログする。受け入れテストの性能確認が
  /// 「スライド切替: ... (Nms)」形式に依存しているので文言を変えないこと。
  /// </summary>
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
  /// 監視フォルダ・秒数の変更に合わせ PlaylistBuilder / Watcher を作り直し、
  /// 再生位置は常に先頭へ戻す（フォルダ自体が変わりうるため）。
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
    if (IsRecoveryMessage)
    {
      RecoveryMessage = _context.Settings.RecoveryMessage;
    }

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
