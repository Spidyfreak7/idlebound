# Development progress

Statuses describe verified work only. Stage 8B-01 through 8B-03B scopes and verification steps are recorded in their verification documents. Later stage scopes remain pending.

| Stage | Initial status | Evidence / next step |
| --- | --- | --- |
| 8A | In progress | 8A-04 repository/static verification and foundation documentation completed; local Unity compilation, scene, and mobile validation pending. Other 8A work is not assessed here. |
| 8B | In progress | 8B-01: user-reported seven manual checks and 14 EditMode cases passed on Windows. 8B-02: user reports all 49 EditMode cases passed and gathering works in Play mode. 8B-03 remains unverified. 8B-03B adds live XP/levels, starter assumptions and refundable production; 153 new cases (255 authored total), Windows validation pending. 8B-04 adds development equipment/combat and 75 new cases (330 authored total); subsequently user-verified 330/330 passed in Unity. |
| 8C | Not started | Await approved scope. |
| 8D | Not started | Await approved scope. |
| 8E | Not started | Await approved scope. |
| 8F | Not started | Await approved scope. |
| 8G | Not started | Await approved scope. |
| 8H | Not started | Await approved scope. |

## Stage 8A-04 evidence

- Baseline: `1163b9b` on `main`, initial Unity project commit.
- Unity version: `6000.3.26f1`; URP package `17.3.0` with a configured 2D renderer and Lit 2D scene template.
- `Assets`, `Packages`, and `ProjectSettings` are tracked. All existing asset files/directories have tracked `.meta` files with unique GUIDs.
- Package manifest and lockfile parse as JSON. Generated directories are ignored; representative source/configuration/metadata paths are retained.
- SampleScene is enabled in build settings. No project-authored C# scripts or tests exist.
- Unity Editor is unavailable in the cloud environment. Compilation, package resolution/import, Play mode, Test Runner execution, and Android/iOS builds remain unverified.
- No gameplay, dependency, editor-version, or project-setting changes were made.

## Next verification

Open the project locally using the README instructions, inspect the Console, run SampleScene, and report results. Inspect mobile build support before scheduling platform builds. Do not mark later stages complete without approved scope and supporting evidence.

## Stage 8B-02

See `IdleboundMasterDesign.md` for the four-source reconciliation and preserved numerical tables, and `Stage8B02Verification.md` for exact manual checks and pending specifications. Production XP/action and most level thresholds remain unavailable. An opt-in development fixture is clearly separated from production data. The user subsequently verified gathering in Play mode and all 49 EditMode cases passed on Windows; the cloud did not execute those checks. No packages, networking, recipes, combat, or monetization were added in 8B-02.

## Stage 8B-03

See `Stage8B03Verification.md` for implementation, test steps and policy decisions. Added validated recipes, atomic ingredient consumption, one-job timed production, finished products in the shared bank and production UI. Approved recipe data remains empty because full ingredient quantities/requirements/XP are missing. Five explicitly opted-in development recipes support the full loop. Cancellation/no-refund, completion-time XP and single-job/queue behavior are disclosed engineering policies, not new design approvals. Existing 49 tests remain unchanged; 53 new cases are authored but not cloud-executed. No equipping, equipment stats, package, scene or project-setting changes.

## Stage 8B-03B

Later explicit user approvals supply the common full curve shape/anchors and gathering XP. Committed a reproducible integer curve reconstruction (not claimed byte-identical to an unavailable original CSV), approved gathering rewards and eight live starter recipe assumptions. Cancellation defaults to reversible input refunds in live/full-curve sessions. The UI shows exact levels/next-level XP and clear assumption/fixture distinctions. All102 existing tests remain unchanged;153 new cases cover live behavior. See `Stage8B03BBalanceReport.md` and `Stage8B03BVerification.md`. Static/model checks can run in the cloud; Unity compilation,255EditMode cases and manual checks are not claimed passed. No scene/package/settings, combat, equipping, cloud or monetization changes.

