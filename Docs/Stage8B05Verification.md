# Stage 8B-05 — Mobile UI verification

## Baseline and evidence

Baseline `44230f5f9d46baa6b3757a57f16bf22850f88c7b` on main. The user reports **330/330 EditMode tests passed in Unity** for Stage 8B-04. All those test files, existing domain simulation files, catalogs, scenes, packages and project settings remain unchanged here. New presentation classes are added beside the domain, not substituted for it.

This stage authors **29 additional EditMode cases**, bringing that suite to **359 authored cases**, plus **3 PlayMode integration tests** in a separate assembly. Counts are source counts pending discovery. None of these Unity tests, C# compilation, Canvas initialization, rendering, touch input, scene execution or mobile builds was executed in the cloud. No Unity Editor or C# compiler is installed. Syntax parsing and static checks are not compilation or runtime verification.

## Entry and screen delivery

Open **`Assets/Scenes/IdleboundPlay.unity`** directly in Unity **6000.3.26f1**. Enter Play mode; the uGUI player interface is the scene's default experience, with no development controls. UI objects are built at runtime, so Edit mode shows the bootstrap/camera scene, not a pre-rendered screen. Do not change build settings to verify it.

The original **`Assets/Scenes/IdleboundFoundation.unity`** remains the IMGUI development fallback. Its serialized scene is unchanged. It now obtains the same host/controller architecture rather than owning a second session or Update loop. All fixture/reset/pause/manual-step/sample-kit/HP tools remain there. Scene changes intentionally create a fresh in-memory session; browsing within the player UI never does. Open one entry scene normally, not both additively for routine testing.

| Screen | Implemented behavior | Explicit limitations |
| --- | --- | --- |
| Roster | Real active-character summary, statuses, assignment/HP/progress, inspect routes, three available characters | Slots 4–9 have no fabricated unlock thresholds or production entitlements |
| Overview | Make Active without confirmation, real work/XP, collapsible activity slice, all nine skills in a 3×3 grid with real levels/XP | Entire page scrolls on short phones; collapsing scene leaves more room for the grid |
| Activities | Compact header, skill picker, expandable gathering/production/combat cards, visible locks, real requirements/stock/progress/HP/XP, start/stop and explicit named-occupant swap confirmation | No large activity animation here; production remains one job per Start; combat style requires its existing mainhand |
| Equipment | Static central adventurer, ten positioned slots, compatible bank browser, requirements, changed-stat comparisons, atomic equip/replace/unequip | Development gear stats; no equipment appearances, enchant application or healing |
| Bank | Shared item grid, quantity/type, inspected names/details, case-insensitive search by name/ID, All/Raw/Gear/Made/Loot filters, explicit equipment target | No animation, sales, pricing, saved custom tabs or manual rearrangement |
| Shop | Working Buy/Sell navigation and styled unavailable states | No stock, GP, transactions, SDKs or actual economy |
| Collection Log | Ten boss-specific identity previews, three item-shaped silhouettes each, taken from existing S2 source table | All marked unavailable/not tracked; no boss definitions, stats, effects, kills or earned ownership |

Shop and Collection Log are honest navigation shells, not completed gameplay systems. Offline welcome-back reporting remains deferred: aggregate bank deltas and character XP can be read, but the existing session cannot reliably attribute shared resources/loot to individual characters across all activity transitions. No fabricated per-character report or duplicate settlement is added. The approved five-minute/account-wide report belongs with durable offline accounting.

## Architecture and UX policies

