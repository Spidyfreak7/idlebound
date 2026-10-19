# Development progress

Statuses describe verified work only. Stage 8B-01 through 8B-03 scopes and verification steps are recorded in their verification documents. Later stage scopes remain pending.

| Stage | Initial status | Evidence / next step |
| --- | --- | --- |
| 8A | In progress | 8A-04 repository/static verification and foundation documentation completed; local Unity compilation, scene, and mobile validation pending. Other 8A work is not assessed here. |
| 8B | In progress | 8B-01: user-reported seven manual checks and 14 EditMode cases passed on Windows. 8B-02: user reports all 49 EditMode cases passed and gathering works in Play mode. 8B-03: production implemented with isolated development samples; 53 new cases authored, Windows validation pending. |
| 8C | Not started | Await approved scope. |
| 8D | Not started | Await approved scope. |
| 8E | Not started | Await approved scope. |
| 8F | Not started | Await approved scope. |
| 8G | Not started | Await approved scope. |
| 8H | Not started | Await approved scope. |

## Stage 8A-04 evidence

- Baseline: `1163b9b` on `main`, initial Unity project commit.
- Unity version: `6000.3.26f1`; URP package `17.3.0` with a configured 2D renderer and Lit 2D scene template.
- `Assets`, `Packages`, and `ProjectSettings` are tracked. All existing asset files/directories have tracked `.meta` files with unique GUIDs.
- Package manifest and lockfile parse as JSON. Generated directories are ignored; representative source/configuration/metadata paths are retained.
- SampleScene is enabled in build settings. No project-authored C# scripts or tests exist.
- Unity Editor is unavailable in the cloud environment. Compilation, package resolution/import, Play mode, Test Runner execution, and Android/iOS builds remain unverified.
- No gameplay, dependency, editor-version, or project-setting changes were made.

## Next verification

Open the project locally using the README instructions, inspect the Console, run SampleScene, and report results. Inspect mobile build support before scheduling platform builds. Do not mark later stages complete without approved scope and supporting evidence.

## Stage 8B-02

See `IdleboundMasterDesign.md` for the four-source reconciliation and preserved numerical tables, and `Stage8B02Verification.md` for exact manual checks and pending specifications. Production XP/action and most level thresholds remain unavailable. An opt-in development fixture is clearly separated from production data. The user subsequently verified gathering in Play mode and all 49 EditMode cases passed on Windows; the cloud did not execute those checks. No packages, networking, recipes, combat, or monetization were added in 8B-02.

## Stage 8B-03

See `Stage8B03Verification.md` for implementation, test steps and policy decisions. Added validated recipes, atomic ingredient consumption, one-job timed production, finished products in the shared bank and production UI. Approved recipe data remains empty because full ingredient quantities/requirements/XP are missing. Five explicitly opted-in development recipes support the full loop. Cancellation/no-refund, completion-time XP and single-job/queue behavior are disclosed engineering policies, not new design approvals. Existing 49 tests remain unchanged; 53 new cases are authored but not cloud-executed. No equipping, equipment stats, package, scene or project-setting changes.
