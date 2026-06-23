# SOD_CityRelations

`SOD_CityRelations` is a Shadows of Doubt BepInEx 6 IL2CPP mod backend for persistent citizen-to-player relationships.

Version: `0.1.2`

## Current Status

- Backend/API: implemented.
- Persistence/config: implemented.
- Lie decision logic: implemented.
- Runnable backend harness: implemented.
- Interop metadata scanner: implemented.
- Real game hooks: not implemented yet.
- Dialogue manipulation: not implemented yet.
- Active Harmony patches: none.
- Required next input from user: generated Shadows of Doubt IL2CPP interop assemblies or local paths to the game/BepInEx install where they can be inspected.

Harmony gameplay patches are intentionally not active until verified Shadows of Doubt assemblies/interop types are available. The mod must not claim dialogue manipulation support until a safe conversation hook is found and tested.

See `docs/INSTALL_AND_INTEROP.md` for the local files and paths needed for real patch-point inspection.
See `docs/PACKAGING.md` for source and release packaging rules.

## Build

Backend-only build:

```powershell
dotnet build
```

BepInEx integration build using local defaults or `build.local.props`:

```powershell
dotnet build -p:EnableBepInExIntegration=true
```

BepInEx integration build with explicit paths:

```powershell
dotnet build -p:EnableBepInExIntegration=true -p:BepInExCorePath="C:\Path\To\BepInEx\core\" -p:Il2CppInteropAssembliesPath="C:\Path\To\BepInEx\interop\"
```

Copy `build.local.props.example` to `build.local.props` for machine-local path overrides. `build.local.props` is ignored by git.

## Developer Harness

`DevTools/BackendHarness/` contains a Unity-free backend harness. It creates a fake citizen profile, mutates trust/fear/suspicion, evaluates several conversation contexts, and prints `LieDecision` results.

```powershell
dotnet run --project DevTools/BackendHarness/BackendHarness.csproj
```

## Interop Metadata Scanner

`DevTools/AssemblyInspector/` contains a discovery-only interop metadata scanner. It reads `.dll` metadata from a generated IL2CPP interop folder without loading game code, scores name/signature matches, and writes `docs/generated/INTEROP_SCAN_REPORT.md`.

```powershell
dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- "C:\Path\To\BepInEx\interop\"
```

Thunderstore/r2modman-style profile example:

```powershell
dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- "C:\Path\To\r2modmanPlus-local\ShadowsOfDoubt\profiles\<profile>\BepInEx\interop"
```

Optional arguments:

```powershell
dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- "C:\Path\To\BepInEx\interop\" --output docs/generated/MY_SCAN.md --max-results 100 --include-low-confidence --verbose
```

The generated report is machine-specific and ignored by git. It does not add patches, manipulate dialogue, or prove a hook is safe by itself.

The folder must contain generated Shadows of Doubt gameplay interop assemblies. A folder with only Unity/BepInEx/runtime support assemblies is useful for testing the scanner, but not for selecting gameplay patch points.

## Packaging

Do not create source zips by compressing the whole working directory. Use the exclusion-based package script:

```powershell
./tools/package-source.ps1
```

See `docs/PACKAGING.md` for release package guidance and forbidden files.
