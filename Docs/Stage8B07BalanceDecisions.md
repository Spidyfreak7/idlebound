# Stage 8B-07 balance and deterministic policies

Authority: the user's explicit Stage 8B-07 handoff. Its individual unlock schedule supersedes older whole-tier schedules. Historical master-design tables are preserved. This stage does not change the existing XP curve, recipe costs, production XP, monster stats or loot tables.

## Exact approved unlocks

| Enchantment | I | II | III | IV |
|---|---:|---:|---:|---:|
| Mining Speed | 15 | 36 | 58 | 79 |
| Fishing Speed | 17 | 38 | 60 | 81 |
| Woodcutting Speed | 19 | 40 | 61 | 83 |
| Smithing Speed | 20 | 42 | 63 | 85 |
| Cooking Speed | 22 | 44 | 65 | 86 |
| Crafting Speed | 24 | 45 | 67 | 88 |
| Defense | 26 | 47 | 69 | 90 |
| Attack Speed | 28 | 49 | 70 | 92 |
| Critical Chance | 29 | 51 | 72 | 94 |
| Offline Duration | 31 | 53 | 74 | 95 |
| Background XP | 33 | 54 | 76 | 97 |
| HP Regeneration | 35 | 56 | 78 | 99 |

## Potencies and caps

Tier IV/caps are established design. I–III potency values are **provisional playtest balance**. Background XP and critical values are additive percentage points. Effects from unequipped items are zero.

| Type | I | II | III | IV | Aggregate cap |
|---|---:|---:|---:|---:|---:|
| Mining Speed | 2% | 4% | 7% | 10% | 50% |
| Fishing Speed | 2% | 4% | 7% | 10% | 50% |
| Woodcutting Speed | 2% | 4% | 7% | 10% | 50% |
| Smithing Speed | 2% | 4% | 7% | 10% | 50% |
| Cooking Speed | 2% | 4% | 7% | 10% | 50% |
| Crafting Speed | 2% | 4% | 7% | 10% | 50% |
| Defense | 1% | 2% | 3% | 5% | 25% |
| Attack Speed | 1% | 2% | 4% | 6% | 30% |
| Critical Chance | 0.5 pp | 1 pp | 2 pp | 3 pp | 15 pp |
| Offline Duration | 3% | 6% | 10% | 15% | 75% |
| Background XP | 2 pp | 4 pp | 7 pp | 10 pp | 50 pp |
| HP Regeneration | 2% | 4% | 7% | 10% | 50% |

## Initial approved test balance (not final economy)

| Tier | Primary materials | Secondary materials | Duration | Magic XP |
|---|---:|---:|---|---:|
| I | 40 | 20 | 30 sec | 400 |
| II | 80 | 40 | 60 sec | 950 |
| III | 160 | 80 | 150 sec | 3,000 |
| IV | 320 | 160 | 360 sec | 8,500 |

| Enchant tier | Primary resource | Secondary resource |
|---|---|---|
| I | Bronze (`metal-1`) | Common Wood (`wood-1`) |
| II | Steel (`metal-20`) | Willow (`wood-20`) |
| III | Adamant (`metal-40`) | Ironwood (`wood-40`) |
| IV | Celestium (`metal-70`) | Elderwood (`wood-70`) |

These rows are source-controlled in `Enchantments.csv`. Every type uses the same tier cost; no gems/essences, recipes or free material grants are added. `EnchantmentRules.csv` holds the provisional 25% matching multiplier, approved initial 1.5× critical multiplier, and provisional regeneration baseline/pulse. There are no inherent item bonuses without an actual enchantment.

## Deterministic engineering choices

- Target must be an exact unenchanted bank instance. Materializing an anonymous bank unit creates identity only; bank quantity does not increase. Equipped items must first be unequipped. The locked target remains part of bank quantity; equip APIs exclude it, including implicit item-type selection. Selling/moving transactions are not implemented; future commands must respect `IsItemLocked`.
- Enchanting runs once per Start; continuous production is unchanged. Exact enchantment ID is its exclusive activity. Replacing a running enchant, including another target for the same ID, requires explicit confirmation; old reservation refunds and new reservation commit together. Failed validation/refund overflow preserves the old job. Identical target taps cannot reserve twice. Completion settles before cancel/reassignment at the same timestamp.
- Only equipped instances contribute. Matching Head/Body/Legs/Feet must share the full type+tier ID. Add 25% of these four pieces' combined potency once, add other slots normally, then cap. Background hooks remain separate; equipment-derived effects are never copied into saved synthetic effects. The existing XP policy clamps the combined pathway at 50%.
- Named gathering/production durations are `ceil(baseTicks / (1 + bonus))`, at least one tick. Completion cannot occur before the rounded boundary. Equipment changes settle old elapsed time first, preserve the completed fraction using floor rounding (less than one tick can be lost), then use the new duration. Existing combat equipment changes reset the player's attack clock to the new interval without a free hit.
- Offline time is limited **per character** to `4h * (1 + OfflineDurationBonus)`, at most 7h. Gathering and production process shared-bank events chronologically, respecting each character's cutoff; existing gathering-before-output-before-ordinal-reservation tie order remains. Pending jobs retain their exact paid partial cycle. Offline XP always uses background rules, including for the selected character. The entire UTC absence is consumed once even when capped. Separate settlement calls represent separate intervals; do not split one capped absence into arbitrary new absences.
- Critical rolls use the saved loot seed with a separate `critical/attack` namespace plus a saved per-character attack ordinal. Loot roll identities remain unchanged. Critical multiplier applies before overkill clipping. XP uses actual HP removed. Incoming damage is `max(0, triangleDamage - flatDefense) * (1 - cappedDefenseBonus)`.
- **New provisional regeneration policy:** only living characters with an equipped HP Regeneration source receive a pulse every 60 eligible seconds. Each pulse heals `maxHP * 0.01 * (1 + cappedRegenBonus)`, capped at max HP. Unenchanted characters gain no free healing; dead characters never resurrect. This source-gated interpretation avoids changing existing unenchanted combat. Pulse phase persists, pauses without a source and resets on removal of the last source. At a combat tie, player attack, enemy attack, then healing; lethal damage wins over healing. No food/auto-recovery loop is added. This baseline/pulse policy is provisional and configurable.

## Scope limits

Existing equipment/bestiary stats remain development-only. Starter recipes do not yet provide every armor slot; use existing explicitly sandboxed development gear tools for a four-piece manual test, never a real-save grant. No boss inherent powers, rare catalysts, shops, entitlement changes, cloud sync, enchant replacement/upgrades or art overhaul are introduced. Prototype navigation/layout remain in place. Mobile rendering, AOT and actual device filesystem behavior require Unity verification.
