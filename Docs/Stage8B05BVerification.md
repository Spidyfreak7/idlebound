# Stage 8B-05B — Equipment, Bank and development combat XP

## Scope and authority

Baseline: `8833667af3717e110c700923d30d161814570b6d`, main, Unity 6000.3.26f1. This follow-up implements the user's three explicit playtest revisions only. The later approval supersedes the previous **development combat XP coefficients**, not the common XP curve or other mechanics. The baseline has 359 authored EditMode cases and 3 authored PlayMode tests; these counts are not a new report of passing tests.

No Unity Editor or C# compiler is available in the cloud. Static/syntax/model checks below were run; **Unity compilation, NUnit execution, rendered layout and touch testing were not performed**. Actual Windows verification is required.

## Equipment

The old 570-unit panel and 110×100 labelled cards are replaced by a 500-unit worn-equipment panel with 56–72-unit square icon buttons. At the normal 448-unit content width, squares are 67.2 units and the character has a 261.6×313.6 reserved rectangle, replacing its old 150×230 rectangle. Because the sprite preserves aspect, its visible presentation grows without stretching. Head/Feet sit above/below the character; the other eight slots occupy left/right perimeter columns, with Body/Legs and Mainhand/Offhand arranged near their corresponding heights. The character and every slot have separate, non-overlapping rectangles.

`EquipmentLayout` contains only presentation geometry. The panel recomputes placements when its content width changes. The helper supports virtual content widths≥200; the existing 480-unit portrait Canvas reference yields roughly 456 units before device safe-area adjustments. Normal 320/360/390px-wide portrait phones therefore retain useful tap targets; virtual test widths do not represent physical pixel widths. Very narrow safe areas outside the supported geometry still require a product decision rather than a gameplay change. The page may scroll vertically on shorter phones; no horizontal scrolling is introduced.

The main layout has equipped item icons and muted empty-slot shapes, with no repeated Empty/Equipped text. Tapping any slot opens its name and status, equipped item name/requirements/identity, an inspection route, Unequip, and compatible bank replacements. Existing item details retain requirements, development stats, neutral style changes and changed-only green/red numeric comparisons. Two-handed returns, stock validation, replacements and instance identity still use the unchanged domain APIs. No animated activity stage, equipment visual layering, new art or enchantments were added; the existing sprite and override hook remain.

## Bank

Every tile now shows **the catalog-provided `BankItemView.Name`**, its item icon and an independently refreshed quantity. Categories are used for filters rather than replacing names. A two-column grid and 184-unit rows give names room to wrap; the name region uses 18–20-unit text fitting and is separate from the right-aligned quantity below the icon. Large quantity strings can fit down to 12-unit text while retaining the exact value. Inspecting a tile still opens the existing details panel.

Search by name/ID, type filters, equipment-target selection and live bank bindings remain unchanged in behavior. No catalog/resource/item is renamed. For example, the current catalog calls `metal-1` **Bronze**, so the tile says Bronze; the request's illustrative “Bronze Ore” is not substituted as new game data. Loot retains its catalog name, including existing development labels. No inventory quantity, ownership, persistence or organization rule changes.

## Provisional combat XP baseline

| Style | Previous XP per actual damage | Stage 8B-05B playtest value |
| --- | --- | --- |
| Melee | 1 | 3 |
| Ranged | 1 | 3 |
| Magic | 0.5 | 1.5 |

These are explicitly **PROVISIONAL PLAYTEST BASELINE** values, not final production balancing. Magic remains half of Melee/Ranged, preserving its lower combat coefficient ahead of future timed enchanting. Only three numeric CSV fields changed; the data header records the approval scope.

The level 1–99 curve, exact 10000000 cumulative XP cap, gathering/production rewards, triangle, damage/overkill accounting, monsters, intervals, equipment, loot, selected/background/offline eligibility, 50% background cap and death behavior are unchanged. Custom tests that instantiate their own `CombatRules` retain 1/1/0.5 or their other explicit fixture values. There is no new XP per attack or kill.

