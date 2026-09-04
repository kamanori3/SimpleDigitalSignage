# SimpleDigitalSignage

PDF・画像・動画をフォルダに置くだけで運用できる Windows 向けデジタルサイネージ（WPF / C#）。

指定したフォルダを監視し、`.jpg` / `.jpeg` / `.pdf` / `.mp4` を全画面で順番にループ表示します。コンテンツの更新はファイルをコピーするだけ、設定は `Ctrl+Shift+M` の管理画面からすべて GUI で行えます。

## どこから読むか

| 立場・目的 | 読む文書 |
|-----------|---------|
| 掲示物を差し替えたい | [操作ガイド](docs/操作ガイド.md)（[スライド版](docs/操作ガイド.pptx)） |
| 現場に導入する・障害を調べる | [運用ガイド](docs/運用ガイド.md) |
| コードを触る | [設計書](docs/設計書.md) → [クラス設計図](docs/クラス設計図.md) → [ADR](docs/adr/) |
| 売り方・価格・ライセンス | [課金方針](docs/課金方針.md) |
| 競合との位置づけ | [競合比較](docs/競合比較.md) |
| リリース前の確認 | [受け入れテスト](docs/受け入れテスト.md) |
| 何が変わったか知りたい | [CHANGELOG](CHANGELOG.md) |
| 当初の要件・開発経緯を辿る | [docs/archive/](docs/archive/)（凍結済み）

## 開発

```powershell
cd PdfSignage
dotnet run
```

`D:\Signage` が存在しない開発 PC では、監視フォルダは自動的に `{出力フォルダ}/SignageData` にフォールバックします。

キオスク表示中に `Ctrl+Shift+M` で管理画面。

### テスト

```powershell
dotnet test
```

対象は外部依存を持たない純粋ロジック（表示秒数の解析、自然順ソート、スケジュール時刻、入力検証、パス解決）。UI とファイル I/O を伴う部分は [受け入れテスト](docs/受け入れテスト.md) の手動確認でカバーしています。

## ポータブル配布（Release）

```powershell
.\scripts\publish.ps1
```

出力先: `publish/PdfSignage-win-x64/`

- self-contained（.NET ランタイム同梱）
- フォルダ一式を現場 PC にコピーして `PdfSignage.exe` を起動
- 初回起動で `settings.json` が自動作成される（テンプレート: `PdfSignage/settings.example.json`）
- **配布先は書き込み権限のある場所に置くこと**（`settings.json` を exe と同じ場所に保存するため）

## 動作要件

- Windows 11（win-x64）、シングルディスプレイ
- 開発時: .NET 8 SDK

## 変更時のドキュメント更新チェックリスト

仕様に触れる変更をしたら、該当する行の文書をすべて更新してから完了とする。同じ値が複数の文書に書かれているため、片方だけ直すと食い違う。

| 変更した内容 | 更新する文書 |
|-------------|-------------|
| 対応ファイル形式 | 操作ガイド（+ pptx 再生成）／運用ガイド／受け入れテスト／設計書 |
| 表示秒数の既定値・範囲 | 操作ガイド（+ pptx 再生成）／運用ガイド／設計書 |
| 管理画面の設定項目 | 操作ガイド（+ pptx 再生成）／運用ガイド／設計書／`settings.example.json` |
| Google Drive 同期 | 操作ガイド／運用ガイド／設計書／ADR 0009／受け入れテスト |
| ショートカットキー | 操作ガイド（+ pptx 再生成）／運用ガイド |
| ログの出力先・保持期間・メッセージ形式 | 運用ガイド／受け入れテスト／設計書 |
| スケジュールや復旧の挙動 | 運用ガイド／受け入れテスト／設計書 |
| クラス構成・処理の流れ | 設計書／クラス設計図 |
| 設計方針の変更 | 新しい [ADR](docs/adr/) を追加（既存の ADR は書き換えない） |
| 価格・契約・競合の方針 | [課金方針](docs/課金方針.md)／[競合比較](docs/競合比較.md) |
| リリース | `CHANGELOG.md` と `PdfSignage.csproj` の `<Version>` を同時に更新 |

`docs/操作ガイド.pptx` は生成物です。本文は `scripts/build-operation-guide-pptx.ps1` 内にあるため、操作ガイドを直したらスクリプトも直して再生成してください。

```powershell
pwsh -File scripts/build-operation-guide-pptx.ps1
```

`docs/archive/` の文書は凍結済みです。更新しないでください。
