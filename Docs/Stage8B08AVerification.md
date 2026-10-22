# Stage 8B-08A — food UI refinement

Baseline `dfe0401250f96d9b8d9ce99b32adbbc05694125e`, permanent branch **develop**. User reports Stage 8B-08 passed **623/623 EditMode and 22/22 PlayMode tests** in Windows Unity. This is baseline evidence; this refinement remains unverified in Unity.

## Changes and compatibility

Overview has no food card, controls or replacement dashboard. Its existing XP/progress block leads directly into the unchanged nine-skill grid. HP remains in the pinned character header. Combat cards retain only a compact food status.

Bank's existing target selector now reads **Equipment & food: [character] — change**. The same inspected-character target governs equipment and consumables. A compact **Food settings** button opens a modal with the target identity, always-active indication, current preference, 10–90% threshold picker, Auto and the two existing foods, plus live shared quantities. The modal is not a new screen or permanent dashboard. Existing Back to Character, Roster return and remembered character/tab/skill/scroll behavior remain unchanged, even after intentionally choosing another Bank target.

Cooked-food item details show healing/quantity and the selected character's HP, **Eat** and **Set as preferred food** / **Already preferred**. Eat explicitly consumes that selected item, without changing preference or falling back to another food. It uses authoritative session validation and the existing character-wide same-timestamp duplicate guard. Dead/full/empty-food requests cannot consume a serving. Preferences and thresholds remain independent per character.

Auto-eating is **unconditionally active in combat**, not merely hidden. `FoodSettings.Enabled` remains a read-compatible always-true property, and existing configuration signatures accept their legacy boolean without allowing it to disable consumption. The persisted v3 bool remains readable: a false flag is normalized to enabled on restore/capture, preserving threshold, preference, cursor and all other progress. Existing v1/v2 migration paths remain unchanged. **No new save version, catalog fingerprint or balance-data change.** Food availability, explicit preference/Auto selection, threshold, chronological ordering, lethal-hit priority, offline caps and XP policies are retained. Historical Stage 8B-08 OFF behavior/Overview instructions are superseded by this explicit approval.

## Automated verification

Actually executed: **617/617 standalone NUnit cases passed, zero failures/skips** on .NET 8.0.32/NUnitLite 3.13.3, with the existing temporary file-backed Resources/TextAsset adapter. Includes all previously runnable cases, updated where the old OFF/Overview requirements were explicitly superseded, plus three new cases:

- Previously disabled v3 data loads enabled, retains other state/preferences and consumes food in combat; saves remain version 3 with the same catalog identity.
- Explicit Bank-item eating consumes exactly one selected serving, clamps/restores HP, leaves preference unchanged and rejects duplicates, raw items and unavailable selected food without fallback.
- Bank character targeting keeps food settings independent and restores the originating character/tab/skill/scroll without changing active selection, assignments or time.

Updated existing threshold/manual/death/settings-timing tests to exercise the new invariant, rather than deleting their validation. The former OFF-survival case now proves OFF cannot disable actual combat consumption while an empty bank still permits normal death. Manual-in-combat coverage uses a valid low threshold so auto-eating does not obscure the manual serving under test.

Updated the existing PlayMode food test to assert no Overview food dashboard or Eat button, all nine skill buttons accessible, Bank settings/Auto/threshold selection, no toggle, exact Bank eating, already-preferred state, independent Bank targets and Back restoration. Existing combat summary checks now expect the always-active wording. Authored totals: **626 EditMode / 22 PlayMode**.

Core .NET Standard 2.1 compilation, C# syntax, metadata/assembly checks, unchanged catalog/fingerprint inputs, progression model and scoped diff checks passed. **Unity 6000.3.26f1 is unavailable here**: Unity import/compilation, nine existing geometry cases, all 22 PlayMode tests, rendered portrait/input and device checks were not run. Standalone passes are not Unity Test Runner evidence.

## Windows Unity verification

