# Stage 8B-03 — Production and progression verification

## Baseline and evidence

Baseline `8e054f05de9e42e5c676a7f2ec91e81fcdfbb5f7`: the user reports all 49 EditMode cases passed in Unity and gathering worked in Play mode. Those existing test files are unchanged. This is user-provided Windows evidence, not a cloud rerun.

Stage 8B-03 adds 53 NUnit cases (102 authored cases total). No Unity Editor or C# compiler is available here; compilation, all NUnit execution, and Play-mode checks for this stage remain pending on Windows. Static checks and independent arithmetic models are not Unity verification.

## Design authority and blockers

Read `IdleboundMasterDesign.md`: S2's ten-tier table specifies standard equipment durations and cooking durations, but full recipe ingredient amounts, individual production unlock levels and XP rewards remain unresolved. Equipment **equip** requirements and provisional staggered unlock examples do not establish approved **production** requirements. GP ingredient-value ratios do not establish quantities.

Consequently `ProductionRecipes.csv` contains **zero approved recipes**. There is no fully supported recipe to populate without inventing values, including cooking: fish-to-food is approved but exact conversion quantities, production requirements and XP have not been supplied. Production data must remain empty until those details are approved. No new chance rolls, healing values, stats or equipping are implemented.

The separate `DevelopmentProductionRecipes.csv` is an opt-in test fixture. Every recipe/product uses `dev-` stable IDs and `[DEV]` labels. Amounts, level-1 requirements and 100000 XP/job are **not approved balance**. The 15-second equipment and 5-second shrimp cooking sample durations match the first-tier source table, without implying the full recipes are approved:

| Recipe ID | Skill/category | Test input | Test output | Test duration |
| --- | --- | --- | --- | --- |
| dev-bronze-blade | Smithing / MetalWeapon | Bronze ×2 | [DEV] Practice blade ×1 | 15s |
| dev-wood-bow | Crafting / RangedWeapon | Common Wood ×2 | [DEV] Practice bow ×1 | 15s |
| dev-wood-wand | Crafting / MagicWeapon | Common Wood ×2 | [DEV] Practice wand ×1 | 15s |
| dev-crossbow | Crafting / RangedWeapon | Common Wood ×1 + Bronze ×1 | [DEV] Practice crossbow ×1 | 15s |
| dev-shrimp-meal | Cooking / Food | Shrimp ×1 | [DEV] Practice meal ×1 | 5s |

The catalog supports Smithing metal weapons/armor/shields, Crafting ranged/magic equipment, leather/cloth armor and offhands, and Cooking food. Unsupported skill/category pairs are rejected. The sample blade consumes the current generic Bronze resource solely to test the loop; it does not define an ore/bar processing chain. No items or materials are granted for free.

## Implementation and deterministic policies

- `ProductionCatalog.cs`: immutable validated definitions, strict invariant-culture 13-field CSV, stable recipe/product IDs, output counts, input quantities, level, integer-tick duration, optional XP and provenance. Rejects duplicate recipe/product/input definitions, unknown inputs/categories, invalid values, self-input, raw/product collisions and unintended fixture loading. Product IDs and raw materials share the bank namespace. Finished products can be future recipe inputs, without equipment behavior.
- `GatheringSession.cs` / `ProductionOperations.cs`: one settlement owner and shared inventory for gathering and production. `StartProductionAt` settles elapsed time first, validates level/occupancy/all ingredients, and consumes the entire ingredient set once at start. Invalid starts preserve existing jobs/assignments/stock after legitimate elapsed-time settlement. Generic `AssignAt` cannot bypass production payment.
- A successful Start runs **one job**, not an automatic repeating queue. Completion pays output quantity and eligible XP once, removes the running job and retains the exact-activity assignment as **Ready for next Start**. No stock is silently reserved for another job. The design's eventual unlimited production queue is not implemented; this single-job test API is not approval of a production queue limit. No automatic retry is implemented on missing stock.
- Repeated Start while the same job runs is rejected without charging again. Starting the next job after completion is a new explicit, paid operation. Repeating a settlement timestamp never pays twice; rewinds are rejected. Batch settlement preflights output/XP/counter arithmetic before applying rewards, preventing partial payouts on overflow.
- **Pending cancellation/refund policy:** cancel, activity change, or Swap Assignment discards unfinished progress and **does not refund consumed ingredients**. The UI warns of this before Start. This is a disclosed development policy, not approved economy behavior. A failed replacement never cancels the previous job. A successful swap cancels the old owner's paid job and charges the new owner's own job from remaining shared stock; no progress transfers.
- Selecting a different active character preserves jobs and progress. As in 8B-02, completion-time role/effects determine XP; there is no proportional mid-cycle split. A command exactly at completion settles the old job **before** selection/cancellation/swap changes. This boundary attribution still requires production approval.
- Active online XP is full configured XP; background baseline is zero; externally supplied applicable effects are capped at 50%; offline uses background XP for **all** characters. Inputs and item output are unaffected by XP eligibility. No bonuses are equipped by the UI. Unknown recipe XP (`TBD`) pays no XP; explicitly configured fixture XP is labelled. The existing sparse milestone loader is unchanged, with no new intermediate thresholds or curve interpolation.
- Offline settlement retains 8B-02's base four-hour per-submitted-absence cap and absolute cursor semantics. A long unfinished job retains only eligible progress, not time beyond the cap. Caller must submit one contiguous absence once; live focus/pause grouping is unchanged. Gear extensions, secure timestamps, persisted jobs/save recovery, membership cutoffs and full production offline replay remain outside this in-memory harness.
- There is no production bank capacity/overflow policy, item loss on entitlement changes, storage serialization, crafting equipment stats, equipping, combat, network or new package. Quantities use checked integer arithmetic; output overflow rejects settlement with the running job and cursor preserved.

