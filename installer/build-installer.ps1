<#
.SYNOPSIS
  Publishes the app (self-contained, win-x64) and builds the Windows installer with Inno Setup.
.EXAMPLE
  pwsh installer\build-installer.ps1
  Result: artifacts\installer\Dotnetstore.PersonalApp-Setup-<version>.exe
.NOTES
  Needs Inno Setup 6: winget install JRSoftware.InnoSetup
  The version comes from <Version> in Directory.Build.props.
#>
[CmdletBinding()]
param([string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root 'artifacts\publish'
$project = Join-Path $root 'src\Dotnetstore.PersonalApp.Host\Dotnetstore.PersonalApp.Host.csproj'

$iscc = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue)?.Source,
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 was not found. Install it with: winget install JRSoftware.InnoSetup' }

$version = (dotnet msbuild $project -getProperty:Version -p:Configuration=$Configuration | Out-String).Trim()
Write-Host "Publishing version $version ..."

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $project -c $Configuration -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

& $iscc "/DAppVersion=$version" "/DPublishDir=$publishDir" (Join-Path $PSScriptRoot 'Dotnetstore.PersonalApp.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed' }

Write-Host "Installer: $(Join-Path $root "artifacts\installer\Dotnetstore.PersonalApp-Setup-$version.exe")"