Against the unchanged 12 HP Chicken, using existing sample weapons:

| Weapon/style | Actual damage per completed first defeat | Time to first defeat | Active online XP |
| --- | --- | --- | --- |
| Dagger / Melee | 5 + 5 + 2 | 6s | 36 |
| Shortbow / Ranged | 2.5 + 2.5 + 2.5 + 2.5 + 2 | 10s | 36 |
| Wand / Magic | 10 + 2 | 4s | 18 |

The dagger's first kill is 36 XP, not 45; the final hit earns only 2×3. Magic's final hit earns 2×1.5, not a full-hit reward. An unarmed Chicken defeat still takes 12s and now yields 36 eligible Melee XP. Loot/HP/timing are not accelerated. At zero background baseline, assigned background characters and every offline character still earn 0 XP; externally supplied effects can raise that only to the existing 50% cap. Active online characters remain full-rate.

## Tests and actual checks

All 359 baseline EditMode cases and 3 baseline PlayMode tests are retained. Exactly seven existing cases have CSV-dependent expectations adjusted: three equipped-style cases, the gathering→crafting→combat integration, equipment-change XP, stop-state XP, and UI combat projection. Explicit custom-rule fixtures and their expectations are unchanged.

Added 24 EditMode cases: 9 equipment geometry cases, 14 CSV coefficient/actual-damage/duplicate-settlement/eligibility cases, and 1 bank catalog-name/quantity/search projection case. **383 authored EditMode cases total**. Added 2 PlayMode tests for rendered equipment geometry/all-slot details and bank names/quantity refresh/long-name fit/session conservation; **5 authored PlayMode tests total**. Source counts remain subject to actual Test Runner discovery. No tests were deleted or disabled.

Executed in this cloud:

- Existing `python Scripts/model_progression.py`: passed, with unchanged common curve, gathering and production data.
- All C# files parsed using the existing temporary tree-sitter C# parser: passed (syntax only, not type checking).
- Independent geometry check across content widths 200–640: all ten slots remain in bounds, square and clear of each other and the reserved character rectangle.
- Development CSV compared against the base: only XP fields 1→3, 1→3, 0.5→1.5 changed; other values identical.
- Independent catalog-driven damage/XP calculation: first Chicken defeat gives 36/36/18 XP for Melee/Ranged/Magic at 6/10/4s respectively, with 12 actual damage each.
- Static preservation checks: shared host/clock, all core gameplay implementations, other catalogs, scenes, settings, packages, old independent fixtures and unaffected tests unchanged.
- Unity metadata/GUID uniqueness, assembly JSON/reference checks and `git diff --check`: passed.

Unity compilation, EditMode/PlayMode test execution, rendering, input and Android/iOS verification remain **not run**. Bounds models and syntax parsing are not a Unity runtime pass.

## Exact Windows verification

