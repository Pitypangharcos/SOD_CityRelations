# Patch Points

Inspection date: 2026-06-23

## Assembly Availability

The local Steam path checked was:

`C:\Program Files (x86)\Steam\steamapps\common\Shadows of Doubt`

That folder contained Doorstop/.NET loader files but did not contain the Shadows of Doubt executable, `GameAssembly.dll`, `global-metadata.dat`, `Assembly-CSharp.dll`, BepInEx, or generated IL2CPP interop assemblies. A broader Steam-library search also did not find usable Shadows of Doubt game assemblies.

Because no verified game assemblies were available, no Shadows of Doubt class or method names are listed here as patch targets. This is intentional: the MVP must not invent method names or pretend dialogue manipulation exists.

## MVP Patch Status

| Candidate area | Class | Method | Useful for | Risk | Used in MVP | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| Citizen conversation start | Not verified | Not verified | Register interaction and familiarity gain | Unknown | No | Backend API only; log warning |
| Question answer generation | Not verified | Not verified | Calculate lie chance before an answer | Unknown | No | Log-only decision API can be called manually |
| Dialogue response mutation | Not verified | Not verified | Refusal, vague answers, omissions, misdirection | High | No | No dialogue mutation |
| Witness statement generation | Not verified | Not verified | Adjust willingness to reveal sensitive details | High | No | No witness statement mutation |

## Required Next Inspection

Before adding Harmony patches:

1. Install or generate BepInEx 6 IL2CPP interop assemblies for Shadows of Doubt.
2. Inspect generated assemblies for actual conversation, citizen interaction, and dialogue classes.
3. Record exact class and method names in this file.
4. Add guarded Harmony patches only for the lowest-risk hook.
5. Keep `EnableDialogueManipulation` disabled by default until tested in game.

## Current Runtime Behavior

`PatchRegistration.Register` logs that no verified patch points are registered. The relationship backend, persistence, lie chance calculation, and public API can still initialize and operate.