- `GameSessionHost` alone constructs the session and calls its `SessionRuntime` clock. Duplicate hosts destroy themselves before creating a session. `SessionRuntime` rejects a second runtime for the same session and rejects a different clock caller. This applies even if both presentation types are attached. No screen has a settlement loop.
- The runtime quantizes absolute realtime to integer ticks, retains a monotonic cursor, groups combined pause/focus absence into one offline settlement, and retains the existing four-hour offline cap and background XP rules. It commits its wall-clock cursor only after domain settlement succeeds. Invalid time/overflow/budget errors stop the host with a visible error; they do not silently reset or partly pay rewards. Retry/recovery tooling for exceptional oversized replays is not added; the debug reset remains explicit.
- `GameUiModel` owns navigation/inspection state and projects existing read-only session views. Its actions call existing domain operations at the host's settled timestamp. Navigation, refreshes, collapse, details and orientation changes do not select a character, reset state, award rewards or settle time. Only Make Active changes the active character.
- UI status is Working for a live assignment, Needs Attention for zero HP or a finished single production job, otherwise Idle. Noncombat work remains subject to the existing domain HP policy, not a new UI restriction. No automatic recovery is invented.
- Occupied activities require an explicit modal naming the displaced character. A stale confirmation cannot silently displace a different occupant. Same-activity gathering remains idempotent; running production/combat starts are rejected by the existing domain. Cancellation refunds, two-handed offhand returns and equipment identity all use existing atomic operations.
- Numeric gear comparisons subtract the replaced item and any offhand displaced by a two-handed weapon. Only changed damage/defense/attack interval values appear, green for improvements/red for reductions. Style changes are neutral text because a style's value depends on the target. No nonexistent enchant/offline bonuses are displayed. Compatible bank stock never includes another character's equipped units.
- Safe-area anchors and CanvasScaler reflow respond to resolution/orientation without touching session state. Portrait uses a 480×854 reference; landscape switches to height-based scaling so fixed navigation retains room. Scroll content is clipped, with bottom navigation outside the scroll view; detail modals have their own bounded scroll area and Close control. Portrait is the design benchmark; device accessibility and actual landscape behavior still require local checks.
- Search/filtering are UI state only and do not modify inventory. There is no promise of persistence for organization or any other state. UI rebuilding preserves the current scroll position when state changes; explicitly navigating to another screen starts at its top. No per-screen saved scroll history is promised.
- Success feedback appears briefly; errors remain readable. Scene animation is an observational projection: work/attack phase uses real session progress, HP/XP are actual values, defeat messages use real kill-counter deltas, and resource/production feedback uses real completed-cycle deltas. Reopening a screen captures a new baseline and never replays an invented reward. Idle breathing is cosmetic. No animation adds a combat/respawn delay.

## Art boundary

`PixelArtwork` contains a small original 48×64 palette-drawn adventurer, Chicken/Goblin/Mage prototypes and item/tool symbols, generated into point-filtered sprites. Charcoal/slate surfaces, restrained gold, normal readable text and large controls form the temporary dark-fantasy skin. These are original prototype graphics, not final character/equipment/bestiary art or imported licensed work. Overview shows a compact side-view work/combat slice; Equipment shows a static character. Production uses a generic work pose, not a complete crafting animation library.

`IdleboundMobileUI` exposes optional adventurer/enemy Sprite overrides; `ActivityStage` separates cosmetic pose/projection from simulation and can accept replacement sprites. Equipment appearance layers, individual attack animation clips, environment art and polished hit effects remain future visual work. Point filtering is applied to generated textures; no project import settings are changed. No character movement controls or fake special-attack button exists.

## Exact Windows verification

