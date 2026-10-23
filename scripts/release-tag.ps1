<#
リリースの生成（VSCode: ctrl+shift+3 / タスク "release-tag-and-push-short"）。

  pwsh ./scripts/release-tag.ps1              … 確認の後、Release vX.Y.Z をコミット -> push -> タグ発行 -> タグを push
  pwsh ./scripts/release-tag.ps1 -DryRun      … 何も実行せず、行う内容だけを表示する
  pwsh ./scripts/release-tag.ps1 -Yes         … 確認を省略する
  pwsh ./scripts/release-tag.ps1 -NoPush      … コミット・タグだけ（push しない）
  pwsh ./scripts/release-tag.ps1 -Recreate    … 既存のタグ（ローカル・リモート）を削除して付け直す（失敗したリリースのやり直し用）

タグ `vX.Y.Z` の push をきっかけに、GitHub Actions（.github/workflows/release.yml）が、
版の検証 -> 変更履歴の抽出 -> ビルド -> zip の作成 -> GitHub Release の作成 を行う。
版は Directory.Build.props の Version（scripts/release-bump.ps1 = ctrl+shift+2 で更新）。
#>
Param(
    [switch]$WithV,   # 旧い呼び出しとの互換のために残す（タグは常に v 付き）
    [string]$Remote = 'origin',
    [string]$Branch = '',
    [switch]$DryRun,
    [switch]$Yes,
    [switch]$NoPush,
    [switch]$Recreate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Resolve-Path -Path (Join-Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent) '..')).ProviderPath
Set-Location $repoRoot

function Die([string]$Message, [int]$Code = 1) {
    Write-Host "`n✗ $Message" -ForegroundColor Red
    exit $Code
}

# 実行の前提の確認。-DryRun のときは、警告だけにして続ける（内容の確認ができるように）
function Need([bool]$Ok, [string]$Message, [int]$Code) {
    if ($Ok) { return }
    if ($DryRun) { Write-Host "! $Message（-DryRun のため、続行）" -ForegroundColor Yellow } else { Die $Message $Code }
}

function Invoke-GitChecked {
    $out = & git @args
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') が失敗しました（終了コード $LASTEXITCODE）" }
    return $out
}

function Invoke-GitQuiet {
    $out = & git @args 2>$null
    return [pscustomobject]@{ Ok = ($LASTEXITCODE -eq 0); Out = @($out) }
}

function Run([string]$Description, [int]$Code, [scriptblock]$Block) {
    & $Block
    if ($LASTEXITCODE -ne 0) { Die "失敗しました: $Description" $Code }
}

# --- 版 ---
$propsPath = Join-Path $repoRoot 'Directory.Build.props'
if (-not (Test-Path $propsPath)) { Die "Directory.Build.props が見つかりません: $propsPath" 2 }
$m = [regex]::Match([System.IO.File]::ReadAllText($propsPath), '<Version>\s*([^<\s]+)\s*</Version>')
if (-not $m.Success) { Die 'Directory.Build.props に <Version> がありません' 3 }
$version = $m.Groups[1].Value
if ($version -notmatch '^\d+\.\d+\.\d+$') { Die "Directory.Build.props の Version が x.y.z ではありません: $version" 2 }
$tag = "v$version"
$current = (Invoke-GitChecked rev-parse --abbrev-ref HEAD | Select-Object -First 1).Trim()

# リリースを行うブランチ: 指定がなければ、リモートの既定のブランチ（取れなければ master）
if (-not $Branch) {
    $head = Invoke-GitQuiet symbolic-ref --short "refs/remotes/$Remote/HEAD"
    $Branch = if ($head.Ok -and $head.Out.Count -gt 0) { ("$($head.Out[0])" -replace "^$([regex]::Escape($Remote))/", '').Trim() } else { 'master' }
}
Write-Host "Version: $version`nTag: $tag`nBranch: $current (リリースは $Branch で行う)"

# CHANGELOG に、該当の節があること
$log = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'CHANGELOG.md'))
if (-not [regex]::IsMatch($log, '(?m)^## \[' + [regex]::Escape($version) + '\]')) {
    Die "CHANGELOG.md に、版 $version の節がありません。先に scripts/release-bump.ps1（ctrl+shift+2）を実行してください。" 3
}