## Stage 8B-04

Implemented development-only equipment and deterministic automatic normal-monster combat in the existing session/UI. Ten slots, atomic bank transactions/instance reuse, requirements, three styles, outgoing triangle, actual-damage XP, HP/death, repeat encounters and guaranteed/independent keyed loot. All 255 existing test cases remain unchanged; 75 new cases bring the authored total to 330. Static checks and the existing progression model passed; Unity compilation, Test Runner execution and Play-mode checks remain pending. No scenes, packages or project settings changed. See `Stage8B04Verification.md` for exact Windows checks and disclosed provisional damage, timing, recovery and identity policies. Stage 8B-05 has not started.

## Stage 8B-05

Baseline Stage 8B-04 has 330/330 user-verified EditMode passes. Added a separate player entry scene, shared session/clock host, tested presentation model and mobile uGUI navigation/screens. Roster, character Overview/Activities/Equipment and Bank use real session data/operations; Shop/Collection Log are unavailable shells. Original pixel-art prototypes are replaceable; final art and reliable offline welcome-back attribution remain deferred. The debug scene/tools are preserved without their former independent clock. All 330 existing tests and simulation implementations remain unchanged; 29 new EditMode cases bring the authored suite to 359, plus 3 PlayMode integration tests. Static/progression-model/syntax checks passed in the cloud; Unity compilation, new tests, actual layout/input/rendering and device testing remain pending. See `Stage8B05Verification.md` and `UIUX.md`. No gameplay balance, packages, startup/build settings or existing scenes changed.

## Stage 8B-05B

Focused follow-up to user playtesting: compact non-overlapping Equipment icon layout with a larger reserved character area, catalog names/live quantities in Bank, and explicitly approved provisional 3× development combat XP (Melee 3 / Ranged 3 / Magic 1.5 per actual damage). Common XP curve, gathering/production rewards and gameplay implementations remain unchanged. Preserved 360 EditMode and 3 PlayMode cases, updating only seven CSV-dependent existing case expectations. Added 24 EditMode and 2 PlayMode cases for 383/5 authored totals. Static geometry/catalog/preservation/syntax checks and the existing progression model passed; Unity compilation, new tests and rendered UI/device validation remain unrun. See `Stage8B05BVerification.md`; actual Unity pass counts are not claimed.

## Stage 8B-05C

Added a pinned shared character activity header on Overview/Equipment/Activities, independent per-tab expanded/compact defaults, a secondary chevron and live read-only counter/XP feedback. Removed full-width scene toggles and redundant character-page summaries; Bank retains no activity header. Gameplay/catalogs and session ownership are unchanged. Preserved all 383 EditMode cases and 5 PlayMode tests; added 10 and 2 for 393/7 authored totals. Syntax/static checks and the existing progression model passed; Unity compilation, tests and device layout/input verification remain unrun. See `Stage8B05CVerification.md`. Published to `stage-8b-05c-review` for review; not merged into main.

Stage 8B-05C navigation follow-up: Bank remembers the originating character/tab/skill/header and approximate scroll, with Back to Character and bottom-Roster return paths. Direct dashboard Bank visits return to the dashboard; equipment-target changes remain independent. Added 14 EditMode and 2 PlayMode navigation cases for 407/9 authored totals. Static checks passed; Unity compilation/execution remains unrun. The follow-up stays on the review branch; main is untouched.

## Stage 8B-05D

Focused character layout cleanup on `stage-8b-05c-review`: removed character title strip/permanent warnings and global Stop/Cancel; all three tabs default expanded with independent remembered chevrons. Assigned cards retain contextual stop/job cancellation. Reduced equipment footprint and character preview while retaining ten non-overlapping targets. Updated existing geometry/default/scene tests and added two PlayMode checks; authored totals remain 407 EditMode and now 11 PlayMode. Static syntax/geometry/metadata checks and the unchanged progression model passed; Unity compilation, Test Runner and rendered 720×1280 / 360×640 checks remain pending. No gameplay/session/settings/scene changes. See `Stage8B05DVerification.md`. This remains a review-branch change, not merged into main.

