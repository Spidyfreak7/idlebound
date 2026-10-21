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
- Stage 8B-06 supplies a single durable, account-wide welcome-back summary after at least five minutes away, with real per-character receipts and Continue acknowledgment.

See `Stage8B05Verification.md` for precise scope, clock/timing policies, known limitations and Windows verification. Device layout, input, performance and accessibility approval remain pending actual Unity testing.

## Stage 8B-05B playtest revisions

The later explicit approval replaces the crowded Equipment cards with compact square icon slots around a reserved, larger static character. Slot names, equipped status, requirements and comparisons belong in details rather than repeated grid captions. All ten slots and existing atomic actions remain. Bank tiles now show actual catalog names and separate live quantities in a two-column grid; categories remain filters. No items are renamed.

Development combat XP is now 3/3/1.5 per actual damage for Melee/Ranged/Magic, explicitly a **provisional playtest baseline**. The common curve and all other gameplay mechanics are unchanged. See `Stage8B05BVerification.md` for current XP examples, the complete file manifest and exact Unity checks; earlier stage XP examples are historical.

## Stage 8B-05C header revisions

A pinned shared header outside the scroll area shows the inspected character, active/background role, live assignment/progress/HP and recent observed counters/XP. Overview starts expanded; Activities/Equipment start compact, with independent remembered per-tab choices. A small secondary chevron replaces all full-width scene-toggle buttons. Start and equipment actions remain separate. Bank has no activity header, preserving its clean inventory layout. No header interaction executes gameplay, advances time or changes active selection. See `Stage8B05CVerification.md` for defaults, feedback limitations and Windows checks.

Character→Bank navigation preserves the originating character/tab/skill/header and best-effort list scroll. Bank exposes Back to Character, and bottom Roster returns to that same context. Dashboard→Bank returns to the dashboard instead. Bank equip-target selection is independent of the return origin; intentional character inspection replaces stale context. Inventory remains shared and navigation never changes active selection or gameplay time. See the additional navigation section in `Stage8B05CVerification.md`.

## Stage 8B-05D layout approval

All character tabs now start with the expanded live header; this supersedes the earlier compact defaults above. Character pages hide the entire dashboard title strip with no reserved space, and permanent prototype warnings stay in documentation. The global Stop/Cancel is replaced by a secondary action within the expanded assigned activity/job card. Equipment uses a 320-unit-high, at-most-304-unit-wide cluster with a small aspect-preserving character preview and ten separate 56–68-unit targets. Gameplay, Bank navigation and session ownership remain unchanged. See `Stage8B05DVerification.md` for both requested portrait checks and limitations.

## Stage 8B-05E — Direct activity cards

Individual activity/recipe expansion is removed. Every card immediately exposes an icon, actual name, Start (or disabled Locked) control, level gate, XP and duration. Recipes show required material amounts with live bank stock. Assigned cards show real progress and contextual cancellation; swaps still require explicit named-character confirmation. Long recipe explanations and development disclaimers are omitted from the cards. Development/balance status remains in the source catalogs and documentation; this presentation does not approve or change those values.

`PlayerNames.Display` removes only leading `[DEV]` markers in player-facing name projections and raw equipment-detail labels. IDs, catalog names and validation stay intact. Bank search/sort uses displayed names plus unchanged IDs. The existing character/Bank return context, expanded header defaults, compact equipment cluster and shared clock remain unchanged.

## Visual specification — approved direction, Stage 8B-05E

Target **detailed modern fantasy pixel art**, with recognizably human proportions, readable face/hair, defined equipment silhouettes, richer material shading and a classic fantasy atmosphere. Avoid oversized cube-like heads/limbs, Minecraft-like proportions, extremely coarse pixels and flat rectangular anatomy. Illustrated 2D remains a potential backup direction, not a simultaneous art replacement.

For a future replaceable character prototype, use a transparent full-body sprite with room for helmet, cloak, weapons and boots, a restrained fantasy palette, distinct light/midtone/shadow groups and small face highlights. A suggested art trial is a 128×192 or 192×256 source canvas; these are proposed authoring sizes, not an approved production atlas/pipeline requirement. Judge the result at the actual 105×140 live-scene and at-most-112×130 equipment preview bounds, not just enlarged source pixels. Faces, hand silhouettes and materials should remain legible after scaling; do not enlarge the UI character rectangle to compensate for unclear art. Preserve sprite aspect ratio and transparent padding that does not shrink the subject excessively. Review point/bilinear filtering at actual phone sizes before approving import policy.

Use only original commissioned/authored/generated artwork with documented provenance or assets with verified compatible licensing. No third-party art is introduced here. The existing serialized hero sprite override remains the replacement seam; animation, equipment geometry and gameplay must not depend on a particular source pixel grid. This stage establishes the visual specification only: the existing original 48×64 prototype is still present and does **not** meet the final visual target. A higher-detail sprite trial, its import policy and animation polish remain pending review; no complete art replacement is claimed.


## Stage 8B-06 persistence surfaces

Existing layout/navigation/art are preserved. Startup/resume uses a blocking, responsive preparation overlay while the candidate session is restored and settled off-thread. Save/load failures show a concise protective Retry surface; diagnostic details stay in logs. The welcome-back modal shows actual away/eligible durations, cap indication, character/assignment, gross earned items, actual skill XP (including an honest 0 XP), and relevant stop reasons. Continue durably acknowledges one report; it is not a reward-claim/grant operation. Multiple unacknowledged absences merge into one report labeled across visits. The underlying live session continues normally after the offline transaction commits. No title/prototype banner or debug clock control is added to character pages.

The player now uses durable local state across Play sessions and app restarts; earlier in-memory-only warnings in historic verification documents no longer describe the normal player path. Explicit development resets remain unsaved sandboxes. Editor-only offline probe menus clone a valid player save into an isolated temporary directory with a controlled UTC offset; they never alter the actual save or device clock. See `Stage8B06Verification.md` before testing or recovering files.


Stage 8B-06 required production addition: Start now runs Smithing, Crafting and Cooking continuously until inputs are unavailable. Cards keep real current-cycle progress and contextual Cancel; exhausted cards/header show Out of materials. The next cycle reserves ingredients immediately, so bank quantities exclude those inputs. Later stock enables Start but never restarts the assignment automatically. Cancel returns only the current unfinished reservation. No quantity selector, extra confirmation or layout redesign is added; swap confirmation remains explicit.