# タグがまだ無いこと（ローカル・リモート）。-Recreate のときだけ、削除して付け直す
$localTag = (Invoke-GitQuiet tag -l $tag).Out | Where-Object { $_ }
if ($localTag -and -not $Recreate) { Die "ローカルに、タグ $tag がすでにあります。版を上げてください（付け直すときは -Recreate）。" 5 }
$remoteTagExists = $false
if (-not $NoPush) {
    $r = Invoke-GitQuiet ls-remote --tags $Remote "refs/tags/$tag"
    if (-not $r.Ok) { Die "$Remote に接続できませんでした。" 6 }
    $remoteTagExists = (@($r.Out | Where-Object { $_ }).Count -gt 0)
    if ($remoteTagExists -and -not $Recreate) { Die "$Remote に、タグ $tag がすでにあります。版を上げてください（付け直すときは -Recreate）。" 6 }
}

# ブランチ: リリースの対象のブランチで行う
Need ($current -eq $Branch) "$Branch ブランチではありません（今: $current）。リリースは $Branch で行います。" 7

# 未コミットの変更: Directory.Build.props・CHANGELOG.md だけなら、Release のコミットにまとめる
$allowed = @('Directory.Build.props', 'CHANGELOG.md')
$dirty = @(Invoke-GitChecked status --porcelain | Where-Object { $_ })
$dirtyFiles = @($dirty | ForEach-Object { ($_.Substring(3)).Trim('"') })
$others = @($dirtyFiles | Where-Object { $allowed -notcontains $_ })
Need ($others.Count -eq 0) ("Directory.Build.props・CHANGELOG.md 以外に、コミットしていない変更があります。先にコミットしてください:`n" + ($dirty -join "`n")) 4
$needCommit = ($others.Count -eq 0) -and (@($dirtyFiles | Where-Object { $allowed -contains $_ }).Count -gt 0)

# リモートより遅れていないこと
if (-not $NoPush) {
    $f = Invoke-GitQuiet fetch $Remote $Branch
    if ($f.Ok) {
        $anc = Invoke-GitQuiet merge-base --is-ancestor "$Remote/$Branch" HEAD
        Need $anc.Ok "ローカルが $Remote/$Branch より遅れています（pull してください）。" 8
    } else {
        Write-Host '! fetch できませんでした（続行）' -ForegroundColor Yellow
    }
}

# --- 計画 ---
$planned = @()
if ($Recreate -and $localTag) { $planned += "git tag -d $tag" }
if ($Recreate -and $remoteTagExists -and -not $NoPush) { $planned += "git push $Remote --delete refs/tags/$tag" }
if ($needCommit) { $planned += "git add Directory.Build.props CHANGELOG.md; git commit -m `"Release $tag`"" }
if (-not $NoPush) { $planned += "git push $Remote $current" }
$planned += "git tag -a $tag -m `"Release $tag`" HEAD"
if (-not $NoPush) { $planned += "git push $Remote refs/tags/$tag" }
Write-Host "`nPlanned commands:"
$planned | ForEach-Object { Write-Host "  $_" }

if ($DryRun) {
    Write-Host "`nDry run: 何も実行していません。"
    exit 0
}

if (-not $Yes) {
    $ans = (Read-Host "`nリリース $tag を生成します（タグを push すると GitHub Actions が、ビルドと Release の作成を行います）。実行しますか? [Y/n]").Trim()
    if ($ans -ne '' -and $ans -notmatch '^[yY]') {
        Write-Host '中止しました。'
        exit 0
    }
}

# --- 実行 ---
if ($Recreate -and $localTag) { Run "git tag -d $tag" 10 { & git tag -d $tag } }
if ($Recreate -and $remoteTagExists -and -not $NoPush) { Run "git push $Remote --delete refs/tags/$tag" 11 { & git push $Remote --delete "refs/tags/$tag" } }
if ($needCommit) {
    Run 'git add' 9 { & git add Directory.Build.props CHANGELOG.md }
    Run 'git commit' 9 { & git commit -m "Release $tag" }
}
if (-not $NoPush) { Run "git push $Remote $current" 12 { & git push $Remote $current } }
Run "git tag -a $tag" 10 { & git tag -a $tag -m "Release $tag" HEAD }
if (-not $NoPush) { Run "git push $Remote refs/tags/$tag" 11 { & git push $Remote "refs/tags/$tag" } }

Write-Host "`n✓ タグ '$tag' を作成$(if ($NoPush) { 'しました' } else { 'し、push しました' })。" -ForegroundColor Green
if (-not $NoPush) {
    $url = (Invoke-GitQuiet remote get-url $Remote).Out | Select-Object -First 1
    Write-Host '  Actions が、ビルドと Release の作成を行います（数分）。'
    if ($url) { Write-Host "  リポジトリ: $url（Actions / Releases のページで確認）" }
}
