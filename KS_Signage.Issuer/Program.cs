using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using KS_Signage.Issuer.ViewModels;
using KS_Signage.Issuer.Views;
using KS_Signage.Licensing;

namespace KS_Signage.Issuer;

/// <summary>
/// 販売側のアクセスキー発行。引数なしでフォーム、引数ありで CLI。
/// 現場のポータブル配布には含めない。秘密鍵は標準出力に出さず、ファイルへだけ書く。
/// </summary>
public static class Program
{
  private const int AttachParentProcess = -1;

  [STAThread]
  public static int Main(string[] args)
  {
    if (args.Length == 0 || args[0] is "gui")
    {
      return RunGui();
    }

    EnsureConsole();
    return args[0] switch
    {
      "gen-keys" => GenKeys(args.AsSpan(1)),
      "issue" => Issue(args.AsSpan(1)),
      "-h" or "--help" or "help" => PrintUsageAndExit(),
      _ => Unknown(args[0])
    };
  }

  private static int RunGui()
  {
    try
    {
      var app = new Application
      {
        ShutdownMode = ShutdownMode.OnMainWindowClose
      };
      app.Run(new IssuerWindow(IssuerViewModel.CreateDefault()));
      return 0;
    }
    catch (Exception ex)
    {
      MessageBox.Show(
        ex.Message,
        "アクセスキー発行",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
      return 1;
    }
  }

  private static int PrintUsageAndExit()
  {
    PrintUsage();
    return 0;
  }

  private static int Unknown(string command)
  {
    Console.Error.WriteLine($"不明なコマンドです: {command}");
    PrintUsage();
    return 1;
  }

  private static void PrintUsage()
  {
    Console.WriteLine("""
      KS_Signage.Issuer — アクセスキーの発行（販売側専用）

      引数なし
        発行フォームを開く。作成済みキーの一覧も出す。

      gen-keys [--out-dir secrets] [--force]
        ECDSA P-256 のキーペアを作る。秘密鍵はファイルにだけ書く（標準出力には出さない）。
        既存ファイルがあるときは --force が必要。公開鍵はファイルと標準出力の両方。

      issue --id <通し番号> --plan standard|site --expires yyyy-MM-dd [--org <組織名>] [--private-key secrets/access-key-private.pem]
        アクセスキーを標準出力へ出す。発行台帳（issued-access-keys.json）にも残す。

      例:
        dotnet run --project KS_Signage.Issuer
        dotnet run --project KS_Signage.Issuer -- gen-keys
        dotnet run --project KS_Signage.Issuer -- issue --id C-0001 --plan standard --expires 2027-09-12 --org "例団体"
      """);
  }

  private static int GenKeys(ReadOnlySpan<string> args)
  {
    var outDir = GetOption(args, "--out-dir") ?? IssuerPaths.ResolveSecretsDirectory();
    var force = HasFlag(args, "--force");
    var privatePath = Path.Combine(outDir, IssuerPaths.PrivateFileName);
    var publicPath = Path.Combine(outDir, IssuerPaths.PublicFileName);

    if (!force && (File.Exists(privatePath) || File.Exists(publicPath)))
    {
      Console.Error.WriteLine(
        $"既にキーがあります: {privatePath} / {publicPath}。上書きする場合だけ --force を付けてください。");
      return 1;
    }

    var (privatePem, publicPem) = AccessKeyKeyPair.CreatePems();
    Directory.CreateDirectory(outDir);
    File.WriteAllText(privatePath, privatePem);
    File.WriteAllText(publicPath, publicPem);

    Console.WriteLine($"秘密鍵を書きました（中身は表示しません）: {Path.GetFullPath(privatePath)}");
    Console.WriteLine($"公開鍵を書きました: {Path.GetFullPath(publicPath)}");
    Console.WriteLine();
    Console.WriteLine("アプリへ埋め込む公開鍵:");
    Console.WriteLine(publicPem.TrimEnd());
    return 0;
  }

  private static int Issue(ReadOnlySpan<string> args)
  {
    var id = GetOption(args, "--id");
    var planText = GetOption(args, "--plan");
    var expiresText = GetOption(args, "--expires");
    var org = GetOption(args, "--org");
    var secretsDir = IssuerPaths.ResolveSecretsDirectory();
    var privatePath = GetOption(args, "--private-key")
                      ?? Path.Combine(secretsDir, IssuerPaths.PrivateFileName);

    if (string.IsNullOrWhiteSpace(id)
        || string.IsNullOrWhiteSpace(planText)
        || string.IsNullOrWhiteSpace(expiresText))
    {
      Console.Error.WriteLine("--id / --plan / --expires は必須です。");
      PrintUsage();
      return 1;
    }

    if (!TryParsePlan(planText, out var plan))
    {
      Console.Error.WriteLine("--plan は standard または site です。");
      return 1;
    }

    if (!DateOnly.TryParseExact(
          expiresText,
          "yyyy-MM-dd",
          CultureInfo.InvariantCulture,
          DateTimeStyles.None,
          out var expiresOn))
    {
      Console.Error.WriteLine("--expires は yyyy-MM-dd です。");
      return 1;
    }

    if (!File.Exists(privatePath))
    {
      Console.Error.WriteLine($"秘密鍵がありません: {privatePath}。先に gen-keys を実行してください。");
      return 1;
    }

    string privatePem;
    try
    {
      privatePem = File.ReadAllText(privatePath);
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"秘密鍵を読めませんでした: {ex.Message}");
      return 1;
    }

    string accessKey;
    AccessKeyPayload payload;
    try
    {
      payload = new AccessKeyPayload(
        AccessKeyPayload.CurrentVersion,
        id,
        org,
        expiresOn,
        plan);
      var signer = new AccessKeySigner(privatePem);
      accessKey = signer.Sign(payload);
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"発行に失敗しました: {ex.Message}");
      return 1;
    }

    try
    {
      var ledgerDir = Path.GetDirectoryName(Path.GetFullPath(privatePath)) ?? secretsDir;
      var store = new IssuedAccessKeyStore(Path.Combine(ledgerDir, IssuerPaths.LedgerFileName));
      store.Append(IssuedAccessKeyRecord.From(payload, accessKey, DateTimeOffset.Now));
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"アクセスキーは出せましたが、台帳に書けませんでした: {ex.Message}");
    }

    Console.WriteLine(accessKey);
    return 0;
  }

  private static bool TryParsePlan(string text, out LicensePlan plan)
  {
    if (string.Equals(text, "standard", StringComparison.OrdinalIgnoreCase))
    {
      plan = LicensePlan.Standard;
      return true;
    }

    if (string.Equals(text, "site", StringComparison.OrdinalIgnoreCase))
    {
      plan = LicensePlan.Site;
      return true;
    }

    plan = default;
    return false;
  }

  private static string? GetOption(ReadOnlySpan<string> args, string name)
  {
    for (var i = 0; i < args.Length - 1; i++)
    {
      if (args[i] == name)
      {
        return args[i + 1];
      }
    }

    return null;
  }

  private static bool HasFlag(ReadOnlySpan<string> args, string name)
  {
    foreach (var arg in args)
    {
      if (arg == name)
      {
        return true;
      }
    }

    return false;
  }

  private static void EnsureConsole()
  {
    AttachConsole(AttachParentProcess);
  }

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool AttachConsole(int dwProcessId);
}
