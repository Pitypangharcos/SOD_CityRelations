# Generated Reports

This folder is for local reports produced by `DevTools/AssemblyInspector`.

Generated interop scan reports are machine-specific and should not usually be committed. Run the scanner locally with:

```powershell
dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- "C:\Path\To\BepInEx\interop"
```

The default generated report path is `docs/generated/INTEROP_SCAN_REPORT.md`.