1. Pull the reviewed commit. Open the repository in Unity Hub with 6000.3.26f1. Wait for import/compilation and inspect Console. No package or editor upgrade is required. Open `Assets/Scenes/IdleboundPlay.unity`, enter Play mode, and confirm one `GameSessionHost`, one player Canvas, and one EventSystem in the hierarchy. Confirm no IMGUI debug panel appears.
2. Use Game views 720×1280 and 1080×2400, plus a short portrait screen; inspect safe-area support using a notched-device simulator if already available. Bottom Roster/Bank/Shop/Collection Log must stay reachable. Scroll every page and modal; confirm no horizontal clipping and that controls can be used with mouse and touch. Rotate/resize during gathering and verify work keeps advancing. No new simulator package is required.
3. Roster must show Character 1 active, real HP 50, actual idle/working statuses and three development characters. Tap Inspect Character 2: active designation remains Character 1. Tap Make Active: Character 2 becomes active immediately; prior assignments and equipment remain. Return to Character 1 and make active for the following loop.
4. Overview: all nine skills appear together in a 3×3 grid, initially Lv 1 / XP 0 with the existing thresholds. Collapse/expand the scene; neither action resets time or selection. Tap Mining: Activities opens, with no large animation panel. Open the first Bronze mining card; read its Lv 1 / 5s / 56 XP / +1 Bronze details. Higher tiers remain visible and locked.
5. Start Bronze mining. Wait at least 10 productive seconds; expect at least 2 Bronze (more if left longer), 56 XP per completed cycle, and a progress bar that loops rather than paying early. Switch main tabs, inspect another character, open/close details and return: mining must continue. The activity's bank quantity must refresh while the stack grows.
6. In Character 1 Activities choose Smithing from the compact skill picker. Expand Bronze dagger. Confirm 2 Bronze inputs and live shared-bank stock, 15s duration, 250 XP and the earlier starter-balance assumption label. Start one job: ingredients deduct once. Before completion, Stop/cancel returns 2 Bronze once and grants no dagger/Smithing XP; a second Stop must not refund again. Start again and wait 15s: one dagger arrives, 250 eligible Smithing XP, status Needs Attention/Completed. No automatic queue or repeated output follows.
7. Equipment: there must be no large activity animation above the ten slots. Tap Mainhand, then the available dagger. Read its requirements and development stats; only changed numerical comparisons appear. Equip: bank dagger decreases by 1, mainhand becomes occupied, profile shows Melee / damage 5 / 2s. Unequip returns the same item instance. Another character must not obtain an already-equipped unit from Bank. Test missing/locked equipment errors without stock loss. For rapid two-handed/other-slot tests, use the separate debug scene's kit; player UI does not grant free items.
8. Return to Activities, choose Melee and expand Chicken. Confirm enemy/loot/XP-per-damage data is labelled development-only. Start combat; observe real player/enemy HP, attack timing, skill XP and defeats. Dagger hits deal 5, 5, 2 actual damage across the first 6 combat seconds for 12 Melee XP, one guaranteed feather and optional seeded egg. View Overview to watch the simple side-view pose/defeat/XP feedback. Navigation must not stop combat or award additional loot. Stop explicitly to end it. Inspect Bank: feather quantity must be real.
9. Style change loop: gather Common Wood, produce a shortbow or wand using the unchanged recipes, equip it, then choose the matching Ranged/Magic activity filter and monster. Incompatible style cards remain visible but locked with a matching-mainhand explanation; UI cannot grant a style without equipment. Check the existing triangle and reduced Magic XP against Stage 8B04Verification.md. Death must show HP 0/Needs Attention, retain gear/XP and reject combat restart. There is no player healing button or invented recovery timer.
10. Occupancy: assign Character 1 to Bronze mining, then inspect Character 2 and try that exact activity. The card names Character 1 and opens Swap Assignment confirmation. Cancel leaves both unchanged. Confirm displaces Character 1 and starts Character 2's fresh progress. Inspection and swapping must not make Character 2 active automatically. Background Character 2 gets resources but 0 baseline XP; making them active affects future eligible XP only.
11. Bank: search case-insensitively for a real item name and ID; change All/Raw/Gear/Made/Loot filters. Quantities must remain unchanged by organization. Tap an item to see its actual name, stock and relevant equipment actions. Choose the explicit equipment target before equipping from Bank. No activity animation, gold, sale or fictitious stack should appear. Let a stack grow while its detail panel is open and confirm the quantity updates.
12. Shop: Buy and Sell tabs work, both explicitly unavailable; no transactions or stock. Collection Log: ten approved boss identity groups open into item-shaped silhouettes marked Not tracked/Unavailable. No earned items, fake boss kill count or unique effects are granted. These previews do not add bosses to combat activities.
13. While gathering, move focus away or pause the application, then return. Work/resources settle once under the existing offline cap; even the selected character gets baseline 0 offline XP. A welcome-back report is intentionally absent pending reliable attribution. Repeated focus/pause callbacks must not duplicate rewards. Exiting Play mode still loses all state by design.
14. Open `Assets/Scenes/IdleboundFoundation.unity` separately. Confirm old IMGUI tools still work: fixture/reset, pause, 1s / 5s/manual-offline steps, gear kit and explicit HP restoration. Pause the clock before manual steps. The harness has no Update/OnApplicationFocus/OnApplicationPause settlement methods; its host owns those. Existing SampleScene and IdleboundTest remain unchanged.