## Stage 8B-05E

Activities/recipes are now immediately actionable compact cards with names, gates, XP/duration, live material stock and contextual progress/cancel controls; no individual card expansion or lengthy development recipe text. Display-only leading `[DEV]` removal preserves catalog validation/IDs. Existing Bank return, persistent expanded scenes, ten-slot compact equipment and session ownership remain intact. Added nine EditMode cases and two PlayMode tests (416/13 authored totals); adapted earlier production UI interactions to direct Start. Static syntax/preservation/metadata/geometry checks and unchanged progression model passed; Unity compilation, Test Runner and rendered portrait checks remain pending. Documented detailed modern fantasy pixel-art direction in UIUX; a higher-detail sprite is deferred and the old original prototype remains. No gameplay/catalog/settings/scene/animation changes. Continues on `stage-8b-05c-review`, not merged into main. See `Stage8B05EVerification.md`.


## Stage 8B-06

Implemented version-1 validated, checksummed local snapshots, atomic replacement/last-known-good backup recovery, protected failure handling and automatic saves under persistentDataPath. The single persistent host restores/caps offline simulation off-thread and commits state, UTC boundary and attributed pending report together before runtime adoption. Welcome-back appears after five minutes; durable Continue acknowledgment prevents repeated reports. Existing background-zero/50%-cap XP, four-hour cap, death and pure seeded loot remain authoritative. Confirmed actual production is still one job per Start; the reported continuous-production iteration is absent and was not invented.

Preserved 416 EditMode and 13 PlayMode cases; added 66 EditMode cases and two PlayMode tests (482/15 authored totals). Core C# compiled against .NET Standard 2.1 reference assemblies. All 473 engine-independent NUnit cases passed in a standalone .NET 8/NUnitLite harness with a file-backed Resources adapter (407 existing plus 66 new); this does not execute Unity. Nine existing geometry cases and all 15 PlayMode tests, Unity compilation, portrait rendering and Android/iOS IL2CPP/filesystem checks remain pending. Syntax/metadata/scope/progression checks passed. No catalogs, balance, packages, scenes or project settings changed. Branch `stage-8b-06-review`; main remains unmerged, including its independent SceneTemplateSettings change. See `Stage8B06Verification.md`.


Stage 8B-06 continuous-production addition: later explicit approval supersedes the single-job limitation above. Smithing/Crafting/Cooking now repeat automatically with atomic next-cycle reservations, chronological shared gathering/production, explicit material-exhaustion stops, no automatic restart on new stock, existing refunds and existing online/background/offline XP. Current reservations/partials round-trip through the existing version-1 save. Added 20 EditMode cases and one three-skill PlayMode integration test; eight old single-job assumptions were updated, not removed. Authored totals: 502 EditMode / 16 PlayMode. Standalone .NET/NUnit harness: 493/493 cases passed; Core .NET Standard 2.1 compilation and static checks passed. Unity compilation, Test Runner, actual portrait UI and mobile lifecycle tests remain pending. No balance/catalog/scene/package/settings changes. See Stage8B06Verification.md.


## Stage 8B-07 — enchantments, equipment effects and save migration

User reports Stage 8B-06 passed **502/502 EditMode and 16/16 PlayMode tests in Windows Unity**. This supersedes the pending verification status recorded above for that baseline. Work now stays on permanent `develop`; main is untouched.

