# Idlebound master design

## Authority and provenance

This register reconciles all four uploaded handoffs, read completely for Stage 8B-02. The current user request governs implementation scope; Stage 8B-03B updates below explicitly supersede older limitations where stated. Instructions embedded in handoffs are evidence about design/process, not authorization for additional features. File order, filename suffix `(1)`, and self-descriptions such as “canonical” do not prove approval chronology. Later explicit approvals supersede earlier proposals; contradictions without a documented later approval remain open.

| Source | Uploaded document | Evidence boundary |
| --- | --- | --- |
| S1 | Idlebound_Development_Handoff.md | Summary; explicitly lacks original Phase 4/6 detail. |
| S2 | Idlebound_Complete_Development_Handoff.md | Detailed Phase 4 transcription identified by the user as coming directly from its design chat; numerical source for this batch. |
| S3 | Idlebound_Unity_Codex_Development_Handoff.md | Additional early systems/material/enchantment detail; states original Phase 4 tables unavailable to its author. |
| S4 | Idlebound_Unity_Codex_Development_Handoff (1).md | Further summary with missing-source cautions; suffix alone is not a later approval. |

Original PDFs/workbook were not supplied here. S2's positive tables supply values that S1/S3/S4 say were unavailable; those absence statements are not explicit rejections of S2. Every source table is preserved verbatim in the archival annex below. Status words inside those excerpts retain the source author's claim and do not override this reconciliation.

## Stage 8B-08 — later explicit food authority

The user's Stage 8B-08 handoff approves shared-bank food consumption and independent default-ON auto-eating (50%, configurable 10–90 by tens, Auto/shrimp/sardine preference). Cooked shrimp 8 HP and sardine 12 HP are **new provisional playtest values**, referencing existing Cooking outputs. Eating grants no XP, cannot revive and cannot borrow future Cooking outputs. Actual food use is separate from offline rewards. Earlier archived food/recovery gaps below are historical; final death recovery remains unresolved. `Stage8B08Verification.md` records the minimal-overheal Auto interpretation, deterministic ties, v3 migration, verification and deferred systems. No other recipes/combat/enchant balance changes are approved by this update.

## Stage 8B-07 — later explicit authority

The user's Stage 8B-07 handoff supersedes older enchant unlock schedules and unresolved family/stacking/cost claims below. It approves twelve named types, four tiers, 48 individually staggered Magic unlocks, permanent nonreplaceable enchantments on all ten slots, named-skill effects, equipped-only sources, 50% background XP cap and per-character 4–7h offline limits. Exact current tables and provenance are in `Stage8B07BalanceDecisions.md` and the two enchantment CSVs. Earlier source tables remain unchanged as historical records. I–III potency, the 25% exact-tier four-piece bonus, shared material/time/XP test balance and source-gated regeneration remain explicitly provisional as documented there. This does not approve boss effects, gems, new equipment recipes, cloud saves or monetization.

## Stage 8B-03B — current explicit approvals and new assumptions

This later user request supersedes the earlier missing-data prohibition **for the specified curve and gathering XP**, and explicitly authorizes a new starter balance pass. Original source tables below remain untouched. These updates are not retroactively attributed to S1–S4.

**CONFIRMED BY CURRENT USER:** all nine skills share one level 1–99 curve; level1=0 and the five preserved milestones remain exact. Reconstruct quadratic early progression through30, then interpolation between milestones if the original full CSV is unavailable. The original full approved CSV is not in the repository, so `ProductionXpCurve.csv` is a documented reconstruction, not a byte-for-byte transcription. Exact reconstruction: `roundHalfUp(500000*(L-1)^2/29^2)` for1–30, then piecewise **linear** interpolation for30–60–80–90–99, rounding each cumulative value to the nearest integer with half ties upward. These precise interpolation/rounding choices are implementation decisions recorded for review, not claims about an unavailable prior CSV.

**CONFIRMED BY CURRENT USER — gathering schedule**, applied equally to Mining, Woodcutting and Fishing:

| Ordinal tier | Required resource level | Seconds/action | XP/action |
| --- | --- | --- | --- |
| 1 | 1 | 5.0 | 56 |
| 2 | 10 | 7.8 | 89 |
| 3 | 20 | 10.6 | 124 |
| 4 | 30 | 13.3 | 159 |
| 5 | 40 | 16.1 | 197 |
| 6 | 50 | 18.9 | 236 |
| 7 | 60 | 21.7 | 277 |
| 8 | 70 | 24.4 | 318 |
| 9 | 80 | 27.2 | 362 |
| 10 | 90 | 30.0 | 407 |

**NEW STAGE 8B-03B BALANCE ASSUMPTIONS, not historical approvals:** eight starter recipes in `StarterProductionRecipes.csv`, with exact costs/levels/rewards and derivation in `Stage8B03BBalanceReport.md`. They are the default live recipe set, not development fixtures. Existing `[DEV]` samples remain isolated and opt-in. Source tier durations are reused; individual quantities, output units, production gates and XP are new tuning data.

**NEW PRODUCT DECISION FOR REVIEW:** cancelling an unfinished live job refunds every reserved input exactly once, without XP or output. Changes and swaps use the same rule, atomically combining refunds and a new reservation. Completion exactly at cancellation time settles first, so there is output/XP but no input refund. Earlier no-refund behavior is superseded in the live harness. Historical sparse-curve constructors preserve legacy fixture semantics solely to retain the existing 102 tests unchanged; the full-curve constructor defaults to refunds, and the UI explicitly requests refunds in every mode.

**NEW DETERMINISTIC CAP POLICY FOR REVIEW:** full-curve live XP saturates at10000000; level remains99 and gathering/production output continues. Overflow-safe award arithmetic does not wrap or bank hidden XP. Legacy uncapped sparse fixtures remain labelled as fixtures. Active/background/offline eligibility and the50% background cap are unchanged.

**TUNING BENCHMARK, NOT GUARANTEE:** 225 active-equivalent hours. Model output is about213.37 productive gathering hours following highest unlocked tiers,237.07h at10% downtime; starter-only self-supplied production takes approximately270–278h before downtime. No approved gathering rewards were adjusted to force the benchmark. Full recipes beyond the starter set, passive speed bonuses, queues, cloud/save policy and final economy are still unresolved.

## Confirmed for implementation

