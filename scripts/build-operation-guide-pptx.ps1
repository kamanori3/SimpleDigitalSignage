<#
.SYNOPSIS
    docs/操作ガイド.md の内容をスライド化した PowerPoint ファイルを生成します。

.DESCRIPTION
    PowerPoint (COM) を使って 16:9 のプレゼンテーションを組み立てます。
    実行には Microsoft PowerPoint のインストールが必要です。

    スライドの本文はこのスクリプト内に直接記述しています。
    docs/操作ガイド.md を更新したら、このスクリプトも合わせて更新して再生成してください。

.EXAMPLE
    pwsh -File scripts/build-operation-guide-pptx.ps1
#>
[CmdletBinding()]
param(
    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputPath) { $OutputPath = Join-Path $repoRoot 'docs/操作ガイド.pptx' }

# ---------------------------------------------------------------- palette ---
$C = @{
    Ink        = '1B2430'
    Body       = '465263'
    Muted      = '8794A8'
    White      = 'FFFFFF'
    Accent     = '2563EB'
    AccentDeep = '1D4ED8'
    AccentSoft = 'E9F0FE'
    Teal       = '0F8E92'
    TealSoft   = 'E3F4F4'
    Amber      = 'B4690E'
    AmberSoft  = 'FDF3E3'
    Surface    = 'F5F7FA'
    Border     = 'DEE5EF'
    Deep       = '111C2E'
    DeepText   = 'C6D2E4'
}

$Font = 'Yu Gothic UI'
$Mono = 'Consolas'

# --------------------------------------------------------------- geometry ---
$SW = 960.0
$SH = 540.0
$ML = 64.0
$CW = $SW - ($ML * 2)
$ContentTop = 150.0
$rl = $ML + 428     # 2 カラムレイアウトの右カラム左端

$NL = [string][char]13

# ------------------------------------------------------------------ enums ---
$msoTrue = -1
$msoFalse = 0
$shpRect = 1
$shpRound = 5
$shpOval = 9
$shpArrow = 33

# --------------------------------------------------------------- helpers ----
function ConvertTo-OfficeColor {
    param([Parameter(Mandatory)][string] $Hex)
    $r = [Convert]::ToInt32($Hex.Substring(0, 2), 16)
    $g = [Convert]::ToInt32($Hex.Substring(2, 2), 16)
    $b = [Convert]::ToInt32($Hex.Substring(4, 2), 16)
    return $r + ($g -shl 8) + ($b -shl 16)
}

# Adjustments は COM のパラメーター付きプロパティのため、通常の代入では設定できない。
function Set-ShapeAdjustment {
    param([Parameter(Mandatory)] $Shape, [int] $Index = 1, [Parameter(Mandatory)][double] $Value)
    $adj = $Shape.Adjustments
    [void]$adj.GetType().InvokeMember(
        'Item',
        [System.Reflection.BindingFlags]::SetProperty,
        $null, $adj, @([int]$Index, [single]$Value))
}

function Get-TextWidth {
    param([string] $Text, [double] $Size)
    $w = 0.0
    foreach ($ch in $Text.ToCharArray()) {
        if ([int]$ch -lt 128) { $w += $Size * 0.56 } else { $w += $Size * 1.02 }
    }
    return $w
}

function New-Box {
    param(
        [Parameter(Mandatory)] $Slide,
        [int] $Type = 1,
        [Parameter(Mandatory)][double] $L,
        [Parameter(Mandatory)][double] $T,
        [Parameter(Mandatory)][double] $W,
        [Parameter(Mandatory)][double] $H,
        [string] $Fill,
        [string] $Border,
        [double] $BorderWeight = 0.75,
        [double] $Round = -1,
        [double] $Transparency = 0
    )
    $s = $Slide.Shapes.AddShape($Type, $L, $T, $W, $H)
    if ($Fill) {
        $s.Fill.Visible = $msoTrue
        $s.Fill.Solid()
        $s.Fill.ForeColor.RGB = ConvertTo-OfficeColor $Fill
        if ($Transparency -gt 0) { $s.Fill.Transparency = $Transparency }
    }
    else { $s.Fill.Visible = $msoFalse }

    if ($Border) {
        $s.Line.Visible = $msoTrue
        $s.Line.ForeColor.RGB = ConvertTo-OfficeColor $Border
        $s.Line.Weight = $BorderWeight
    }
    else { $s.Line.Visible = $msoFalse }

    try { $s.Shadow.Visible = $msoFalse } catch { }
    if ($Round -ge 0 -and $Type -eq $shpRound) { Set-ShapeAdjustment -Shape $s -Value $Round }
    return $s
}

function Add-Text {
    param(
        [Parameter(Mandatory)] $Slide,
        [Parameter(Mandatory)][AllowEmptyString()][string] $Text,
        [Parameter(Mandatory)][double] $L,
        [Parameter(Mandatory)][double] $T,
        [Parameter(Mandatory)][double] $W,
        [double] $H = 24,
        [double] $Size = 14,
        [switch] $Bold,
        [string] $Color = '465263',
        [int] $Align = 1,
        [double] $Line = 1.3,
        [double] $After = 0,
        [int] $Anchor = 1,
        [string] $FontName
    )
    if (-not $FontName) { $FontName = $script:Font }
    $tb = $Slide.Shapes.AddTextbox(1, $L, $T, $W, $H)
    $tb.Line.Visible = $msoFalse
    $tb.Fill.Visible = $msoFalse
    try { $tb.Shadow.Visible = $msoFalse } catch { }

    $tf = $tb.TextFrame
    $tf.WordWrap = $msoTrue
    $tf.AutoSize = 0
    $tf.MarginLeft = 0; $tf.MarginRight = 0; $tf.MarginTop = 0; $tf.MarginBottom = 0
    $tf.VerticalAnchor = $Anchor

    $tr = $tf.TextRange
    $tr.Text = $Text
    $tr.Font.Name = $FontName
    $tr.Font.NameAscii = $FontName
    $tr.Font.NameFarEast = $FontName
    $tr.Font.Size = $Size
    $tr.Font.Bold = $(if ($Bold) { $msoTrue } else { $msoFalse })
    $tr.Font.Color.RGB = ConvertTo-OfficeColor $Color
    $tr.ParagraphFormat.Alignment = $Align
    $tr.ParagraphFormat.LineRuleWithin = $msoTrue
    $tr.ParagraphFormat.SpaceWithin = $Line
    $tr.ParagraphFormat.LineRuleAfter = $msoFalse
    $tr.ParagraphFormat.SpaceAfter = $After
    $tr.ParagraphFormat.LineRuleBefore = $msoFalse
    $tr.ParagraphFormat.SpaceBefore = 0

    # AddTextbox 直後は自動フィットで高さが変わるため、指定サイズへ戻す
    # （Anchor=中央 のときは箱の高さがそのまま配置に効く）
    $tb.Width = $W
    $tb.Height = $H
    return $tb
}

