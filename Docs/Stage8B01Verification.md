# Stage 8B-01 foundation verification

## Scope and missing specifications

This is an offline, deterministic in-memory development harness, not production persistence. Each Play session starts with three characters, one selected active character, nine skill records per character, and no assignments. Capacity is nine; initial access is three. `RosterAccess` accepts an externally supplied entitlement count without defining membership unlocking or monetization.

No approved skill names, activity catalog, initial levels, XP curve, unlock thresholds, economy rules, or synchronization rules exist in the supplied documents. `PrototypeDefinitions` uses explicitly synthetic numbered labels, level 1, XP 0, and three available activities plus one unavailable fixture. These are test data, not approved mechanics. Skill records currently validate levels 1–99 and nonnegative stored XP; the starting level and any additional level semantics require design approval. XP storage has no level calculation or award path. Neither active nor inactive characters generate XP. Selection only identifies the single active character; assignment does not imply active status or future XP eligibility.

## Files and boundaries

- `Assets/Foundation/Core/GameState.cs`: UI-independent definitions, immutable skill/character records, separate roster access, active selection, assignment validation, read-only collections.
- `Assets/Foundation/Core/PrototypeDefinitions.cs`: isolated synthetic fixture.
- `Assets/Foundation/Development/FoundationDevelopmentUI.cs`: portrait-width, safe-area-aware, vertically scrollable IMGUI harness. Select a roster member to make it active and inspect it; assign or unassign an activity. Occupied and unavailable activities are disabled. All roster assignments are displayed.
- `Assets/Foundation/Tests/Editor/GameStateTests.cs`: EditMode NUnit coverage using the existing Unity Test Framework.
- Assembly definitions isolate engine-independent core, development UI, and Editor tests. Every new Unity file and folder has metadata.
- `Assets/Scenes/IdleboundFoundation.unity`: dedicated scene with the existing template camera/light and new harness component. Existing scenes, packages, settings, and build scene list remain untouched.

Assignment uses exact activity IDs: each character has at most one assignment and each activity has at most one character. Reassignment releases the previous activity only after validation. Rejected operations preserve state. Unknown or unavailable activities are rejected; unassignment explicitly removes the assignment. Public collections cannot bypass these rules.

## Windows Unity verification (not yet run)

1. Open the repository in Unity **6000.3.26f1**. Wait for import/compilation and inspect the Console.
2. Open `Assets/Scenes/IdleboundFoundation.unity` manually; do not replace the startup scene or build settings.
3. Enter Play mode. Use 720×1280, then 1080×2400 Game view sizes. Scroll through the UI; labels and buttons should remain reachable.
4. Confirm three character buttons, six unavailable slots, nine skills on the selected character, level 1/99, and XP 0.
5. Select Character 2. Confirm only it is ACTIVE. Assign Prototype activity 1, then select Character 1: activity 1 must be occupied/disabled and Character 2's assignment must remain visible.
6. Assign Character 1 to activity 2; switch back to Character 2 and reassign to activity 3. Confirm activity 1 becomes available. Unassign Character 2 and confirm activity 3 becomes available. Activity 4 must always be unavailable.
7. Switch active characters repeatedly. Assignments must persist during this Play session; XP must remain zero for everyone. Stop/re-enter Play: deterministic initial state must return.
8. Open Window > General > Test Runner > EditMode; run `Idlebound.Foundation.Tests`. Record actual executed/passed/failed counts and Console output. No automated Unity tests were executed in the cloud environment.
9. Reopen SampleScene and IdleboundTest manually to confirm they remain unaffected. Real Android/iOS device validation remains future work.

Static checks in the cloud cover metadata completeness/GUID uniqueness, assembly JSON and references, scene component references, dependency/settings preservation, test presence, and whitespace. The environment has no Unity Editor or C# compiler; these checks do not establish compilation, UI behavior, or passing NUnit tests.
