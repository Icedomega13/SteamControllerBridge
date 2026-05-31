param(
    [string]$PackageDirectory = "",
    [switch]$SkipLoopbackExemption
)

$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell window."
    }
}

Assert-Administrator

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $packageRoot = Join-Path $repoRoot "SteamControllerBridge.GameBarWidget\AppPackages"
    $PackageDirectory = Get-ChildItem -Path $packageRoot -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not (Test-Path $PackageDirectory)) {
    throw "Package directory not found: $PackageDirectory"
}

$msix = Get-ChildItem -Path $PackageDirectory -Filter "*.msix" | Select-Object -First 1
if (-not $msix) {
    throw "No .msix package found in $PackageDirectory"
}

$cert = Get-ChildItem -Path $PackageDirectory -Filter "*.cer" | Select-Object -First 1
if ($cert) {
    Write-Host "Installing test certificate: $($cert.FullName)"
    Import-Certificate -FilePath $cert.FullName -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    Import-Certificate -FilePath $cert.FullName -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
}

Write-Host "Installing Game Bar widget package: $($msix.FullName)"
$dependencyRoot = Join-Path $PackageDirectory "Dependencies\x64"
$dependencies = @()
if (Test-Path $dependencyRoot) {
    $dependencies = Get-ChildItem -Path $dependencyRoot -Filter "*.appx" |
        Select-Object -ExpandProperty FullName
}

if ($dependencies.Count -gt 0) {
    Write-Host "Installing with dependencies:"
    $dependencies | ForEach-Object { Write-Host " - $_" }
    Add-AppxPackage -Path $msix.FullName -DependencyPath $dependencies -ForceUpdateFromAnyVersion
} else {
    Add-AppxPackage -Path $msix.FullName -ForceUpdateFromAnyVersion
}

$package = Get-AppxPackage -Name "IcedOmega13.SteamControllerBridge.GameBarWidget"
if (-not $package) {
    throw "Package installed, but could not resolve package family name."
}

if (-not $SkipLoopbackExemption) {
    Write-Host "Adding loopback exemption for $($package.PackageFamilyName)"
    & CheckNetIsolation.exe LoopbackExempt -a "-n=$($package.PackageFamilyName)" | Out-Host
}

Write-Host "Installed. Open Xbox Game Bar with Win+G, then add the Steam Controller Bridge widget from the widget menu."
