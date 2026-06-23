# Diagnostic Test Plan

This plan is for the first read-only Shadows of Doubt diagnostics layer.

## Build

Backend-only build:

```powershell
dotnet build --no-restore
```

BepInEx plus SoD diagnostics compile mode:

```powershell
dotnet build --no-restore -p:EnableBepInExIntegration=true -p:EnableSodInteropIntegration=true
```

## Install

1. Copy the compiled mod DLL into the BepInEx `plugins` folder for the Shadows of Doubt profile.
2. Start the game once so BepInEx config is generated.
3. Open the generated config file.

## Enable Diagnostics

Set:

```text
EnableHarmonyPatches = true
EnableDialogDiagnostics = true
EnableInteractionDiagnostics = false
EnableSpeechDiagnostics = false
EnableFamiliarityFromDialogEnd = false
```

Keep dialogue manipulation disabled.

## In-Game Test

1. Start or load a game.
2. Speak to a citizen.
3. End a dialog interaction.
4. Check BepInEx logs for `DialogController.OnDialogEnd`.
5. Confirm the log includes `dialogPresetStr`, `jobRef`, `saidBy`, and `saysToInteractable` descriptions.
6. Confirm no gameplay behavior changed.
7. Confirm no dialogue text, choices, DDS messages, or answers were modified.
8. Disable diagnostics after the test.

## Optional Interaction Diagnostics

Only after dialog diagnostics are stable:

```text
EnableInteractionDiagnostics = true
```

Repeat the test and check for:

- `InteractionController.SetCurrentPlayerInteraction`
- `Interactable.OnInteraction`

Do not use these logs to change relationships yet. Familiarity registration should wait until a safe player-to-citizen interaction hook is confirmed.