function Add-Pill {
    param(
        [Parameter(Mandatory)] $Slide,
        [Parameter(Mandatory)][string] $Text,
        [double] $L = 0,
        [Parameter(Mandatory)][double] $T,
        [double] $H = 24,
        [double] $Size = 11.5,
        [string] $Fill = 'E9F0FE',
        [string] $Color = '2563EB',
        [switch] $AlignRight,
        [double] $RightEdge = 0
    )
    $w = (Get-TextWidth -Text $Text -Size $Size) + 26
    if ($AlignRight) { $L = $RightEdge - $w }
    $box = New-Box -Slide $Slide -Type $shpRound -L $L -T $T -W $w -H $H -Fill $Fill -Round 0.5
    [void](Add-Text -Slide $Slide -Text $Text -L $L -T $T -W $w -H $H -Size $Size -Bold -Color $Color -Align 2 -Anchor 3)
    return $box
}

function Add-Slide {
    param(
        [Parameter(Mandatory)] $Pres,
        [string] $Kicker = '',
        [string] $Title = '',
        [string] $Accent = '2563EB',
        [string] $Badge = '',
        [switch] $NoChrome
    )
    $slide = $Pres.Slides.Add($Pres.Slides.Count + 1, 12)
    [void](New-Box -Slide $slide -L 0 -T 0 -W $script:SW -H $script:SH -Fill $C.White)
    if ($NoChrome) { return $slide }

    [void](New-Box -Slide $slide -L $script:ML -T 46 -W 5 -H 46 -Fill $Accent)
    [void](Add-Text -Slide $slide -Text $Kicker -L ($script:ML + 17) -T 45 -W 600 -H 16 -Size 10.5 -Bold -Color $Accent)
    [void](Add-Text -Slide $slide -Text $Title -L ($script:ML + 17) -T 61 -W 640 -H 34 -Size 27 -Bold -Color $C.Ink)
    [void](New-Box -Slide $slide -L $script:ML -T 118 -W $script:CW -H 1 -Fill $C.Border)

    if ($Badge) {
        [void](Add-Pill -Slide $slide -Text $Badge -T 58 -H 26 -Size 11.5 `
                -Fill $Accent -Color $C.White -AlignRight -RightEdge ($script:SW - $script:ML))
    }

    $n = $Pres.Slides.Count
    [void](Add-Text -Slide $slide -Text '簡易デジタルサイネージ ｜ 操作ガイド' -L $script:ML -T 502 -W 400 -H 14 -Size 9 -Color $C.Muted)
    [void](Add-Text -Slide $slide -Text ([string]$n) -L ($script:SW - $script:ML - 100) -T 502 -W 100 -H 14 -Size 9 -Color $C.Muted -Align 3)
    return $slide
}

# @(@(..), @(..)) はフラット化されてしまうため、行の配列はこの関数で組み立てる。
function New-TableData {
    param([Parameter(ValueFromRemainingArguments)][object[]] $Rows)
    $list = [System.Collections.Generic.List[object]]::new()
    foreach ($r in $Rows) { $list.Add([string[]]$r) }
    return , $list.ToArray()
}

function Add-StyledTable {
    param(
        [Parameter(Mandatory)] $Slide,
        [Parameter(Mandatory)] [object[]] $Data,
        [Parameter(Mandatory)][double] $L,
        [Parameter(Mandatory)][double] $T,
        [Parameter(Mandatory)][double[]] $ColWidth,
        [double] $HeaderHeight = 34,
        [double] $RowHeight = 44,
        [string] $Accent = '2563EB',
        [double] $HeaderSize = 12.5,
        [double] $BodySize = 12.5,
        [switch] $BoldFirstColumn
    )
    $rows = $Data.Count
    $cols = $Data[0].Count
    $w = ($ColWidth | Measure-Object -Sum).Sum
    $h = $HeaderHeight + $RowHeight * ($rows - 1)

    $shape = $Slide.Shapes.AddTable($rows, $cols, $L, $T, $w, $h)
    $tbl = $shape.Table
    try { $tbl.ApplyStyle('{2D5ABB26-0587-4C30-8999-92F81FD0307C}', $true) } catch { }
    try { $tbl.FirstRow = $msoFalse; $tbl.HorizBanding = $msoFalse } catch { }

    # $c / $r はパレット変数 $C と衝突するため使わない（PowerShell の変数名は大文字小文字を区別しない）
    for ($ci = 1; $ci -le $cols; $ci++) { $tbl.Columns.Item($ci).Width = $ColWidth[$ci - 1] }

    for ($ri = 1; $ri -le $rows; $ri++) {
        for ($ci = 1; $ci -le $cols; $ci++) {
            $cell = $tbl.Cell($ri, $ci)
            $cs = $cell.Shape

            $cs.Fill.Visible = $msoTrue
            $cs.Fill.Solid()
            if ($ri -eq 1) { $cs.Fill.ForeColor.RGB = ConvertTo-OfficeColor $Accent }
            elseif ($ri % 2 -eq 1) { $cs.Fill.ForeColor.RGB = ConvertTo-OfficeColor $C.Surface }
            else { $cs.Fill.ForeColor.RGB = ConvertTo-OfficeColor $C.White }

            foreach ($b in 1, 2, 4) { $cell.Borders.Item($b).Visible = $msoFalse }
            $bottom = $cell.Borders.Item(3)
            $bottom.Visible = $msoTrue
            $bottom.Weight = $(if ($ri -eq 1) { 1.5 } else { 0.75 })
            $bottom.ForeColor.RGB = ConvertTo-OfficeColor $(if ($ri -eq 1) { $Accent } else { $C.Border })

            $tf = $cs.TextFrame
            $tf.MarginLeft = 12; $tf.MarginRight = 12; $tf.MarginTop = 5; $tf.MarginBottom = 5
            $tf.VerticalAnchor = 3

            $tr = $tf.TextRange
            $tr.Text = $Data[$ri - 1][$ci - 1]
            $tr.Font.Name = $Font
            $tr.Font.NameAscii = $Font
            $tr.Font.NameFarEast = $Font
            $tr.ParagraphFormat.Alignment = 1
            $tr.ParagraphFormat.LineRuleWithin = $msoTrue
            $tr.ParagraphFormat.SpaceWithin = 1.15

            if ($ri -eq 1) {
                $tr.Font.Size = $HeaderSize
                $tr.Font.Bold = $msoTrue
                $tr.Font.Color.RGB = ConvertTo-OfficeColor $C.White
            }
            else {
                $tr.Font.Size = $BodySize
                $isKey = ($ci -eq 1 -and $BoldFirstColumn)
                $tr.Font.Bold = $(if ($isKey) { $msoTrue } else { $msoFalse })
                $tr.Font.Color.RGB = ConvertTo-OfficeColor $(if ($isKey) { $C.Ink } else { $C.Body })
            }
        }
        $tbl.Rows.Item($ri).Height = $(if ($ri -eq 1) { $HeaderHeight } else { $RowHeight })
    }
    return $shape
}

function Add-Callout {
    param(
        [Parameter(Mandatory)] $Slide,
        [Parameter(Mandatory)][string] $Label,
        [Parameter(Mandatory)][string] $Text,
        [Parameter(Mandatory)][double] $L,
        [Parameter(Mandatory)][double] $T,
        [Parameter(Mandatory)][double] $W,
        [double] $H = 56,
        [string] $Fill = 'FDF3E3',
        [string] $Accent = 'B4690E',
        [double] $Size = 13
    )
    $labelW = (Get-TextWidth -Text $Label -Size ($Size - 1)) + 20
    [void](New-Box -Slide $Slide -Type $shpRound -L $L -T $T -W $W -H $H -Fill $Fill -Round 0.12)
    [void](New-Box -Slide $Slide -L $L -T $T -W 4 -H $H -Fill $Accent)
    [void](Add-Text -Slide $Slide -Text $Label -L ($L + 22) -T $T -W $labelW -H $H -Size ($Size - 1) -Bold -Color $Accent -Anchor 3)
    [void](Add-Text -Slide $Slide -Text $Text -L ($L + 22 + $labelW) -T $T -W ($W - 44 - $labelW) -H $H -Size $Size -Color $C.Body -Anchor 3 -Line 1.25)
}

function Add-StepCard {
    param(
        [Parameter(Mandatory)] $Slide,
        [Parameter(Mandatory)][string] $Number,
        [Parameter(Mandatory)][string] $Title,
        [Parameter(Mandatory)][string] $Body,
        [Parameter(Mandatory)][double] $L,
        [Parameter(Mandatory)][double] $T,
        [Parameter(Mandatory)][double] $W,
        [Parameter(Mandatory)][double] $H,
        [string] $Accent = '2563EB',
        [string] $AccentSoft = 'E9F0FE'
    )
    [void](New-Box -Slide $Slide -Type $shpRound -L $L -T $T -W $W -H $H -Fill $C.White -Border $C.Border -Round 0.08)
    [void](New-Box -Slide $Slide -Type $shpOval -L ($L + 24) -T ($T + 24) -W 36 -H 36 -Fill $AccentSoft)
    [void](Add-Text -Slide $Slide -Text $Number -L ($L + 24) -T ($T + 24) -W 36 -H 36 -Size 15 -Bold -Color $Accent -Align 2 -Anchor 3)
    [void](Add-Text -Slide $Slide -Text $Title -L ($L + 24) -T ($T + 74) -W ($W - 48) -H 26 -Size 16 -Bold -Color $C.Ink)
    [void](Add-Text -Slide $Slide -Text $Body -L ($L + 24) -T ($T + 104) -W ($W - 48) -H ($H - 120) -Size 12.5 -Color $C.Body -Line 1.4)
}

function Add-Faq {
    param(
        [Parameter(Mandatory)] $Slide,
        [Parameter(Mandatory)][string] $Question,
        [Parameter(Mandatory)][string] $Answer,
        [Parameter(Mandatory)][double] $T,
        [double] $H = 92
    )
    [void](New-Box -Slide $Slide -Type $shpRound -L $ML -T $T -W $CW -H $H -Fill $C.Surface -Round 0.16)
    [void](New-Box -Slide $Slide -Type $shpOval -L ($ML + 22) -T ($T + 20) -W 30 -H 30 -Fill $C.Accent)
    [void](Add-Text -Slide $Slide -Text 'Q' -L ($ML + 22) -T ($T + 20) -W 30 -H 30 -Size 14 -Bold -Color $C.White -Align 2 -Anchor 3)
    [void](Add-Text -Slide $Slide -Text $Question -L ($ML + 66) -T ($T + 18) -W ($CW - 96) -H 24 -Size 14.5 -Bold -Color $C.Ink)
    [void](Add-Text -Slide $Slide -Text $Answer -L ($ML + 66) -T ($T + 45) -W ($CW - 96) -H ($H - 58) -Size 12.5 -Color $C.Body -Line 1.35)
}

# ------------------------------------------------------------------ build ---
Write-Host 'PowerPoint を起動しています...'
$ppt = New-Object -ComObject PowerPoint.Application
$ppt.Visible = $true
$pres = $ppt.Presentations.Add()
$pres.PageSetup.SlideSize = 15
$pres.PageSetup.SlideWidth = $SW
$pres.PageSetup.SlideHeight = $SH

try {
    # ---------------------------------------------------------- 1. 表紙 ---
    $s = Add-Slide -Pres $pres -NoChrome
    [void](New-Box -Slide $s -L 0 -T 0 -W $SW -H $SH -Fill $C.Deep)
    [void](New-Box -Slide $s -Type $shpOval -L 610 -T -140 -W 520 -H 520 -Fill $C.Accent -Transparency 0.82)
    [void](New-Box -Slide $s -Type $shpOval -L 730 -T 250 -W 380 -H 380 -Fill $C.Teal -Transparency 0.86)
    [void](New-Box -Slide $s -L $ML -T 150 -W 68 -H 5 -Fill $C.Accent)

    [void](Add-Text -Slide $s -Text 'SIMPLE DIGITAL SIGNAGE' -L $ML -T 118 -W 500 -H 20 -Size 12.5 -Bold -Color '7FA6F5')
    [void](Add-Text -Slide $s -Text '操作ガイド' -L $ML -T 180 -W 620 -H 72 -Size 52 -Bold -Color $C.White)
    [void](Add-Text -Slide $s -Text ('フォルダに入れるだけ。あとは全画面で自動ループ。' + $NL + 'PDF・画像・動画をそのまま掲示できるサイネージアプリの使いかた。') `
            -L $ML -T 266 -W 560 -H 60 -Size 15 -Color $C.DeepText -Line 1.5)

    [void](Add-Pill -Slide $s -Text 'コンテンツ担当者向け' -L $ML -T 358 -H 30 -Size 12 -Fill '1E3A6E' -Color 'BBD1FA')
    [void](Add-Pill -Slide $s -Text 'システム管理者向け' -L ($ML + 168) -T 358 -H 30 -Size 12 -Fill '17414A' -Color 'A9DDDF')

    [void](New-Box -Slide $s -L $ML -T 432 -W 300 -H 1 -Fill '2E4059')
    [void](Add-Text -Slide $s -Text '2026年8月版　｜　操作ガイド.md より作成' -L $ML -T 448 -W 480 -H 18 -Size 11 -Color '7C8CA6')

    # -------------------------------------------------- 2. 対象と役割 ---
    $s = Add-Slide -Pres $pres -Kicker 'OVERVIEW' -Title 'このガイドの対象と役割'
    [void](Add-Text -Slide $s -Text '日常の運用は 2 つの役割に分かれます。自分の役割のページだけ読めば運用できます。' `
            -L $ML -T $ContentTop -W $CW -H 22 -Size 13.5 -Color $C.Body)

    $cardY = 186.0
    $cardH = 236.0
    $cardW = 404.0

    [void](New-Box -Slide $s -Type $shpRound -L $ML -T $cardY -W $cardW -H $cardH -Fill $C.White -Border $C.Border -Round 0.06)
    [void](New-Box -Slide $s -L $ML -T $cardY -W $cardW -H 5 -Fill $C.Teal)
    [void](Add-Pill -Slide $s -Text 'ROLE 01' -L ($ML + 26) -T ($cardY + 26) -H 22 -Size 10 -Fill $C.TealSoft -Color $C.Teal)
    [void](Add-Text -Slide $s -Text 'コンテンツ担当者' -L ($ML + 26) -T ($cardY + 58) -W ($cardW - 52) -H 30 -Size 21 -Bold -Color $C.Ink)
    [void](Add-Text -Slide $s -Text '表示するファイルを扱う人' -L ($ML + 26) -T ($cardY + 90) -W ($cardW - 52) -H 20 -Size 12 -Color $C.Muted)
    [void](Add-Text -Slide $s -Text (@(
                '・表示したいファイルをフォルダに入れる',
                '・不要になったファイルを削除する',
                '・ファイル名で表示順と表示時間を調整する'
            ) -join $NL) -L ($ML + 26) -T ($cardY + 124) -W ($cardW - 52) -H 90 -Size 13.5 -Color $C.Body -Line 1.35 -After 8)

    $l2 = $ML + $cardW + 24
    [void](New-Box -Slide $s -Type $shpRound -L $l2 -T $cardY -W $cardW -H $cardH -Fill $C.White -Border $C.Border -Round 0.06)
    [void](New-Box -Slide $s -L $l2 -T $cardY -W $cardW -H 5 -Fill $C.Accent)
    [void](Add-Pill -Slide $s -Text 'ROLE 02' -L ($l2 + 26) -T ($cardY + 26) -H 22 -Size 10 -Fill $C.AccentSoft -Color $C.Accent)
    [void](Add-Text -Slide $s -Text 'システム管理者' -L ($l2 + 26) -T ($cardY + 58) -W ($cardW - 52) -H 30 -Size 21 -Bold -Color $C.Ink)
    [void](Add-Text -Slide $s -Text 'アプリと設定を扱う人' -L ($l2 + 26) -T ($cardY + 90) -W ($cardW - 52) -H 20 -Size 12 -Color $C.Muted)
    [void](Add-Text -Slide $s -Text (@(
                '・アプリの起動と管理画面の操作',
                '・監視フォルダ・表示秒数などの設定',
                '・夜間停止と朝の自動起動、ログの確認'
            ) -join $NL) -L ($l2 + 26) -T ($cardY + 124) -W ($cardW - 52) -H 90 -Size 13.5 -Color $C.Body -Line 1.35 -After 8)

    Add-Callout -Slide $s -Label 'MEMO' -Text 'ログ・権限・技術的な制限事項などの詳細は「運用ガイド」を参照してください。' `
        -L $ML -T 444 -W $CW -H 46 -Fill $C.Surface -Accent $C.Muted -Size 12.5

    # ---------------------------------------------------- 3. 表示のしくみ ---
    $s = Add-Slide -Pres $pres -Kicker 'HOW IT WORKS' -Title '表示のしくみ（全体像）'
    [void](Add-Text -Slide $s -Text '操作はフォルダにファイルを置くだけ。検知も表示もアプリが自動で行います。' `
            -L $ML -T $ContentTop -W $CW -H 22 -Size 13.5 -Color $C.Body)

    # $sw / $sh はスライド幅・高さの $SW / $SH と衝突するため使わない
    $stepY = 190.0
    $stepH = 186.0
    $stepW = 250.0
    $xs = @($ML, ($ML + $stepW + 41), ($ML + ($stepW + 41) * 2))
    Add-StepCard -Slide $s -Number '1' -Title 'ファイルを置く' -Body ('指定した監視フォルダの直下に' + $NL + 'PDF・画像・動画を保存します。') -L $xs[0] -T $stepY -W $stepW -H $stepH
    Add-StepCard -Slide $s -Number '2' -Title 'アプリが検知' -Body ('追加・削除を自動で見つけて' + $NL + '再生リストを作り直します。') -L $xs[1] -T $stepY -W $stepW -H $stepH
    Add-StepCard -Slide $s -Number '3' -Title '全画面でループ' -Body ('順番に全画面表示し、' + $NL + '最後まで行くと先頭へ戻ります。') -L $xs[2] -T $stepY -W $stepW -H $stepH

    foreach ($ax in @(($xs[0] + $stepW + 8), ($xs[1] + $stepW + 8))) {
        [void](New-Box -Slide $s -Type $shpArrow -L $ax -T ($stepY + 72) -W 25 -H 20 -Fill 'BFD0F2')
    }

    Add-Callout -Slide $s -Label '反映のタイミング' -Text 'ファイルを追加・削除しても、いま表示しているスライドが終わったあとに切り替わります。普段は全画面（キオスクモード）のまま運用します。' `
        -L $ML -T 404 -W $CW -H 62 -Fill $C.AccentSoft -Accent $C.Accent -Size 13

    # ------------------------------------------ 4. 担当者：ファイル追加 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR CONTENT OWNER' -Title 'ファイルを追加する・止める' -Accent $C.Teal -Badge 'コンテンツ担当者'
    $rowY = 152.0
    $rowH = 60.0
    $steps = @(
        @('1', '監視フォルダを開く', '本番の例：D:\Signage（場所は管理者に確認）'),
        @('2', 'ファイルをフォルダ直下にコピーする', '対応形式は JPG / JPEG・PDF・MP4'),
        @('3', '自動で表示に反映される', '現在のスライドが終わったタイミングで切り替わります')
    )
    foreach ($st in $steps) {
        [void](New-Box -Slide $s -Type $shpRound -L $ML -T $rowY -W $CW -H $rowH -Fill $C.White -Border $C.Border -Round 0.14)
        [void](New-Box -Slide $s -Type $shpOval -L ($ML + 20) -T ($rowY + 16) -W 30 -H 30 -Fill $C.TealSoft)
        [void](Add-Text -Slide $s -Text $st[0] -L ($ML + 20) -T ($rowY + 16) -W 30 -H 30 -Size 14 -Bold -Color $C.Teal -Align 2 -Anchor 3)
        [void](Add-Text -Slide $s -Text $st[1] -L ($ML + 64) -T ($rowY + 12) -W 400 -H 22 -Size 15 -Bold -Color $C.Ink)
        [void](Add-Text -Slide $s -Text $st[2] -L ($ML + 64) -T ($rowY + 34) -W ($CW - 96) -H 20 -Size 12 -Color $C.Muted)
        $rowY += $rowH + 12
    }

    Add-Callout -Slide $s -Label '注意' -Text 'サブフォルダの中のファイルは表示されません。必ずフォルダ直下に置いてください。' `
        -L $ML -T 372 -W $CW -H 50
    Add-Callout -Slide $s -Label '止めるとき' -Text '監視フォルダから該当のファイルを削除するだけです。反映はやはり現在のスライドが終わったあとです。' `
        -L $ML -T 430 -W $CW -H 50 -Fill $C.TealSoft -Accent $C.Teal

    # ------------------------------------------- 5. 担当者：ファイル種類 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR CONTENT OWNER' -Title '使えるファイルの種類' -Accent $C.Teal -Badge 'コンテンツ担当者'
    [void](Add-Text -Slide $s -Text '次の 3 種類だけが表示対象です。それ以外の形式は無視されます。' `
            -L $ML -T $ContentTop -W $CW -H 22 -Size 13.5 -Color $C.Body)
    $d = New-TableData `
        @('種類', '拡張子', '表示のしかた') `
        @('画像', '.jpg / .jpeg', '1 ファイル = 1 枚のスライド') `
        @('PDF', '.pdf', '1 ページ = 1 枚のスライド（ページ順に表示）') `
        @('動画', '.mp4', '最後まで再生してから次のスライドへ')
    [void](Add-StyledTable -Slide $s -Data $d -L $ML -T 192 -ColWidth @(150, 190, 492) -HeaderHeight 36 -RowHeight 54 -Accent $C.Teal -BoldFirstColumn)
    Add-Callout -Slide $s -Label '使えない形式' -Text 'PNG・GIF・MOV などは表示できません。JPEG・PDF・MP4 に変換してから置いてください。' `
        -L $ML -T 410 -W $CW -H 52

    # ------------------------------------------------- 6. 担当者：表示順 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR CONTENT OWNER' -Title '表示順はファイル名で決まる' -Accent $C.Teal -Badge 'コンテンツ担当者'
    [void](Add-Text -Slide $s -Text 'ファイル名の自然な順（人が読む順に近い並び）で表示します。' `
            -L $ML -T $ContentTop -W $CW -H 22 -Size 13.5 -Color $C.Body)
    $d = New-TableData `
        @('ファイル名の例', '実際の表示順') `
        @('001.jpg → 002.jpg → 010.jpg', '001 → 002 → 010（意図どおり）') `
        @('1.jpg → 10.jpg → 2.jpg', '1 → 10 → 2（意図とずれることがある）')
    [void](Add-StyledTable -Slide $s -Data $d -L $ML -T 192 -ColWidth @(430, 402) -HeaderHeight 36 -RowHeight 58 -Accent $C.Teal)
    Add-Callout -Slide $s -Label 'おすすめ' -Text '先頭を 001・002・003 … のように桁を揃えておくと、並び替えも差し替えも管理しやすくなります。' `
        -L $ML -T 376 -W $CW -H 60 -Fill $C.TealSoft -Accent $C.Teal

    # ----------------------------------------------- 7. 担当者：表示時間 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR CONTENT OWNER' -Title '表示時間はファイル名で変えられる' -Accent $C.Teal -Badge 'コンテンツ担当者'
    [void](Add-Text -Slide $s -Text '画像と PDF は、拡張子の直前に「_秒数」を付けると、そのファイルだけ表示時間を変えられます。' `
            -L $ML -T $ContentTop -W $CW -H 22 -Size 13.5 -Color $C.Body)

    [void](New-Box -Slide $s -Type $shpRound -L $ML -T 186 -W $CW -H 84 -Fill '10233D' -Round 0.12)
    [void](Add-Text -Slide $s -Text '案内_120.pdf' -L ($ML + 34) -T 200 -W 400 -H 34 -Size 26 -Bold -Color 'FFFFFF' -FontName $Mono)
    [void](Add-Text -Slide $s -Text '「_120」の部分が表示秒数。付けなければ管理者が決めた標準の秒数になります。' `
            -L ($ML + 34) -T 236 -W ($CW - 68) -H 22 -Size 12.5 -Color '9FB4D4')

    $ex = @(
        @('案内.pdf', '標準の秒数', '管理画面の設定値（初期値 15 秒）'),
        @('案内_120.pdf', '120 秒', '2 分間しっかり読ませたい掲示に'),
        @('写真_30.jpg', '30 秒', '短めに流したい写真に')
    )
    $cx = $ML
    foreach ($e in $ex) {
        [void](New-Box -Slide $s -Type $shpRound -L $cx -T 292 -W 264 -H 110 -Fill $C.White -Border $C.Border -Round 0.1)
        [void](Add-Text -Slide $s -Text $e[0] -L ($cx + 20) -T 310 -W 224 -H 22 -Size 14 -Bold -Color $C.Ink -FontName $Mono)
        [void](Add-Text -Slide $s -Text $e[1] -L ($cx + 20) -T 336 -W 224 -H 26 -Size 19 -Bold -Color $C.Teal)
        [void](Add-Text -Slide $s -Text $e[2] -L ($cx + 20) -T 366 -W 224 -H 30 -Size 11.5 -Color $C.Muted -Line 1.25)
        $cx += 284
    }

    Add-Callout -Slide $s -Label '動画は例外' -Text '.mp4 は再生が終わるまで表示します。秒数指定は効きません。' `
        -L $ML -T 420 -W $CW -H 48

    # ------------------------------------------------- 8. 担当者：注意点 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR CONTENT OWNER' -Title '知っておきたい 3 つのこと' -Accent $C.Teal -Badge 'コンテンツ担当者'
    $notes = @(
        @('コピー中は反映されない', 'ファイルのコピー途中だと読み込みに失敗することがあります。コピーが完了すれば自動で反映されます。'),
        @('壊れたファイルはスキップ', '読めないファイルは自動で飛ばし、残りのファイルは続けて表示されます。掲示が止まることはありません。'),
        @('設定変更は管理者が行う', 'フォルダの場所や標準の表示秒数などの変更は、管理者が管理画面で行います。')
    )
    $ny = 168.0
    foreach ($n in $notes) {
        [void](New-Box -Slide $s -Type $shpRound -L $ML -T $ny -W $CW -H 96 -Fill $C.White -Border $C.Border -Round 0.14)
        [void](New-Box -Slide $s -L $ML -T $ny -W 4 -H 96 -Fill $C.Teal)
        [void](Add-Text -Slide $s -Text $n[0] -L ($ML + 28) -T ($ny + 22) -W ($CW - 56) -H 24 -Size 16 -Bold -Color $C.Ink)
        [void](Add-Text -Slide $s -Text $n[1] -L ($ML + 28) -T ($ny + 50) -W ($CW - 56) -H 34 -Size 12.5 -Color $C.Body -Line 1.35)
        $ny += 110
    }

    # ------------------------------------------- 9. 管理者：起動と管理画面 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR ADMINISTRATOR' -Title 'アプリの起動と管理画面' -Badge 'システム管理者'

    [void](New-Box -Slide $s -Type $shpRound -L $ML -T $ContentTop -W 404 -H 212 -Fill $C.White -Border $C.Border -Round 0.07)
    [void](Add-Pill -Slide $s -Text 'START' -L ($ML + 26) -T ($ContentTop + 24) -H 22 -Size 10 -Fill $C.AccentSoft -Color $C.Accent)
    [void](Add-Text -Slide $s -Text 'アプリを起動する' -L ($ML + 26) -T ($ContentTop + 58) -W 352 -H 28 -Size 19 -Bold -Color $C.Ink)
    [void](Add-Text -Slide $s -Text (@(
                '1.  配布フォルダの PdfSignage.exe をダブルクリック',
                '2.  全画面でサイネージ表示が始まる',
                '3.  初回起動時は設定ファイルが自動作成される'
            ) -join $NL) -L ($ML + 26) -T ($ContentTop + 100) -W 352 -H 90 -Size 13 -Color $C.Body -Line 1.35 -After 8)

    [void](New-Box -Slide $s -Type $shpRound -L $rl -T $ContentTop -W 404 -H 212 -Fill '10233D' -Round 0.07)
    [void](Add-Pill -Slide $s -Text 'SHORTCUT' -L ($rl + 26) -T ($ContentTop + 24) -H 22 -Size 10 -Fill '1E3A6E' -Color 'BBD1FA')
    [void](Add-Text -Slide $s -Text '管理画面を開く / 閉じる' -L ($rl + 26) -T ($ContentTop + 58) -W 352 -H 28 -Size 19 -Bold -Color $C.White)

    $kx = $rl + 26
    foreach ($k in @('Ctrl', 'Shift', 'M')) {
        $kw = (Get-TextWidth -Text $k -Size 15) + 34
        [void](New-Box -Slide $s -Type $shpRound -L $kx -T ($ContentTop + 100) -W $kw -H 44 -Fill '25406E' -Border '3E5C8F' -Round 0.18)
        [void](Add-Text -Slide $s -Text $k -L $kx -T ($ContentTop + 100) -W $kw -H 44 -Size 15 -Bold -Color $C.White -Align 2 -Anchor 3)
        $kx += $kw
        if ($k -ne 'M') {
            [void](Add-Text -Slide $s -Text '+' -L $kx -T ($ContentTop + 100) -W 26 -H 44 -Size 15 -Bold -Color '7FA6F5' -Align 2 -Anchor 3)
            $kx += 26
        }
    }
    [void](Add-Text -Slide $s -Text '同時に押すと管理画面が開きます。もう一度押すか「通常モード（キオスク）に戻る」でキオスク表示へ戻ります。' `
            -L ($rl + 26) -T ($ContentTop + 156) -W 352 -H 40 -Size 11.5 -Color '9FB4D4' -Line 1.3)

    Add-Callout -Slide $s -Label '保存を忘れずに' -Text '値を変更したら「保存して設定する」をクリック。再起動なしで即時反映されます。画面下部が緑なら成功、赤なら入力内容を確認してください。' `
        -L $ML -T 396 -W $CW -H 66 -Fill $C.AccentSoft -Accent $C.Accent

    # ---------------------------------------------- 10. 管理者：設定項目 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR ADMINISTRATOR' -Title '管理画面の設定項目' -Badge 'システム管理者'
    $d = New-TableData `
        @('設定項目', '説明') `
        @('監視フォルダ', 'コンテンツを置くフォルダ。Drive URL 指定時は使わない') `
        @('Google Drive フォルダ URL', '公開フォルダのリンク。空ならローカルフォルダ運用') `
        @('Google Drive API キー', 'Drive URL を使うときに必須') `
        @('デフォルト表示秒数', '画像・PDF の標準表示時間（5〜300 秒）') `
        @('Windows 起動時に自動起動', 'ON にすると PC 起動・ログオン後にアプリが自動で立ち上がる') `
        @('アプリ終了時刻', '毎日、指定時刻にアプリを終了する（例：18:00）。無効にすれば終了しない') `
        @('PC 電源オフ時刻', '毎日、指定時刻に PC の電源を切る（例：18:03）。無効にすれば電源オフしない') `
        @('復帰不能時メッセージ', 'すべてのファイルが表示できないときに全画面へ出す文言')
    [void](Add-StyledTable -Slide $s -Data $d -L $ML -T 146 -ColWidth @(252, 580) -HeaderHeight 34 -RowHeight 42 -BoldFirstColumn -BodySize 12)
    Add-Callout -Slide $s -Label '時刻の入力形式' -Text 'HH:mm（24 時間制）で入力します。午後 6 時は 18:00、午前 9 時 30 分は 09:30。' `
        -L $ML -T 444 -W $CW -H 50 -Fill $C.AccentSoft -Accent $C.Accent

    # ---------------------------------------------- 11. 管理者：夜間停止 ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR ADMINISTRATOR' -Title '夜に止めて、朝は自動で始める' -Badge 'システム管理者'
    [void](Add-Text -Slide $s -Text 'スケジュール設定と Windows 自動起動を組み合わせると、毎日の入切が不要になります。' `
            -L $ML -T $ContentTop -W $CW -H 22 -Size 13.5 -Color $C.Body)

    $ty = 250.0
    [void](New-Box -Slide $s -L ($ML + 40) -T ($ty + 30) -W ($CW - 80) -H 2 -Fill $C.Border)
    $nodes = @(
        @('18:00', 'アプリ終了', 'サイネージ表示を終了'),
        @('18:03', 'PC 電源オフ', '任意。無効にもできる'),
        @('翌朝', 'PC 起動・ログオン', '出勤時や電源スケジュールで'),
        @('自動', 'サイネージ開始', 'Windows 自動起動が ON なら')
    )
    $nx = $ML
    $nw = ($CW - 3 * 16) / 4
    $i = 0
    foreach ($nd in $nodes) {
        $isNight = ($i -lt 2)
        $col = $(if ($isNight) { $C.Accent } else { $C.Teal })
        $soft = $(if ($isNight) { $C.AccentSoft } else { $C.TealSoft })
        [void](Add-Text -Slide $s -Text $nd[0] -L $nx -T ($ty - 34) -W $nw -H 24 -Size 17 -Bold -Color $col -Align 2)
        [void](New-Box -Slide $s -Type $shpOval -L ($nx + $nw / 2 - 9) -T ($ty + 22) -W 18 -H 18 -Fill $C.White -Border $col -BorderWeight 2.25)
        [void](New-Box -Slide $s -Type $shpRound -L $nx -T ($ty + 60) -W $nw -H 96 -Fill $soft -Round 0.1)
        [void](Add-Text -Slide $s -Text $nd[1] -L ($nx + 16) -T ($ty + 78) -W ($nw - 32) -H 24 -Size 14.5 -Bold -Color $C.Ink -Align 2)
        [void](Add-Text -Slide $s -Text $nd[2] -L ($nx + 16) -T ($ty + 106) -W ($nw - 32) -H 36 -Size 11.5 -Color $C.Body -Align 2 -Line 1.25)
        $nx += $nw + 16
        $i++
    }

    Add-Callout -Slide $s -Label '補足' -Text 'アプリ終了時刻を有効にすると、PC 電源オフの初期値はその 3 分後になります（変更可）。電源オフに失敗した場合はログに記録されるため、社内の PC 管理ポリシーを確認してください。' `
        -L $ML -T 428 -W $CW -H 60 -Fill $C.Surface -Accent $C.Muted -Size 12.5

    # ------------------------------------------- 12. 管理者：終了とログ ---
    $s = Add-Slide -Pres $pres -Kicker 'FOR ADMINISTRATOR' -Title 'アプリの終了とログの場所' -Badge 'システム管理者'

    [void](New-Box -Slide $s -Type $shpRound -L $ML -T $ContentTop -W 404 -H 292 -Fill $C.White -Border $C.Border -Round 0.05)
    [void](New-Box -Slide $s -L $ML -T $ContentTop -W 404 -H 5 -Fill $C.Accent)
    [void](Add-Text -Slide $s -Text 'アプリを終了する' -L ($ML + 26) -T ($ContentTop + 30) -W 352 -H 28 -Size 19 -Bold -Color $C.Ink)
    $ey = $ContentTop + 76
    $exits = @(
        @('管理画面から', 'Ctrl ＋ Shift ＋ M →「アプリを終了」'),
        @('管理画面を閉じる', '管理画面の × ボタンで閉じるとアプリも終了'),
        @('スケジュール', '設定した終了時刻に到達すると自動で終了')
    )
    foreach ($e in $exits) {
        [void](Add-Text -Slide $s -Text $e[0] -L ($ML + 26) -T $ey -W 352 -H 20 -Size 13 -Bold -Color $C.Accent)
        [void](Add-Text -Slide $s -Text $e[1] -L ($ML + 26) -T ($ey + 22) -W 352 -H 34 -Size 12.5 -Color $C.Body -Line 1.3)
        $ey += 64
    }

    [void](New-Box -Slide $s -Type $shpRound -L $rl -T $ContentTop -W 404 -H 292 -Fill $C.White -Border $C.Border -Round 0.05)
    [void](New-Box -Slide $s -L $rl -T $ContentTop -W 404 -H 5 -Fill $C.Amber)
    [void](Add-Text -Slide $s -Text 'ログを確認する' -L ($rl + 26) -T ($ContentTop + 30) -W 352 -H 28 -Size 19 -Bold -Color $C.Ink)
    [void](Add-Text -Slide $s -Text '表示に問題があるときは、管理者がログを確認できます。' `
            -L ($rl + 26) -T ($ContentTop + 66) -W 352 -H 20 -Size 12 -Color $C.Muted)
    $ly = $ContentTop + 96
    $logs = @(
        @('場所', '監視フォルダと同じ階層の logs フォルダ'),
        @('例', '監視フォルダが D:\Signage → ログは D:\logs'),
        @('保存期間', '7 日間（古いログは自動削除）')
    )
    foreach ($lg in $logs) {
        [void](New-Box -Slide $s -L ($rl + 26) -T ($ly + 4) -W 3 -H 40 -Fill $C.AmberSoft)
        [void](Add-Text -Slide $s -Text $lg[0] -L ($rl + 42) -T $ly -W 352 -H 18 -Size 11.5 -Bold -Color $C.Amber)
        [void](Add-Text -Slide $s -Text $lg[1] -L ($rl + 42) -T ($ly + 20) -W 336 -H 30 -Size 12.5 -Color $C.Body -Line 1.3)
        $ly += 56
    }

    # ------------------------------------------------------- 13-14. FAQ ---
    $s = Add-Slide -Pres $pres -Kicker 'FAQ' -Title 'よくある質問（1/2）'
    Add-Faq -Slide $s -T 152 -Question 'ファイルを入れたのに、すぐ変わりません' `
        -Answer 'いま表示中のスライドが終わるまで待ちます。動画の場合は再生が完了するまで切り替わりません。'
    Add-Faq -Slide $s -T 256 -Question 'サブフォルダに入れたファイルが表示されません' `
        -Answer '監視フォルダの直下だけが対象です。ファイルを一つ上のフォルダへ移動してください。'
    Add-Faq -Slide $s -T 360 -Question '管理画面が開けません' `
        -Answer 'Ctrl・Shift・M を同時に押してください。キオスク表示の画面がアクティブな状態で試すのがポイントです。'

    $s = Add-Slide -Pres $pres -Kicker 'FAQ' -Title 'よくある質問（2/2）'
    Add-Faq -Slide $s -T 152 -Question '画面に「表示を復旧しています」と出ます' `
        -Answer 'フォルダ内のファイルがすべて読み込めない状態です。ファイルの破損や形式の誤りを確認してください。文言は管理画面の「復帰不能時メッセージ」で変更できます。' -H 100
    Add-Faq -Slide $s -T 264 -Question '動画だけ表示時間を短くしたい' `
        -Answer '動画は再生完了まで表示され、秒数指定はできません。短くしたい場合は動画自体を編集してください。'
    Add-Faq -Slide $s -T 368 -Question '複数の PC で同じフォルダを見せたい' `
        -Answer '各 PC にアプリを入れて同じ共有フォルダを指定する運用は可能ですが、ネットワーク共有パスの正式サポートは将来拡張です。まずはローカルフォルダを推奨します。' -H 100

    # ---------------------------------------------------------- 15. 早見表 ---
    $s = Add-Slide -Pres $pres -Kicker 'CHEAT SHEET' -Title '操作の早見表'

    [void](Add-Text -Slide $s -Text 'コンテンツ担当者' -L $ML -T 152 -W 404 -H 24 -Size 15 -Bold -Color $C.Teal)
    $d = New-TableData `
        @('やりたいこと', '操作') `
        @('表示を追加', '監視フォルダ直下にファイルを保存') `
        @('表示を削除', '監視フォルダからファイルを削除') `
        @('表示順を変える', 'ファイル名を変更（001, 002 …）') `
        @('表示時間を変える', 'ファイル名に _秒数 を付ける（動画を除く）')
    [void](Add-StyledTable -Slide $s -Data $d -L $ML -T 184 -ColWidth @(150, 254) -HeaderHeight 32 -RowHeight 44 -Accent $C.Teal -BodySize 11.5 -HeaderSize 12 -BoldFirstColumn)

    [void](Add-Text -Slide $s -Text 'システム管理者' -L $rl -T 152 -W 404 -H 24 -Size 15 -Bold -Color $C.Accent)
    $d = New-TableData `
        @('やりたいこと', '操作') `
        @('設定を変更', 'Ctrl＋Shift＋M →変更→「保存して設定する」') `
        @('キオスクに戻る', '「通常モード（キオスク）に戻る」または Ctrl＋Shift＋M') `
        @('アプリを終了', '管理画面の「アプリを終了」') `
        @('夜に止めて朝始める', 'アプリ終了時刻・PC 電源オフ時刻＋自動起動を ON')
    [void](Add-StyledTable -Slide $s -Data $d -L $rl -T 184 -ColWidth @(150, 254) -HeaderHeight 32 -RowHeight 44 -Accent $C.Accent -BodySize 11.5 -HeaderSize 12 -BoldFirstColumn)

    # -------------------------------------------------------- 16. 締め ---
    $s = Add-Slide -Pres $pres -NoChrome
    [void](New-Box -Slide $s -L 0 -T 0 -W $SW -H $SH -Fill $C.Deep)
    [void](New-Box -Slide $s -Type $shpOval -L -160 -T 300 -W 460 -H 460 -Fill $C.Accent -Transparency 0.85)
    [void](New-Box -Slide $s -L $ML -T 96 -W 68 -H 5 -Fill $C.Accent)
    [void](Add-Text -Slide $s -Text '困ったときは' -L $ML -T 124 -W 600 -H 50 -Size 38 -Bold -Color $C.White)
    [void](Add-Text -Slide $s -Text 'まずは「反映は次のスライドから」「フォルダ直下だけ」「Ctrl＋Shift＋M」の 3 つを確認。' `
            -L $ML -T 194 -W 700 -H 24 -Size 14 -Color $C.DeepText)

    $dx = $ML
    $docs = @(
        @('運用ガイド', 'ログ・権限・技術的な制限事項'),
        @('受け入れテスト', '導入時の確認チェックリスト'),
        @('アプリ要件定義書', '機能仕様の詳細')
    )
    foreach ($d in $docs) {
        [void](New-Box -Slide $s -Type $shpRound -L $dx -T 254 -W 264 -H 116 -Fill '1B2C46' -Round 0.09)
        [void](New-Box -Slide $s -L ($dx + 24) -T 278 -W 28 -H 3 -Fill '7FA6F5')
        [void](Add-Text -Slide $s -Text $d[0] -L ($dx + 24) -T 296 -W 216 -H 26 -Size 17 -Bold -Color $C.White)
        [void](Add-Text -Slide $s -Text $d[1] -L ($dx + 24) -T 326 -W 216 -H 32 -Size 11.5 -Color '9FB4D4' -Line 1.3)
        $dx += 284
    }

    [void](New-Box -Slide $s -L $ML -T 418 -W $CW -H 1 -Fill '2E4059')
    [void](Add-Text -Slide $s -Text '簡易デジタルサイネージ ｜ 操作ガイド　（出典：操作ガイド.md）' -L $ML -T 436 -W 600 -H 20 -Size 11.5 -Color '7C8CA6')

    # ------------------------------------------------------------ save ---
    if (Test-Path -LiteralPath $OutputPath) { Remove-Item -LiteralPath $OutputPath -Force }
    $pres.SaveAs($OutputPath, 24)
    Write-Host ('保存しました: {0}（{1} スライド）' -f $OutputPath, $pres.Slides.Count)
}
finally {
    $pres.Close()
    # 利用者が開いていた別のプレゼンテーションを巻き添えで閉じない
    if ($ppt.Presentations.Count -eq 0) { $ppt.Quit() }
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
    [GC]::Collect()
}
