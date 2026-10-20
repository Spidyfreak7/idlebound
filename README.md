# Idlebound

A 2D mobile-first idle RPG targeting Android and iOS, using Unity, C#, and a planned Supabase backend.

## Requirements

- Unity Hub and Unity **6000.3.26f1** (Unity 6.3 LTS), matching `ProjectSettings/ProjectVersion.txt`.
- Git and a C# IDE compatible with Unity.
- For Android builds: the matching editor's Android Build Support, SDK/NDK Tools, and OpenJDK modules installed through Unity Hub.
- For iOS builds: iOS Build Support; final compilation and signing require macOS with a compatible Xcode installation and signing configuration.

## Open locally

1. Clone `https://github.com/Spidyfreak7/idlebound.git` or update your existing checkout.
2. In Unity Hub, choose Add project from disk and select the repository root containing `Assets`, `Packages`, and `ProjectSettings`.
3. Open with **6000.3.26f1**. Allow Unity to resolve the committed package manifest/lockfile and import assets. Do not upgrade the editor or packages as part of verification.
4. Open `Assets/Scenes/SampleScene.unity`. Check the Console for compilation/import errors, enter Play mode, and inspect the scene.
5. Open Window > General > Test Runner to inspect EditMode and PlayMode tests. Run the `Idlebound.Foundation.Tests` EditMode assembly. Stage 8B-02 passed all 49 cases on Windows per user report. Stages 8B-03/03B/04 remain unverified in Unity; 330 cases are authored in total, including 75 new equipment/combat cases.
6. For mobile validation, use File > Build Profiles to inspect Android/iOS support and perform a development build on the appropriate host. Device behavior and signing still require separate verification.

No Supabase setup is required to inspect the current template. Backend configuration and integration are not implemented.

## Project structure

- `Assets/Scenes/`: initial `SampleScene.unity` (enabled in build settings), `IdleboundTest.unity`, and `IdleboundFoundation.unity`. Open the foundation scene manually for Stage 8B-04; see `Docs/Stage8B04Verification.md`.
- `Assets/Foundation/`: engine-independent state/simulation, resource CSV data, development UI, and Editor tests.
- `Assets/Settings/`: Universal Render Pipeline asset, 2D renderer, volume profile, input actions, and Lit 2D scene template with its template scene.
- `Packages/`: committed manifest and dependency lockfile; includes URP 17.3.0 and Unity Test Framework 1.6.0.
- `ProjectSettings/`: committed editor, rendering, input, and other project configuration.
- `Docs/DevelopmentProgress.md`: stage status and outstanding validation.
- `Docs/Architecture.md`: approved architecture scope and deferred decisions.
- `Docs/IdleboundMasterDesign.md`: reconciliation of four handoffs, status/conflict register, and preserved source tables.

## Git conventions and verification

Commit `Assets` together with their `.meta` files, `Packages`, and `ProjectSettings`. Preserve existing GUIDs. Visible Meta Files and text serialization are configured. `.gitignore` excludes generated Library, Temp, Obj, build, log, user-settings, and IDE outputs while retaining project source and metadata.

Static verification confirmed the editor pin, valid package JSON, tracked asset metadata, ignore coverage, and URP 2D configuration. The cloud environment has no available Unity Editor, so compilation, package import, scene execution, and mobile builds have not been verified. Use the local steps above before treating the project as runtime-validated.

Cloud tasks should use the existing isolated checkout; no extra Git worktree is needed.

## Progression and balance data

The live harness uses `ProductionXpCurve.csv`, `GatheringXp.csv` and `StarterProductionRecipes.csv` in `Assets/Foundation/Development/Resources/Idlebound`. Starter recipes are labelled **NEW STAGE8B-03B BALANCE ASSUMPTIONS**. Legacy sparse XP and `[DEV]` recipe fixtures remain separate. See `Docs/Stage8B03BBalanceReport.md`; run `python Scripts/model_progression.py` to reproduce the curve validation and training models without Unity.

Equipment and combat use separate development-only catalogs (`DevelopmentGear`, `DevelopmentCombatRules`, `DevelopmentMonsters`, `DevelopmentLoot`). Open the foundation scene for the gather → craft → equip → fight → loot loop. All combat tuning and explicit gear/HP test tools are provisional; no save persistence or final equipment balance is implied. Exact Windows Play-mode/Test Runner steps and remaining policy decisions are in `Docs/Stage8B04Verification.md`.
