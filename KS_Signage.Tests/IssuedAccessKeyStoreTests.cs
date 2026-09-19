using System.IO;
using KS_Signage.Issuer;
using KS_Signage.Licensing;

namespace KS_Signage.Tests;

public class SerialNumberSuggesterTests
{
  [Fact]
  public void 台帳が空ならC0001()
  {
    Assert.Equal("C-0001", SerialNumberSuggester.SuggestNext([]));
  }

  [Fact]
  public void 既存の最大番号の次を返す()
  {
    var records = new[]
    {
      Record("C-0001"),
      Record("C-0003"),
      Record("C-0002")
    };

    Assert.Equal("C-0004", SerialNumberSuggester.SuggestNext(records));
  }

  [Fact]
  public void C形式以外は無視する()
  {
    var records = new[]
    {
      Record("ACME-1"),
      Record("C-0009")
    };

    Assert.Equal("C-0010", SerialNumberSuggester.SuggestNext(records));
  }

  private static IssuedAccessKeyRecord Record(string serial)
  {
    return new IssuedAccessKeyRecord { SerialNumber = serial };
  }
}

public class IssuedAccessKeyStoreTests
{
  [Fact]
  public void 無いファイルは空で読みファイルへ追記できる()
  {
    var dir = Directory.CreateTempSubdirectory("pdfsignage-issuer-");
    try
    {
      var path = Path.Combine(dir.FullName, "issued-access-keys.json");
      var store = new IssuedAccessKeyStore(path);
      Assert.Empty(store.Load());

      var payload = new AccessKeyPayload(
        AccessKeyPayload.CurrentVersion,
        "C-0001",
        "例団体",
        new DateOnly(2027, 9, 12),
        LicensePlan.Standard);
      var record = IssuedAccessKeyRecord.From(payload, "PDS1.abc.def", new DateTimeOffset(2026, 9, 18, 11, 0, 0, TimeSpan.FromHours(9)));
      store.Append(record);

      var loaded = store.Load();
      Assert.Single(loaded);
      Assert.Equal("C-0001", loaded[0].SerialNumber);
      Assert.Equal("例団体", loaded[0].Organization);
      Assert.Equal("standard", loaded[0].Plan);
      Assert.Equal("2027-09-12", loaded[0].ExpiresOn);
      Assert.Equal("PDS1.abc.def", loaded[0].AccessKey);
      Assert.Equal("標準", loaded[0].PlanLabel);
    }
    finally
    {
      dir.Delete(true);
    }
  }
}
