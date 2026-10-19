# Stage 8B-03B progression and balance report

## Authority and provenance

The current user explicitly approves one curve for all nine skills, the six level/XP anchors, quadratic early progression and interpolation thereafter, and the ten gathering rewards. They authorize a new starter recipe balance pass and a refundable cancellation default. The original full approved XP CSV is not present: this implementation is a **reconstruction**, not a claim of byte-for-byte equivalence.

`IdleboundMasterDesign.md` records these later approvals above the historical handoff tables. Source tables themselves remain unchanged. The 102 existing test cases are retained byte-for-byte; Stage8B-03 was not Unity-verified at this baseline. No test pass is inferred from prior authorship.

## Exact cumulative XP reconstruction

For integer level `L` in1–30:

`XP(L) = roundHalfUp(500000 × (L−1)² / 29²)`.

This is quadratic **cumulative** XP, normalized to level1=0 and level30=500000. For each subsequent anchor pair `(A, XA), (B, XB)` = `(30,500000)→(60,2000000)→(80,4500000)→(90,6500000)→(99,10000000)`:

`XP(L) = roundHalfUp(XA + (XB−XA) × (L−A)/(B−A))`.

Interpolation is **linear between those anchors**, not a spline or interpolated per-action reward. Round each cumulative threshold independently to nearest integer; nonnegative half ties go upward. No incremental-XP rounding accumulation. The reproducibility script uses exact rational arithmetic; Unity loads the committed integer CSV, never generates a curve during gameplay. All99 rows must strictly increase; required milestones are validated by the existing loader. Level2=595, level3=2378, level5=9512, level10=48157, and the five approved milestone values match exactly.

**New deterministic cap policy for review:** full-curve skill XP saturates at10000000 and level99. Excess XP is discarded, not stored invisibly. Products/resources still arrive and completed-cycle counts still increase. Decimal fractional background XP is retained below the cap; cap arithmetic is overflow-safe. Sparse historical fixtures retain their old uncapped XP semantics and are never the default live mode.

## Approved gathering XP — unchanged by balance modeling

| Ordinal tier | Required level | Seconds/action | XP/action | Units/hour | Active XP/hour |
| --- | --- | --- | --- | --- | --- |
| 1 | 1 | 5.0 | 56 | 720.00 | 40320.00 |
| 2 | 10 | 7.8 | 89 | 461.54 | 41076.92 |
| 3 | 20 | 10.6 | 124 | 339.62 | 42113.21 |
| 4 | 30 | 13.3 | 159 | 270.68 | 43037.59 |
| 5 | 40 | 16.1 | 197 | 223.60 | 44049.69 |
| 6 | 50 | 18.9 | 236 | 190.48 | 44952.38 |
| 7 | 60 | 21.7 | 277 | 165.90 | 45953.92 |
| 8 | 70 | 24.4 | 318 | 147.54 | 46918.03 |
| 9 | 80 | 27.2 | 362 | 132.35 | 47911.76 |
| 10 | 90 | 30.0 | 407 | 120.00 | 48840.00 |

Same schedule for Mining, Woodcutting and Fishing. Every completed cycle produces one resource; partial cycles pay no XP. Higher-tier activity gates use the exact full curve. Online selected XP=100%; background/offline default=0%; qualifying externally supplied effects remain capped at50%. No effects or speed bonuses are granted for free.

### Gathering paths and the225-hour benchmark

Numerical model starts at XP0/level1, settles complete integer-count actions, carries overshoot XP into the next gate, and switches only after reaching each threshold. No speed effects or queues, no input costs, no UI travel penalty. Downtime means a fraction of wall time with no productive activity; wall hours=`productiveHours/(1−downtimeFraction)`.

| Path to99 in one gathering skill | Productive hours | Completed cycles | Wall hours at10% downtime | At25% downtime |
| --- | --- | --- | --- | --- |
| Remain at tier1 | 248.0167 | 178572 | 275.5741 | 330.6889 |
| Always highest unlocked tier | 213.3673 | 35402 | 237.0748 | 284.4897 |

