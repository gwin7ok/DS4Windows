<#
更新履歴の更新（VSCode: ctrl+shift+2 / タスク "Update Changelog"）。

  pwsh ./scripts/release-bump.ps1                    … 対話的に、次の版を選んで更新する
  pwsh ./scripts/release-bump.ps1 -Version 4.1.0     … 版を指定する
  pwsh ./scripts/release-bump.ps1 -DryRun            … ファイルは変えず、内容だけを表示する

流れ:
  1. CHANGELOG.md の `[Unreleased]` が空なら、前のリリース（最新のタグ v*。無ければ CHANGELOG.md を最後に変えたコミット）以降の
     コミットから、変更内容を生成する（feat -> Added / fix -> Fixed / その他 -> Changed。docs・test・chore・ci・style・build・diag は載せない）。
     確認してから書き込む。生成した文は、利用者に分かる文に、後で直す。
  2. 次の版（パッチ / マイナー / メジャー、または x.y.z）を選ぶ。
  3. Directory.Build.props の Version / FileVersion / AssemblyVersion を更新し、CHANGELOG.md の `[Unreleased]` を `[x.y.z] - 日付` に切り出す。
この後、scripts/release-tag.ps1（VSCode: ctrl+shift+3）で、コミット -> push -> タグ -> Actions が Release の作成まで行う。
#>
Param(
    [string]$Version = '',
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Resolve-Path -Path (Join-Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent) '..')).ProviderPath
Set-Location $repoRoot

$propsPath = Join-Path $repoRoot 'Directory.Build.props'
$logPath = Join-Path $repoRoot 'CHANGELOG.md'

function Die([string]$Message, [int]$Code = 1) {
    Write-Host "`n✗ $Message" -ForegroundColor Red
    exit $Code
}

function Read-TextFile([string]$Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $bom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $text = [System.Text.UTF8Encoding]::new($false).GetString($bytes, $(if ($bom) { 3 } else { 0 }), $bytes.Length - $(if ($bom) { 3 } else { 0 }))
    return [pscustomobject]@{ Text = $text; Bom = $bom; Crlf = $text.Contains("`r`n") }
}

function Write-TextFile([string]$Path, [string]$Text, [bool]$Bom, [bool]$Crlf) {
    if ($Crlf) { $Text = $Text.Replace("`r`n", "`n").Replace("`n", "`r`n") }
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($Bom))
}

# 置換文字列に `$` が含まれても（コミットの文など）、そのまま入れる
function Replace-First([string]$Text, [string]$Pattern, [string]$Replacement) {
    $mm = [regex]::Match($Text, $Pattern)
    if (-not $mm.Success) { return $Text }
    return $Text.Substring(0, $mm.Index) + $Replacement + $Text.Substring($mm.Index + $mm.Length)
}

function Invoke-Git {
    $out = & git @args 2>$null
    if ($LASTEXITCODE -ne 0) { return @() }
    return @($out)
}

# --- 現在の版 ---
$props = Read-TextFile $propsPath
$m = [regex]::Match($props.Text, '<Version>\s*([^<\s]+)\s*</Version>')
if (-not $m.Success) { Die 'Directory.Build.props に <Version> がありません' 2 }
$current = $m.Groups[1].Value
if ($current -notmatch '^\d+\.\d+\.\d+$') { Die "Directory.Build.props の Version が x.y.z ではありません: $current" 2 }

# --- CHANGELOG.md ---
$logFile = Read-TextFile $logPath
$log = $logFile.Text.Replace("`r`n", "`n")
if ($log -notmatch '(?m)^## \[Unreleased\]') {
    # 無ければ、最初の版の節の前に作る
    $log = Replace-First $log '(?m)^## \[' "## [Unreleased]`n`n## ["
    if ($log -notmatch '(?m)^## \[Unreleased\]') { Die 'CHANGELOG.md に版の節（## [x.y.z]）がありません' 4 }
}

function Get-UnreleasedBody([string]$Text) {
    $mm = [regex]::Match($Text, '(?ms)^## \[Unreleased\][^\n]*\n(.*?)(?=^## \[|\z)')
    if ($mm.Success) { return $mm.Groups[1].Value }
    return ''
}
function Test-Section([string]$Text, [string]$Ver) {
    return [regex]::IsMatch($Text, '(?m)^## \[' + [regex]::Escape($Ver) + '\]')
}

$tags = @(Invoke-Git tag --list 'v*' --sort=-v:refname)

# --- まだリリースしていない版が、すでにある（最初のリリースなど） ---
if ((Get-UnreleasedBody $log).Trim() -eq '' -and (Test-Section $log $current) -and ($tags -notcontains "v$current")) {
    Write-Host "版 $current は、CHANGELOG.md に節があり、まだリリース（タグ v$current）されていません。"
    Write-Host 'そのまま、scripts/release-tag.ps1（VSCode: ctrl+shift+3）でリリースできます。'
    exit 0
}