## Test Runner

**Window → General → Test Runner → EditMode → Run All**: expect 359 authored cases, including the unchanged 330 baseline and 29 new `GameUiTests`. Record actual discovery/pass/fail/skip counts and save results. New coverage includes projections, all nine skills, inspection/Make Active, requirements/occupants/stale swap, stock/refunds, equipment conservation/comparisons, search/filtering, combat HP/XP/loot/death, navigation, refresh idempotence, one clock owner, absolute-time partitioning, grouped offline presence and paused development stepping.

**PlayMode → Run All** in the Test Runner's isolated test scene: 3 new `MobileNavigationTests` initialize the Canvas/safe area, check host uniqueness and ScrollRects, invoke real bottom navigation and character Make Active/tab buttons, and assert no large ActivityStage survives on Activities/Equipment. These test Unity initialization and button callbacks, not a full touch/visual/performance certification. Windows manual verification remains essential. Do not run these tests inside an already-running player/debug scene with an existing host.

## Checks actually executed in the cloud

- Existing `python Scripts/model_progression.py`: passed.
- All C# files parsed with the tree-sitter C# grammar: passed (syntax only; not type checking).
- Static preservation, scene/script GUID references, metadata uniqueness, package/assembly JSON and existing-package references: passed.
- New source test counts, one settlement-owner call path and presentation mutation boundaries: checked.
- Collection preview identities compared against the preserved master source table: checked; no new gameplay data.
- Independent absolute-clock arithmetic/partition/offline-boundary model: passed.
- `git diff --check`: passed.

No Unity/C# compilation or Unity test/runtime pass is claimed. An optional Python syntax-parser tool was installed under `/tmp` only; no project dependency, package, toolchain pin or environment configuration was changed.

## Changed files

34 intended files, including Unity metadata:

```text
Assets/Foundation/Core/GameUiModel.cs
Assets/Foundation/Core/GameUiModel.cs.meta
Assets/Foundation/Core/SessionRuntime.cs
Assets/Foundation/Core/SessionRuntime.cs.meta
Assets/Foundation/Development/ActivityStage.cs
Assets/Foundation/Development/ActivityStage.cs.meta
Assets/Foundation/Development/BossCollectionShell.cs
Assets/Foundation/Development/BossCollectionShell.cs.meta
Assets/Foundation/Development/FoundationDevelopmentUI.cs
Assets/Foundation/Development/GameSessionHost.cs
Assets/Foundation/Development/GameSessionHost.cs.meta
Assets/Foundation/Development/Idlebound.Foundation.Development.asmdef
Assets/Foundation/Development/IdleboundMobileUI.cs
Assets/Foundation/Development/IdleboundMobileUI.cs.meta
Assets/Foundation/Development/MobileUiKit.cs
Assets/Foundation/Development/MobileUiKit.cs.meta
Assets/Foundation/Development/PixelArtwork.cs
Assets/Foundation/Development/PixelArtwork.cs.meta
Assets/Foundation/Development/Resources/Idlebound/CollectionPreview.txt
Assets/Foundation/Development/Resources/Idlebound/CollectionPreview.txt.meta
Assets/Foundation/Tests/Editor/GameUiTests.cs
Assets/Foundation/Tests/Editor/GameUiTests.cs.meta
Assets/Foundation/Tests/PlayMode.meta
Assets/Foundation/Tests/PlayMode/Idlebound.Mobile.PlayMode.Tests.asmdef
Assets/Foundation/Tests/PlayMode/Idlebound.Mobile.PlayMode.Tests.asmdef.meta
Assets/Foundation/Tests/PlayMode/MobileNavigationTests.cs
Assets/Foundation/Tests/PlayMode/MobileNavigationTests.cs.meta
Assets/Scenes/IdleboundPlay.unity
Assets/Scenes/IdleboundPlay.unity.meta
Docs/Architecture.md
Docs/DevelopmentProgress.md
Docs/Stage8B05Verification.md
Docs/UIUX.md
README.md
```
