$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repositoryRoot 'CotizadorSanjCorp3D\CotizadorSanjCorp3D.csproj'
$installerProject = Join-Path $repositoryRoot 'Installer\SanjCorp3D.Installer.csproj'
$appOutput = Join-Path $repositoryRoot 'artifacts\app'
$installerOutput = Join-Path $repositoryRoot 'artifacts\installer'
$payloadDirectory = Join-Path $repositoryRoot 'Installer\Payload'

New-Item -ItemType Directory -Force -Path $appOutput, $installerOutput, $payloadDirectory | Out-Null
dotnet publish $appProject -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true -o $appOutput
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item -LiteralPath (Join-Path $appOutput 'CotizadorSanjCorp3D.exe') `
    -Destination (Join-Path $payloadDirectory 'CotizadorSanjCorp3D.exe') -Force
dotnet publish $installerProject -c Release -r win-x64 --self-contained true -o $installerOutput
exit $LASTEXITCODE
