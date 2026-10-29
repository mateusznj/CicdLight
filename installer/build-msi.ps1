param(
	[string]$Version = '1.0.0',
	[ValidateSet('x64', 'arm64')][string]$Arch = 'x64'
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$publish = Join-Path $root "artifacts\publish-$Arch"
$msi = Join-Path $root "artifacts\CicdLight-$Version-$Arch.msi"

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
	dotnet tool install --global wix --version 5.0.2
}

$rid = "win-$Arch"
$platform = if ($Arch -eq 'arm64') { 'ARM64' } else { 'x64' }

dotnet publish (Join-Path $root 'CicdLight.csproj') -c Release -r $rid --self-contained -p:Platform=$platform -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

$icon = Join-Path $root 'Assets\app.ico'
wix build (Join-Path $PSScriptRoot 'Package.wxs') -arch $Arch -d "Version=$Version" -d "PublishDir=$publish" -d "IconPath=$icon" -o $msi
if ($LASTEXITCODE -ne 0) { throw 'wix build failed' }

Write-Host "MSI: $msi"
