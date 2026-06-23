# SOD_CityRelations Design

The MVP is split into backend and game-facing layers.

## Backend

- `Models/` contains serializable profile data, public snapshots, relationship tiers, conversation context, and lie decision types.
- `Config/CityRelationsConfig.cs` holds defaults matching the requested MVP options.
- `RelationshipPersistenceService` owns versioned JSON load/save, save throttling, missing-file creation, and corrupted-file fallback.
- `CityRelationsService` owns profile mutation, clamping, tier calculation, interaction registration, public snapshots, and events.
- `LieDecisionService` owns lie chance math and response type selection.
- `CityRelationsAPI` is a static facade for other mods.

## Patching Strategy

The backend is useful even without gameplay patches. Harmony patches should only be added after the target class and method have been verified from generated IL2CPP interop assemblies or another reliable assembly inspection path.

Until then, `Patches/` contains documented stubs and logs a warning instead of registering risky hooks.

## Stability Rules

- Missing saves create an empty versioned save.
- Corrupted saves are moved aside and replaced with a clean store.
- Relationship values clamp to `-100..100`.
- Dialogue manipulation is disabled by default.
- Critical information protection caps crime-related lie chance when the base game would reveal information.