# --- 1. [Unreleased] が空なら、コミットから生成 ---
if ((Get-UnreleasedBody $log).Trim() -eq '') {
    $lastTag = if ($tags.Count -gt 0) { $tags[0] } else { '' }
    $base = $lastTag
    $baseLabel = $lastTag
    if (-not $base) {
        $base = (Invoke-Git log -1 --format=%H CHANGELOG.md | Select-Object -First 1)
        if ($base) { $baseLabel = "CHANGELOG.md の最終変更コミット ($($base.Substring(0, 7)))" }
    }
    $range = if ($base) { "$base..HEAD" } else { 'HEAD' }
    $subjects = @(Invoke-Git -c core.quotepath=off log --no-merges --reverse --format=%s $range)

    $skip = @('docs', 'test', 'chore', 'ci', 'style', 'build', 'diag')
    $groups = [ordered]@{ Added = @(); Changed = @(); Fixed = @() }
    $seen = @{}
    foreach ($s in $subjects) {
        $s = "$s".Trim()
        if (-not $s) { continue }
        if ($s -match '^(Release v|Merge )' -or $s -match '^chore\(release\)') { continue }
        $mm = [regex]::Match($s, '^(\w+)(?:\(([^)]*)\))?(!)?:\s*(.+)$')
        $type = if ($mm.Success) { $mm.Groups[1].Value.ToLower() } else { '' }
        if ($skip -contains $type) { continue }
        $body = if ($mm.Success) { $mm.Groups[4].Value } else { $s }
        $body = ($body -replace '\s*[＊*]\s*ビルド.*$', '').Trim()
        if (-not $body -or $seen.ContainsKey($body)) { continue }
        $seen[$body] = $true
        $key = switch ($type) { 'feat' { 'Added' } 'fix' { 'Fixed' } default { 'Changed' } }
        $groups[$key] += "- $body"
    }
    $total = 0
    foreach ($k in $groups.Keys) { $total += $groups[$k].Count }
    if ($total -eq 0) {
        Write-Host "[Unreleased] が空で、起点 ($(if ($baseLabel) { $baseLabel } else { 'なし' })) 以降に、載せる対象のコミットもありません。" -ForegroundColor Yellow
        Write-Host 'CHANGELOG.md に、変更内容を書いてから実行してください。' -ForegroundColor Yellow
        exit 6
    }
    $generated = ''
    foreach ($k in $groups.Keys) {
        if ($groups[$k].Count -gt 0) { $generated += "### $k`n" + ($groups[$k] -join "`n") + "`n`n" }
    }
    $generated = $generated.TrimEnd()

    Write-Host "[Unreleased] が空です。起点 [$(if ($baseLabel) { $baseLabel } else { 'なし' })] 以降のコミットから生成します（$total 件）:`n"
    Write-Host $generated
    Write-Host ''
    $written = Replace-First $log '(?m)^## \[Unreleased\][ \t]*\n' "## [Unreleased]`n`n$generated`n`n"
    if ($DryRun) {
        Write-Host 'Dry run: CHANGELOG.md は変更しません（上記が [Unreleased] に入る想定で続行します）。'
        $log = $written
    } else {
        $ans = (Read-Host 'この内容を [Unreleased] に書き込みますか? [Y=書き込んで続行 (Enter) / e=書き込んで編集 / n=中止]').Trim().ToLower()
        if ($ans -eq '' -or $ans -eq 'y') {
            $log = $written
        } elseif ($ans -eq 'e') {
            Write-TextFile $logPath $written $logFile.Bom $logFile.Crlf
            Write-Host 'CHANGELOG.md に書き込みました。内容（利用者に分かる文）を編集して、もう一度実行してください。'
            exit 0
        } else {
            Write-Host '中止しました。'
            exit 0
        }
    }
}

# --- 2. 次の版 ---
if (-not $Version) {
    $p = $current.Split('.') | ForEach-Object { [int]$_ }
    $patch = "$($p[0]).$($p[1]).$($p[2] + 1)"
    $minor = "$($p[0]).$($p[1] + 1).0"
    $major = "$($p[0] + 1).0.0"
    Write-Host "現在のバージョン: $current"
    Write-Host "  1) $patch  (パッチ: 不具合の修正・表示の変更)"
    Write-Host "  2) $minor  (マイナー: 機能の追加)"
    Write-Host "  3) $major  (メジャー: 保存データの互換を壊す変更・大きな作り直し)"
    $c = (Read-Host '番号、または x.y.z を入力 (空Enterで中止)').Trim()
    switch -Regex ($c) {
        '^1$' { $Version = $patch; break }
        '^2$' { $Version = $minor; break }
        '^3$' { $Version = $major; break }
        '^\d+\.\d+\.\d+$' { $Version = $c; break }
        default { Write-Host '中止しました。'; exit 0 }
    }
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') { Die "版は x.y.z で指定してください（指定: '$Version'）" 1 }
if ([version]$Version -le [version]$current) { Die "新しい版 $Version は、現在の $current より大きくしてください。" 3 }
if (Test-Section $log $Version) { Die "CHANGELOG.md に、版 $Version の節がすでにあります。" 5 }
if ($tags -contains "v$Version") { Die "タグ v$Version がすでにあります。別の版を指定してください。" 5 }

# --- 3. 更新 ---
$date = Get-Date -Format 'yyyy-MM-dd'
$next = Replace-First $log '(?m)^## \[Unreleased\][ \t]*\n' "## [Unreleased]`n`n## [$Version] - $date`n"
$next = [regex]::Replace($next, '\n{3,}(## \[)', "`n`n" + '$1')
$nextProps = [regex]::Replace($props.Text,
    '(<(?:Version|FileVersion|AssemblyVersion)>)[^<]+(</(?:Version|FileVersion|AssemblyVersion)>)',
    ('${1}' + $Version + '${2}'))

Write-Host "Directory.Build.props : $current -> $Version (Version / FileVersion / AssemblyVersion)"
Write-Host "CHANGELOG.md          : [Unreleased] -> [$Version] - $date"
if ($DryRun) {
    Write-Host 'Dry run: ファイルは変更していません。'
    exit 0
}
Write-TextFile $propsPath $nextProps $props.Bom $props.Crlf
Write-TextFile $logPath $next $logFile.Bom $logFile.Crlf
Write-Host "`n更新しました。CHANGELOG.md の文を確認・編集し、scripts/release-tag.ps1（VSCode: ctrl+shift+3）でリリースを生成してください。"
