# Build DS4Windows (Debug, x64). Retries with a CLEAN obj/bin when a build fails.
#
# Why: this build always runs right after scripts\Remove-bin.bat ("Debug ALL" = Remove bin -> Build Debug -> Build Test -> Execute Test),
# i.e. it is always a clean build. A clean WPF build sometimes fails intermittently while it compiles the temporary
# "*_wpftmp.csproj" project, with the same sources that build fine on another run (errors such as
# "InitializeComponent does not exist" or "BG1002 ... .baml not found" for a XAML control).
# The failed run leaves a half-written obj folder, so simply building again fails the same way. The retry therefore
# cleans bin/obj first (same steps as Remove-bin.bat) and builds from scratch.
# If every attempt fails, the error is real: read the output of the LAST attempt.
param(
    [int]$Retries = 2
)

Set-Location (Join-Path $PSScriptRoot '..')

$buildArgs = @(
    'build', './DS4Windows/DS4WinWPF.csproj',
    '-c', 'Debug',
    '/p:platform=x64',
    '-o', './DS4Windows/bin/x64/Debug',
    '-nodeReuse:false'   # do not leave MSBuild worker nodes holding files open between attempts
)

dotnet @buildArgs
$exitCode = $LASTEXITCODE

$attempt = 0
while ($exitCode -ne 0 -and $attempt -lt $Retries) {
    $attempt++
    Write-Host ''
    Write-Host "[Build-Debug] The build failed (exit code $exitCode). Cleaning bin/obj and retrying from scratch ($attempt/$Retries)..." -ForegroundColor Yellow

    & "$PSScriptRoot\Remove-bin.bat"
    if ($LASTEXITCODE -ne 0) {
        Write-Host '[Build-Debug] Could not clean bin/obj (a process may be using the files). Giving up.' -ForegroundColor Red
        exit 1
    }

    dotnet @buildArgs
    $exitCode = $LASTEXITCODE
}

if ($exitCode -ne 0) {
    Write-Host "[Build-Debug] The build failed after $attempt clean retries. The error above is real." -ForegroundColor Red
}

exit $exitCode
