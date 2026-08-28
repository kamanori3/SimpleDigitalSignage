# SimpleDigitalSignage

PDF・画像・動画をフォルダに置くだけで運用できる Windows 向けデジタルサイネージ（WPF / C#）。

## ドキュメント

- [アプリ要件定義書](アプリ要件定義書.md)
- [開発計画](開発計画.md)

## 開発

```powershell
cd PdfSignage
dotnet run
```

## ビルド（ポータブル）

```powershell
cd PdfSignage
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 要件

- Windows 11
- .NET 8 SDK
