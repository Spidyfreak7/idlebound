# Stage 8B-03B — Windows verification

## Evidence and scope

Baseline `ecc63ceb0b885bae7f28a42227bedc7d0870c675` has102 authored NUnit cases. Only the earlier49 cases/gathering were user-verified on Windows; Stage8B-03's tests and manual checks remain unverified. This stage preserves all102 test cases unchanged and adds153 (255 total authored). No Unity Editor or C# compiler is available in the cloud: no compilation, NUnit execution or Play-mode pass is claimed for this stage.

Read `Stage8B03BBalanceReport.md` for the exact reconstructed curve, approvals versus new balance assumptions, numerical models and remaining decisions. Original source tables, scene files, package files and settings are unchanged. `IdleboundMasterDesign.md` now records the later approvals above its historical annex.

## Data and mode boundaries

Default live mode uses `GatheringXp.csv`, `ProductionXpCurve.csv` and `StarterProductionRecipes.csv`. All nine skills share the full99-row curve. Approved gathering rewards replace the prior unresolved live rewards. The curve is reconstructed from the authorized shape and anchors; it cannot be claimed byte-identical to a prior CSV that was not supplied.

Eight starter recipes carry `new-assumption` authority and the label **NEW STAGE8B-03B BALANCE ASSUMPTIONS**. Costs, gates, output quantities and recipe XP are new tuning decisions; timings reuse the known first/second-tier source values. The five older `[DEV]` recipes remain separate and opt-in. The old empty `ProductionRecipes.csv` is retained as historical approved-only data, not the current live recipe source.

`GatheringCatalog.Load()` and sparse-curve constructor calls preserve the legacy fixture contract for the unchanged tests. `LoadProduction()` plus the complete curve defines the live configuration; full-curve sessions default to refundable cancellation. The UI explicitly enables refunds in **all modes**, including opt-in legacy samples. Sparse fixture modes are clearly labelled, not a hidden substitute for live balance. Default full-curve XP caps deterministically at10000000; fixture XP remains intentionally uncapped for compatibility.

## Play-mode checks in Unity6000.3.26f1 on Windows