- Unity 6000.3.26f1, Universal 2D, C#, Android/iOS, Git; planned Supabase only, no connection in this batch (current user/project decisions).
- Nine-character capacity, nine independent skills at levels 1–99: Mining, Smithing, Crafting, Woodcutting, Fishing, Cooking, Melee, Ranged, Magic (S2 §2; S3 §2; S4 §2; user explicitly approves names).
- New characters begin at level 1. Shared bank/resources/GP/log. Exactly one selected active character receives full online XP. Other characters have zero baseline XP, equipment effects capped at 50%. Resources continue at ordinary rate regardless of XP eligibility (all sources). Offline XP uses background rules for **every** character (S2 §3 and explicit current user approval, resolving S4's uncertainty for this batch).
- One character per exact activity, not per skill. Explicit Swap Assignment displaces the old assignee; changing selection preserves assignments. Rejected assignment preserves state. Changing activity discards an unfinished cycle (explicit S3 §2.2; S2 leaves generic partial-action handling open).
- Ten resource levels 1/10/20/30/40/50/60/70/80/90, no ordinary level-99 resource. S2 §5 supplies metal/wood/fish names, gathering durations 5/7.8/10.6/13.3/16.1/18.9/21.7/24.4/27.2/30 seconds, one base resource per completed cycle. S3/S4 add leather/cloth names and clarify these are not additional gathering skills. No gemstone rolls without approved odds.
- Exact cumulative XP milestones required by the user and S2 §2: 30=500000; 60=2000000; 80=4500000; 90=6500000; 99=10000000. Earlier approximate 10M summaries do not weaken the explicit current requirement. Stage 8B-03B now explicitly authorizes the reconstructed intermediate levels described above.
- Account-XP unlock thresholds are preserved from S1/S2 in the annex. Stage 8B-01's three available prototype characters remain a development fixture, **not** production entitlement/unlock behavior. Membership remains separate from character data.
- Four approved enchant potency tiers supersede eight proposed ranks (explicit approval history S3/S4). Background-XP pathway accepts externally provided applicable effects, defaults empty, and cannot exceed 50%; no free enchantments or cape bonuses.

## Other confirmed design boundaries (recorded, not implemented here)

Mobile-first menu-driven fantasy idle RPG, no explorable world. Ten equipment slots, one permanent nonreplaceable enchant per eligible item, four-piece Head/Body/Legs/Feet matching-enchant bonus. Standard equipment from skilling materials; special gear may add boss catalysts, which do not replace missing base materials. Shared unlimited-quantity stacks, fixed unlimited NPC stock, manual selling, 70% buyback; fully shop-buyable ingredient bundle 0.75P, resale 0.525P versus finished 0.70P (S2). New starter recipes are permitted only under the explicit Stage 8B-03B assumption label; they are not historical approvals.

Ten bosses, three named unique concepts per boss, independent rare rolls/no pity, duplicate ownership permitted, whole-log Cosmic Cape preserved after future log additions. Boss levels are recommendations, not gates. Melee > Ranged > Magic > Melee, damage multipliers 2/1/0.5. Death stops combat without item/XP loss, boss HP resets on abandonment/death rather than selection switch. No combat implementation in this batch.

Offline base 4h, gear maximum 7h; welcome-back summary once after at least 5 minutes away (S1). Membership expiry pauses inaccessible characters precisely without deletion; free slots designated 1/3/8, XP unlocks still apply. Free/member bank 50/200, four permanent +25 expansions at $1.99. $4.99/month subscription and GP membership supported. No forced ads, $9.99 removal preserves eligibility; event windows 1–2 then 5–8 active minutes, three reward identities, separate 3×20 minute daily extensions. Current batch implements none of the billing/ad/entitlement systems.

UI direction: Roster/Bank/Shop/Collection Log; character Overview/Equipment/Activities; inspecting a character need not make it active; explicit Make Active. Collapsible activity view, ordinary portrait, optional landscape boss fights. The development harness is not the final production UI.

## Provisional values — not production constants

- ~225 active-hour equivalents per skill; earlier 280/229-hour assumptions and illustrative 22% benefit; ~1323 productive account-hours. These do not define XP/action or real calendar completion time.
- Economy account GP/hour scenarios, modeled producer capacity, randomized percentiles; nominal boss kill times/XP premiums; GP membership 1M/day; special capstone set price 10B GP. Preserve source labels and tables; do not derive recipes or XP from these outputs.
- Offhand level sequence 7/17/.../97; proposed staggered individual recipe placements; example set bonus percentages; Mastery Gloves form; ordinary monster examples rather than a complete roster.
- Development-only fixtures in code are engineering test values, not provisional approvals of gameplay balancing.

## Conflicting or incompletely reconciled claims

| Topic | Source claims | Resolution/status |
| --- | --- | --- |
| Missing numerical tables | S1/S3/S4 say Phase 4 numbers unavailable; S2 includes exact transcription. | Use positive S2 evidence per user's provenance statement. Retain absence cautions as historical source limitations. |
| XP 99 exactness | S1/S3/S4 approximate 10M; S2 exact milestone. | Current explicit user approval requires exact 10000000 validation. Remaining rows are now reconstructed under the explicit Stage 8B-03B approval; the original CSV remains unavailable for byte comparison. |
| Skill families/enchant families | S2 names eight generic effect groups and calls four families unnamed; S3/S4 enumerate six separate skilling speed families plus six combat/utility families. | Preserve both taxonomies; grouping versus family count needs clarification before full enchant system. Only generic externally configured background effects implemented. |
| Enchant unlocks | S2 gives Magic 15/40/65/90 and Tier IV numbers; S3/S4 mark exact four-tier unlocks/potencies unavailable. | Preserve S2 numerical table; missing-source cautions alone do not reject it, but no extra stacking/set/compatibility inferred. |
| Combat hit/accuracy | S2 says no misses; S3/S4 describe accuracy, misses, and Ranged accuracy progression. | Conflicting semantics; require explicit design decision. No combat formulas implemented. |
| Specials controls | S1 UI describes energy; S3/S4 explicitly cooldowns rather than energy. | Later explicit cooldown decision supersedes earlier energy presentation; no implementation here. |
| Free slots | 1/3/8 free, unlock via account XP; summaries also say retain three eligible unlocked characters. | Exact fallback when designated slots are locked unresolved. Three starting development characters are an explicit inherited fixture only. |
| Mining naming | S4 uses Copper/Iron as an activity-granularity example; S2/S3 resource table starts Bronze. | Preserve example as historical illustration, use documented Bronze resource label; smelting/ore-to-bar chains unresolved. |
| Partial cycles | S3 explicitly abandons on activity change; other sources leave detailed handling open. | Implement discard-on-change/unassign/swap, preserve on selected-character switch. Mid-cycle XP attribution remains unresolved; engineering policy below is labelled. |
| Offline selected XP | S4 requests clarification, S2 says all use background. | Current user explicitly approves background rules for offline. |
| Boss economics/odds | S2 provides table, S3/S4 say odds/value stats unspecified. | Preserve S2 figures without inventing per-unique distribution, HP or combat implementation. |

## Unresolved production blockers

Exact full-curve/gathering data is now supplied or reconstructed for 8B-03B. Remaining blockers: XP/action and full recipes beyond the starter assumptions; production save format; passive skill/tool speed modifiers; gem odds; ore-processing chains; nonstarter recipe quantities, final tuning and persisted cancellation reconciliation; enchant I–III values, compatibility and stacking; full enemy stats/loot/food; bank overflow; entitlement edge cases; offline gear/extension/session ordering; cloud authority, security, authentication, persistence and synchronization. No OSRS substitution, free bonuses, unlabelled balancing assumptions, or speculative combat.

## Historical Stage 8B-02 engineering policy (superseded by explicit 8B-03B updates above)

The in-memory harness uses absolute integer tick timestamps and one settlement owner. Repeating a timestamp grants nothing; rewinds are rejected. Time is settled before selection, assignment, or effect changes. Completed cycles produce one resource; partial ticks remain on the same assignment and are discarded when that assignment changes. Fractional XP is stored exactly in decimal units, with no rounding up. XP role/effect attribution follows the state at **completion**, not proportional split across a cycle; this boundary policy needs production approval. It is deliberately tested and disclosed.

Production action XP remains null; the default harness gathers resources without fabricated XP. An explicit development-only fixture supplies 100000 XP/action to exercise milestones quickly. The fixture loads only known milestone rows (plus level-1 zero start); its displayed level is a **known lower bound**, never an interpolated exact level. Unknown level requirements stay locked conservatively. A production curve requires every level and exact milestone matches. Production character unlocks, bank capacity and persistence are not implemented. Offline calls accept a caller-defined interval capped at the approved base four hours; the live UI groups focus/pause absence into one call, but no persisted or trusted-clock save/replay, gear extension, or entitlement handling is claimed.

## Stage 8B-04 implementation authority

The later explicit Stage 8B-04 request approves never-miss combat for this implementation, superseding the unresolved historical accuracy/misses claims recorded above. The conflict history and original source tables remain preserved. It also confirms ten loadout slots, the standard armor equip sequence 5/15/25/35/45/55/65/75/85/95, actual-damage combat XP with lower configurable Magic coefficient, normal-enemy repeat combat, deterministic independent drops, and death stopping combat without gear/XP loss. Existing triangle and selected/background/offline rules remain in force.

All newly supplied gear stats, requirement skill choices, monster numbers, loot quantities/odds, HP and combat XP coefficients are explicitly **development-only**, isolated in four `Development*.csv` catalogs. They are not approved final content or production tuning. Defense arithmetic, event tie order, attack-clock reset on gear changes, immediate respawn and two-handed compatibility are provisional engineering policies. Explicit sample-gear/HP buttons exist only to test the harness; there is no automatic grant or recovery policy. Existing starter recipes remain the earlier new balance assumptions. See `Stage8B04Verification.md` for the complete decisions and limitations; no historical numerical table is changed.

## Archival source tables — verbatim, not independently reconciled approvals

Every Markdown table from each supplied file is reproduced below in source order. Source-status language inside a table is retained for traceability; consult the register above for current interpretation. Contextual section heading identifies each table. S2's detailed Phase 4 numerical tables are preserved exactly, including modeled/provisional annotations. Full originals remain the uploaded source artifacts.

### S1 — Idlebound_Development_Handoff.md

**Original section: 2. Characters, skills, XP and activities**

| Character unlocked | Cumulative account XP | Status |
|---|---:|---|
| 2 | 29,000 | Confirmed in project handoff |
| 3 | 139,000 | Confirmed in project handoff |
| 4 | 473,000 | Confirmed in project handoff |
| 5 | 1,560,000 | Confirmed in project handoff |
| 6 | 4,960,000 | Confirmed in project handoff |
| 7 | 13,960,000 | Confirmed in project handoff |
| 8 | 36,100,000 | Confirmed in project handoff |
| 9 | 100,000,000 | Confirmed in project handoff |

**Original section: 5. Normal monsters and loot**

| Illustrative enemy | Illustrative role / drops | Status |
|---|---|---|
| Cow | Reliable early hide / Worn Leather; possible extras | Example, not final table |
| Chicken | Starter combat and minor loot | Example |
| Goblin | Gold, sellables, basic gear such as iron sword | Example |
| Wolf | Leather/hides, possible fangs | Example |
| Apprentice Mage | Cloth and enchanting materials | Example |
| Skeleton | Bones, old valuables, possibly worn equipment | Example |

**Original section: 6. Approved boss roster**

| # | Boss | Recommended level | Style | Signature mechanic |
|---|---|---:|---|---|
| 1 | Goblin Chieftain | 10 | Melee | Crushing Blow: dodge heavy club strike |
| 2 | Fang of the Wilds | 20 | Ranged | Feral Flurry: rapid strikes, dodge strongest |
| 3 | The Crypt Arcanist | 30 | Magic | Arcane Channel: interrupt spell |
| 4 | The Ironbound Colossus | 40 | Melee | Earthshatter: heavy hit and defensive phases |
| 5 | The Shadow Huntress | 50 | Ranged | Hunter’s Mark: dodge marked volley |
| 6 | The Ember Sovereign | 60 | Magic | Infernal Surge: interrupt charging fire spell |
| 7 | The Fallen King | 70 | Melee | King’s Judgment: dodge execution strike |
| 8 | The Tempest Roc | 80 | Ranged | Storm Barrage: dodge strongest projectile |
| 9 | The Void Weaver | 90 | Magic | Void Collapse: barrier and interruptible spell |
| 10 | Astrax, the Worldbreaker | 99 | Melee | Worldrender: crushing hits and dragon breath |

**Original section: 7. All 30 boss-exclusive unique items**

| Boss | Unique | Slot / family | Approved inherent effect concept |
|---|---|---|---|
| Goblin Chieftain | Chieftain’s Cleaver | Main Hand / Sword | Extra damage to normal monsters |
| Goblin Chieftain | Goblin Bulwark | Off Hand / Shield | Better defense against heavy single hits |
| Goblin Chieftain | Chieftain’s Lucky Ring | Ring | Modest increased gold from monster drops |
| Fang of the Wilds | Fangstrike Dagger | Main Hand / Dagger | Faster attacks after a critical hit |
| Fang of the Wilds | Alpha Hide Boots | Feet | Increased in-combat HP regeneration |
| Fang of the Wilds | Mantle of the Wild | Cape | Increased offline activity duration |
| The Crypt Arcanist | Gravecaller’s Wand | Main Hand / Wand | Critical hits deal additional Magic damage |
| The Crypt Arcanist | Tome of Forgotten Rites | Off Hand / Magical Book | Improved Magic damage against high defense |
| The Crypt Arcanist | Arcanist’s Pendant | Necklace | Improved selected-character Magic combat XP |
| The Ironbound Colossus | Colossus Greataxe | Main Hand / Greataxe | Extra damage against high-defense enemies |
| The Ironbound Colossus | Ironbound Platebody | Body | Reduces heavy incoming hits |
| The Ironbound Colossus | Stoneheart Ring | Ring | Improves natural HP regeneration outside combat |
| The Shadow Huntress | Nightfall Longbow | Main Hand / Longbow | Consecutive hits build critical chance on target |
| The Shadow Huntress | Shadowstalker Hood | Head | Ranged accuracy and critical chance |
| The Shadow Huntress | Hunter’s Quiver | Off Hand / Quiver | Increased damage against Ranged-vulnerable foes |
| The Ember Sovereign | Scepter of Embers | Main Hand / Staff | Chance of bonus fire damage on Magic hit |
| The Ember Sovereign | Emberwoven Robes | Body | Extra Magic damage at high HP |
| The Ember Sovereign | Sovereign’s Cape | Cape | Improves background combat XP efficiency |
| The Fallen King | Kingsbane | Main Hand / Two-handed Sword | Extra damage against bosses |
| The Fallen King | Crown of the Fallen | Head | Maximum HP and defense |
| The Fallen King | Royal Signet | Ring | Extra damage when at combat-style disadvantage |
| The Tempest Roc | Stormpiercer | Main Hand / Crossbow | Critical hits partly bypass defense |
| The Tempest Roc | Tempest Mantle | Cape | Hybrid offline duration and background XP |
| The Tempest Roc | Stormfeather Boots | Feet | Slight attack-speed bonus to all styles |
| The Void Weaver | Voidbound Tome | Main Hand / Tome | Critical hit strengthens next Magic attack |
| The Void Weaver | Veil of Eternity | Head | Magic defense and critical chance |
| The Void Weaver | Voidheart Necklace | Necklace | Background XP efficiency across all nine skills |
| Astrax, the Worldbreaker | Worldbreaker’s Greatsword | Main Hand / Two-handed Sword | Successive attacks against same boss ramp damage |
| Astrax, the Worldbreaker | Astral Dragonplate | Body | Strong cross-style defense |
| Astrax, the Worldbreaker | Wings of Astrax | Cape | Modest HP regen, offline duration, background XP |

**Original section: 14. Unity/C# implementation contract (guidance, not newly approved mechanics)**

| Responsibility | Data / behavior that must be represented | Approval boundary |
|---|---|---|
| Account progression | 9 characters, account XP, unlock milestones, selected active ID | Do not redefine XP curve |
| Character state | Nine skill XP/levels, activity assignment, 10 equipment slots, HP and status | Skill names/stat formulas pending source |
| Activity definitions | Activity ID, required skill/level, action time, XP, inputs, outputs | Catalog and numbers missing |
| Assignment coordinator | One assignee per exact activity, Swap Assignment | Must preserve current production rules |
| Simulation | Online/offline elapsed time, active vs background, resource/XP settlement | Formula details pending |
| Combat | Style triangle, boss encounters, death, specials | Missing stats and timing must not be invented |
| Items / inventory | Shared bank, stacks, equipped ownership, enchanting, NPC transactions | No dupes; no item loss |
| Loot | Creature-specific independent rolls, uniques, collection discovery, KC | Exact odds missing |
| Membership | Entitlements, expiration timestamp, capacity and slot access | Billing implementation pending |
| Reward events | Active-time timer, three reward types, optional video bypass | Exact probabilities pending |
| UI | Phase 5 navigation, scene, tabs, bank, combat, welcome-back | UI must not change simulation |

### S2 — Idlebound_Complete_Development_Handoff.md

**Original section: 2. Character, account and skill model**

| Level | Cumulative XP |
|---:|---:|
|30|500,000|
|60|2,000,000|
|80|4,500,000|
|90|6,500,000|
|99|10,000,000|

**Original section: 2. Character, account and skill model**

| Unlock | Cumulative account XP | Modeled incremental productive hours [PROVISIONAL] | Modeled cumulative hours [PROVISIONAL] |
|---:|---:|---:|---:|
|2|29,000|2|2|
|3|139,000|6|8|
|4|473,000|15|23|
|5|1,560,000|40|63|
|6|4,960,000|100|163|
|7|13,960,000|200|363|
|8|36,100,000|350|713|
|9|100,000,000|~611|~1,323|

**Original section: 4. Equipment and enchantments**

| Family | Tier IV per enchanted slot | Aggregate cap / interpretation |
|---|---:|---|
|Gathering Speed|+10%|+50%|
|Production Speed|+10%|+50%|
|Defense|+5%|+25%|
|Attack Speed|+6%|+30%|
|Critical Chance|+3 percentage points|+15 percentage points|
|Offline Duration|+15%|+75% bonus, 7h maximum offline time|
|Background XP|+10 percentage points|50% XP efficiency|
|HP Regeneration|+10%|+50%|

**Original section: 5. Materials, gathering, production and tiers**

| Tier | Metals | Wood | Fish | Base gather sec/unit | Standard equipment production time | Cooking sec/action |
|---:|---|---|---|---:|---|---:|
|1|Bronze|Common|Shrimp|5|15 sec|5|
|10|Iron|Oak|Sardine|7.8|30 sec|7|
|20|Steel|Willow|Trout|10.6|1 min|9|
|30|Mithril|Maple|Salmon|13.3|3 min|12|
|40|Adamant|Ironwood|Tuna|16.1|7 min|15|
|50|Orichalcum|Yew|Lobster|18.9|15 min|18|
|60|Runite|Emberwood|Swordfish|21.7|35 min|22|
|70|Celestium|Elderwood|Shark|24.4|1 h 15 min|26|
|80|Aetherium|Spiritwood|Manta Ray|27.2|2 h 30 min|30|
|90|Astralium|Worldroot|Leviathan Fish|30|5 h|35|

**Original section: 7. Boss roster and encounter identity**

| Lv | Boss | Style | Signature mechanic |
|---:|---|---|---|
|10|Goblin Chieftain|Melee|Crushing Blow: dodge heavy club strike|
|20|Fang of the Wilds|Ranged|Feral Flurry: rapid strikes, dodge strongest|
|30|The Crypt Arcanist|Magic|Arcane Channel: interrupt spell|
|40|The Ironbound Colossus|Melee|Earthshatter: heavy hit and defensive phases|
|50|The Shadow Huntress|Ranged|Hunter's Mark: dodge marked volley|
|60|The Ember Sovereign|Magic|Infernal Surge: interrupt charging fire spell|
|70|The Fallen King|Melee|King's Judgment: dodge execution strike|
|80|The Tempest Roc|Ranged|Storm Barrage: dodge strongest projectile|
|90|The Void Weaver|Magic|Void Collapse: barrier and interruptible spell|
|99|Astrax, the Worldbreaker|Melee|Worldrender: crushing hits and dragon breath|

**Original section: 7. Boss roster and encounter identity**

| Lv | Kill time | Common loot GP/kill | Average unique NPC resale | Rare chance | Expected gross GP/hour |
|---:|---:|---:|---:|---:|---:|
|10|1.5 min|1,000|30,000|1/50|~64,000|
|20|4 min|3,000|150,000|1/60|~82,500|
|30|6.5 min|8,000|500,000|1/70|~140,000|
|40|10 min|20,000|1,500,000|1/80|~233,000|
|50|15 min|45,000|5,000,000|1/90|~402,000|
|60|22 min|90,000|15,000,000|1/100|~655,000|
|70|30 min|180,000|50,000,000|1/110|~1,270,000|
|80|40 min|350,000|150,000,000|1/120|~2,400,000|
|90|52.5 min|700,000|450,000,000|1/150|~4,230,000|
|99|75 min|1,500,000|1,500,000,000|1/75|~17,200,000|

**Original section: 8. All 30 approved boss-exclusive uniques**

| Boss | Unique | Slot | Inherent effect concept |
|---|---|---|---|
|Goblin Chieftain|Chieftain's Cleaver|Mainhand / Sword|Extra damage to normal monsters|
||Goblin Bulwark|Offhand / Shield|Better defense against heavy single hits|
||Chieftain's Lucky Ring|Ring|Modest increased gold from monster drops|
|Fang of the Wilds|Fangstrike Dagger|Mainhand / Dagger|Faster attacks after a critical hit|
||Alpha Hide Boots|Feet|Increased in-combat HP regeneration|
||Mantle of the Wild|Cape|Increased offline activity duration|
|The Crypt Arcanist|Gravecaller's Wand|Mainhand / Wand|Critical hits deal additional Magic damage|
||Tome of Forgotten Rites|Offhand / Magical Book|Improved Magic damage against high defense|
||Arcanist's Pendant|Necklace|Improved selected-character Magic combat XP|
|The Ironbound Colossus|Colossus Greataxe|Mainhand / Greataxe|Extra damage against high-defense enemies|
||Ironbound Platebody|Body|Reduces heavy incoming hits|
||Stoneheart Ring|Ring|Improves natural HP regeneration outside combat|
|The Shadow Huntress|Nightfall Longbow|Mainhand / Longbow|Consecutive hits build critical chance on target|
||Shadowstalker Hood|Head|Ranged accuracy and critical chance (accuracy terminology is an approved effect concept; core attacks still do not miss)|
||Hunter's Quiver|Offhand / Quiver|Increased damage against Ranged-vulnerable foes|
|The Ember Sovereign|Scepter of Embers|Mainhand / Staff|Chance of bonus fire damage on Magic hit|
||Emberwoven Robes|Body|Extra Magic damage at high HP|
||Sovereign's Cape|Cape|Improves background combat XP efficiency|
|The Fallen King|Kingsbane|Mainhand / Two-handed Sword|Extra damage against bosses|
||Crown of the Fallen|Head|Maximum HP and defense|
||Royal Signet|Ring|Extra damage at combat-style disadvantage|
|The Tempest Roc|Stormpiercer|Mainhand / Crossbow|Critical hits partly bypass defense|
||Tempest Mantle|Cape|Hybrid offline duration and background XP|
||Stormfeather Boots|Feet|Slight attack-speed bonus to all styles|
|The Void Weaver|Voidbound Tome|Mainhand / Tome|Critical hit strengthens next Magic attack|
||Veil of Eternity|Head|Magic defense and critical chance|
||Voidheart Necklace|Necklace|Background XP efficiency across all nine skills|
|Astrax, the Worldbreaker|Worldbreaker's Greatsword|Mainhand / Two-handed Sword|Successive attacks against same boss ramp damage|
||Astral Dragonplate|Body|Strong cross-style defense|
||Wings of Astrax|Cape|Modest HP regen, offline duration and background XP|

**Original section: 10. NPC shop, pricing and gold economy**

| Resource tier | Full five-piece set NPC purchase |
|---:|---:|
|1|1,000 GP|
|10|11,000 GP|
|20|370,000 GP|
|30|1,100,000 GP|
|40|3,800,000 GP|
|50|14,500,000 GP|
|60|39,000,000 GP|
|70|118,000,000 GP|
|80|350,000,000 GP|
|90|1,000,000,000 GP|
|99 special capstone|10,000,000,000 GP **[PROVISIONAL]**|

**Original section: 10. NPC shop, pricing and gold economy**

| Tier | Baseline GP/hr |
|---:|---:|
|1|1,000|
|10|3,000|
|20|8,000|
|30|20,000|
|40|50,000|
|50|100,000|
|60|200,000|
|70|400,000|
|80|700,000|
|90|1,000,000|

**Original section: 14. Explicit unresolved implementation/balancing register**

| ID | Open specification | Constraint to preserve |
|---|---|---|
|U01|XP thresholds for every level 1–99|Keep confirmed milestones and 10M XP at 99|
|U02|XP per gathering/craft/cook/combat/enchant action|Magic combat XP lower; enchanting grants Magic XP|
|U03|Names/potencies of four unlisted enchant families, Tier I–III, set bonuses|12 families, four tiers and confirmed Tier IV caps|
|U04|Actual crafting/enchanting recipes and materials at levels 50/70/90/99|Standard gear skilling-only; specials use boss catalysts|
|U05|Normal-monster bestiary (~30), HP/attack stats and drops|Thematic per-monster loot, no normal specials|
|U06|Boss HP/damage/special timings, rare-roll distribution among three uniques|10 bosses, nominal durations and independent rates|
|U07|Food/healing, regen, autofood consumption and death edge cases|Death stops activity, no equipment/XP loss|
|U08|Boss unique numeric effects, item equip restrictions, special cooldowns|Preserve identity and enchant slot rules|
|U09|Real recipe-constrained economic/GP/XP simulations|No free materials or double-counted boss loot|
|U10|Offline tick, partial action, cancellation, clock security|4–7h cap, proper membership cutoffs|
|U11|Gold membership affordability and renewal calendar|$4.99 subscription, GP membership allowed|
|U12|Random event reward chances, GP scaling, extension rollover|Approved intervals, 3×20min/day|
|U13|Bank overflow and item variant slot rules|No loss on membership expiration|
|U14|Cloud/save, backend, anti-cheat, purchase verification|No established implementation architecture|
|U15|MVP content boundaries and development order|Architecture must support approved full game|
|U16|Mastery Gloves, quests, advanced modes|Not approved as implementable launch features|
|U17|Exact source Phase 5/6 PDFs and additional detailed decisions not present here|Do not infer content from summary alone|

### S3 — Idlebound_Unity_Codex_Development_Handoff.md

**Original section: 2.1 Skills and progression**

| Skill | Approved gameplay role |
|---|---|
| Mining | Deterministic ore per cycle, bonus chance for gemstones; mining tool and passive gathering speed progression |
| Smithing | Metal armor, melee weapons and metal/leather shields; timed production |
| Crafting | Leather armor, cloth robes, ranged and magic weapons, offhands, jewelry; combines fletching/tailoring/leatherworking |
| Woodcutting | Deterministic wood per cycle, axe/tool and passive gathering speed progression |
| Fishing | Deterministic selected fish per cycle, fishing tool and passive gathering speed progression |
| Cooking | Cook selected fish into food with fixed healing values, no burn/failure |
| Melee | Melee damage; increases universal maximum HP; associated with Defense enchantment requirement |
| Ranged | Ranged damage; increases universal accuracy; associated with Attack Speed enchantment requirement; glove requirements |
| Magic | Magic damage; timed enchanting activity grants Magic XP; associated with Critical Chance enchantment requirement |

**Original section: 3.1 Ten named tiers — CONFIRMED**

| Material unlock level | Metal | Wood | Fish | Leather | Cloth |
|---:|---|---|---|---|---|
| 1 | Bronze | Common Wood | Shrimp | Worn Leather | Linen |
| 10 | Iron | Oak | Sardine | Tanned Leather | Wool |
| 20 | Steel | Willow | Trout | Hard Leather | Cotton |
| 30 | Mithril | Maple | Salmon | Reinforced Leather | Silk |
| 40 | Adamant | Ironwood | Tuna | Thick Hide | Mysticweave |
| 50 | Orichalcum | Yew | Lobster | Direhide | Moonweave |
| 60 | Runite | Emberwood | Swordfish | Dragonscale | Emberweave |
| 70 | Celestium | Elderwood | Shark | Celestial Hide | Starweave |
| 80 | Aetherium | Spiritwood | Manta Ray | Spirit Hide | Aetherweave |
| 90 | Astralium | Worldroot | Leviathan Fish | Astralhide | Astralweave |

**Original section: 3.2 Level milestones**

| Progression element | Levels | Status |
|---|---|---|
| Resource families | 1, 10, 20, 30, 40, 50, 60, 70, 80, 90 | CONFIRMED |
| Standard equipment equip tiers | 5, 15, 25, 35, 45, 55, 65, 75, 85, 95 | CONFIRMED |
| Universal glove Ranged requirements | 1, 12, 22, 32, 42, 52, 62, 72, 82, 92 | CONFIRMED |
| Offhand equip levels | 7, 17, 27, 37, 47, 57, 67, 77, 87, 97 | PROVISIONAL |
| Magic enchant potency | Four potency tiers, individual unlock levels require approved final reference | CONFIRMED rank count; placement unresolved |

**Original section: 3.3 Gems and jewelry**

| Gem | Fixed utility identity | Gem utility tier |
|---|---|---:|
| Sapphire | Offline Duration | 1 |
| Emerald | Background XP | 1 |
| Ruby | HP Regeneration | 1 |
| Diamond | Offline Duration | 2 |
| Dragonstone | Background XP | 2 |
| Onyx | HP Regeneration | 2 |

**Original section: 4.2 Standard armor**

| Combat style | Producer | Standard primary ingredient | Armor pieces |
|---|---|---|---|
| Melee | Smithing | Metal bars | Head, Body, Legs, Feet |
| Ranged | Crafting | Leather | Head, Body, Legs, Feet |
| Magic | Crafting | Cloth | Head, Body, Legs, Feet |

**Original section: 4.3 Standard weapon families**

| Style | Family | Producer | Ingredients | Combat identity |
|---|---|---|---|---|
| Melee | Dagger | Smithing | Metal | Fast/light |
| Melee | Sword | Smithing | Metal | Balanced |
| Melee | Greataxe | Smithing | Metal | Slow/heavy |
| Melee | Two-handed Sword | Smithing | Metal | Slowest/heaviest individual hits |
| Ranged | Shortbow | Crafting | Wood | Fast |
| Ranged | Longbow | Crafting | Wood | Slower/heavier |
| Ranged | Crossbow | Crafting | Wood + metal bars | Accuracy-focused |
| Magic | Wand | Crafting | Wood | Fast/light |
| Magic | Staff | Crafting | Wood | Slow/heavy |
| Magic | Tome | Crafting | Wood + cloth + small leather cover | Medium-speed/critical-focused |

**Original section: 4.4 Offhands**

| Item | Producer | Ingredients | Role |
|---|---|---|---|
| Shield | Smithing | Metal + leather | Defense |
| Quiver | Crafting | Leather + wood | Ranged combat, no ammo supply |
| Magical Book | Crafting | Wood + cloth + small leather | Magic combat |

**Original section: 4.6 Enchanting**

| Family | Enchant | Associated skill / constraint |
|---|---|---|
| Gathering | Mining Speed | Mining |
| Gathering | Woodcutting Speed | Woodcutting |
| Gathering | Fishing Speed | Fishing |
| Production | Smithing Speed | Smithing |
| Production | Crafting Speed | Crafting |
| Production | Cooking Speed | Cooking |
| Combat | Defense | Melee; cross-style effect |
| Combat | Attack Speed | Ranged; cross-style effect |
| Combat | Critical Chance | Magic; cross-style effect |
| Utility | Offline Duration | Item-purpose-dependent secondary requirement |
| Utility | Background XP | Item-purpose-dependent secondary requirement |
| Utility | HP Regeneration | Item-purpose-dependent secondary requirement |

**Original section: 5.1 Combat triangle — CONFIRMED**

| Attacker | Favored target | Disfavored target | Advantage | Disadvantage | Neutral |
|---|---|---|---:|---:|---:|
| Melee | Ranged | Magic | 2× damage | 0.5× damage | 1× damage |
| Ranged | Magic | Melee | 2× | 0.5× | 1× |
| Magic | Melee | Ranged | 2× | 0.5× | 1× |

**Original section: 7. TEN BOSSES — APPROVED PHASE 3 ROSTER**

| # | Boss | Recommended level (NOT gate) | Style | Signature mechanic |
|---:|---|---:|---|---|
| 1 | Goblin Chieftain | 10 | Melee | Crushing Blow: dodge heavy club strike |
| 2 | Fang of the Wilds | 20 | Ranged | Feral Flurry: rapid strikes, dodge strongest |
| 3 | The Crypt Arcanist | 30 | Magic | Arcane Channel: interrupt spell |
| 4 | The Ironbound Colossus | 40 | Melee | Earthshatter: heavy hit and defensive phases |
| 5 | The Shadow Huntress | 50 | Ranged | Hunter's Mark: dodge marked volley |
| 6 | The Ember Sovereign | 60 | Magic | Infernal Surge: interrupt charging fire spell |
| 7 | The Fallen King | 70 | Melee | King's Judgment: dodge execution strike |
| 8 | The Tempest Roc | 80 | Ranged | Storm Barrage: dodge strongest projectile |
| 9 | The Void Weaver | 90 | Magic | Void Collapse: barrier and interruptible spell |
| 10 | Astrax, the Worldbreaker | 99 | Melee | Worldrender: crushing hits and dragon breath |

**Original section: 8. THIRTY APPROVED BOSS-EXCLUSIVE UNIQUES**

| Boss | Unique | Slot/family | Approved inherent effect identity |
|---|---|---|---|
| Goblin Chieftain | Chieftain's Cleaver | Main Hand / Sword | Extra damage to normal monsters |
| Goblin Chieftain | Goblin Bulwark | Off Hand / Shield | Better defense against heavy single hits |
| Goblin Chieftain | Chieftain's Lucky Ring | Ring | Modest increased gold from monster drops |
| Fang of the Wilds | Fangstrike Dagger | Main Hand / Dagger | Faster attacks after a critical hit |
| Fang of the Wilds | Alpha Hide Boots | Feet | Increased in-combat HP regeneration |
| Fang of the Wilds | Mantle of the Wild | Cape | Increased offline activity duration |
| The Crypt Arcanist | Gravecaller's Wand | Main Hand / Wand | Critical hits deal additional Magic damage |
| The Crypt Arcanist | Tome of Forgotten Rites | Off Hand / Magical Book | Improved Magic damage against high defense |
| The Crypt Arcanist | Arcanist's Pendant | Necklace | Improved selected-character Magic combat XP |
| The Ironbound Colossus | Colossus Greataxe | Main Hand / Greataxe | Extra damage against high-defense enemies |
| The Ironbound Colossus | Ironbound Platebody | Body | Reduces heavy incoming hits |
| The Ironbound Colossus | Stoneheart Ring | Ring | Improves natural HP regeneration outside combat |
| The Shadow Huntress | Nightfall Longbow | Main Hand / Longbow | Consecutive hits build critical chance on target |
| The Shadow Huntress | Shadowstalker Hood | Head | Ranged accuracy and critical chance |
| The Shadow Huntress | Hunter's Quiver | Off Hand / Quiver | Increased damage against Ranged-vulnerable foes |
| The Ember Sovereign | Scepter of Embers | Main Hand / Staff | Chance of bonus fire damage on Magic hit |
| The Ember Sovereign | Emberwoven Robes | Body | Extra Magic damage at high HP |
| The Ember Sovereign | Sovereign's Cape | Cape | Improves background combat XP efficiency |
| The Fallen King | Kingsbane | Main Hand / Two-handed Sword | Extra damage against bosses |
| The Fallen King | Crown of the Fallen | Head | Maximum HP and defense |
| The Fallen King | Royal Signet | Ring | Extra damage when at combat-style disadvantage |
| The Tempest Roc | Stormpiercer | Main Hand / Crossbow | Critical hits partly bypass defense |
| The Tempest Roc | Tempest Mantle | Cape | Hybrid offline duration and background XP |
| The Tempest Roc | Stormfeather Boots | Feet | Slight attack-speed bonus to all styles |
| The Void Weaver | Voidbound Tome | Main Hand / Tome | Critical hit strengthens next Magic attack |
| The Void Weaver | Veil of Eternity | Head | Magic defense and critical chance |
| The Void Weaver | Voidheart Necklace | Necklace | Background XP efficiency across all nine skills |
| Astrax, the Worldbreaker | Worldbreaker's Greatsword | Main Hand / Two-handed Sword | Successive attacks against same boss ramp damage |
| Astrax, the Worldbreaker | Astral Dragonplate | Body | Strong cross-style defense |
| Astrax, the Worldbreaker | Wings of Astrax | Cape | Modest HP regen, offline duration and background XP |

**Original section: 14. OPEN DECISION REGISTER — BLOCKERS FOR PRODUCTION VALUES**

| Priority | Missing artifact or decision | Consequence |
|---|---|---|
| Critical | Full approved Phase 4 Progression/Balance/Economy PDF | Exact XP curve, level thresholds, per-activity XP/time/yields, character unlock thresholds, gold economy cannot be implemented faithfully |
| Critical | Updated approved 1–99 unlock sheet (replacing eight-rank draft) | Exact unlock placements and four-tier enchant progression cannot be treated as final |
| High | Full Phase 5 UI/UX PDF | Precise interaction and screen behavior not fully source-verifiable |
| High | Full Phase 6 Membership PDF | Some expiry, reward and stacking edge cases not fully source-verifiable |
| High | Completed ~30-monster roster and exact loot tables | Only illustrative normal enemies currently specified |
| High | Numeric monster/boss stats, mechanics and item effects | Boss and combat concepts approved; numeric execution remains open |
| High | Crafting recipes, enchanting costs, eligibility and jewelry rank interaction | Cannot faithfully calculate item consumption or apply effects |
| High | Final architecture/security/account/save decisions | Cannot safely choose authoritative game-state/purchase design |
| Medium | Mastery Gloves, quests, cosmetic completions | Future content; not approved launch mechanics |
| Medium | App Store/domain/trademark availability for Idlebound | Brand selected but not cleared for release |

### S4 — Idlebound_Unity_Codex_Development_Handoff (1).md

**Original section: 2. Skills and progression — CONFIRMED except marked**

| Skill | Category | Established inputs / outputs or properties |
|---|---|---|
| Mining | Gathering | Ore/metals; chance for six gemstones; gathering-speed enchanting |
| Smithing | Production | Metal weapons, melee armor, shields; timed action grants Smithing XP |
| Crafting | Production | Leather/cloth armor, wood-based weapons, jewelry and relevant offhands; timed XP |
| Woodcutting | Gathering | Wood; gathering-speed enchanting |
| Fishing | Gathering | Fish; gathering-speed enchanting |
| Cooking | Production | Fish → food; timed Cooking XP |
| Melee | Combat | Melee damage and **universal maximum HP** |
| Ranged | Combat | Ranged damage and **universal accuracy** |
| Magic | Combat + production | Magic damage; access to **timed enchanting that grants Magic XP** |

**Original section: 2.1 Level bands and exact established requirements**

| Tier index | Resource tier level | Standard equipment level | Gloves: Ranged requirement | Offhand requirement (PROVISIONAL) |
|---:|---:|---:|---:|---:|
| 1 | 1 | 5 | 1 | 7 |
| 2 | 10 | 15 | 12 | 17 |
| 3 | 20 | 25 | 22 | 27 |
| 4 | 30 | 35 | 32 | 37 |
| 5 | 40 | 45 | 42 | 47 |
| 6 | 50 | 55 | 52 | 57 |
| 7 | 60 | 65 | 62 | 67 |
| 8 | 70 | 75 | 72 | 77 |
| 9 | 80 | 85 | 82 | 87 |
| 10 | 90 | 95 | 92 | 97 |

**Original section: 2.2 XP curve / rate provenance**

| Metric | Recovered value | Status / limitation |
|---|---|---|
| Max skill level | 99 | CONFIRMED |
| XP to 99 in one skill | **Approximately 10,000,000 XP** | CONFIRMED approximate design target from later handoff, **not** an exact threshold |
| Full XP function `xpRequiredForLevel(level)` | **UNSPECIFIED** | Actual Phase 4 curve/table not available |
| XP for every individual level | **UNSPECIFIED** | Do not generate an OSRS table or interpolate 10M |
| Identical curve for all nine skills | **UNSPECIFIED** | Prior Phase 4 questions, no complete table attached |
| Per-action gathering / production XP | **UNSPECIFIED** | Phase 4 PDF needed |
| Active efficiency ceiling | **Approximately 2×** | CONFIRMED high-level rule; action-specific formula unavailable |
| Modeled productive account time | **Approximately 1,323 account-hours** | Phase 7 reports this **under its modeling assumptions**; assumptions not accessible; not a guaranteed completion estimate |
| Magic combat XP | Lower than otherwise comparable combat XP because enchanting awards Magic XP | CONFIRMED direction; exact reduction UNSPECIFIED |
| Combat XP | Based on **actual damage dealt**; miss = no damage XP; overkill = no extra XP | CONFIRMED; exact XP-per-damage and boss multiplier UNSPECIFIED |

**Original section: 4. Offline, active and background progression**

| Rule | Value | Status |
|---|---|---|
| Baseline offline duration | **4 hours** | CONFIRMED later handoff |
| Maximum via equipment | **7 hours** | CONFIRMED later handoff; stacking rules UNSPECIFIED |
| Offline activity | Calculates eligible assigned actions, equipment and elapsed time | CONFIRMED behavior; per-action algorithm UNSPECIFIED |
| Background XP | 0% unequipped; up to **50%** with equipment | CONFIRMED upper bound; rank values UNSPECIFIED |
| Active efficiency | Up to approximately **2×** | CONFIRMED design ceiling; interaction-specific implementation UNSPECIFIED |
| Welcome-back screen | **One account-wide summary** | CONFIRMED |
| Offline bonus rewarded claim | **+20 minutes**, **3 claims daily** | CONFIRMED; stacking/rollover and exact day boundary UNSPECIFIED |
| Expiration mid-offline session | Rewards only until **exact membership expiration timestamp** for affected entitlements | CONFIRMED |
| Death in offline combat | Stop combat; **forfeit any remaining eligible offline combat duration** | CONFIRMED |

**Original section: 5.1 Approved ten-tier material lookup**

| Unlock level | Metal | Wood | Fish | Leather | Cloth |
|---:|---|---|---|---|---|
| 1 | Bronze | Common Wood | Shrimp | Worn Leather | Linen |
| 10 | Iron | Oak | Sardine | Tanned Leather | Wool |
| 20 | Steel | Willow | Trout | Hard Leather | Cotton |
| 30 | Mithril | Maple | Salmon | Reinforced Leather | Silk |
| 40 | Adamant | Ironwood | Tuna | Thick Hide | Mysticweave |
| 50 | Orichalcum | Yew | Lobster | Direhide | Moonweave |
| 60 | Runite | Emberwood | Swordfish | Dragonscale | Emberweave |
| 70 | Celestium | Elderwood | Shark | Celestial Hide | Starweave |
| 80 | Aetherium | Spiritwood | Manta Ray | Spirit Hide | Aetherweave |
| 90 | Astralium | Worldroot | Leviathan Fish | Astralhide | Astralweave |

**Original section: 6.2 Twelve families**

| Group | Enchantment | Associated combat skill where specified |
|---|---|---|
| Gathering | Mining Speed | Not specified |
| Gathering | Woodcutting Speed | Not specified |
| Gathering | Fishing Speed | Not specified |
| Production | Smithing Speed | Not specified |
| Production | Crafting Speed | Not specified |
| Production | Cooking Speed | Not specified |
| Combat | Defense | Melee |
| Combat | Attack Speed | Ranged |
| Combat | Critical Chance | Magic |
| Utility | Offline Duration | Item-dependent |
| Utility | Background XP | Item-dependent |
| Utility | HP Regeneration | Item-dependent |

**Original section: 6.3 Gemstones and jewelry — established relations**

| Gem | Jewelry utility enchant association |
|---|---|
| Sapphire | Offline Duration, Tier 1 |
| Emerald | Background XP, Tier 1 |
| Ruby | HP Regeneration, Tier 1 |
| Diamond | Offline Duration, Tier 2 |
| Dragonstone | Background XP, Tier 2 |
| Onyx | HP Regeneration, Tier 2 |

**Original section: 9. Boss progression — approved names, styles and mechanics**

| # | Boss | Recommended level (not gate) | Style | Signature mechanic |
|---:|---|---:|---|---|
| 1 | Goblin Chieftain | 10 | Melee | **Crushing Blow**: dodge heavy club hit |
| 2 | Fang of the Wilds | 20 | Ranged | **Feral Flurry**: rapid strikes, dodge strongest |
| 3 | The Crypt Arcanist | 30 | Magic | **Arcane Channel**: interrupt spell |
| 4 | The Ironbound Colossus | 40 | Melee | **Earthshatter**: heavy strike / defensive phase |
| 5 | The Shadow Huntress | 50 | Ranged | **Hunter's Mark**: dodge marked volley |
| 6 | The Ember Sovereign | 60 | Magic | **Infernal Surge**: interrupt charging fire spell |
| 7 | The Fallen King | 70 | Melee | **King's Judgment**: dodge execution strike |
| 8 | The Tempest Roc | 80 | Ranged | **Storm Barrage**: dodge major projectile |
| 9 | The Void Weaver | 90 | Magic | **Void Collapse**: barrier and interruptible spell |
| 10 | Astrax, the Worldbreaker | 99 | Melee | **Worldrender**: crushing hits and dragon breath |

**Original section: 9.1 All 30 boss-exclusive unique items — approved identity, no approved numerical buffs**

| Boss | Unique item | Slot / family | Inherent effect concept |
|---|---|---|---|
| Goblin Chieftain | Chieftain's Cleaver | Main Hand / Sword | Increased damage against normal monsters |
| Goblin Chieftain | Goblin Bulwark | Off Hand / Shield | Better defense against heavy individual hits |
| Goblin Chieftain | Chieftain's Lucky Ring | Ring | Small increase to gold from monster drops |
| Fang of the Wilds | Fangstrike Dagger | Main Hand / Dagger | Faster attacks after a critical hit |
| Fang of the Wilds | Alpha Hide Boots | Feet | Increased in-combat HP regeneration |
| Fang of the Wilds | Mantle of the Wild | Cape | Increased offline duration |
| The Crypt Arcanist | Gravecaller's Wand | Main Hand / Wand | Extra Magic damage on critical hits |
| The Crypt Arcanist | Tome of Forgotten Rites | Off Hand / Magical Book | More Magic damage versus high-defense opponents |
| The Crypt Arcanist | Arcanist's Pendant | Necklace | Greater selected-character Magic combat XP |
| The Ironbound Colossus | Colossus Greataxe | Main Hand / Greataxe | Extra damage against high-defense targets |
| The Ironbound Colossus | Ironbound Platebody | Body | Mitigates particularly heavy incoming hits |
| The Ironbound Colossus | Stoneheart Ring | Ring | Greater natural out-of-combat HP regeneration |
| The Shadow Huntress | Nightfall Longbow | Main Hand / Longbow | Consecutive hits build crit chance vs current target |
| The Shadow Huntress | Shadowstalker Hood | Head | Ranged accuracy and crit chance |
| The Shadow Huntress | Hunter's Quiver | Off Hand / Quiver | More damage vs Ranged-vulnerable enemies |
| The Ember Sovereign | Scepter of Embers | Main Hand / Staff | Chance of bonus fire damage on Magic hit |
| The Ember Sovereign | Emberwoven Robes | Body | Greater Magic damage at high HP |
| The Ember Sovereign | Sovereign's Cape | Cape | Better background combat XP efficiency |
| The Fallen King | Kingsbane | Main Hand / Two-handed Sword | Bonus damage against bosses |
| The Fallen King | Crown of the Fallen | Head | Increased maximum HP and defense |
| The Fallen King | Royal Signet | Ring | Bonus damage when player's combat style is disadvantaged |
| The Tempest Roc | Stormpiercer | Main Hand / Crossbow | Critical hits partly bypass defense |
| The Tempest Roc | Tempest Mantle | Cape | Hybrid offline duration + background XP |
| The Tempest Roc | Stormfeather Boots | Feet | Slight attack speed improvement for all styles |
| The Void Weaver | Voidbound Tome | Main Hand / Tome | Critical hit empowers next Magic attack |
| The Void Weaver | Veil of Eternity | Head | Magic defense and crit chance |
| The Void Weaver | Voidheart Necklace | Necklace | Background XP efficiency across all nine skills |
| Astrax, the Worldbreaker | Worldbreaker's Greatsword | Main Hand / Two-handed Sword | Repeated hits vs same boss ramp up damage |
| Astrax, the Worldbreaker | Astral Dragonplate | Body | Strong defenses across all combat styles |
| Astrax, the Worldbreaker | Wings of Astrax | Cape | Modest HP regen, offline duration, background XP |

**Original section: 11.1 Bank capacity and expansions (approved Phase 6)**

| Entitlement | Bank slots |
|---|---:|
| Free | **50** |
| Active member | **200** |
| Permanent purchased expansion | **+25** per purchase |
| Maximum permanent expansions | **4** |
| Maximum permanent added capacity | **+100** |

**Original section: 12.1 Membership**

| Feature | Value / rule | Status |
|---|---|---|
| Monthly subscription | **$4.99 USD** | CONFIRMED |
| Renewals | Automatic | CONFIRMED |
| Gold membership | Supported | CONFIRMED |
| Working gold exchange | **1,000,000 GP per membership day**, or **30,000,000 GP for 30 days** | **PROVISIONAL** pending economy validation |
| Time period | Calendar days; extended purchases allowed | CONFIRMED |
| Multi-duration permanent bundle discount | None | CONFIRMED |
| Special real-money promotions | Allowed | CONFIRMED |
| Member availability | Account-wide; all XP-unlocked slots subject to entitlement | CONFIRMED |
| Free characters | 3 (designated slots **1, 3, 8**) | CONFIRMED handoff |
| Member characters | Up to 9 unlocked | CONFIRMED |

**Original section: 14. Unity/C# implementation contract (design constraints, NOT an approved architecture)**

| Logical data concern | State/definitions that must be representable |
|---|---|
| Account | account ID; membership/ad removal; 9 slot unlocks; gold; shared bank; account XP aggregation; collection log; last settlement timestamp |
| Character | slot index; 9 skill XP/levels; current HP; equipped items; selected status; one activity assignment; preferred food |
| Activity | category and exact unique activity key, target resource/recipe/monster/boss; start/stop state; time progression |
| Equipment | item identity, rarity, tier, slot, main/offhand restriction, inherent effects, optional immutable enchant rank/family |
| Skills | level cap 99, XP curve lookup **from source data**, unlock lists; never infer level XP from ~10M target |
| Combat | style, damage triangle, actual HP damage, regen, food use, manual special cooldowns, boss state and pattern |
| Drop tables | per-monster guaranteed drops and independent randomized entries; rare discovery events; sellable-only flag |
| Progression settlement | online selected/background state; offline period; caps, XP eligibility, resource production, death, entitlement expiry |
| Monetization | fixed/free/member capacity, purchase ledger, timed entitlement, ad claim eligibility, reward limits |
| Presentation | UI state, offline summary, roster/bank/shop/log, landscape/portrait view |
