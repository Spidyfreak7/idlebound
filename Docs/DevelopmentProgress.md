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

Focused follow-up to user playtesting: compact non-overlapping Equipment icon layout with a larger reserved character area, catalog names/live quantities in Bank, and explicitly approved provisional 3× development combat XP (Melee 3 / Ranged 3 / Magic 1.5 per actual damage). Common XP curve, gathering/production rewards and gameplay implementations remain unchanged. Preserved 359 EditMode and 3 PlayMode cases, updating only seven CSV-dependent existing case expectations. Added 24 EditMode and 2 PlayMode cases for 383/5 authored totals. Static geometry/catalog/preservation/syntax checks and the existing progression model passed; Unity compilation, new tests and rendered UI/device validation remain unrun. See `Stage8B05BVerification.md`; actual Unity pass counts are not claimed.

## Stage 8B-05C

Added a pinned shared character activity header on Overview/Equipment/Activities, independent per-tab expanded/compact defaults, a secondary chevron and live read-only counter/XP feedback. Removed full-width scene toggles and redundant character-page summaries; Bank retains no activity header. Gameplay/catalogs and session ownership are unchanged. Preserved all 383 EditMode cases and 5 PlayMode tests; added 10 and 2 for 393/7 authored totals. Syntax/static checks and the existing progression model passed; Unity compilation, tests and device layout/input verification remain unrun. See `Stage8B05CVerification.md`. Published to `stage-8b-05c-review` for review; not merged into main.

Stage 8B-05C navigation follow-up: Bank remembers the originating character/tab/skill/header and approximate scroll, with Back to Character and bottom-Roster return paths. Direct dashboard Bank visits return to the dashboard; equipment-target changes remain independent. Added 14 EditMode and 2 PlayMode navigation cases for 407/9 authored totals. Static checks passed; Unity compilation/execution remains unrun. The follow-up stays on the review branch; main is untouched.

## Stage 8B-05D

Focused character layout cleanup on `stage-8b-05c-review`: removed character title strip/permanent warnings and global Stop/Cancel; all three tabs default expanded with independent remembered chevrons. Assigned cards retain contextual stop/job cancellation. Reduced equipment footprint and character preview while retaining ten non-overlapping targets. Updated existing geometry/default/scene tests and added two PlayMode checks; authored totals remain 407 EditMode and now 11 PlayMode. Static syntax/geometry/metadata checks and the unchanged progression model passed; Unity compilation, Test Runner and rendered 720×1280 / 360×640 checks remain pending. No gameplay/session/settings/scene changes. See `Stage8B05DVerification.md`. This remains a review-branch change, not merged into main.
