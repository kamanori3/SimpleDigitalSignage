using System.Windows.Media;
using KS_Signage.Models;

namespace KS_Signage.Services;

/// <summary>
/// 次スライドの ImageSource をバックグラウンドで先読みする。
/// PDF のレンダリング遅延を、表示切替の待ち時間に吸収する。
/// </summary>
public sealed class SlidePreloader : IDisposable
{
  private readonly object _lock = new();
  private CancellationTokenSource? _cancellation;
  private ImageSource? _preloadedImage;
  private int _preloadedIndex = -1;

  /// <summary>
  /// 指定インデックスのスライドをバックグラウンドで先読みする。
  /// 進行中の先読みはキャンセルされる。
  /// </summary>
  public void Preload(int slideIndex, IReadOnlyList<Slide> slides, Func<Slide, ImageSource> loadFunc)
  {
    if (slides.Count == 0)
    {
      return;
    }

    lock (_lock)
    {
      _cancellation?.Cancel();
      _cancellation?.Dispose();
      _cancellation = new CancellationTokenSource();
    }

    var token = _cancellation.Token;
    var slide = slides[slideIndex];

    Task.Run(() =>
    {
      try
      {
        var image = loadFunc(slide);
        if (token.IsCancellationRequested)
        {
          return;
        }

        lock (_lock)
        {
          if (!token.IsCancellationRequested)
          {
            _preloadedImage = image;
            _preloadedIndex = slideIndex;
          }
        }
      }
      catch
      {
        // 先読み失敗は本表示時に再試行するため、ここでは握りつぶす
      }
    }, token);
  }

  /// <summary>
  /// 先読み済みの ImageSource を取得する。インデックスが一致しない場合は null。
  /// </summary>
  public ImageSource? TryTakePreloaded(int slideIndex)
  {
    lock (_lock)
    {
      if (_preloadedIndex == slideIndex && _preloadedImage is not null)
      {
        var image = _preloadedImage;
        _preloadedImage = null;
        _preloadedIndex = -1;
        return image;
      }
    }

    return null;
  }

  public void Cancel()
  {
    lock (_lock)
    {
      _cancellation?.Cancel();
      _cancellation?.Dispose();
      _cancellation = null;
      _preloadedImage = null;
      _preloadedIndex = -1;
    }
  }

  public void Dispose()
  {
    Cancel();
  }
}
