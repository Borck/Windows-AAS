#requires -Version 5.1
<#
.SYNOPSIS
  Publishes the Windows-AAS service and builds (optionally signs) the MSI.
.NOTES
  Requires Windows, the .NET 10 SDK and the WiX 5 toolset:
    dotnet tool install --global wix
  Authenticode signing requires a code-signing certificate.
#>
param(
  [string] $Configuration = "Release",
  [string] $Runtime = "win-x64",
  [string] $Version = "0.1.0",
  [string] $CertThumbprint  # optional: sign the MSI when provided
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\.."
$publishDir = Join-Path $root "src\WindowsAas.Service\bin\$Configuration\net10.0\$Runtime\publish"

Write-Host "Publishing service to $publishDir ..."
dotnet publish (Join-Path $root "src\WindowsAas.Service\WindowsAas.Service.csproj") `
  -c $Configuration -r $Runtime --self-contained true `
  /p:Version=$Version

Write-Host "Building MSI ..."
dotnet build (Join-Path $PSScriptRoot "WindowsAas.Installer.wixproj") `
  -c $Configuration "/p:PublishDir=$publishDir"

$msi = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter "WindowsAAS.msi" | Select-Object -First 1
Write-Host "MSI: $($msi.FullName)"

if ($CertThumbprint) {
  Write-Host "Signing MSI ..."
  & signtool sign /sha1 $CertThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $msi.FullName
}
