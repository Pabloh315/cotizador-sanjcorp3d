$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot 'CotizadorSanjCorp3D\CotizadorSanjCorp3D.csproj'

dotnet restore $project --locked-mode
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $project -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project $project -c Release --no-build -- --self-test
exit $LASTEXITCODE
