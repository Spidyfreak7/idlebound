# Architecture

## Approved foundation

Idlebound targets Android and iOS with a mobile-first 2D Unity client. The editor is pinned to Unity 6.3 LTS `6000.3.26f1`. C# is the gameplay implementation language. Rendering uses the Universal Render Pipeline with its 2D renderer. Git tracks source assets, their metadata, package definitions, and project settings.

Supabase is the planned backend. Its integration is future work: there is no backend client, schema, authentication flow, or persistence implementation in this template. Concrete backend responsibilities and interfaces require approved requirements before implementation. Privileged backend credentials must not be committed or embedded in a distributed client.

## Current repository

The repository contains the Universal 2D foundation: SampleScene, rendering settings, input actions, a Lit 2D scene template, package definitions, and Unity project configuration. The Stage 8A presentation harness and Stage 8B-01 core foundation are present. `Assets/Foundation/Core` separates state and definitions from the development UI and Editor tests. Stage 8B-02 adds an engine-independent gathering catalog/session, shared resource inventory, validated XP loader, and selected/background XP policy. Stage 8B-03B now supplies the approved gathering schedule and complete reconstructed common XP curve; sparse milestone fixtures remain available only as labelled compatibility data. Stage 8B-03 adds a validated production catalog and single-job operations within the same settlement owner and shared bank. Stage 8B-03B supplies eight live starter recipes marked as new balance assumptions, separately from old opt-in development samples. Live cancellation refunds reserved inputs atomically; full-curve XP saturates at10M. The common curve and production/gathering session remain engine-independent. Stage 8B-04 extends the same session with character-specific equipment loadouts, shared-bank instance identity, and deterministic event-based combat. Separate development-only CSV catalogs supply gear, monsters, loot and combat tuning; no final combat balance or backend integration is claimed. See `Stage8B04Verification.md` for transaction, timing, replay and identity constraints. Documentation is stored outside Assets in `Docs`; see `Stage8B01Verification.md` for prototype constraints and pending design specifications.

## Future data-driven direction

Future gameplay should follow the approved data-driven direction, with game definitions and tuning data kept distinct from the C# behavior that consumes them. Specific data formats, asset types, schemas, validation, and ownership boundaries remain to be approved. This document does not prescribe additional packages or a folder hierarchy for unimplemented systems.

`IdleboundMasterDesign.md` reconciles approved resource timing/yields, skill names, XP milestones, and selected/background rules from the supplied handoffs. The later 8B-03B request supersedes missing curve/gathering data restrictions and authorizes starter balancing assumptions. Further content recipes/XP, full offline replay, synchronization and client/server authority remain unresolved. `Stage8B02Verification.md` distinguishes the current deterministic in-memory policies from production balancing and persistence.

## Stage 8B-05 presentation boundary

`GameSessionHost` is the sole Unity session/clock owner. It loads the unchanged catalogs into one `GatheringSession` and registers one engine-independent `SessionRuntime`. The runtime rejects duplicate owners, quantizes absolute realtime into ticks, groups focus/pause absence into a single offline interval and retains the domain's existing eligibility/cap rules. `GameUiModel` projects read-only session state and routes validated commands; navigation/inspection do not settle time or change active selection.

`IdleboundMobileUI`, `MobileUiKit` and `ActivityStage` build the player uGUI experience in `Assets/Scenes/IdleboundPlay.unity`. Existing uGUI/Input System packages are referenced explicitly by the presentation assembly; no new packages are added. `FoundationDevelopmentUI` now reads the same host and delegates explicit debug clock/reset commands, preserving its original scene as a fallback. There is no second gameplay session or settlement loop in either UI. Scene changes still start fresh in-memory state; this is not save persistence.

Original palette sprites and cosmetic activity projection are replaceable independently of the domain. `CollectionPreview.txt` supplies only approved boss/item identities for unavailable feature shells, not implemented monsters, ownership or effects. Shop transactions, durable offline attribution/welcome-back reporting, custom bank organization and full art remain deferred. See `UIUX.md` and `Stage8B05Verification.md`.
