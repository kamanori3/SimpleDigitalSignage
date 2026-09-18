using System.Collections.ObjectModel;
using System.Windows;
using KS_Signage.Licensing;

namespace KS_Signage.Issuer.ViewModels;

/// <summary>
/// 販売側のアクセスキー発行フォーム。
/// </summary>
public sealed class IssuerViewModel : ViewModelBase
{
  private readonly IssuedAccessKeyStore _store;
  private readonly Func<bool> _privateKeyExists;
  private readonly Func<string> _readPrivateKeyPem;
  private readonly Action<string> _copyToClipboard;
  private readonly Func<DateTimeOffset> _clock;

  private string _serialNumber = "";
  private string _organization = "";
  private LicensePlan _plan = LicensePlan.Standard;
  private DateTime? _expiresOnDate;
  private string _statusMessage = "";
  private bool _hasValidationError;
  private string _privateKeyStatus = "";
  private bool _hasPrivateKey;

  public IssuerViewModel(
    IssuedAccessKeyStore store,
    string privateKeyPath,
    Func<bool> privateKeyExists,
    Func<string> readPrivateKeyPem,
    Action<string> copyToClipboard,
    DateOnly today,
    Func<DateTimeOffset>? clock = null)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPath);
    _privateKeyExists = privateKeyExists ?? throw new ArgumentNullException(nameof(privateKeyExists));
    _readPrivateKeyPem = readPrivateKeyPem ?? throw new ArgumentNullException(nameof(readPrivateKeyPem));
    _copyToClipboard = copyToClipboard ?? throw new ArgumentNullException(nameof(copyToClipboard));
    _clock = clock ?? (() => DateTimeOffset.Now);
    _expiresOnDate = today.AddYears(1).ToDateTime(TimeOnly.MinValue);

    IssueCommand = new RelayCommand(Issue, () => CanIssue);
    CopyKeyCommand = new RelayCommand<IssuedAccessKeyRecord>(CopyKey);

    LoadLedger();
    RefreshPrivateKeyStatus(privateKeyPath);
    if (string.IsNullOrWhiteSpace(_serialNumber))
    {
      SerialNumber = SerialNumberSuggester.SuggestNext(IssuedKeys);
    }
  }

  public static IssuerViewModel CreateDefault()
  {
    var secrets = IssuerPaths.ResolveSecretsDirectory();
    var privatePath = Path.Combine(secrets, IssuerPaths.PrivateFileName);
    var store = new IssuedAccessKeyStore(Path.Combine(secrets, IssuerPaths.LedgerFileName));
    return new IssuerViewModel(
      store,
      privatePath,
      () => File.Exists(privatePath),
      () => File.ReadAllText(privatePath),
      Clipboard.SetText,
      DateOnly.FromDateTime(DateTime.Today));
  }

  public ObservableCollection<IssuedAccessKeyRecord> IssuedKeys { get; } = [];

  public string SerialNumber
  {
    get => _serialNumber;
    set
    {
      if (SetProperty(ref _serialNumber, value))
      {
        OnPropertyChanged(nameof(CanIssue));
      }
    }
  }

  public string Organization
  {
    get => _organization;
    set => SetProperty(ref _organization, value);
  }

  public bool IsStandardPlan
  {
    get => _plan == LicensePlan.Standard;
    set
    {
      if (!value)
      {
        return;
      }

      if (SetProperty(ref _plan, LicensePlan.Standard, nameof(IsStandardPlan)))
      {
        OnPropertyChanged(nameof(IsSitePlan));
      }
    }
  }

  public bool IsSitePlan
  {
    get => _plan == LicensePlan.Site;
    set
    {
      if (!value)
      {
        return;
      }

      if (SetProperty(ref _plan, LicensePlan.Site, nameof(IsSitePlan)))
      {
        OnPropertyChanged(nameof(IsStandardPlan));
      }
    }
  }

  public DateTime? ExpiresOnDate
  {
    get => _expiresOnDate;
    set
    {
      if (SetProperty(ref _expiresOnDate, value))
      {
        OnPropertyChanged(nameof(CanIssue));
      }
    }
  }

  public string StatusMessage
  {
    get => _statusMessage;
    private set
    {
      if (SetProperty(ref _statusMessage, value))
      {
        OnPropertyChanged(nameof(HasStatusMessage));
      }
    }
  }

  public bool HasStatusMessage => !string.IsNullOrWhiteSpace(_statusMessage);

  public bool HasValidationError
  {
    get => _hasValidationError;
    private set => SetProperty(ref _hasValidationError, value);
  }

  public string PrivateKeyStatus
  {
    get => _privateKeyStatus;
    private set => SetProperty(ref _privateKeyStatus, value);
  }

  public bool HasPrivateKey
  {
    get => _hasPrivateKey;
    private set
    {
      if (SetProperty(ref _hasPrivateKey, value))
      {
        OnPropertyChanged(nameof(CanIssue));
      }
    }
  }

  public bool HasIssuedKeys => IssuedKeys.Count > 0;

  public bool CanIssue =>
    HasPrivateKey
    && !string.IsNullOrWhiteSpace(SerialNumber)
    && ExpiresOnDate is not null;

  public RelayCommand IssueCommand { get; }

  public RelayCommand<IssuedAccessKeyRecord> CopyKeyCommand { get; }

  public void Issue()
  {
    if (string.IsNullOrWhiteSpace(SerialNumber))
    {
      SetError("契約の通し番号は必須です。");
      return;
    }

    if (ExpiresOnDate is null)
    {
      SetError("契約期限は必須です。");
      return;
    }

    if (!_privateKeyExists())
    {
      SetError("秘密鍵がありません。先に gen-keys を実行してください。");
      HasPrivateKey = false;
      return;
    }

    string privatePem;
    try
    {
      privatePem = _readPrivateKeyPem();
    }
    catch (Exception ex)
    {
      SetError($"秘密鍵を読めませんでした: {ex.Message}");
      return;
    }

    AccessKeyPayload payload;
    try
    {
      payload = new AccessKeyPayload(
        AccessKeyPayload.CurrentVersion,
        SerialNumber,
        Organization,
        DateOnly.FromDateTime(ExpiresOnDate.Value),
        _plan);
    }
    catch (Exception ex)
    {
      SetError(ex.Message);
      return;
    }

    string accessKey;
    try
    {
      var signer = new AccessKeySigner(privatePem);
      accessKey = signer.Sign(payload);
    }
    catch (Exception ex)
    {
      SetError($"発行に失敗しました: {ex.Message}");
      return;
    }

    var record = IssuedAccessKeyRecord.From(payload, accessKey, _clock());
    try
    {
      _store.Append(record);
    }
    catch (Exception ex)
    {
      SetError($"アクセスキーは作れましたが、台帳に書けませんでした: {ex.Message}");
      InsertIssued(record);
      TryCopy(accessKey);
      return;
    }

    InsertIssued(record);
    SerialNumber = SerialNumberSuggester.SuggestNext(IssuedKeys);
    HasValidationError = false;
    if (TryCopy(accessKey))
    {
      StatusMessage = "発行しました。クリップボードにコピー済みです。改行せずメールに貼ってください。";
    }
    else
    {
      StatusMessage = "発行しました。一覧からコピーして、改行せずメールに貼ってください。";
    }
  }

  private void CopyKey(IssuedAccessKeyRecord? record)
  {
    if (record is null || string.IsNullOrWhiteSpace(record.AccessKey))
    {
      SetError("コピーするアクセスキーがありません。");
      return;
    }

    if (TryCopy(record.AccessKey))
    {
      HasValidationError = false;
      StatusMessage = $"クリップボードにコピーしました（{record.SerialNumber}）。";
    }
  }

  private void LoadLedger()
  {
    try
    {
      var records = _store.Load()
        .OrderByDescending(r => r.IssuedAt)
        .ToList();
      IssuedKeys.Clear();
      foreach (var record in records)
      {
        IssuedKeys.Add(record);
      }

      OnPropertyChanged(nameof(HasIssuedKeys));
    }
    catch (Exception ex)
    {
      SetError($"発行台帳を読めませんでした: {ex.Message}");
    }
  }

  private void InsertIssued(IssuedAccessKeyRecord record)
  {
    IssuedKeys.Insert(0, record);
    OnPropertyChanged(nameof(HasIssuedKeys));
  }

  private void RefreshPrivateKeyStatus(string privateKeyPath)
  {
    HasPrivateKey = _privateKeyExists();
    PrivateKeyStatus = HasPrivateKey
      ? $"秘密鍵: {privateKeyPath}"
      : $"秘密鍵がありません: {privateKeyPath}。リポジトリのルートで gen-keys を実行してください。";
  }

  private bool TryCopy(string accessKey)
  {
    try
    {
      _copyToClipboard(accessKey);
      return true;
    }
    catch (Exception ex)
    {
      SetError($"クリップボードへコピーできませんでした: {ex.Message}");
      return false;
    }
  }

  private void SetError(string message)
  {
    HasValidationError = true;
    StatusMessage = message;
  }
}
