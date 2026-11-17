<#
.SYNOPSIS
  Carga usuarios (Admin y Usuario), materias y alumnos de prueba en la base.

.EXAMPLE
  # Desde la raíz del repositorio:
  powershell -ExecutionPolicy Bypass -File scripts\cargar-datos-prueba.ps1

.EXAMPLE
  # Otra instancia o base:
  powershell -ExecutionPolicy Bypass -File scripts\cargar-datos-prueba.ps1 -Server ".\SQLEXPRESS" -Database "otraBase"
#>
param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "testprueba"
)

$ErrorActionPreference = "Stop"
$sqlFile = Join-Path $PSScriptRoot "seed-datos-prueba.sql"

# Buscar sqlcmd (en el PATH o en las rutas habituales de instalación).
$sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
if (-not $sqlcmd) {
    $sqlcmd = Get-ChildItem "C:\Program Files\Microsoft SQL Server" -Recurse -Filter sqlcmd.exe -ErrorAction SilentlyContinue |
              Select-Object -First 1 -ExpandProperty FullName
}
if (-not $sqlcmd) {
    Write-Host "No se encontró sqlcmd. Abra scripts\seed-datos-prueba.sql en SSMS o Visual Studio y ejecútelo sobre la base '$Database'." -ForegroundColor Red
    exit 1
}

Write-Host "Cargando datos de prueba en $Server / $Database ..." -ForegroundColor Cyan

# -b: corta con error si falla el SQL | -f 65001: UTF-8 (tildes y ñ)
& $sqlcmd -S $Server -d $Database -E -b -W -f 65001 -i $sqlFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "La carga falló. ¿Aplicó las migraciones (dotnet ef database update)?" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Datos cargados. Credenciales de prueba:" -ForegroundColor Green
Write-Host "  Admin   -> admin@test.com      / Admin123!"
Write-Host "  Admin   -> secretaria@test.com / Admin123!"
Write-Host "  Usuario -> docente@test.com    / Usuario123!"
Write-Host "  Usuario -> preceptor@test.com  / Usuario123!"
Write-Host "  Usuario -> invitado@test.com   / Usuario123!"
