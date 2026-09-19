using System.IO;
using KS_Signage.Issuer;
using KS_Signage.Issuer.ViewModels;
using KS_Signage.Licensing;

namespace KS_Signage.Tests;

public class IssuerViewModelTests
{
  [Fact]
  public void 発行すると台帳と一覧に残り通し番号が進む()
  {
    using var harness = new Harness();
    harness.ViewModel.SerialNumber = "C-0001";
    harness.ViewModel.Organization = "例団体";
    harness.ViewModel.IsSitePlan = true;
    harness.ViewModel.ExpiresOnDate = new DateTime(2027, 9, 18);

    harness.ViewModel.Issue();

    Assert.False(harness.ViewModel.HasValidationError);
    Assert.Contains("発行しました", harness.ViewModel.StatusMessage, StringComparison.Ordinal);
    Assert.Single(harness.ViewModel.IssuedKeys);
    Assert.Single(harness.Copied);
    var record = harness.ViewModel.IssuedKeys[0];
    Assert.Equal("C-0001", record.SerialNumber);
    Assert.Equal("例団体", record.Organization);
    Assert.Equal("site", record.Plan);
    Assert.Equal("2027-09-18", record.ExpiresOn);
    Assert.StartsWith(AccessKeyCodec.Prefix, record.AccessKey, StringComparison.Ordinal);
    Assert.Equal("C-0002", harness.ViewModel.SerialNumber);
    Assert.Equal(record.AccessKey, harness.Store.Load()[0].AccessKey);
  }

  [Fact]
  public void 通し番号が空なら発行しない()
  {
    using var harness = new Harness();
    harness.ViewModel.SerialNumber = "  ";
    harness.ViewModel.Issue();

    Assert.True(harness.ViewModel.HasValidationError);
    Assert.Equal("契約の通し番号は必須です。", harness.ViewModel.StatusMessage);
    Assert.Empty(harness.ViewModel.IssuedKeys);
    Assert.Empty(harness.Copied);
  }

  [Fact]
  public void 秘密鍵が無いと発行しない()
  {
    using var harness = new Harness(hasPrivateKey: false);
    harness.ViewModel.SerialNumber = "C-0001";
    harness.ViewModel.Issue();

    Assert.True(harness.ViewModel.HasValidationError);
    Assert.Contains("秘密鍵がありません", harness.ViewModel.StatusMessage, StringComparison.Ordinal);
    Assert.Empty(harness.ViewModel.IssuedKeys);
  }

  [Fact]
  public void 既存台帳から次の通し番号を入れる()
  {
    using var harness = new Harness(existing: [Record("C-0002")]);
    Assert.Equal("C-0003", harness.ViewModel.SerialNumber);
    Assert.True(harness.ViewModel.HasIssuedKeys);
    Assert.Equal("C-0002", harness.ViewModel.IssuedKeys[0].SerialNumber);
  }

  private static IssuedAccessKeyRecord Record(string serial)
  {
    return new IssuedAccessKeyRecord
    {
      IssuedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
      SerialNumber = serial,
      Plan = "standard",
      ExpiresOn = "2027-01-01",
      AccessKey = "PDS1.existing.key"
    };
  }

  private sealed class Harness : IDisposable
  {
    private readonly DirectoryInfo _dir;

    public Harness(bool hasPrivateKey = true, IReadOnlyList<IssuedAccessKeyRecord>? existing = null)
    {
      _dir = Directory.CreateTempSubdirectory("pdfsignage-issuer-vm-");
      var ledgerPath = Path.Combine(_dir.FullName, "issued-access-keys.json");
      Store = new IssuedAccessKeyStore(ledgerPath);
      if (existing is not null)
      {
        foreach (var record in existing)
        {
          Store.Append(record);
        }
      }

      var (privatePem, _) = AccessKeyKeyPair.CreatePems();
      Copied = [];
      ViewModel = new IssuerViewModel(
        Store,
        Path.Combine(_dir.FullName, "access-key-private.pem"),
        () => hasPrivateKey,
        () => privatePem,
        Copied.Add,
        new DateOnly(2026, 9, 18),
        () => new DateTimeOffset(2026, 9, 18, 11, 32, 0, TimeSpan.FromHours(9)));
    }

    public IssuedAccessKeyStore Store { get; }

    public IssuerViewModel ViewModel { get; }

    public List<string> Copied { get; }

    public void Dispose()
    {
      _dir.Delete(true);
    }
  }
}