1. Pull the reviewed commit; open the project with the pinned editor, allow import/compilation, inspect Console. Manually open `Assets/Scenes/IdleboundFoundation.unity`. Do not replace startup/build scenes. Use720×1280 and1080×2400 Game views and confirm the scrollable UI remains reachable.
2. Enter Play mode and pause automatic time immediately. Confirm Stage8B-03B, **LIVE XP**, full reconstructed levels (not≥milestone labels), starter assumptions displayed, and no development fixture enabled. Inspect all nine skills atlevel1/XP0 with595XP to next level. Three characters remain development roster fixtures, not production membership unlocks.
3. Select Character1 active, assign Bronze Mining. Advance5s once: Bronze×1, MiningXP56, level1,539XP to next level. Advance another45s: Bronze×10, XP560,35XP to next level. Advance5s: Bronze×11, XP616, **level2**,1762XP to level3. No partial action should pay early.
4. Inspect Iron Mining: locked atlevel2. For a live gate check, stay on Bronze for4300 total productive seconds (860 cycles): XP48160 and Mining level10; Iron now unlocks at48157XP. This takes about72min at1× speed, so use the automated gate test for an immediate check; do not enable the legacy XP fixture and call it production balance. After assigning Iron, each complete7.8s cycle gives1Iron and89XP.
5. Starter Smithing loop: while Character1 is active and has at least2Bronze, open Smithing and start **Bronze dagger**, level1,2Bronze,15s,250XP. Bank deducts2immediately; no SmithingXP/output yet. At14s no output; at15s one dagger and250SmithingXP. The job becomes Ready; advancing more time must not repeat it. Dagger has no equipping/stats feature.
6. Recipe gates: Bronze helm and Common wood crossbow (level5) and Iron dagger/Cooked sardine (level10) stay visible but locked for a new corresponding skill. Do not confuse gathering level with production level. Six completed level1daggers yield1500SmithingXP/level2; level5 requires9512XP (40daggers give10000), so automated tests are preferable to extended manual gate grinding.
7. Crafting: gather2Common Wood, select Crafting/Shortbow, start and wait15s. Two wood deduct once, one shortbow and250CraftingXP arrive at completion for the selected active character. Repeat with a newly gathered2wood for Wand. Crossbow remains gated until Crafting5 and needs both1wood and1Bronze; failed starts must not partly charge the bank.
8. Cooking: gather6Shrimp, then complete six **Cook shrimp** jobs (one Shrimp and5s each,100XP/job), with the producer selected active. Verify600CookingXP and **level2**; products appear in the same bank as raw resources. No heal amount or equipment stats are implied.
9. Refund check just before completion: start another funded dagger, advance14s, cancel. All2reservedBronze return once; no dagger/SmithingXP from that unfinished job. Press cancel again and advance time: no extra refund or ghost completion. Reassigning the unfinished job to Fishing should also refund the Bronze once.
10. Exact boundary: start a dagger and advance15s before cancelling. Completion pays one dagger/250eligibleXP; cancel after that gives **no** ingredient refund. Automated tests also invoke cancellation at the exact timestamp to establish settlement-before-command ordering.
11. Swap with no free stock: reserve the only2Bronze in Character1's unfinished dagger. Inspect Character2: **Swap Assignment and Start** should be enabled because the old reservation can be refunded/re-reserved. Swap: Character1 becomes unassigned, Character2 progress restarts at0, bankBronze stays0. Cancelling Character2 returns exactly2Bronze total, not4. Finished output/XP never transfers as progress. Bank counts show **unreserved** quantities.
12. Failed replacement: while a funded dagger is unfinished, try starting an unfunded bow on the same character. Start is disabled; the existing dagger continues without an early refund. Failure must never cancel a valid job merely because the replacement cannot afford all materials.
13. XP eligibility: let Character2 gather Common Wood while Character1 is active. Wood arrives but Character2XP remains0 without effects. Make Character2 active at a cycle boundary: subsequent completed cycles give56XP to it. The former active character continues valid production with zero baseline XP. Selection alone must not interrupt progress or assignments.
14. Offline: pause automatic time; start a funded meal with its producer selected, then click **Simulate5seconds OFFLINE**. A meal arrives but no full selected-character XP is granted without background effects. Repeat using a live focus/pause absence with automatic time resumed. Qualifying effect tests cover fractional XP and the50% cap; the UI grants none.
15. Mode isolation: enabling DEVELOPMENT XP resets state, switches to the old100000/cycle sparse fixture and labels levels as milestone floors. It is not production progression. DEVELOPMENT recipes add distinctly labelled `[DEV]` samples; live starter assumptions remain distinguishable. Refunds remain enabled in both live UI modes. Stop/re-enter Play to restore default live data with empty bank/assignments.
16. Level99: use the automated cap test for an immediate check rather than waiting hundreds of hours. It must retain level99/XP10000000, no wrap/hidden overflow, while subsequent resources and products continue. Next-level text should say capped. Reopen SampleScene and IdleboundTest to confirm unchanged assets/configuration.

## Unity EditMode Test Runner

Window > General > Test Runner > EditMode → run `Idlebound.Foundation.Tests`.

- Existing102 cases unchanged:14GameState +35Gathering +53Production. Their sparse fixtures preserve historical contract, including former no-refund behavior; they do not define current live defaults.
- New153:99full-curve level/boundary cases,30approved gathering activity cases,8starter-recipe balance cases,14ordinary tests and2background/offline parameter cases.
- Expected authored total255. Record actual executed/passed/failed/skipped counts and Console output. No test pass is claimed until executed. Investigate any regression rather than deleting or weakening assertions.

New tests cover exact milestones/next thresholds, live early levels and gates, configured XP, production rewards, default/live versus legacy fixture boundaries, reversible cancellation and exact completion boundary, reassignment/swap/refund reuse, failed replacement atomicity, multi-input refunds, checked refund overflow, level99 saturation and unchanged background/offline invariants.

## Cloud checks and outstanding risks

Run `python Scripts/model_progression.py` for the exact-rational data checks and level-gated training models. Additional static checks cover metadata/importers/GUIDs, assembly references, unchanged baseline tests/scenes/packages, archival source-table preservation, explicit assumption labels, UI live wiring and whitespace. They are not a C# compiler or Unity test execution.

Review the reconstructed rounding/linear interpretation against the original CSV if provided. New starter quantities/levels/XP,10Mcap and refundable cancellation are current product/balance choices for review, not earlier handoff approvals. Queues, complete future recipes, shops/GP values, speed/equipment, persisted refund reconciliation, secure offline replay, membership and cloud remain outside scope. The live UI still uses one job per Start; model rates exclude click/queue overhead. At numeric bank limits, checked refund overflow blocks cancellation without losing the job or partly paying a refund.
