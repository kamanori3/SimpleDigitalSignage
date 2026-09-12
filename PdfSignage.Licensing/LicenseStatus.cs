namespace PdfSignage.Licensing;

/// <summary>
/// 1 台の PC の課金状態。
/// </summary>
public enum LicenseStatus
{
  Trial,
  TrialExpired,
  Licensed,
  LicenseExpired
}
