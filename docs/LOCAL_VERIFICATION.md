# Local Verification

Run these checks before creating or uploading a source package.

```powershell
git status
git ls-files lib/BepInEx
dotnet build --no-restore
dotnet run --project DevTools/BackendHarness/BackendHarness.csproj --no-restore
dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj --no-restore -- "<real interop path>"
./tools/package-source.ps1
```

After packaging, inspect the produced zip under `artifacts/source/`.

Confirm the source zip does not contain:

- `.git/`
- `bin/`
- `obj/`
- `lib/BepInEx/`
- `*.dll`
- `*.pdb`
- `*.deps.json`
- `*.runtimeconfig.json`
- `docs/generated/INTEROP_SCAN_REPORT.md`

The scanner must be run against generated Shadows of Doubt gameplay interop assemblies before any gameplay hook is considered. Reports produced from Unity/BepInEx/runtime-only folders are not valid patch-point evidence.
