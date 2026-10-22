# Stage 8B-08 — food, auto-eating and AFK combat

Permanent branch: **develop**. Baseline: `4e547f14d8b868f2cc0ef2eb11b6156a97a6d6a1`. The user reports Stage 8B-07 passed 562/562 EditMode and 19/19 PlayMode cases in Windows Unity. Those are baseline results; this stage still requires Unity verification.

## Implemented balance and behavior

`Foods.csv` defines **Cooked shrimp (cooked-shrimp-item): 8 HP** and **Cooked sardine (cooked-sardine-item): 12 HP**. These are new **provisional playtest assumptions**, not final economy values. They reference existing Cooking outputs, with no duplicate item IDs, new recipes, stat changes or second inventory. Each serving consumes exactly one shared-bank item, clamps healing at maximum HP and grants no XP.

Each character defaults independently to Auto-eat ON, 50% threshold and Auto preference. Thresholds are 10–90% in 10-point increments. Explicit food preference never falls back to another type. Auto chooses the lowest-healing available serving sufficient to fill the deficit; otherwise the strongest, with stable item-ID ties. An exact fit avoids waste; if all adequate servings overfill (for example 6 HP missing with 8/12 HP foods), the smaller adequate serving minimizes unavoidable clamped overheal. This spells out the handoff's “without waste” wording for deficits that cannot be filled exactly.

Combat start/resume checks alive fighters. At/below threshold, eat only until **above** threshold or food is unavailable. Chronological boundaries also recheck after incoming damage, cooking/loot availability and regeneration. A lethal hit wins before healing/food; food cannot resurrect. Existing explicit development recovery remains available only in the debug harness; production-ready death recovery remains unresolved. Manual Eat works alive/below max, in or out of combat. One serving per character per simulation timestamp prevents duplicate taps at the same settled boundary; this cursor persists. Later legitimate taps can consume another serving if HP is still missing. Settings changes settle old elapsed state first.

## Settlement policy

The existing single session/clock remains authoritative. With live food and combat, a detached transaction steps to the next real event: gathering/production/enchant completion, player/enemy attack, regeneration pulse or character-specific offline cutoff. Frames before any event use the existing atomic batch path. No UI clock or per-frame food timer is introduced.

At ties: existing gathering grants, completed production outputs and ordinal next-input reservations settle first; fighters resolve in ordinal character-ID order, with player attack before enemy retaliation, existing regeneration next, then food. Death prevents both regeneration and eating. Initial/resume food checks use only already-existing stock. A Cooking output at time 30 cannot rescue a hit at time 20. Available food at a nonlethal shared boundary can be used; simultaneous shared-food contests are stable. Settlements publish bank/HP/XP/jobs/encounters only after the full transaction succeeds. Overflow/budget errors leave the original state untouched. Long jumps and equivalent uncapped subdivisions match; splitting one capped absence into multiple absences remains invalid.

Per-character 4–7h eligibility and consumed UTC boundaries remain unchanged. Food cannot extend eligibility. All offline XP follows background rules, zero without eligible effects, maximum 50%; online selected-character full XP is unchanged. Receipts record gross rewards and separate positive **Consumed** entries per character/activity. Welcome-back renders food as **Used**, never as a received reward. Defeat is reported separately. Continue only acknowledges a persisted report.

## V3 compatibility and recovery

The current player path is **Application.persistentDataPath/Idlebound/player-v3.json**, envelope version 3. It retains checksum, atomic temporary write, backup and high-water semantics. Settings persist enabled/preference/threshold and the manual request cursor; all prior character/XP, bank, gear IDs/enchantments, job reservations/partials, HP/encounter/RNG clocks and pending reports remain.

- Load v3 primary/backup/temporary evidence first. Invalid, incompatible or future evidence cannot fall back to obsolete v2/v1 and replay progress. Valid fully flushed first-write v3 temporary state is validated and promoted. Conflicting archives block for review.
- With no v3 evidence, validate the existing v2 using its exact prior catalog identity/schema. Only if no v2 evidence exists may the guarded v1 reader validate its known legacy identity. The v1 LF/CRLF identities and v2 LF-normalized identity remain unchanged.
- Convert the validated old snapshot with new default food settings. Validate before committing. Preserve original older files and copy exact primary/backup bytes to **.pre-v3** archives. The existing v1 bridge can also create its .pre-v2 archive; direct v1 → v3 does **not** write a v2 player save.
- V3 fingerprint is `SHA256("Idlebound-live-v3\n" + exactV2Identity + "\n" + LF-normalized Foods.csv)`. Unexpected prior catalog edits remain blocked; changed food balance also needs deliberate compatibility review. No catalog mismatch bypass is added.
- Converted state, offline rewards/food use, UTC and pending report commit together. Reopening the same boundary cannot reconsume food or award rewards. Missing/invalid v3 settings block rather than silently defaulting damaged current data. Old formats default only during explicit conversion.

Tests include authentic schema fixtures produced by compiling unchanged baseline Core (`0bd4601` v1 and `4e547f1` v2), not merely encoding new DTOs as old versions. The v2 fixture contains enchanted equipped gear, an ongoing enchant, Cooking, combat and a pending report. Fixtures contain synthetic progress; the user's real save and mobile-device migration are not verified here. Keep an external folder backup. Do not run old/new branches or multiple processes against the same player folder, manually rename save versions, discard progress or edit fingerprints to bypass errors.

## Tests actually run

