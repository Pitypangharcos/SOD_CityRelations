# Packaging

## Source Zips

Source archives should include source code, project files, docs, and examples only.

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

## Local References

Local development references belong in either:

- `build.local.props`, copied from `build.local.props.example`
- ignored local folders such as `lib/BepInEx/`

These paths are for local compile/test convenience only and should not be treated as repository source.
