# Stage 8B-05E — Activity simplification and visual direction

## Scope and file manifest

Review branch: `stage-8b-05c-review`, continuing the committed Stage 8B-05C/05D work. No main merge.

- `Assets/Foundation/Core/GameUiModel.cs`: display-only name normalization in projections and equipped-item feedback. Commands/navigation/clock behavior unchanged.
- `Assets/Foundation/Core/PlayerNames.cs` and `.meta`: pure leading `[DEV]` name-marker removal; internal markers and stable IDs remain unchanged.
- `Assets/Foundation/Development/IdleboundMobileUI.cs`: directly actionable compact cards, concise requirements/live stock/progress, contextual cancellation, and clean equipment-detail names. The shared header chevron remains; individual card toggles are gone.
- `Assets/Foundation/Tests/Editor/GameUiTests.cs`: eight name-normalization cases and one catalog/projection preservation test.
- `Assets/Foundation/Tests/PlayMode/MobileNavigationTests.cs`: adapted existing production tests to direct buttons; added one-click/locks/stock/completion/Bank continuity and clean Bank/detail-name coverage.
- `Docs/DevelopmentProgress.md`, `Docs/UIUX.md`, this file: current behavior, approved visual direction and Windows verification.

The Bank return flow was already implemented in Stage 8B-05C. It is preserved rather than duplicated: originating character/tab/skill/header/best-effort scroll, Back to Character and bottom Roster return, dashboard fallback, independent equipment target. No new session, inventory or navigation implementation was needed. Cards still delegate to the existing session commands; swaps retain explicit confirmation and cancellation retains existing input refunds.

Underlying development labels, provisional balance, IDs and catalog validation remain intact. Hiding name prefixes does not convert fixtures/assumptions into approved production balance. Existing rules include the development 3/3/1.5 combat XP coefficients; no data, curve, balance, assignment or offline rules changed. No scenes, packages, settings, artwork or animation implementation changed.

## Tests and available validation

Preserved the existing 407 EditMode cases and 11 PlayMode tests, updating superseded individual-card expansion interactions. Added nine EditMode cases and two PlayMode tests: authored totals **416 EditMode / 13 PlayMode**. Coverage includes clean names without catalog/ID mutation, direct assignment, disabled locked starts and defensive requirement validation, live stock enabling production, contextual refunds, completion once, Bank context continuity, existing session/time/reward checks, shared header and all ten equipment slots.

Cloud checks: C# syntax parsing (not compilation), scoped method/diff preservation, metadata/assembly JSON, removed-toggle/concise-card structure, test-case inventory, unchanged equipment geometry and existing progression model. No Unity Editor or C# compiler is available. No Unity compilation, Test Runner, rendering or runtime passes are claimed.

## Exact Windows verification

1. In GitHub Desktop click Fetch origin, then Current Branch → `stage-8b-05c-review`; pull the latest commit. Open the project in Unity **6000.3.26f1**. Allow import and confirm no Console errors. Keep build/startup settings unchanged.
2. Open `Assets/Scenes/IdleboundPlay.unity`. Configure Game view fixed portrait sizes **720×1280** and **360×640**. Repeat all UI checks at both, with the complete viewport visible. Enter Play mode; Inspect Character 1.
3. Visit Overview, Activities and Equipment. The live scene must start expanded on each, pinned above tab content, with no large title/page-title/permanent-warning strip or global Stop/Cancel. Collapse one tab with the small chevron; other tab preferences remain independent. Reopen it. Equipment keeps its compact panel, small aspect-preserving character and ten non-overlapping tappable slots; open/close all ten details.
4. Open Activities → Mining. Every card immediately has its actual name, level/XP/duration and Start or Locked control. There are no per-card plus/minus/detail-opening buttons. Iron remains visible, shows Requires Mining Lv 10 and has a disabled Locked control at level 1. Navigating or tapping a non-button title must not start work.
5. Click Start on Bronze **once**, without opening any details. Confirm the assignment and live progress; after 5 seconds confirm one Bronze in the shared Bank. Start controls are separate from the scene chevron. Expand/collapse the scene during work and verify no reset/extra reward.
6. With only one Bronze, select Smithing. Bronze dagger shows Needs 2 Bronze and live bank quantity; its Start is disabled. As gathering reaches two Bronze it enables. Click Start once: exactly two Bronze are deducted. The card shows actual progress and a secondary Cancel job. No long refund/balance-assumption explanation is shown in the card.
7. Cancel a partial job: inputs return once, with no finished output. Restart and allow 15 seconds: one Bronze dagger appears. Repeated clicks while running must not reserve more inputs; switching tabs/Bank/chevron must not complete or reward another copy. Existing single-job behavior remains (no automatic recipe queue).
8. From Overview, Equipment and Activities (use a nondefault skill and scrolled list), open Bank. Back to Character restores the same character/tab/skill/header and approximate scroll; repeat with bottom Roster. From the dashboard, Bank → Roster returns to dashboard. Inspect Character 2 intentionally and verify subsequent Bank return restores Character 2, while active selection stays unchanged unless Make Active is explicitly used. Check running gathering/production continues without interruption.
9. Verify real item/monster/activity names in Bank tiles, details, equipment replacements and live assignment contain no leading `[DEV]`. For a quick optional debug-only coverage path, stop Play, open `IdleboundFoundation.unity` and use its development gear-kit action, or use the Test Runner's clean-name PlayMode test. Debug and player scenes have separate in-memory sessions; kit items are not expected to persist across scenes. For loot in the player scene, fight Chicken using Melee when HP permits, then inspect earned loot in Bank. No development kit is granted automatically and no XP/time cheats are added to player UI.
10. Check names and essential requirements fit at both portrait sizes; long labels may wrap. Scroll list-heavy content while the scene remains pinned. Confirm swaps still request confirmation naming the displaced character. Confirm raw catalog CSVs still contain development markers and all original balancing values.
11. Stop Play mode. Window → General → Test Runner → run all EditMode tests, then all PlayMode tests in the runner's isolated test scene. Record actual counts/results and Console output. Existing tests for header defaults, Bank context, equipment accessibility, session ownership and duplicate rewards must remain passing.

## Limitations and pending decisions

Actual phone readability/touch targets and Unity compilation/runtime remain unverified here. Expanded pinned scenes reduce visible list rows; scroll remains necessary. Current lists build all cards (not virtualized), so a much larger future catalog may require profiling. Name cleaning intentionally preserves internal `[DEV]` text and technical IDs for search; debug surfaces retain development labels. The existing coarse original art remains: this stage documents the approved modern fantasy pixel direction and replaceable sprite seam, but supplies no higher-detail sprite, final filtering policy or animation overhaul. Detailed art and illustrated-2D backup remain future art-review work. Progress remains in memory and is not saved.
