# Stage 8B-05C — Persistent activity header

Baseline: `768a9bad552ca353730781f84c1465088e993f64`. Unity 6000.3.26f1. This is presentation/interaction polish; gameplay formulas, catalog values, assignments, equipment operations, Bank logic, production logic and the session/clock owner are unchanged.

## Delivered behavior

A shared activity header is pinned **above the character tabs, outside the scrolling page**, on Overview, Equipment and Activities. It follows the **inspected character**, labels ACTIVE/BACKGROUND explicitly and never selects them automatically. It shows their name, working/idle/attention and HP state, actual skill/assignment, enemy HP during combat, action progress and recent observed completion/defeat/XP deltas.

Default state is expanded on Overview and compact on Equipment/Activities. Each tab remembers its own expansion choice for this UI session, including when inspecting another character; switching tabs does not carry an expanded Overview into Activities. Explicit choices are not saved between Play sessions. Reopening a tab uses its remembered choice rather than forcibly resetting its default every time.

The full-width Open/Expand/Collapse activity-scene controls are removed. A muted 48×48 chevron on the right of the header is the only toggle. It has a fixed width and cannot expand across the row. Gameplay Start buttons remain larger raised controls in scrolling activity cards; the toggle is physically outside that action area. The header defaults compact on list-heavy screens, but a deliberate chevron press can show the scene on any character tab. Equipment's static worn-equipment figure remains separate.

The compact header is approximately 127 virtual units for a one-line assignment (long targets can wrap). Expansion adds 180 units plus spacing. Duplicate character summary cards were removed from Overview/Activities, the Equipment section caption was removed, and the former 70-unit brand/page heading area is now a 32-unit row. The notice strip is 32 rather than 42 units. Bottom navigation and all three character tabs are preserved.

**Bank choice:** no activity header or new status strip. It retains its clean item/search/filter layout. Roster keeps its existing active-character dashboard. Shop and Collection Log remain unchanged feature shells.

`ActivityHeaderFeedback` only observes existing per-character counters and skill XP. Repeated refreshes do not pay anything. Switching inspected characters or replacing a session establishes a fresh baseline; no historical reward is replayed as a new tick. Completion/defeat counts and XP deltas are real, but the header does not invent per-character bank-item attribution or an offline report. The latest summary remains until another observed event or inspection change; it is not a persistent reward journal. Captions inside the expanded cosmetic scene are hidden to avoid duplicating header text.

## Tests

All 383 prior EditMode cases and 5 PlayMode tests are retained unchanged. Added 10 EditMode cases for defaults, independent per-tab choices, navigation/work preservation, toggle/action separation and faithful cycle/background/combat feedback. Added 2 PlayMode tests covering pinned header presence across all character tabs, default and toggle behavior, a small separate toggle, Bank header absence, session preservation, and actual Start-one-job interactions with toggling before/during/after production.

Authored totals: **393 EditMode cases and 7 PlayMode tests** (subject to Unity discovery). **Unity compilation and all Unity tests remain unrun here.** No runtime, touch or visual pass is claimed.

## Checks actually run

- All C# source parsed with the existing temporary tree-sitter C# parser: syntax passed, not type checking.
- Existing `python Scripts/model_progression.py`: passed.
- Static preservation checks: simulation/clock ownership, catalogs, other UI actions/Bank operations, old test methods, scenes, packages and project settings unchanged.
- Asset metadata/GUIDs, assembly JSON, new authored-case counts, removed full-width toggle strings and pinned hierarchy/toggle callback boundaries checked.
- `git diff --check`: passed.

No Unity Editor or C# compiler is available in this cloud environment. Windows Unity verification is required. Changes are left uncommitted for review.

## Windows Unity verification

1. Open with Unity 6000.3.26f1. Let import/compilation finish; inspect Console. Open `Assets/Scenes/IdleboundPlay.unity` and enter Play mode. Use 320×568,720×1280 and 1080×2400 portrait Game views. No project setting changes are needed.
2. Roster → Inspect Character 1: Overview's header starts expanded. Confirm name, ACTIVE designation, assignment/status, HP and progress. The former full-width scene buttons must be absent. Collapse with the small chevron, then expand again. Scroll the nine-skill grid: the header and character tabs must remain above the scrolling content and bottom navigation must stay reachable.
3. Open Activities: header defaults compact, with no large scene or full-width scene toggle above the cards. Choose Mining, expand Bronze and assign. The Start/assignment button must remain the dominant action. Wait for a cycle: the header shows actual progress and “Completed × 1 / +56 Mining XP” for an active online character. Toggling the scene must not start, cancel, restart or complete gathering.
4. While working, navigate Overview → Equipment → Activities. Assignment, partial progress, active selection and session must persist. Equipment starts compact; all ten slots remain usable. Explicitly expand Equipment, return to Activities and confirm its separate compact choice is retained. Overview remembers its own chosen state. Expanded scenes consume additional space only after a deliberate press.
5. Gather 2 Bronze and start a Bronze dagger job. Toggle the header repeatedly before Start, during its 15s progress, and after completion. Only Start consumes ingredients. Toggling neither refunds nor consumes inputs; completion gives one dagger and 250 eligible Smithing XP once. Current cancellation/refund rules and one-job-per-start behavior must be unchanged.
6. Inspect a background character without Make Active. Header must identify that character as BACKGROUND, not show Character 1's rewards as its new tick, and leave active selection unchanged. Assign a valid gathering activity: completion feedback can appear with no invented XP. Make Active remains an explicit Overview action.
7. Fight Chicken with the existing dagger while active. Compact header shows the real combat target, player/enemy HP and attack progress. First defeat still gives 36 Melee XP under Stage8B-05B tuning. Repeated chevron presses must not alter timing, damage, HP, loot or XP. Death must show real HP 0 / Needs Attention with no recovery policy added.
8. Open Bank: no activity scene/header, normal named tiles/live quantities/search/filters. Activities continue in the existing session. Return to character management; no reset or duplicate rewards. Resizing/orientation and modal opening/closing must likewise preserve work.
9. Run **Window → General → Test Runner → EditMode → Run All**, then **PlayMode → Run All** in the isolated test scene. Expect 393 and 7 authored cases respectively; record actual discovered/pass/fail/skip results. The two new PlayMode tests exercise actual buttons and production, but do not replace manual device/readability review.

## Remaining limitations

Real Canvas sizing, touch accuracy and readability require Windows/device testing. Long assignment/feedback text can wrap or be limited by the compact feedback line; this is a summary, not a detailed reward log. Expansion on a short display reduces scrollable content height, so compact remains the management default. Per-tab header preferences and recent feedback are in-memory presentation state only. Existing offline/persistence, recovery, artwork and feature-shell limitations remain.

## Changed files

```text
Assets/Foundation/Core/ActivityHeaderFeedback.cs
Assets/Foundation/Core/ActivityHeaderFeedback.cs.meta
Assets/Foundation/Core/GameUiModel.cs
Assets/Foundation/Development/ActivityStage.cs
Assets/Foundation/Development/IdleboundMobileUI.cs
Assets/Foundation/Tests/Editor/GameUiTests.cs
Assets/Foundation/Tests/PlayMode/MobileNavigationTests.cs
Docs/DevelopmentProgress.md
Docs/Stage8B05CVerification.md
Docs/UIUX.md
```
