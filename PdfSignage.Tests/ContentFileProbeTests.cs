using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSignage.Services;

namespace PdfSignage.Tests;

public class ContentFileProbeTests : IDisposable
{
  private readonly string _folder;

  public ContentFileProbeTests()
  {
    _folder = Path.Combine(Path.GetTempPath(), $"pdfsignage-probe-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_folder);
  }

  public void Dispose()
  {
    try
    {
      Directory.Delete(_folder, recursive: true);
    }
    catch (IOException)
    {
      // 一時ファイルの後始末失敗はテスト結果にしない
    }
  }

  [Fact]
  public void 空のJPEGは壊れている()
  {
    var path = CreateFile("empty.jpg", []);
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void JPEGではない中身は壊れている()
  {
    var path = CreateFile("text.jpg", "this is not a jpeg"u8.ToArray());
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void 先頭だけJPEGの破損ファイルは壊れている()
  {
    var path = CreateFile("truncated.jpg", [0xFF, 0xD8, 0xFF, 0x00]);
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void 正しいJPEGは読める()
  {
    var path = CreateFile("ok.jpg", CreateMinimalJpeg());
    Assert.True(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void 空のPDFは壊れている()
  {
    var path = CreateFile("empty.pdf", []);
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void PDFではない中身は壊れている()
  {
    var path = CreateFile("text.pdf", "not a pdf"u8.ToArray());
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void ヘッダだけの破損PDFは壊れている()
  {
    var path = CreateFile("truncated.pdf", "%PDF-1.4 truncated"u8.ToArray());
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void 正しいPDFは読める()
  {
    var path = CreateFile("ok.pdf", CreateMinimalPdf());
    Assert.True(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void 既に開いているPDFも壊れているとしない()
  {
    var path = CreateFile("open.pdf", CreateMinimalPdf());
    using var reader = Docnet.Core.DocLib.Instance.GetDocReader(
      path,
      PdfRenderDimensions.Create(8, 8));
    Assert.True(reader.GetPageCount() > 0);
    Assert.True(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void 空のMP4は壊れている()
  {
    var path = CreateFile("empty.mp4", []);
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void MP4ではない中身は壊れている()
  {
    var path = CreateFile("text.mp4", "not a video"u8.ToArray());
    Assert.False(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void ftypヘッダのあるMP4は読める扱い()
  {
    var path = CreateFile("ok.mp4", CreateFtypBox());
    Assert.True(ContentFileProbe.IsReadable(path));
  }

  [Fact]
  public void FormatBrokenFilesMessageは壊れたファイルが無ければnull()
  {
    var jpeg = CreateFile("ok.jpg", CreateMinimalJpeg());
    var pdf = CreateFile("ok.pdf", CreateMinimalPdf());
    var mp4 = CreateFile("ok.mp4", CreateFtypBox());

    Assert.Null(ContentFileProbe.FormatBrokenFilesMessage([jpeg, pdf, mp4]));
  }

  [Fact]
  public void FormatBrokenFilesMessageは案内と対象ファイル名を出す()
  {
    var brokenJpeg = CreateFile("壊れた.jpg", "nope"u8.ToArray());
    var okJpeg = CreateFile("正しい.jpg", CreateMinimalJpeg());
    var brokenPdf = CreateFile("壊れた.pdf", "%PDF"u8.ToArray());

    var message = ContentFileProbe.FormatBrokenFilesMessage([brokenJpeg, okJpeg, brokenPdf]);

    Assert.NotNull(message);
    Assert.Contains("このファイルは壊れているため表示できません。正しいファイルに差し替えてください。", message);
    Assert.Contains("壊れた.jpg", message);
    Assert.Contains("壊れた.pdf", message);
    Assert.DoesNotContain("正しい.jpg", message);
  }

  [Fact]
  public void FindUnreadableは壊れたファイルだけを返す()
  {
    var broken = CreateFile("a.jpg", []);
    var ok = CreateFile("b.jpg", CreateMinimalJpeg());

    var found = ContentFileProbe.FindUnreadable([broken, ok]);
    Assert.Equal([broken], found);
  }

  private string CreateFile(string name, byte[] bytes)
  {
    var path = Path.Combine(_folder, name);
    File.WriteAllBytes(path, bytes);
    return path;
  }

  private static byte[] CreateMinimalJpeg()
  {
    var pixels = new byte[4];
    var source = BitmapSource.Create(
      1,
      1,
      96,
      96,
      PixelFormats.Bgr32,
      null,
      pixels,
      4);

    var encoder = new JpegBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(source));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return stream.ToArray();
  }

  /// <summary>
  /// PDFium が開ける最小構成の 1 ページ PDF。xref オフセットは本文から計算する。
  /// </summary>
  private static byte[] CreateMinimalPdf()
  {
    const string header = "%PDF-1.4\n";
    string[] objects =
    [
      "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n",
      "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n",
      "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 3 3]>>endobj\n"
    ];

    var body = header + string.Concat(objects);
    var offsets = new List<int>();
    var cursor = header.Length;
    foreach (var obj in objects)
    {
      offsets.Add(cursor);
      cursor += obj.Length;
    }

    var xref = new System.Text.StringBuilder();
    xref.Append("xref\n0 4\n0000000000 65535 f \n");
    foreach (var offset in offsets)
    {
      xref.Append(offset.ToString("D10"));
      xref.Append(" 00000 n \n");
    }

    var pdf = body
              + xref
              + "trailer<</Size 4/Root 1 0 R>>\n"
              + "startxref\n"
              + body.Length
              + "\n%%EOF\n";
    return System.Text.Encoding.ASCII.GetBytes(pdf);
  }

  private static byte[] CreateFtypBox()
  {
    return
    [
      0x00, 0x00, 0x00, 0x18,
      (byte)'f', (byte)'t', (byte)'y', (byte)'p',
      (byte)'i', (byte)'s', (byte)'o', (byte)'m',
      0x00, 0x00, 0x00, 0x00,
      (byte)'i', (byte)'s', (byte)'o', (byte)'m',
      (byte)'m', (byte)'p', (byte)'4', (byte)'2'
    ];
  }
}
