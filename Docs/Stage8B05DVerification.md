# Stage 8B-05D — Character layout cleanup

Review branch: `stage-8b-05c-review`; do not merge into main until reviewed.

## Changes and scope

The dashboard heading strip is inactive on character pages and reserves no space. The permanent prototype banner and Overview warning are removed. Only transient command/error feedback appears, below the pinned live header. Progress is still in memory and is not saved.

Overview, Activities and Equipment now start expanded. Their small secondary chevrons retain independent per-tab preferences during the session, including Bank return. This later approval supersedes the compact defaults documented for Stage 8B-05C. Bank still has no full activity header.

There is no global Stop/Cancel assignment button. Expand the currently assigned activity card for its secondary Stop activity control; a running production job instead has Cancel job on that card. These call the existing unassign/refund operation. Primary Start controls and assignment replacement remain unchanged.

The equipment panel is 320 rather than 500 reference units high, with a centered cluster at most 304 units wide, ten 56–68 unit square targets, and an aspect-preserving character area at most 112×130. The character image is supporting context; no slot overlaps its reserved rectangle. Geometry supports replacement artwork without requiring chunky pixel proportions. No animation, art, balance, inventory, assignment, equipment or clock implementation changed.

## Tests and cloud validation

Updated existing EditMode default-state and equipment geometry expectations to the newly approved layout. All eight width cases (200–640) retain bounds, target-size and non-overlap assertions. Updated existing PlayMode scene expectations; retained session/navigation/production reward checks. Added two PlayMode tests for title/banner/global-stop absence and contextual production cancellation/refunds. Authored suite: 407 EditMode cases and 11 PlayMode tests.

Available checks: C# tree-sitter syntax parsing, whitespace checks, independent equipment geometry checks, asset metadata/assembly JSON checks, unchanged progression model and scoped diff review. These are not C# compilation or Unity execution. No Unity Editor or C# compiler is available in this environment; Test Runner, rendered layouts, touch interaction and Play mode must be verified on Windows.

## Exact Windows Unity checks

1. Fetch origin and switch to `stage-8b-05c-review` in GitHub Desktop. Open the repository in Unity **6000.3.26f1**. Let scripts import and confirm no Console compilation errors.
2. Open `Assets/Scenes/IdleboundPlay.unity` manually. Do not change startup/build settings. In Game view create fixed-resolution portrait presets **720×1280** and **360×640**. Run every step below at both resolutions; use 1× zoom or Fit as needed to inspect the full viewport.
3. Enter Play mode, Inspect Character 1. On Overview, Equipment and Activities verify the first major element is the live activity header, with no IDLEBOUND/page heading, permanent warning or empty title gap. Each initially shows the animated scene. The three tab buttons themselves remain visible.
4. Collapse Equipment with its small chevron; visit Activities (still expanded), then Equipment (still collapsed). Reopen Equipment. Verify no start/stop, inventory change, active-character change or progress reset comes from the chevron. A fresh Play session restores all three expanded defaults.
5. On Equipment verify the scene remains pinned above the tab content, the character preview is smaller, all ten slots are distinct and unobscured, and each opens the correct details. Scroll the equipment content if needed to reach its bottom. Confirm readable stats/details and comfortable tap targets, especially at 360×640. Resize between presets and recheck slot bounds.
6. On Activities select Mining, expand a gathering card and Start activity. Ensure Start remains visually dominant and no global Stop/Cancel appears. Scroll the list: the live scene stays pinned. Expand the assigned card to use its secondary Stop activity control, then restart gathering.
7. Gather two Bronze resources; change to Smithing, expand Bronze dagger, Start one job. Confirm ingredients deducted. On the same assigned card, Cancel job must refund inputs once with no output. Restart, let the job complete and confirm one output; collapse/reopen the header and navigate tabs/Bank without awarding another item. Existing production timing/balance applies.
8. From each character tab open Bank, then Back to Character; repeat using bottom Roster. Confirm the same inspected character/tab/skill/header preference and approximate scroll return. Bank remains shared. From the dashboard, Bank → Roster must return to dashboard. Assignments and the active character must remain unchanged by navigation.
9. Leave gathering/combat running while switching tabs and Bank. Verify normal progress continues, with no session reset or duplicate completion. Navigation does not itself advance time; the existing live session clock continues normally.
10. Stop Play mode. Open Window → General → Test Runner. Run all **EditMode** tests, then all **PlayMode** tests in the runner's isolated test scene. Record actual results and Console errors; cloud validation does not certify these tests as passed.

## Remaining tradeoffs

Expanded pinned scenes leave fewer visible activity rows, especially on short phones. Equipment/help and expanded cards remain vertically scrollable; all ten slots need not fit simultaneously above the fold. Actual readability, clipping, safe areas and physical tap comfort require the two requested rendered checks and device testing. Portrait reference scaling makes typical equipment targets about 51 pixels wide at 360px width. The live header and equipment preview intentionally show separate representations. Animation polish and art direction remain deferred. Temporary errors/success feedback can briefly consume one additional line.
