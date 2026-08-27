# Script para compilar y empaquetar AirLyrics como un ejecutable portable independiente (.exe)
$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Compilando AirLyrics (Standalone .EXE)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Detener instancias previas
Stop-Process -Name "AirLyrics" -Force -ErrorAction SilentlyContinue

# 2. Publicar como archivo único auto-contenido (no requiere instalar .NET en la PC del usuario)
dotnet publish src/AirLyrics.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish/

Write-Host "`n✅ ¡Compilación exitosa!" -ForegroundColor Green
Write-Host "El ejecutable final se encuentra en: .\publish\AirLyrics.exe" -ForegroundColor Yellow
Write-Host "Puedes mover o compartir 'AirLyrics.exe' a cualquier PC con Windows 10/11 sin necesidad de instalar nada más." -ForegroundColor Gray