## Exact Windows Unity Play-mode checks

1. Open the pulled repository in Unity **6000.3.26f1**, allow imports, inspect Console, and manually open `Assets/Scenes/IdleboundFoundation.unity`. Do not change build settings. Test portrait Game views 720×1280 and 1080×2400; scroll all sections.
2. Enter Play mode. Confirm Stage 8B-03, three development characters, nine skills and gathering activities. Default production catalog is empty with an honest missing-design warning. Existing gathering and the opt-in gathering-XP fixture must continue working. No recipe should appear as approved.
3. Click **Reset / enable DEVELOPMENT recipe samples**, then **Pause automatic time**. The reset intentionally clears the bank, XP and assignments. Confirm prominent sample warnings and no-refund cancellation warning. Keep the separate gathering-XP fixture disabled for these checks.
4. Inspect Character 1, select Mining, assign Bronze. Advance 5 seconds twice. Bank must contain Bronze ×2; Mining XP remains zero. In **Production > Smithing**, inspect the sample blade: requires Bronze ×2, bank shows2, duration15s, output1, fixture XP100000. Click **Start one job**. Bronze must immediately drop to0. No blade and no Smithing XP yet; roster shows the running production job. Start is disabled while it runs.
5. Advance 5 seconds twice: progress10/15s, no output/XP. Advance another5s: [DEV] Practice blade ×1, Smithing XP100000, known level floor≥1. Roster shows **Ready for next Start**. Advance another15s: no second blade and no extra XP. Starting again is unavailable without another two Bronze.
6. Assign Character 1 to Woodcutting/Common Wood, gather two units (10s), then **Production > Crafting** and start [DEV] Wood practice bow. Inputs drop immediately, one bow arrives after15s. Repeat with two freshly gathered wood for the practice wand. No equipping/stats controls should exist.
7. Multi-input check: gather one Common Wood with no Bronze. Practice crossbow must show one missing Bronze and disable Start without consuming wood. Gather one Bronze, then start the crossbow. Both ingredients deduct once; output appears after15s. Use the bank section to verify raw and finished products together.
8. Assign Character 1 to Fishing/Shrimp; gather two units (10s). Inspect Character 2 **without** making it active and start the Cooking meal. One Shrimp deducts; after5s one meal appears, Character 2 Cooking XP remains0. Start the next meal, make Character 2 active before completion, advance5s: another meal and100000 Cooking XP. No unsupported free background bonuses appear.
9. Use one of the banked Shrimp (or gather another if needed) and start a meal on the selected character. Click **Simulate 5 seconds OFFLINE** while automatic time is paused: one meal, **zero additional XP** without background effects. In a separate check, resume automatic time, start a funded job, unfocus/minimize and return: absent time must settle once with background XP. Actual mobile pause behavior still needs device validation.
10. Cancellation policy check: gather two Bronze, start a blade, advance5s, then **Unassign / cancel inspected job (no input refund)**. Progress/job disappear; inputs stay consumed. Advance20s: no blade and no XP from that cancelled job. Repeat by assigning a gathering activity instead of pressing cancel: same policy, gathering resumes normally.
11. Swap check: gather four Bronze with Character 1 (swap the gathering assignment if occupied), start a blade on Character 1, advance5s. Inspect Character 2: the same blade recipe offers **Swap Assignment and Start** with two Bronze remaining. Click it. Character 1 becomes unassigned, Character 2 starts at0/15, remaining Bronze deducts, only one new blade arrives after15s. Neither cancelled progress nor paid inputs transfer. With fewer than two remaining Bronze, a swap must be disabled and the old job must continue.
12. No double charge/reward: do not expect UI to send duplicate starts, but inspect that Start is disabled while running and that no extra output arrives after a completed job sits ready. Repeated-timestamp and same-job request cases are automated below. Inspection and Make Active must never reset progress.
13. Progression fixture: keep samples enabled, select Character 1 active, gather five Shrimp, and complete five sample cooking jobs. Cooking should display known floor≥30 at500000 XP, with no invented intermediate levels. Level-gate edge cases are tested using isolated test-only recipe definitions; no unsupported higher-tier recipe is shipped as approved.
14. Stop/re-enter Play: bank, jobs, assignments, sample opt-in and XP reset to default; recipes again absent until explicitly enabled. SampleScene and IdleboundTest still open unchanged. The development harness is not persistence.

## Unity Test Runner

Window > General > Test Runner > EditMode, run all `Idlebound.Foundation.Tests`:

- `GameStateTests`: original14, unchanged.
- `GatheringTests`: original35, unchanged.
- `ProductionTests`: new53 cases, covering catalog/fixture isolation and all supported categories; duplicate/invalid definitions; stock/level gates; atomic multi-input consumption; jobs, cancellation/reassignment/swaps; duplicate start/settlement; shared bank and finished-item chaining; online/background/offline XP and cap; known milestone progression; offline duration; checked overflow and read-only views.

Expected **authored count:102**. Record actual executed/passed/failed/skipped counts, Console output and Unity version. Do not mark this stage verified from static checks alone. Any failing baseline case is a regression to investigate.

## Cloud validation

Read source-controlled authority before coding. No source handoff files are stored separately in the repository; their preserved tables are in the master design. Static checks verify CSV shapes/labels/IDs/references, empty approved catalog, fixture schema, durations, metadata completeness/GUID uniqueness/importers, assembly/test setup, C# delimiter sanity, unchanged baseline tests/scenes/packages/settings/source tables, and whitespace/scope. Independent arithmetic checks exercise single-job timing/consumption/XP scenarios but do not execute the C# implementation.
