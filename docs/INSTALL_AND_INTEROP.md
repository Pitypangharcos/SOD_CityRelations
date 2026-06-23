# Install and Interop Setup

`SOD_CityRelations` can build its backend without Shadows of Doubt, Unity, or BepInEx assemblies. Real Harmony patch work needs local game and BepInEx IL2CPP files so class and method names can be inspected safely.

Do not commit game binaries or generated game interop assemblies to this repository.

## Expected Shadows of Doubt Folder

A normal Steam install usually looks similar to:

```text
Shadows of Doubt/
  Shadows of Doubt.exe
  GameAssembly.dll
  UnityPlayer.dll
  Shadows of Doubt_Data/
    globalgamemanagers
    il2cpp_data/
      Metadata/
        global-metadata.dat
    Managed/
      UnityEngine*.dll
```

Exact filenames and layout can vary by game version and platform. For IL2CPP inspection, the most important files are usually `GameAssembly.dll` and `Shadows of Doubt_Data/il2cpp_data/Metadata/global-metadata.dat`.

## Expected BepInEx IL2CPP Folder

A BepInEx 6 IL2CPP install or Thunderstore/r2modman profile usually looks similar to:

```text
BepInEx/
  core/
    BepInEx.Core.dll
    BepInEx.Unity.IL2CPP.dll
    0Harmony.dll
    Il2CppInterop.Runtime.dll
  config/
  plugins/
  interop/
    Il2Cppmscorlib.dll
    UnityEngine.CoreModule.dll
    Assembly-CSharp.dll
    ...
```

Some profiles place `Il2CppInterop.Runtime.dll` in `BepInEx/core/`; others expose generated assemblies in `BepInEx/interop/`. Thunderstore managers may keep this under a profile directory instead of the game install directory.

## Files Needed for Inspection

Provide paths to one of these:

- A generated `BepInEx/interop/` folder for Shadows of Doubt.
- Or the raw IL2CPP files: `GameAssembly.dll` plus `global-metadata.dat`, so interop assemblies can be generated externally.
- BepInEx core DLLs: `BepInEx.Core.dll`, `BepInEx.Unity.IL2CPP.dll`, and `0Harmony.dll`.

Generated interop assemblies are the preferred input for patch-point discovery because they expose actual class and method names that Harmony can target.

## Do Not Commit

Do not commit these files if they come from the game install or generated game interop output:

- `Shadows of Doubt.exe`
- `GameAssembly.dll`
- `UnityPlayer.dll`
- `global-metadata.dat`
- Generated `Assembly-CSharp.dll` or other Shadows of Doubt interop assemblies
- Full `BepInEx/interop/` folders from a local game profile
- Thunderstore/r2modman profile folders

Small redistributable build-reference DLLs may be handled separately if their licenses allow it, but game binaries should stay local.

## Local Build Properties

Copy the example file:

```powershell
Copy-Item build.local.props.example build.local.props
```

Edit `build.local.props` with local paths. This file is ignored by git.

Supported MSBuild properties:

- `BepInExCorePath`: folder containing BepInEx core DLLs.
- `Il2CppInteropAssembliesPath`: folder containing generated IL2CPP interop assemblies.
- `GameManagedPath`: optional managed/game assembly folder for inspection.
- `ThunderstoreProfilePath`: optional Thunderstore/r2modman profile root.

You can also pass paths directly:

```powershell
dotnet build -p:EnableBepInExIntegration=true -p:BepInExCorePath="C:\Path\To\BepInEx\core\" -p:Il2CppInteropAssembliesPath="C:\Path\To\BepInEx\interop\"
```

## Build Modes

Backend-only build:

```powershell
dotnet build
```

BepInEx integration build:

```powershell
dotnet build -p:EnableBepInExIntegration=true
```

The BepInEx integration build validates plugin bootstrap/config references. It still does not enable real gameplay patches until verified Shadows of Doubt patch points are documented in `docs/PATCH_POINTS.md`.

## Current Tooling State

- Backend/API: implemented.
- Persistence/config: implemented.
- Lie decision logic: implemented.
- Runnable backend harness: implemented in `DevTools/BackendHarness/`.
- Interop metadata scanner: implemented in `DevTools/AssemblyInspector/`.
- Real game hooks: not implemented.
- Dialogue manipulation: not implemented.
- Active Harmony patches: none.