1. Pull the reviewed commit and open the project using Unity 6000.3.26f1. Let imports finish and inspect Console. Open **`Assets/Scenes/IdleboundPlay.unity`** and enter Play mode. No package, startup scene or build-setting change is needed.
2. Check Game views 320×568,360×640,720×1280 and1080×2400. Inspect Character 1 → Equipment. Confirm all ten compact square icons, no repeated Empty/Equipped captions, a prominent unobscured static character, reachable bottom navigation and no horizontal scrolling. Resize while on Equipment; no slot may overlap another slot or character art. Scroll vertically where needed.
3. Tap every slot. Each modal must identify the slot and Empty/Equipped state, with Close reachable. For populated slots check item name, requirements, instance identity, Inspect equipped item, Unequip and compatible bank replacements. Empty silhouettes and occupied icons must be recognizable and distinct. Equipment must not contain the Overview activity animation.
4. Gather at least 2 Bronze, start one Bronze dagger job, and complete 15 productive seconds. Equip it from Mainhand details. Bank decreases by 1; the Mainhand icon changes. Inspect its development stats. Unequip returns it; re-equip preserves its identity. Inspect Character 2 without making active: that character cannot borrow the equipped unit. Stock and assignment behavior should match Stage 8B-05.
5. Test replacements and the two-handed/offhand rule with existing crafted stock or the preserved debug kit in `IdleboundFoundation.unity`. The kit is a development test tool, not a player grant. Compare changed stats: green improvements, red reductions, no unchanged entries. Standard helm requirements remain unchanged. Failed equips must not partly consume stock.
6. While gathering, open Bank. The tile must display **Bronze**, not RESOURCES, and an exact growing quantity. Inspect it to verify the same catalog name/quantity. Search “BRONZE” and `metal-1`; both should find the stock. Check All/Raw/Gear/Made/Loot filters without quantity changes. Let gathering, production or combat add to an existing stack while Bank/details remain open; the quantity must refresh without resetting the session.
7. Inspect long real catalog names using existing production stock or the debug kit. Names should wrap legibly in the two-column grid while quantities remain separately visible. Tap each to confirm the full item name in details. Loot tiles must display the actual catalog loot name, not LOOT. No fictitious items or renamed resource data should appear.
8. With Character 1 active, Melee dagger equipped and a fresh Chicken encounter, watch the first three 2s attacks: damage 5/5/2; Melee XP increases 15/15/6 for 36 total. One guaranteed feather appears; optional loot odds are unchanged. Continue or stop/reopen screens: no duplicate defeat payout, session reset or flat kill bonus. Prior-stage documents' 12 XP dagger example is historical and superseded here.
9. With real crafted shortbow and wand, repeat from a fresh Chicken. Shortbow: five attacks over 10s give 36 Ranged XP. Wand: two attacks over 4s give 18 Magic XP. Damage, enemy HP, player HP, intervals and loot must match the unchanged sample mechanics. Make another character active: background fighters get resources/loot but 0 baseline XP. Offline replay still uses background eligibility for everyone, including the selected character.
10. Mining still gives 56 XP per first-tier cycle; a selected Bronze dagger production still gives 250 Smithing XP. Inspect the same level thresholds and 10M cap. Death still stops combat with no item/XP loss and no new healing/recovery policy.
11. **Window → General → Test Runner → EditMode → Run All**: expect 383 authored cases. Then run **PlayMode → Run All** in the Test Runner's isolated scene: expect 5 authored tests. Save actual discovered/pass/fail/skip counts and Console output; source counts are not pass claims. The existing EditMode assembly adds a presentation-assembly reference solely to test the geometry helper. No new package is needed.

## Limitations

Actual phone readability, sprite prominence, rendered name fitting, input and pixel-scale behavior still need Windows/device review. Current art remains a prototype; extremely long future catalog names or extreme safe-area widths may need later typography/layout work. No new gameplay, persistence, animation system, economy, enchantment or recovery feature is included.

## Modified files

13 intended files, including metadata:

```text
Assets/Foundation/Development/EquipmentLayout.cs
Assets/Foundation/Development/EquipmentLayout.cs.meta
Assets/Foundation/Development/IdleboundMobileUI.cs
Assets/Foundation/Development/Resources/Idlebound/DevelopmentCombatRules.csv
Assets/Foundation/Tests/Editor/EquipmentCombatTests.cs
Assets/Foundation/Tests/Editor/EquipmentLayoutTests.cs
Assets/Foundation/Tests/Editor/EquipmentLayoutTests.cs.meta
Assets/Foundation/Tests/Editor/GameUiTests.cs
Assets/Foundation/Tests/Editor/Idlebound.Foundation.Tests.asmdef
Assets/Foundation/Tests/PlayMode/MobileNavigationTests.cs
Docs/DevelopmentProgress.md
Docs/Stage8B05BVerification.md
Docs/UIUX.md
```
