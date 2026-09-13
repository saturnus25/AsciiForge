$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'AsciiForge\AsciiForge.csproj'
$version = '5.1.1'
Write-Host "Building AsciiForge $version C# GPU Edition..." -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Host 'No encuentro dotnet. Abre Visual Studio Installer > Modify e instala .NET desktop development.' -ForegroundColor Yellow
  exit 1
}

dotnet --version | Out-Host
$publish = Join-Path $root 'publish\win-x64'
$dist = Join-Path $root 'dist'
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
New-Item -ItemType Directory -Path $publish -Force | Out-Null
New-Item -ItemType Directory -Path $dist -Force | Out-Null

dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

$pdb = Join-Path $publish 'AsciiForge.pdb'
if (Test-Path $pdb) { Remove-Item $pdb -Force }
$exe = Join-Path $publish 'AsciiForge.exe'
if (-not (Test-Path $exe)) { throw "No se genero $exe" }

$releaseZip = Join-Path $dist "AsciiForge-$version-win-x64.zip"
if (Test-Path $releaseZip) { Remove-Item $releaseZip -Force }
Compress-Archive -Path $exe -DestinationPath $releaseZip -CompressionLevel Optimal

Write-Host "`nEXE para GitHub Releases:" -ForegroundColor Green
Write-Host "  $exe" -ForegroundColor White
Write-Host "ZIP binario opcional:" -ForegroundColor Green
Write-Host "  $releaseZip" -ForegroundColor White
