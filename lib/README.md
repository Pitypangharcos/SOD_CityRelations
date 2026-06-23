# Local build references

This project targets `net6.0`, matching the current BepInEx IL2CPP ecosystem. Build with .NET SDK 6 or newer.

This project can optionally use local BepInEx IL2CPP assemblies at:

```text
SOD_CityRelations/lib/BepInEx/core/BepInEx.Core.dll
SOD_CityRelations/lib/BepInEx/core/BepInEx.Unity.IL2CPP.dll
SOD_CityRelations/lib/BepInEx/core/0Harmony.dll
SOD_CityRelations/lib/BepInEx/core/SemanticVersioning.dll
SOD_CityRelations/lib/BepInEx/interop/Il2CppInterop.Runtime.dll
SOD_CityRelations/lib/BepInEx/interop/Il2Cppmscorlib.dll
SOD_CityRelations/lib/BepInEx/interop/UnityEngine.CoreModule.dll
SOD_CityRelations/lib/BepInEx/interop/UnityEngine.InputLegacyModule.dll
```

Some BepInEx IL2CPP profiles place `Il2CppInterop.Runtime.dll` under `BepInEx/interop` instead of `BepInEx/core`. The project file accepts either location.

You can also build with:

```powershell
dotnet build .\SOD_CityRelations.csproj -p:EnableBepInExIntegration=true -p:BepInExCorePath="C:\Path\To\Your\BepInEx\core\"
```

The mod intentionally does not reference Shadows of Doubt gameplay assemblies until verified IL2CPP interop assemblies are available.

DLLs under `lib/BepInEx/` are local developer references only. They are ignored by git and should not be included in source zips unless a future release deliberately vendors redistributable binaries with license review.