- **614/614 standalone NUnit cases passed, zero failures/skips**: all 553 previously runnable cases plus 61 new `FoodTests` cases. NUnitLite 3.13.3/.NET 8.0.32, temporary file-backed Resources/TextAsset adapter loading the actual repository catalogs. These are not Unity Test Runner passes.
- Core compiled against .NET Standard 2.1. C# syntax, GUID/meta coverage, assembly JSON/link XML, catalog preservation, existing progression model and scoped Git checks passed.
- Added three Unity PlayMode tests: Overview/manual/config/navigation, combat summary/Bank healing detail, and cold-host persistence of preferences/stock/mid-combat state. Previous tests are preserved. Authored totals: **623 EditMode / 22 PlayMode**.

**Not run:** Unity compilation/import, nine existing Unity geometry cases, all 22 PlayMode cases, actual 360×640/720×1280 rendering/input, Android/iOS builds and device lifecycle/filesystem/AOT checks. Unity 6000.3.26f1 is unavailable in this environment. Cloud standalone execution is not Unity validation.

## Exact Windows verification

1. Stop Play. Back up the entire real save folder externally (Idlebound → Local save → Reveal real save folder). GitHub Desktop: Fetch/Pull **develop** and confirm the reported stage commits. Open the correct project in Unity **6000.3.26f1**, Assets → Refresh, wait for compilation and check Console. Preserve scenes, packages and build settings.
2. Open **Assets/Scenes/IdleboundPlay.unity**. Verify the first launch creates player-v3.json while original v2/v1 and .pre-v3 bytes remain. Note bank/XP, assignments, gear IDs/enchantments and any pending report before/after. Stop/reopen twice; no duplicated food, output, XP or report acknowledgment. If migration fails, preserve every file plus Console logs rather than reset.
3. Set Game view to **720×1280**, then **360×640**. Overview's compact food card should show HP, ON/OFF, Threshold, Food preference, available shared quantities and Eat. Check scrolling, wrapping and touch targets; the pinned activity scene and existing navigation remain usable. Food-control rows use 64 reference pixels (48 physical pixels at 360-wide Canvas scaling).
4. Fish shrimp and cook it with existing continuous production. In Bank, Cooked shrimp should show **8 HP** healing. Repeat sardine with Fishing/Cooking 10 as required by the existing recipe; its Bank detail shows **12 HP**. No new raw-to-food recipes or free stock are added.
5. Take nonlethal combat damage, stop combat if desired, then Overview → Eat. Exactly one serving disappears, HP rises/clamps and XP does not change from eating. At full/dead HP or with an empty explicit food, Eat is disabled. With a paused development clock, a duplicate same-timestamp request cannot eat twice. With normal time advancing, distinct deliberate taps each serve once while HP is missing.
6. Set explicit shrimp preference, ON, 50%. Start combat once. At/below 50% HP, it eats enough to rise above 50%, consuming actual shrimp. Set threshold 10/90 and toggle OFF/ON; other characters retain their own settings. With shrimp exhausted but sardine stocked, explicit shrimp must not fall back; Auto may use sardine. Combat card displays the live setting and shared stock.
7. Keep a cook producing while another character fights. Navigate Bank/Back and between tabs: the same session continues. With limited food, survival eventually ends normally; food is not used after a lethal hit. Preserve gear, earned XP/loot and the existing death-stop behavior. For exact same-time/future-food cases use `FoodTests` rather than relying on visual timing.
8. Save/close mid-combat with custom settings. Reopen and confirm threshold/preference/ON state, partial encounter clocks, remaining food and all other jobs. No default reset. Background/offline fighters may eat and earn loot, but zero baseline XP still applies, including selected-character offline Magic.
9. Use isolated 30-minute/10-hour offline probes after a normal v3 save has been created and closed. Probes now copy **v3** only. Verify report shows **Used N cooked food** separately from rewards, correct defeat reason, per-character 4–7h cap and no extra food/loot/XP after restarting or Continue. Test HP Regeneration with real enchanted gear: existing pulse value is unchanged, food/regen cannot revive. Preserve original save before any recovery/corruption experiments.
10. Window → General → Test Runner: run all **623 EditMode**, then **22 PlayMode** cases in its isolated test scene. Export XML and Console logs. New tests use isolated directories/hosts, never the real save. Re-run the existing gathering, continuous production, equipment, enchanting, Bank return and welcome tests.

Batch runner (close the interactive Editor for this project first): set `$unity` to `C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe`, `$project` to the Windows checkout and `$results` to an existing folder outside Git. Run each process to completion before starting the next:

```powershell
& $unity -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testResults "$results/EditMode.xml" -logFile "$results/EditMode.log"
& $unity -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testResults "$results/PlayMode.xml" -logFile "$results/PlayMode.log"
```

Inspect actual XML counts/errors and logs. Do not substitute cloud results for Unity execution. Verify Android/iOS AOT, atomic file replacement, suspension/force-stop/relaunch and save permissions on real devices.

## Remaining limitations

Healing numbers and the Auto minimal-overheal interpretation are playtest policies. Food is account-wide; there is no per-character reservation, alternate inventory or unlimited supply. Dead characters still need a future production recovery design. No food cooldown, combat penalty, buy/sell, new recipes, cloud saves, boss systems or stat changes are introduced. The event budget is a protective limit; unusually expensive custom catalogs/very long online jumps can fail atomically and require smaller settlement chunks. The event transaction uses detached snapshots at real boundaries; profile actual mobile allocation/performance before release. Real-save/device migration and rendered UI require Windows/device acceptance.
