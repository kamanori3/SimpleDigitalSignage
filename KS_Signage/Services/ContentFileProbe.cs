using System.Windows.Media.Imaging;
using Docnet.Core;

namespace KS_Signage.Services;

/// <summary>
/// 監視フォルダ内のコンテンツが実際に開けるかを調べる。
/// <para>
/// 壊れていても再生は止めずスキップする（既存の挙動）。
/// 管理モード入場時の MessageBox 用に、開けないファイル名だけを集める。
/// コピー途中の排他ロックは壊れているとはみなさない。
/// </para>
/// </summary>
public static class ContentFileProbe
{
  private static readonly byte[] JpegSoi = [0xFF, 0xD8];
  private static readonly byte[] PdfMagic = "%PDF"u8.ToArray();
  private static readonly byte[] Mp4Ftyp = "ftyp"u8.ToArray();

  /// <summary>
  /// ファイルがサイネージ表示に使える見込みがあるか。
  /// ロック中・消失は true（アラートしない）。空・ヘッダ不正・デコード失敗は false。
  /// </summary>
  public static bool IsReadable(string filePath)
  {
    try
    {
      if (ContentFolderScanner.IsImageFile(filePath))
      {
        return CanReadImage(filePath);
      }

      if (ContentFolderScanner.IsPdfFile(filePath))
      {
        return CanReadPdf(filePath);
      }

      if (ContentFolderScanner.IsVideoFile(filePath))
      {
        return CanReadVideo(filePath);
      }

      return true;
    }
    catch (IOException)
    {
      // コピー途中の排他ロックや、探査中の削除は破損として扱わない
      return true;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// 開けないファイルを、渡された順のまま返す。
  /// </summary>
  public static IReadOnlyList<string> FindUnreadable(IEnumerable<string> filePaths)
  {
    return filePaths.Where(path => !IsReadable(path)).ToList();
  }

  /// <summary>
  /// 開けないファイルがあれば案内文＋ファイル名。無ければ null。
  /// </summary>
  public static string? FormatBrokenFilesMessage(IEnumerable<string> filePaths)
  {
    var names = FindUnreadable(filePaths)
      .Select(Path.GetFileName)
      .ToList();

    if (names.Count == 0)
    {
      return null;
    }

    return "このファイルは壊れているため表示できません。正しいファイルに差し替えてください。"
           + Environment.NewLine
           + Environment.NewLine
           + string.Join(Environment.NewLine, names);
  }

  private static bool CanReadImage(string filePath)
  {
    using (var stream = OpenRead(filePath))
    {
      if (!HasPrefix(stream, JpegSoi))
      {
        return false;
      }
    }

    var bitmap = new BitmapImage();
    bitmap.BeginInit();
    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
    bitmap.CacheOption = BitmapCacheOption.OnLoad;
    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
    // 管理画面入場時の探査なので、全画素は読まず縮小デコードする
    bitmap.DecodePixelWidth = 32;
    bitmap.EndInit();
    bitmap.Freeze();
    return true;
  }

  private static bool CanReadPdf(string filePath)
  {
    using (var stream = OpenRead(filePath))
    {
      if (!HasPrefix(stream, PdfMagic))
      {
        return false;
      }
    }

    using var reader = DocLib.Instance.GetDocReader(filePath, PdfRenderDimensions.Create(8, 8));
    return reader.GetPageCount() > 0;
  }

  private static bool CanReadVideo(string filePath)
  {
    using var stream = OpenRead(filePath);
    if (stream.Length < 8)
    {
      return false;
    }

    var header = new byte[8];
    if (stream.Read(header, 0, header.Length) < header.Length)
    {
      return false;
    }

    // ISO BMFF: 先頭ボックス種別が ftyp（オフセット 4）なら MP4 として扱う
    return header.AsSpan(4, 4).SequenceEqual(Mp4Ftyp);
  }

  private static FileStream OpenRead(string filePath)
  {
    return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
  }

  private static bool HasPrefix(Stream stream, byte[] prefix)
  {
    if (stream.Length < prefix.Length)
    {
      return false;
    }

    var header = new byte[prefix.Length];
    return stream.Read(header, 0, header.Length) == prefix.Length
           && header.AsSpan().SequenceEqual(prefix);
  }
}
