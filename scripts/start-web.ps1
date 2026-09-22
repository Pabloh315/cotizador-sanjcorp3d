param([switch]$NoBrowser)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$pgBin = 'C:\Program Files\PostgreSQL\18\bin'
$dataDirectory = Join-Path $env:LOCALAPPDATA 'SanjCorp3D\PostgreSQL18\data'
$logFile = Join-Path $env:LOCALAPPDATA 'SanjCorp3D\PostgreSQL18\postgresql.log'

if (-not (Test-Path -LiteralPath $dataDirectory)) {
    throw 'No se encontró la instancia PostgreSQL aislada de SANJ CORP 3D.'
}

& "$pgBin\pg_isready.exe" -h 127.0.0.1 -p 55432 | Out-Null
if ($LASTEXITCODE -ne 0) {
    & "$pgBin\pg_ctl.exe" start -D $dataDirectory -l $logFile -o '-p 55432 -h 127.0.0.1' -w
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL no pudo iniciarse.' }
}

if (-not (Get-NetTCPConnection -LocalPort 5079 -State Listen -ErrorAction SilentlyContinue)) {
    Start-Process dotnet -ArgumentList 'run','--project','Web\SanjCorp3D.Api\SanjCorp3D.Api.csproj','--launch-profile','http' -WorkingDirectory $repository -WindowStyle Hidden
}

if (-not (Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue)) {
    Start-Process npm.cmd -ArgumentList 'run','dev','--','--host','127.0.0.1' -WorkingDirectory (Join-Path $repository 'Web\sanjcorp3d-web') -WindowStyle Hidden
}

$deadline = (Get-Date).AddSeconds(30)
do {
    try { Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:5173/' -TimeoutSec 2 | Out-Null; break }
    catch { if ((Get-Date) -ge $deadline) { throw 'La página no respondió dentro del tiempo esperado.' }; Start-Sleep -Milliseconds 500 }
} while ($true)

Write-Host 'SANJ CORP 3D está disponible en http://127.0.0.1:5173/' -ForegroundColor Green
if (-not $NoBrowser) { Start-Process 'http://127.0.0.1:5173/' }