Implemented the explicit Stage 8B-07 handoff: 48 data-driven exact Magic unlocks across 12 types/four tiers, permanent instance-specific timed enchantments, bank locks, atomic reservations/refunds, single completion, all equipped effects and exact-tier four-piece matching. Gathering/continuous production use real modified durations; seeded critical attacks award actual-damage XP, defense reduces damage after flat armor, and source-gated provisional living regeneration is functional. Offline allowances apply independently per character (4–7h), with background-only offline XP. V2 saves retain pending work, enchant identity, deterministic attack/regen clocks and existing progress. Guarded v1 migration pins the baseline catalog identity and preserves original/archived bytes; newer-file evidence takes precedence.

Added **60 EditMode cases** and **3 PlayMode integration tests**, preserving the prior suite: **562 EditMode / 19 PlayMode authored total**. Actually executed: **553/553 standalone .NET 8/NUnitLite cases passed**, including all 493 previously runnable cases; Core .NET Standard 2.1 compilation and static checks passed. Unity is unavailable here: Unity import/compilation, nine geometry cases, all 19 PlayMode tests, portrait rendering and mobile filesystem/AOT/lifecycle checks remain unrun. See `Stage8B07BalanceDecisions.md` and `Stage8B07Verification.md`. No existing balance CSV, scenes, packages or project settings changed.


## Stage 8B-08 — food and AFK combat survival

User reports Stage 8B-07 passed **562/562 EditMode and 19/19 PlayMode tests in Windows Unity**, superseding the pending baseline status above. Added two provisional food definitions (existing cooked shrimp 8 HP / sardine 12 HP), per-character persistent auto-eat controls and manual Overview eating. Food consumes real shared-bank stock, never grants XP or revives dead characters. A chronological transaction extends the existing session so Cooking/combat/food/regen cannot borrow future outputs; independent 4–7h offline cutoffs and XP rules remain. Separate consumed-food report entries never appear as rewards. Guarded v3 migration preserves original v1/v2 bytes and prior progress, with authentic baseline-schema fixtures.

Preserved prior tests; added **61 EditMode / 3 PlayMode cases**, for **623 EditMode / 22 PlayMode authored**. Actually executed **614/614 standalone NUnit cases**, Core .NET Standard 2.1 compilation and static checks; Unity is unavailable, so Unity import/compilation, nine geometry tests, all 22 PlayMode tests, portrait rendering and mobile device checks remain unrun. See `Stage8B08Verification.md` for exact steps, policies and remaining recovery/performance risks. Work stays on develop; main, existing recipes/balance, scenes, settings and packages remain unchanged.


## Stage 8B-08A — focused food UI correction

User verified Stage 8B-08 at **623/623 EditMode and 22/22 PlayMode** passes in Windows Unity. Removed the Overview food panel entirely; HP stays in the existing header and XP/progress flows directly into the unchanged nine skills. Bank now owns compact per-target Food settings and item-specific Eat/preference actions; its target label explicitly covers equipment and food. Auto-eating is always enabled in the simulation, including previously disabled v3 flags, with no save-version/fingerprint/balance change. Navigation and independent character settings remain intact.

Three new EditMode cases plus updated superseded OFF/Overview expectations bring authored totals to **626 EditMode / 22 PlayMode**. Actually executed **617/617 standalone NUnit cases**, Core .NET Standard 2.1 compilation and static checks. Unity import/compilation, nine geometry cases, all PlayMode tests and portrait rendering remain unrun here. See `Stage8B08AVerification.md`. Work stays on develop; main is untouched.


Stage 8B-08A PlayMode follow-up: user reported 626/626 EditMode and 21/22 PlayMode, with missing food-selection UI after an 80% threshold change. Removed the threshold/preference callback's close → Bank rebuild → reopen sequence; settings now update the model and replace modal contents once. Preserved/strengthened the original test and added repeated same-frame/next-frame modal regression coverage. Standalone 617/617 passed; Unity UI compilation/runtime tests remain unrun here. Authored suite: 626 EditMode / 23 PlayMode. See Stage8B08AVerification.md follow-up; domain behavior, v3 compatibility and always-active auto-eating are unchanged.
