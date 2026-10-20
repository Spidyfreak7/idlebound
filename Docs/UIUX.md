# Stage 8B-05 UI/UX contract

The explicit Stage 8B-05 request supplies the current interaction authority; the unavailable original Phase 5 PDF is not claimed reviewed. These later revisions supersede earlier sample layouts without altering gameplay balance.

- Bottom navigation: **Roster / Bank / Shop / Collection Log**. Shop is not Town; there is no explorable map.
- Character navigation: **Overview / Equipment / Activities**. Inspection is independent of active selection. Make Active is immediate and preserves work and gear.
- Overview alone has the collapsible large activity slice. All nine skills appear in one 3×3 grid and route to their matching Activities filter.
- Activities is a full-height scrolling card view below a compact status/skill picker. Locked cards remain visible. Occupied activities use named-occupant displacement confirmation; no silent swap.
- Equipment is a full-height ten-slot loadout/browser around a static central character, without the animated work scene. Stat comparisons omit unchanged values. Current enchant/offline effects are absent rather than fabricated.
- Bank has an item grid, inspection, live quantities, name/ID search, type filters and explicit equipment target. No activity animation. Organization is unsaved UI state; custom tabs/reordering are not delivered.
- Shop Buy/Sell and boss-specific Collection Log are marked unavailable shells. Collection preview names come from the master design; silhouettes do not imply locked-but-earnable implementation or tracked ownership.
- Current art is a replaceable original pixel-art prototype. It is not final item appearance, illustrated art, a full character library or completed combat animation. Readability and real actions take priority.
- No screen owns simulation time. Only `GameSessionHost` drives its registered `SessionRuntime`; navigation and visuals read/project the shared session. Cosmetics never generate rewards or insert extra simulated time.
- Offline return report is deferred until real per-character reward attribution exists. No fictitious summary is shown.

See `Stage 8B05Verification.md` for precise scope, clock/timing policies, known limitations and Windows verification. Device layout, input, performance and accessibility approval remain pending actual Unity testing.
