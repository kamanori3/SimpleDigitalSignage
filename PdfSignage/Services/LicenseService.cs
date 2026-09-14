using PdfSignage.Licensing;

namespace PdfSignage.Services;

/// <summary>
/// 試用マーカーとアクセスキーから、いまの課金状態を決める。
/// 検証失敗でもキオスクは止めない。
/// </summary>
public static class LicenseService
{
  public const string RenewalBannerMessage =
    "契約期限が切れています。更新のうえ、管理画面（Ctrl+Shift+M）にアクセスキーを入力してください。";
  public static LicenseEvaluation Evaluate(string? accessKey, FileLogger logger, DateOnly? today = null)
  {
    var todayLocal = today ?? DateOnly.FromDateTime(DateTime.Now);
    DateOnly firstLaunch;
    try
    {
      firstLaunch = TrialMarkerStore.GetOrCreate(logger, todayLocal);
    }
    catch (Exception ex)
    {
      logger.Error("試用開始日の取得に失敗しました。", ex);
      firstLaunch = todayLocal;
    }

    var keyResult = VerifyAccessKey(accessKey, logger);
    var evaluation = LicenseStateEvaluator.Evaluate(keyResult, firstLaunch, todayLocal);
    logger.Info(
      $"ライセンス状態: {evaluation.Status} / 期限 {evaluation.ExpiresOn:yyyy-MM-dd} / 残り {evaluation.RemainingDays} 日");
    return evaluation;
  }

  public static AccessKeyVerifyResult VerifyAccessKey(string? accessKey, FileLogger logger)
  {
    if (string.IsNullOrWhiteSpace(AccessKeyPublicKey.Pem))
    {
      if (!string.IsNullOrWhiteSpace(accessKey))
      {
        logger.Warn("検証用公開鍵が未設定のため、アクセスキーを無効として扱います。");
      }

      return AccessKeyVerifyResult.Invalid;
    }

    try
    {
      return new AccessKeyVerifier(AccessKeyPublicKey.Pem).Verify(accessKey);
    }
    catch (Exception ex)
    {
      logger.Error("アクセスキーの検証に失敗しました。", ex);
      return AccessKeyVerifyResult.Invalid;
    }
  }

  public static string FormatInvalidKeyMessage()
  {
    if (string.IsNullOrWhiteSpace(AccessKeyPublicKey.Pem))
    {
      return "この版ではアクセスキーをまだ検証できません。";
    }

    return "アクセスキーが正しくありません。メールで届いた文字列をそのまま貼り付けてください。";
  }
}