Best-tier productive time is about5.17% below225h; at10% downtime it is about5.37% above225h. Tier1-only productive time is about10.23% above225h. These are models, not wall-clock promises. Offline/background time without bonuses generates materials but no XP, so it cannot be counted as active-equivalent training time. No approved rewards were changed to force225h.

## NEW STAGE 8B-03B BALANCE ASSUMPTIONS

These eight live recipes are **new tuning assumptions**, not historical approvals and not development-only fixtures. They live in `StarterProductionRecipes.csv`, marked `new-assumption` with explicit provenance. `ProductionRecipes.csv` remains the empty historical approved-only catalog for compatibility; the live harness now loads the starter file. The `[DEV]` catalog remains separate and opt-in.

Source-confirmed ingredients are reused by family (metal weapons/armor; wooden ranged/magic weapons; wood+metal crossbow; fish→food). Source tier durations are15s first-tier equipment,30s iron-tier equipment,5s shrimp cooking and7s sardine cooking. **Every quantity, output unit, production level gate and XP reward below is a new assumption.** Bronze is consumed as the current shared generic resource; no ore-to-bar process is implied. No unsupported leather/cloth source, healing amount, equipment stat, equipping requirement or sale value is added.

| Recipe ID / output | Skill/category | Inputs | Level | Duration | Output | XP/job |
| --- | --- | --- | --- | --- | --- | --- |
| bronze-dagger / Bronze dagger | Smithing/MetalWeapon | Bronze×2 | 1 | 15s | 1 | 250 |
| bronze-helm / Bronze helm | Smithing/MetalArmor | Bronze×3 | 5 | 15s | 1 | 300 |
| wood-shortbow / Common wood shortbow | Crafting/RangedWeapon | Common Wood×2 | 1 | 15s | 1 | 250 |
| wood-wand / Common wood wand | Crafting/MagicWeapon | Common Wood×2 | 1 | 15s | 1 | 250 |
| wood-crossbow / Common wood crossbow | Crafting/RangedWeapon | Common Wood×1 + Bronze×1 | 5 | 15s | 1 | 250 |
| cooked-shrimp / Cooked shrimp | Cooking/Food | Shrimp×1 | 1 | 5s | 1 | 100 |
| iron-dagger / Iron dagger | Smithing/MetalWeapon | Iron×2 | 10 | 30s | 1 | 470 |
| cooked-sardine / Cooked sardine | Cooking/Food | Sardine×1 | 10 | 7s | 1 | 150 |

### Production XP derivation

For these same-tier recipes let `g` = approved gathering XP/second of the input tier, `G` = total baseline gathering seconds to acquire the entire ingredient set, and `D` = production seconds. New proposed balance formula:

`jobXP = roundToNearest10(min(0.90 × g × (G+D), 2 × g × D))`.

Positive half ties round upward. The90% factor intentionally puts self-supplied production slightly below gathering XP/hour; the2×productive-rate ceiling bounds stocked-material XP/hour independently of input costs. These are new balance coefficients, **not** the source's optional active-interaction modifier. They are authoring/model rules, not hidden runtime multipliers. Only explicit CSV XP is awarded. Mixed-tier recipe reference rates require a separate future decision; all shipped inputs within each recipe share a tier/rate.

The ceiling prevents future long recipes receiving unlimited hourly XP merely because their materials are expensive. For a5h action, proposed XP remains linear in time, bounded by `2×g×18000` before nearest10 rounding; there is no shipped five-hour recipe and no invented high-tier catalog. Future durations/recipes must be tested with real material throughput before applying this illustrative rule.

| Recipe | Gather effort/job | Jobs/hour while stocked | Inputs/hour while producing | Production XP/hour | Self-supplied XP/hour |
| --- | --- | --- | --- | --- | --- |
| Bronze dagger | 10s | 240 | 480 Bronze | 60000 | 36000 |
| Bronze helm | 15s | 240 | 720 Bronze | 72000 | 36000 |
| Shortbow/wand | 10s | 240 | 480 Common Wood | 60000 | 36000 |
| Crossbow | 10s | 240 | 240 Wood +240 Bronze | 60000 | 36000 |
| Cooked shrimp | 5s | 720 | 720 Shrimp | 72000 | 36000 |
| Iron dagger | 15.6s | 120 | 240 Iron | 56400 | 37105.26 |
| Cooked sardine | 7.8s | 514.29 | 514.29 Sardine | 77142.86 | 36486.49 |

