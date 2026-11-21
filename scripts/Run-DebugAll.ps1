# Debug ALL の最小版: 本体のビルド -> テストのビルド -> テストの実行（Benchmark を除く）を、この順に行い、
# どれかが失敗したら、終了コード 1 を返す。
# 警告はエラー扱い（TreatWarningsAsErrors）なので、ビルドの成功が「警告 0・エラー 0」を意味する。
# 理由: DS4WindowsWPF.sln のビルドにはテストプロジェクトが含まれず、本体だけをビルドして
# テストを走らせると、古いテストの DLL を走らせてしまう（テストの using 漏れなどを見逃す）。
param(
    [string]$Filter = 'Category!=Benchmark'
)

Set-Location (Join-Path $PSScriptRoot '..')

function Invoke-Step([string]$Name, [scriptblock]$Block) {
    Write-Host "[Run-DebugAll] $Name" -ForegroundColor Cyan
    $output = & $Block 2>&1 | Out-String
    $code = $LASTEXITCODE
    Write-Host ($output.TrimEnd() -split "`n" | Select-Object -Last 12 | Out-String)
    if ($code -ne 0) { Write-Host "[Run-DebugAll] $Name failed (exit $code)" -ForegroundColor Red; exit 1 }
    return $output
}

Invoke-Step 'Build app' { dotnet build DS4WindowsWPF.sln -c Debug '/p:platform=x64' --nologo -v q }
Invoke-Step 'Build tests' { dotnet build DS4WindowsTests -c Debug '/p:platform=x64' --nologo -v q }
Invoke-Step 'Run tests' { dotnet test DS4WindowsTests --no-build -c Debug '/p:platform=x64' --filter $Filter --nologo } | Out-Null
Write-Host '[Run-DebugAll] OK' -ForegroundColor Green
