# Stage 8B-05 UI/UX contract

The explicit Stage 8B-05 request supplies the current interaction authority; the unavailable original Phase 5 PDF is not claimed reviewed. These later revisions supersede earlier sample layouts without altering gameplay balance.

- Bottom navigation: **Roster / Bank / Shop / Collection Log**. Shop is not Town; there is no explorable map.
- Character navigation: **Overview / Equipment / Activities**. Inspection is independent of active selection. Make Active is immediate and preserves work and gear.
- Stage 8B-05C supersedes the original Overview-only scene: a pinned shared activity header spans all character tabs, expanded by default on Overview, Activities and Equipment (Stage 8B-05D). All nine skills appear in one 3×3 grid and route to their matching Activities filter.
- Activities is a full-height scrolling card view below a compact status/skill picker. Locked cards remain visible. Occupied activities use named-occupant displacement confirmation; no silent swap.
- Equipment keeps the static ten-slot loadout/browser, beneath the shared header which now defaults expanded. Each tab remembers its chevron preference. Stat comparisons omit unchanged values. Current enchant/offline effects are absent rather than fabricated.
- Bank has an item grid, inspection, live quantities, name/ID search, type filters and explicit equipment target. No activity animation. Organization is unsaved UI state; custom tabs/reordering are not delivered.
- Shop Buy/Sell and boss-specific Collection Log are marked unavailable shells. Collection preview names come from the master design; silhouettes do not imply locked-but-earnable implementation or tracked ownership.
- Current art is a replaceable original pixel-art prototype. It is not final item appearance, illustrated art, a full character library or completed combat animation. Readability and real actions take priority.
- No screen owns simulation time. Only `GameSessionHost` drives its registered `SessionRuntime`; navigation and visuals read/project the shared session. Cosmetics never generate rewards or insert extra simulated time.
- Offline return report is deferred until real per-character reward attribution exists. No fictitious summary is shown.

See `Stage8B05Verification.md` for precise scope, clock/timing policies, known limitations and Windows verification. Device layout, input, performance and accessibility approval remain pending actual Unity testing.

## Stage 8B-05B playtest revisions

The later explicit approval replaces the crowded Equipment cards with compact square icon slots around a reserved, larger static character. Slot names, equipped status, requirements and comparisons belong in details rather than repeated grid captions. All ten slots and existing atomic actions remain. Bank tiles now show actual catalog names and separate live quantities in a two-column grid; categories remain filters. No items are renamed.

Development combat XP is now 3/3/1.5 per actual damage for Melee/Ranged/Magic, explicitly a **provisional playtest baseline**. The common curve and all other gameplay mechanics are unchanged. See `Stage8B05BVerification.md` for current XP examples, the complete file manifest and exact Unity checks; earlier stage XP examples are historical.

## Stage 8B-05C header revisions

A pinned shared header outside the scroll area shows the inspected character, active/background role, live assignment/progress/HP and recent observed counters/XP. Overview starts expanded; Activities/Equipment start compact, with independent remembered per-tab choices. A small secondary chevron replaces all full-width scene-toggle buttons. Start and equipment actions remain separate. Bank has no activity header, preserving its clean inventory layout. No header interaction executes gameplay, advances time or changes active selection. See `Stage8B05CVerification.md` for defaults, feedback limitations and Windows checks.

Character→Bank navigation preserves the originating character/tab/skill/header and best-effort list scroll. Bank exposes Back to Character, and bottom Roster returns to that same context. Dashboard→Bank returns to the dashboard instead. Bank equip-target selection is independent of the return origin; intentional character inspection replaces stale context. Inventory remains shared and navigation never changes active selection or gameplay time. See the additional navigation section in `Stage8B05CVerification.md`.

## Stage 8B-05D layout approval

All character tabs now start with the expanded live header; this supersedes the earlier compact defaults above. Character pages hide the entire dashboard title strip with no reserved space, and permanent prototype warnings stay in documentation. The global Stop/Cancel is replaced by a secondary action within the expanded assigned activity/job card. Equipment uses a 320-unit-high, at-most-304-unit-wide cluster with a small aspect-preserving character preview and ten separate 56–68-unit targets. Gameplay, Bank navigation and session ownership remain unchanged. See `Stage8B05DVerification.md` for both requested portrait checks and limitations.
