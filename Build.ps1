#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Builds PlayerHighlighter.dll for BONELAB.

.DESCRIPTION
  - Checks for .NET SDK; guides you to install if missing.
  - Accepts your BONELAB install path as a parameter (or auto-detects Steam).
  - Outputs PlayerHighlighter.dll into .\Mods\ and optionally copies it
    straight into your BONELAB Mods folder.

.EXAMPLE
  .\Build.ps1
  .\Build.ps1 -BonelabPath "D:\Games\BONELAB" -AutoDeploy
#>

param(
    [string]$BonelabPath = "",
    [switch]$AutoDeploy
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Colours ────────────────────────────────────────────────────────────────
function Info  ($m) { Write-Host "[INFO]  $m" -ForegroundColor Cyan }
function Good  ($m) { Write-Host "[OK]    $m" -ForegroundColor Green }
function Warn  ($m) { Write-Host "[WARN]  $m" -ForegroundColor Yellow }
function Fail  ($m) { Write-Host "[ERROR] $m" -ForegroundColor Red; exit 1 }

Info "=== PlayerHighlighter Build Script ==="

# ─── 1. Check .NET SDK ───────────────────────────────────────────────────────
try {
    $sdkVer = (dotnet --version 2>$null)
    Good ".NET SDK found: $sdkVer"
} catch {
    Fail @"
.NET SDK not found. Install it from:
  https://dotnet.microsoft.com/download  (choose .NET 6 or later)
Then re-run this script.
"@
}

# ─── 2. Locate BONELAB ───────────────────────────────────────────────────────
$steamPaths = @(
    "C:\Program Files (x86)\Steam\steamapps\common\BONELAB",
    "D:\SteamLibrary\steamapps\common\BONELAB",
    "E:\SteamLibrary\steamapps\common\BONELAB",
    "$env:PROGRAMFILES\Oculus\Software\Software\stress-level-zero-inc-bonelab"
)

if (-not $BonelabPath) {
    foreach ($p in $steamPaths) {
        if (Test-Path $p) { $BonelabPath = $p; break }
    }
}

if (-not $BonelabPath -or -not (Test-Path $BonelabPath)) {
    Warn "Could not auto-detect BONELAB. Set -BonelabPath manually."
    Warn "Example: .\Build.ps1 -BonelabPath 'C:\Games\BONELAB'"
    $BonelabPath = Read-Host "Enter your BONELAB install path"
}

if (-not (Test-Path $BonelabPath)) {
    Fail "Path not found: $BonelabPath"
}
Good "BONELAB found: $BonelabPath"
$env:BONELAB_PATH = $BonelabPath

# ─── 3. Check key references exist ───────────────────────────────────────────
$managed = Join-Path $BonelabPath "BONELAB_Steam_Windows64_Data\Managed"
$mlDll   = Join-Path $BonelabPath "MelonLoader\MelonLoader.dll"
$boneDll = Join-Path $BonelabPath "Mods\BoneLib.dll"

if (-not (Test-Path $managed)) { Fail "Managed folder not found at: $managed`nIs MelonLoader installed?" }
if (-not (Test-Path $mlDll))   { Fail "MelonLoader.dll not found. Install MelonLoader first: https://melonwiki.xyz" }
if (-not (Test-Path $boneDll)) { Warn "BoneLib.dll not found in Mods\. Download from Thunderstore and place it there." }

Good "References verified."

# ─── 4. Build ────────────────────────────────────────────────────────────────
Info "Building in Release mode..."
$proj = Join-Path $PSScriptRoot "PlayerHighlighter.csproj"

dotnet build $proj -c Release /p:BONELAB_PATH="$BonelabPath"
if ($LASTEXITCODE -ne 0) { Fail "Build failed. Check output above." }

$outDll = Join-Path $PSScriptRoot "Mods\PlayerHighlighter.dll"
if (-not (Test-Path $outDll)) { Fail "Build succeeded but DLL not found at $outDll" }

Good "DLL built: $outDll"

# ─── 5. Optional deploy ──────────────────────────────────────────────────────
if ($AutoDeploy) {
    $target = Join-Path $BonelabPath "Mods\PlayerHighlighter.dll"
    Copy-Item $outDll $target -Force
    Good "Deployed to: $target"
} else {
    Info "Copy the DLL manually:"
    Info "  $outDll  →  $BonelabPath\Mods\"
    Info ""
    Info "Or re-run with -AutoDeploy to copy automatically."
}

Info "=== Done ==="