1. Stop Play, back up the player save folder, fetch/pull **develop** in GitHub Desktop and confirm the reported commit. Open the same project in Unity **6000.3.26f1**, Assets → Refresh, wait for compilation and inspect Console. Do not delete saves, change packages or upgrade the editor.
2. Open **Assets/Scenes/IdleboundPlay.unity**. Test **720×1280** and **360×640** portrait. Overview should show the existing header HP, activity XP/progress and then the nine skills, with no food panel/toggle/Eat and no reserved blank area. All nine skills must be reachable and tappable by normal scrolling.
3. Overview → Bank → Food settings: verify the selected character name, always-active message, current preference, threshold and shared quantities. Select 80%, shrimp, then Auto. Close and reopen; values persist. The Bank remains account-wide and its lists/navigation remain usable.
4. Open a cooked shrimp item: verify **8 HP**, quantity and target HP. With the character alive and injured, Eat removes exactly one shrimp and restores at most 8 HP, regardless of another saved preference. The action cannot consume twice at the same paused simulation timestamp. Full/dead/empty selected food disables eating. Sardine remains **12 HP**. Set as preferred food becomes Already preferred; choose Auto again through Food settings.
5. Change **Equipment & food** target to Character 2. Set a different threshold/preference and verify Character 1 is unchanged. Food actions apply to Character 2, never implicitly Make Active. Back to Character (and bottom Roster from Bank) returns to the original character screen/tab/skill and approximate scroll. Repeat from Overview, Equipment and Activities.
6. With cooked food stocked, fight until HP crosses threshold. Auto-eating must occur without any enable control. Reload an isolated copy of an old v3 state containing Enabled=false using `FoodTests.PreviouslyDisabledV3LoadsEnabledWithoutResettingPreferencesOrProgress`; it must normalize only the flag, preserve preferences/progress and eat in combat. Do not hand-edit the real save/checksum to perform this test.
7. Stop/reopen and run existing isolated offline probes. Confirm thresholds/preferences remain, food is genuinely consumed, food use is separate from received rewards, combat death cannot be reversed by food and Continue/restarts never replay food/rewards. Gathering, continuous Cooking, enchanting, equipment and XP/caps remain unchanged. Existing Stage 8B-08 migration/recovery tests still apply; the file remains **player-v3.json**.
8. Window → General → Test Runner: run all **626 EditMode** and **22 PlayMode** tests in the isolated runner scene; export XML and inspect Console. Batch-mode commands from Stage8B08Verification.md still apply with these new expected counts. Actual phone rendering, touch and lifecycle verification remain required.

Changes are limited to food domain/projection/UI, the affected food/UI tests and documentation. No balance CSV, schema, migrator, scheduler, scenes, equipment layout, nine-skill grid or project/package settings changed.


## PlayMode modal follow-up

Windows baseline verification reported **626/626 EditMode and 21/22 PlayMode**, with the food flow failing to find “Cooked shrimp” after changing the threshold to 80%. Source inspection confirmed that label is unchanged, but the callback closed/deactivated the modal, rebuilt Bank and reopened the modal in one frame while old UI objects awaited Unity destruction. Threshold and preference changes now update the authoritative model and replace the modal contents once, without closing its root or rebuilding Bank. This removes that unnecessary lifecycle path; exact Windows runtime confirmation remains required.

The original failing test retains all assertions and adds explicit threshold, returned modal title and selected-preference checks. Added `FoodSettingsKeepPreferenceButtonsAcrossRepeatedSameFrameModalTransitions`: exercises 10/80/90/50 thresholds, immediate shrimp/Auto selection, next-frame survival of both food buttons, one active modal, unchanged session snapshot and Bank return. Authored totals are now **626 EditMode / 23 PlayMode**. The cloud reran **617/617 standalone cases**, zero failures/skips, and static syntax/scope checks. Unity is unavailable; the UI fix/new PlayMode regression have not been executed here.

On Windows, pull develop, stop Play, refresh/recompile and rerun the original failing test plus the new regression, then all **626 EditMode / 23 PlayMode** tests. Manually open Bank → Food settings → Change HP threshold → 80%; verify the Food settings modal immediately returns with Cooked shrimp/Cooked sardine/Auto available. Select shrimp, then Auto, repeat threshold changes and verify target identity, stock, HP, settings and Back to Character. Repeat 360×640 and 720×1280. No gameplay, balance, save schema/fingerprint or domain code changed in this follow-up.