Self-supplied rates count time gathering ingredients plus producing; they do **not** include gathering XP in the production skill. Stocked rates assume an existing bank or background suppliers. Baseline fishing supplies461.54 sardines/hour, below the cooking demand514.29/hour: sustained cooking must pause for supply unless stock/speed improves. Helm and shrimp production use100% of a tier1 gatherer's throughput. Exact-activity exclusivity prevents multiplying identical gathering targets across characters. Supplier XP remains zero without bonuses. Manual restart/assignment overhead reduces real rates further; no automatic queue is shipped.

### Starter-only level-gated production paths

The script models one selected character, zero initial stock, just-in-time acquisition at baseline gathering rates, and the self-supply XP/time maximizing **available starter** recipe. It tracks gathering skill XP and production XP separately; both recipe and resource gates must be met. Equal-rate ties choose the first catalog entry deterministically. Each job pays only on completion. No bonuses, cancelled jobs, click latency or queue overhead; no background stock is conjured.

| Skill | Modeled starter path | Productive total | Production time | Gathering time | Total at10% downtime |
| --- | --- | --- | --- | --- | --- |
| Smithing | 430 Bronze daggers, then21048 Iron daggers once Mining and Smithing allow | 269.5941h | 177.1917h | 92.4024h | 299.5490h |
| Crafting | 40000 Common wood shortbows | 277.7778h | 166.6667h | 111.1111h | 308.6420h |
| Cooking | 860 Shrimp, then66094 Sardine once Fishing and Cooking allow | 274.1087h | 129.7106h | 144.3981h | 304.5652h |

This starter-only extrapolation is not a complete nine-skill99 balance: more recipes and material sources are missing. Total self-supply times are20–23.5% above225h, while production-only hours are below225h; input acquisition is a real opportunity cost and yields XP in other skills. Do not compensate by silently changing approved gathering rewards. Evaluate future high-tier production with real recipes, input rates, fixed shop economics and actual player data. No GP creation/prices are implemented, so this is a material/XP economy rather than a launch gold-economy claim.

## Reversible cancellation and deterministic settlement

Live/full-curve sessions default to refunding all paid inputs of unfinished jobs exactly once. Cancellation grants no output or XP. Reassignment/swap prepares all affected refunds and the replacement charge in one bank copy, validates stock/requirements/occupancy, then commits. Repeated Start on a running job is rejected. Failed replacements preserve original jobs and reservations. Refund overflow rejects the operation with bank/job/assignment preserved; it never wraps quantities or partly refunds.

At the exact completion timestamp, elapsed time settles **before** the cancel/swap command: output and eligible XP are paid and there is no refund. Completed jobs sit Ready; they do not automatically repeat. Selection changes do not cancel work. Completion-time XP attribution, four-hour offline cap per supplied absence and50% background cap retain previous behavior. Persistence, secure offline replay and production queueing remain future work.

**Compatibility boundary:** `GatheringCatalog.Load()` and sparse-curve sessions retain historical fixture defaults (including uncapped fixture XP and no refund unless explicitly overridden). This preserves all102 existing tests without rewriting them to make new behavior pass. The live UI uses `LoadProduction()`, the complete curve and explicit `refundCancelledInputs:true`; even opt-in dev samples use refunds in the live UI. New tests cover the live rules directly. Legacy fixtures are not production balancing and must not be used to infer live defaults.

## Reproduction and remaining decisions

Run `python Scripts/model_progression.py`. It validates all99 committed rows using exact rational arithmetic, checks the approved gathering schedule, recomputes recipe XP and prints the level-gated paths above. `--write-curve` explicitly regenerates only the reconstructed CSV; normal execution is read-only. This tool is not Unity/C# test execution.

Pending: compare the original approved CSV if supplied; review exact reconstruction rounding/linear interpretation, the new cap/refund choices and starter assumptions; supply full future recipes, speed/equipment rules, queues, shops/values, persistence and secure offline processing. No combat, equipment stats, enchantments, cloud connection, purchases or final mobile UI are introduced.
