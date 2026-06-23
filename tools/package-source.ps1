param(
    [string]$Version = "0.1.2",
    [string]$OutputDirectory = "artifacts/source"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputRoot = Join-Path $repoRoot $OutputDirectory
$packageName = "SOD_CityRelations-source-v$Version.zip"
$packagePath = Join-Path $outputRoot $packageName
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("SOD_CityRelations_source_" + [Guid]::NewGuid().ToString("N"))
$stagingProject = Join-Path $stagingRoot "SOD_CityRelations"

$excludedDirectories = @(
    ".git",
    ".agents",
    ".vs",
    ".idea",
    "artifacts",
    "lib/BepInEx",
    "Thunderstore",
    "ThunderstoreModManager",
    "r2modman",
    "profiles"
)

$excludedDirectoryNames = @(
    "bin",
    "obj"
)

$excludedFiles = @(
    "build.local.props",
    "docs/generated/INTEROP_SCAN_REPORT.md"
)

$excludedExtensions = @(
    ".dll",
    ".pdb",
    ".deps.json",
    ".runtimeconfig.json",
    ".log",
    ".cache",
    ".user",
    ".suo",
    ".r2z"
)

$excludedExactNames = @(
    "GameAssembly.dll",
    "UnityPlayer.dll",
    "global-metadata.dat",
    "Assembly-CSharp.dll"
)

function Convert-ToPackagePath {
    param([string]$Path)
    $root = $repoRoot.ProviderPath
    if (-not $root.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $root += [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = [Uri]$root
    $pathUri = [Uri]$Path
    $relative = [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString())
    return $relative.Replace([System.IO.Path]::DirectorySeparatorChar, "/").Replace("\", "/")
}

function Test-IsExcluded {
    param([System.IO.FileInfo]$File)

    $relative = Convert-ToPackagePath $File.FullName
    $segments = $relative -split "/"

    foreach ($directoryName in $excludedDirectoryNames) {
        if ($segments -contains $directoryName) {
            return $true
        }
    }

    foreach ($directory in $excludedDirectories) {
        if ($relative -eq $directory -or $relative.StartsWith($directory + "/", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($file in $excludedFiles) {
        if ($relative.Equals($file, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($name in $excludedExactNames) {
        if ($File.Name.Equals($name, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($extension in $excludedExtensions) {
        if ($relative.EndsWith($extension, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

if (Test-Path $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingProject | Out-Null
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force |
    Where-Object { -not (Test-IsExcluded $_) } |
    ForEach-Object {
        $relative = Convert-ToPackagePath $_.FullName
        $destination = Join-Path $stagingProject $relative
        $destinationDirectory = Split-Path -Parent $destination
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
    }

Compress-Archive -Path (Join-Path $stagingRoot "SOD_CityRelations") -DestinationPath $packagePath -Force
Remove-Item -LiteralPath $stagingRoot -Recurse -Force

$item = Get-Item $packagePath
Write-Host "Created source package: $($item.FullName)"
Write-Host "Size: $($item.Length) bytes"
