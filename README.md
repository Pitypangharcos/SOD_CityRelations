# SOD_CityRelations

`SOD_CityRelations` is a Shadows of Doubt BepInEx 6 IL2CPP mod backend for persistent citizen-to-player relationships.

## Current Status

- Backend/API: implemented.
- Persistence/config: implemented.
- Lie calculation: implemented.
- Real game hooks: not implemented yet.
- Dialogue manipulation: not implemented yet.
- Required next input from user: generated Shadows of Doubt IL2CPP interop assemblies or local paths to the game/BepInEx install where they can be inspected.

Harmony gameplay patches are intentionally not active until verified Shadows of Doubt assemblies/interop types are available. The mod must not claim dialogue manipulation support until a safe conversation hook is found and tested.

See `docs/INSTALL_AND_INTEROP.md` for the local files and paths needed for real patch-point inspection.

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

`DevTools/BackendHarness.cs` contains a Unity-free backend harness. It creates a fake citizen profile, mutates trust/fear/suspicion, evaluates several conversation contexts, and prints `LieDecision` results.
