param(
    [string]$Version = "0.1.2",
    [string]$Configuration = "Release",
    [string]$TargetFramework = "net6.0",
    [string]$OutputDirectory = "artifacts/release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dllPath = Join-Path $repoRoot "bin/$Configuration/$TargetFramework/SOD_CityRelations.dll"

if (-not (Test-Path $dllPath)) {
    throw "Release DLL not found at '$dllPath'. Build it first with: dotnet build -c $Configuration"
}

$outputRoot = Join-Path $repoRoot $OutputDirectory
$packageName = "SOD_CityRelations-v$Version.zip"
$packagePath = Join-Path $outputRoot $packageName
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("SOD_CityRelations_release_" + [Guid]::NewGuid().ToString("N"))
$stagingProject = Join-Path $stagingRoot "SOD_CityRelations"

if (Test-Path $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingProject | Out-Null
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

Copy-Item -LiteralPath $dllPath -Destination (Join-Path $stagingProject "SOD_CityRelations.dll") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination (Join-Path $stagingProject "README.md") -Force

$docsOutput = Join-Path $stagingProject "docs"
New-Item -ItemType Directory -Path $docsOutput -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot "docs/PACKAGING.md") -Destination (Join-Path $docsOutput "PACKAGING.md") -Force

if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

Compress-Archive -Path (Join-Path $stagingRoot "SOD_CityRelations") -DestinationPath $packagePath -Force
Remove-Item -LiteralPath $stagingRoot -Recurse -Force

$item = Get-Item $packagePath
Write-Host "Created release package: $($item.FullName)"
Write-Host "Size: $($item.Length) bytes"
