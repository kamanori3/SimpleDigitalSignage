# ポータブル配布用ビルド（Phase 10）
# 出力: publish/KS_Signage-win-x64/

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "KS_Signage\KS_Signage.csproj"
$OutDir = Join-Path $Root "publish\KS_Signage-win-x64"

Write-Host "Publishing KS_Signage (Release, win-x64, self-contained)..."
dotnet publish $Project `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o $OutDir

if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Publish completed: $OutDir"
Write-Host "配布時はフォルダ一式をコピーし、KS_Signage.exe を起動してください。"
Write-Host "初回起動で settings.json が自動作成されます（settings.example.json を参照可）。"
Write-Host "Issuer（アクセスキー発行）はこの配布物に含めません。"
