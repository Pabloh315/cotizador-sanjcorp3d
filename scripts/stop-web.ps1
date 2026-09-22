$ErrorActionPreference = 'Stop'

foreach ($port in 5079, 5173) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { Stop-Process -Id $_ -ErrorAction SilentlyContinue }
}

$pgCtl = 'C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe'
$dataDirectory = Join-Path $env:LOCALAPPDATA 'SanjCorp3D\PostgreSQL18\data'
if (Test-Path -LiteralPath $dataDirectory) {
    & $pgCtl status -D $dataDirectory *> $null
    if ($LASTEXITCODE -eq 0) { & $pgCtl stop -D $dataDirectory -m fast -w }
}

Write-Host 'Servicios locales de SANJ CORP 3D detenidos.' -ForegroundColor Yellow
