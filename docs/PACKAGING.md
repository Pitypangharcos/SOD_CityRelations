# Packaging

## Source Zips

Source archives should include source code, project files, docs, and examples only.

Create source packages with:

```powershell
./tools/package-source.ps1
```

Do not manually zip the whole working folder. That captures `.git/`, `bin/`, `obj/`, local DLLs, generated reports, and other machine-specific files.

Do not include:

- `.git/`
- `bin/`
- `obj/`
- `.vs/` or `.idea/`
- `build.local.props`
- local BepInEx DLLs
- Unity DLLs
- generated IL2CPP interop assemblies
- Shadows of Doubt game binaries
- Thunderstore/r2modman profile dumps
- machine-specific generated reports under `docs/generated/`

## Release DLL Packages

Release packages should include the compiled `SOD_CityRelations.dll` and any intentional metadata/docs required by the target mod manager.

Do not package local game binaries, generated game interop assemblies, or developer-only `build.local.props`.

Create release packages with:

```powershell
./tools/package-release.ps1
```

The release script expects `bin/Release/net6.0/SOD_CityRelations.dll` to exist. Build it first with:

```powershell
dotnet build -c Release
```

## Local References

Local development references belong in either:

- `build.local.props`, copied from `build.local.props.example`
- ignored local folders such as `lib/BepInEx/`

These paths are for local compile/test convenience only and should not be treated as repository source.
