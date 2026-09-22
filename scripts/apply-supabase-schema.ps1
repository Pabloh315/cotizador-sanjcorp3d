param()

$ErrorActionPreference = 'Stop'

Write-Host 'Pega la URI de Supabase Session pooler completa.'
Write-Host 'Debe verse como: postgresql://postgres.xxxxx:TU_PASSWORD@aws-...pooler.supabase.com:5432/postgres'
$connection = Read-Host 'URI'

if ([string]::IsNullOrWhiteSpace($connection)) {
    throw 'No se ingreso ninguna URI.'
}

if ($connection.Contains('[YOUR-PASSWORD]')) {
    throw 'Reemplaza [YOUR-PASSWORD] por la contrasena real antes de continuar.'
}

if (-not ($connection.StartsWith('postgresql://') -or $connection.StartsWith('postgres://'))) {
    throw 'La URI debe comenzar con postgresql:// o postgres://.'
}

$env:SANJCORP_POSTGRES = $connection

dotnet ef database update --project .\Web\SanjCorp3D.Api\SanjCorp3D.Api.csproj

Write-Host ''
Write-Host 'Migracion aplicada. La base de Supabase ya tiene el esquema del proyecto.'