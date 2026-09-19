using KS_Signage.Licensing;
using KS_Signage.ViewModels;

namespace KS_Signage.Tests;

public class LicenseAdminDisplayTests
{
  [Fact]
  public void 試用は契約番号と組織名が欠ける()
  {
    var license = new LicenseEvaluation(
      LicenseStatus.Trial,
      new DateOnly(2026, 11, 16),
      60,
      payload: null);

    Assert.Equal("試用中", LicenseAdminDisplay.StatusLabel(license.Status));
    Assert.Equal("2026-11-16（残り 60 日）", LicenseAdminDisplay.ExpiresLabel(license));
    Assert.Equal(LicenseAdminDisplay.MissingValue, LicenseAdminDisplay.SerialNumber(license.Payload));
    Assert.Equal(LicenseAdminDisplay.MissingValue, LicenseAdminDisplay.Organization(license.Payload));
  }

  [Fact]
  public void 契約中は通し番号と組織名を出す()
  {
    var payload = new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-SELF",
      "kamanori",
      new DateOnly(2036, 9, 15),
      LicensePlan.Site);
    var license = new LicenseEvaluation(
      LicenseStatus.Licensed,
      payload.ExpiresOn,
      1,
      payload);

    Assert.Equal("契約中", LicenseAdminDisplay.StatusLabel(license.Status));
    Assert.Equal("2036-09-15（残り 1 日）", LicenseAdminDisplay.ExpiresLabel(license));
    Assert.Equal("C-SELF", LicenseAdminDisplay.SerialNumber(payload));
    Assert.Equal("kamanori", LicenseAdminDisplay.Organization(payload));
  }

  [Fact]
  public void 組織名が空なら欠ける()
  {
    var payload = new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-0001",
      null,
      new DateOnly(2027, 9, 12),
      LicensePlan.Standard);

    Assert.Equal("C-0001", LicenseAdminDisplay.SerialNumber(payload));
    Assert.Equal(LicenseAdminDisplay.MissingValue, LicenseAdminDisplay.Organization(payload));
  }
}
