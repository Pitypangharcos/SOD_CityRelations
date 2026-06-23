# Patch Points

Inspection date: 2026-06-23

## Assembly Availability

Initial local inspection checked:

`C:\Program Files (x86)\Steam\steamapps\common\Shadows of Doubt`

That folder contained Doorstop/.NET loader files but did not contain the Shadows of Doubt executable, `GameAssembly.dll`, `global-metadata.dat`, `Assembly-CSharp.dll`, BepInEx, or generated IL2CPP interop assemblies. A broader Steam-library search also did not find usable Shadows of Doubt game assemblies at that time.

A later real generated Shadows of Doubt IL2CPP interop scan found `Assembly-CSharp` and the diagnostic candidates listed below. These are scanner-discovered candidates and are not gameplay behavior hooks.

## MVP Patch Status

| Candidate area | Class | Method | Useful for | Risk | Used in MVP | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| Citizen conversation start | Not verified | Not verified | Register interaction and familiarity gain | Unknown | No | Backend API only; log warning |
| Question answer generation | Not verified | Not verified | Calculate lie chance before an answer | Unknown | No | Log-only decision API can be called manually |
| Dialogue response mutation | Not verified | Not verified | Refusal, vague answers, omissions, misdirection | High | No | No dialogue mutation |
| Witness statement generation | Not verified | Not verified | Adjust willingness to reveal sensitive details | High | No | No witness statement mutation |

## First Manually Selected Diagnostic Hooks

These hooks come from a real generated Shadows of Doubt IL2CPP interop scan report. They are still diagnostic-only and must not be treated as gameplay behavior hooks until tested in game.

| Candidate | Scanned signature | Current use | Risk | Notes |
| --- | --- | --- | --- | --- |
| `DialogController.OnDialogEnd` | `Void (AISpeechPreset dialog, String dialogPresetStr, Interactable saysToInteractable, Actor saidBy, Int32 jobRef)` | First preferred read-only dialog diagnostic postfix | Medium | Logs dialog preset string, job ref, and defensive descriptions of actor/interactable arguments. No state changes. |
| `InteractionController.SetCurrentPlayerInteraction` | `Void (InteractionKey key, Interactable newInteractable, InteractableCurrentAction newCurrentAction, Boolean fpsItem, Int32 forcePriority)` | Optional read-only interaction diagnostic postfix | Medium | Disabled unless `EnableInteractionDiagnostics=true`. Useful for locating player-to-citizen interaction flow. |
| `Interactable.OnInteraction` | `Void (InteractionKey input, Actor who)` and `Void (InteractionAction action, Actor who, Boolean allowDelays, Single additionalDelay)` | Optional broader read-only interaction diagnostic postfixes | Medium/High | Broader hook surface. Logging only. Do not use for relationship changes yet. |
| `SpeechController.Speak` | Found by scan | Future high-interest hook, not patched | High | Do not patch for modification. Consider read-only diagnostics only after dialog/interaction hooks are understood. |
| `DialogController.SeenOrHeardUnusual` | Found by scan | Future lie-decision logging candidate, not patched | High | Not patched in this pass. May touch information reveal logic. |

Current diagnostics are compiled only with `CITYRELATIONS_BEPINEX` and `CITYRELATIONS_SOD_INTEROP`, and runtime registration still requires `EnableHarmonyPatches=true`.

## Required Next Inspection

Before adding Harmony patches:

1. Install or generate BepInEx 6 IL2CPP interop assemblies for Shadows of Doubt.
2. Run `DevTools/AssemblyInspector/` against the generated interop folder.
3. Record exact class and method names in this file.
4. Add guarded Harmony patches only for the lowest-risk hook.
5. Keep `EnableDialogueManipulation` disabled by default until tested in game.

## How To Generate The Scan Report

Run:

```powershell
dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- "C:\Path\To\BepInEx\interop\"
```

Optional arguments:

```powershell
dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- "C:\Path\To\BepInEx\interop\" --output docs/generated/INTEROP_SCAN_REPORT.md --max-results 100 --include-low-confidence --verbose
```

The default output is `docs/generated/INTEROP_SCAN_REPORT.md`. Generated reports are local, machine-specific, and ignored by git.

## How To Interpret Confidence And Risk

Scanner confidence is metadata-based only:

- High: multiple strong matches across type name, member name, and signature.
- Medium: one strong match or several weaker matches.
- Low: weak name-only match.

Patch risk is a first-pass heuristic:

- Low: read-only-looking properties, fields, getters, or query-style methods.
- Medium: interaction/conversation methods that may be useful for logging but still need manual review.
- High: methods that appear related to cases, AI state, murders, evidence, save state, or dialogue result generation.

Confidence does not mean a hook is safe. Risk does not mean a hook is impossible. Both require manual review against the real generated interop assembly and in-game behavior.

## Verified By Scanner, Not Yet Manually Confirmed

Candidates in `docs/generated/INTEROP_SCAN_REPORT.md` are only scanner-discovered metadata matches. They are not manually confirmed patch points until a developer reviews the class, method, call context, and runtime behavior.

Do not move a candidate into the active patch list unless:

- The class and method are present in the generated Shadows of Doubt interop assemblies.
- The method signature is stable enough to target safely.
- The hook can fail without breaking save state, investigations, or dialogue.
- The patch remains behind config and try/catch guards.

## Manual Review Checklist Before Adding Any Harmony Patch

- Confirm the exact assembly, namespace, class, method, and signature.
- Confirm the method is reached during the intended gameplay action.
- Confirm whether prefix/postfix is safer for read-only logging.
- Avoid modifying return values or arguments in the first pass.
- Confirm missing target behavior logs a warning and disables only that patch.
- Confirm the patch does not require dialogue manipulation.
- Confirm `ProtectCriticalInfo` remains respected.
- Update this document with the chosen patch point and fallback behavior.

## First Safe Patch Strategy

The first real patch should:

- Start with read-only logging only.
- Avoid modifying return values.
- Avoid dialogue manipulation.
- Register familiarity only after a safe player-to-citizen interaction hook is confirmed.
- Stay behind config.
- Fail safely if the target method is missing.

The first implemented diagnostic layer follows this strategy: read-only postfixes, no return-value changes, no dialogue manipulation, no relationship changes, config-gated registration, and safe missing-target logging.

## Current Runtime Behavior

`PatchRegistration.Register` logs that no verified patch points are registered. The relationship backend, persistence, lie chance calculation, and public API can still initialize and operate.

## Current Development Tools

`DevTools/AssemblyInspector/` can scan metadata from DLL files in a provided interop folder and write `docs/generated/INTEROP_SCAN_REPORT.md`. The output is discovery-only and still requires manual confirmation before any Harmony patch is added.

No active Harmony patch is registered in version `0.1.2`.
