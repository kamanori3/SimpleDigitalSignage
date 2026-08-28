# SimpleDigitalSignage

PDF・画像・動画をフォルダに置くだけで運用できる Windows 向けデジタルサイネージ（WPF / C#）。

## ドキュメント

| ドキュメント | 内容 |
|-------------|------|
| [アプリ要件定義書](アプリ要件定義書.md) | 機能要件・仕様 |
| [開発計画](開発計画.md) | フェーズ別開発計画 |
| [操作ガイド](操作ガイド.md) | **ユーザー向け** 日常操作（コンテンツ担当・管理者） |
| [運用ガイド](運用ガイド.md) | 現場運用・ログ・権限・制限事項 |
| [受け入れテスト](受け入れテスト.md) | MVP チェックリスト |

## 開発

```powershell
cd PdfSignage
dotnet run
```

管理画面: `Ctrl+Shift+M`

## ポータブル配布（Release）

```powershell
.\scripts\publish.ps1
```

出力先: `publish/PdfSignage-win-x64/`

- self-contained（.NET ランタイム同梱）
- フォルダ一式を現場 PC にコピーして `PdfSignage.exe` を起動
- 初回起動で `settings.json` が自動作成される

## 要件

- Windows 11（win-x64）
- 開発時: .NET 8 SDK
