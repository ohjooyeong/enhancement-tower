# Decision register (current, 2026-10-22)

## LOCKED DIRECTION (design-level; prototype validation still required)
- Portrait-mobile-first 2D handcrafted pixel-art, forward-looking quarter-view, camera orientation static (position can track modestly).
- Lane-free autonomous movement in bounded area; primary monster arrival from ahead, not omnidirectional bullet-hell. Neither tower game mode has manual player movement.
- **Boss uses same camera/movement/targeting core**; adds warnings, side-step, charge, turn-around and recovery. Passing positions is permitted naturally, not scripted swap. Four-direction visual fallback can be prototyped; animation quality needs review.
- 2026-10-22 USER-APPROVED: replace primary-target + reduced splash with attack-specific direct multi-target swings. Slashes select up to 3 nearest enemies in the hero's forward geometry with equal base damage; thrust selects 1 in a narrow forward shape with higher damage. No random victim selection. Each enemy can be hit once per swing, with a shared total target budget across the active window. Attack variants have different timing, shape, damage, knockback and presentation. No mandatory body lunge.
- Prototype combo order (diagonal slash → reverse slash → thrust), angles, damage multipliers and timings remain tunable assumptions, not a locked three-hit roster for every weapon.
- 2026-10-22 USER-APPROVED: preserve the fixed baseline combo and add a conditional breakthrough slash only after sustained crowd/wall blocking. Push first, then advance into collision-checked open space; no hero phasing, forced escape or attack-cancel invulnerability. Movement decisions use enemy threat, distance, attack readiness, walls and re-engagement value; no scripted left/right alternation or perfect dodge. Thresholds, weights, push/advance lengths and cooldown remain tunable.
- 2026-10-22 USER-APPROVED UPDATE: valid AI movement routes and focus targets use seeded weighted probability, favouring better scores without always selecting the best. Movement can choose a shorter or longer bounded step. Obstacles, retreat limits, cooldowns and action locks remain hard constraints. Attack victim selection inside the swing remains nearest-first geometry, not random damage lottery. Movement temperature 0 restores best-route selection and target temperature 0 restores the prior nearest-focus selection; default temperatures and eligibility bands are prototype tuning values.
- Monster-monster mild overlap; monster-player foot center separation; no endless backward kite; at retreat limit re-engage and fight.
- Intro: initial Challenge entry may briefly establish hero location and enemies. User toggle to disable + skip, no full 50 enemy objects in advance; persistent Farming omits recurring intro. Visual ahead silhouettes are optional enhancement; gradual spawns are first implementation.
- Challenge finite; Farming endless + offline settlement; Rage manual Farming-only; same art/scene logic with different flow rules.
- Weapon/armor/rings2/orb/auto skill loadout; no character EXP/level and no skill leveling at MVP; RP rebirth progression.
- Enhancement no item destruction; pity/regression/blessing logic as defined in enhancement GDD.

- 2026-10-22 USER-APPROVED: add configurable minimum/maximum damage, critical chance/multiplier and per-defender DEF to the prototype. Victim geometry and target caps remain unchanged. Each eligible hit rolls independently using a seeded damage stream separate from tactical choices; equal slash base coefficients do not guarantee identical final damage.

## PROTOTYPE ASSUMPTIONS, NOT LOCKED NUMBERS
- Finite encounter prototype: initial 5, one arrival per 1.2s, max 8 living, total 50, limit 60s. This single-arrival refill is a provisional alternative to ~5-enemy waves for pressure testing. Occupied spawn points defer arrivals; no catch-up burst. Clear requires no pending reservations and no living enemies. Death/timeout freeze the encounter; no floor/reward/auto-retry flow yet.
- Fast melee prototype: shared ground AI/foot collision, lower HP/damage, higher approach speed, shorter windup; user feedback led to .16s windup/.32s recovery and threat 1.35 to add contact pressure; configurable `FastMonster.asset`. Normal → Fast → Normal → Heavy is an authored comparison roster, not final stage composition. No flying/phasing or new hero attack-speed progression is implied.
- Damage variance 85–115%, hero/monster critical chance 15%/5%, critical multiplier 1.5, DEF hero/normal/heavy 1/0/3. Integer uniform roll → action/critical multipliers → rounding → flat DEF subtraction → minimum 1 is provisional, not the final progression formula.
- 60-sec challenge / 50 kills / concurrent ~5 (stress-test 8 and 12), 1-second contact interval per monster, starting ASPD 1.0–1.1/s, S1 cap ~1.5/s.
- Per-action slash/thrust angles, reach, active windows, combo ordering and damage/knockback multipliers, hit-stop frames, knockback meters, boss charge speed, AI search/reposition cooldown, boundary shape and camera follow threshold.
- Boss collision/path/telegraph geometry and four-vs-eight-direction animation output.
- Intro time/skip details and Farming scrolling illusion vs walking to next segment.
- Economy rates, rebirth point curve, equipment upgrade and item counts, Day30 top 200 floors targets are balancing hypotheses.

- 2026-10-22 user-approved prototype: attack knockback may temporarily overlap/pass through other monsters. Arena walls and hero foot-center separation remain enforced; no attack-cancel stun or hero phasing is implied. Distance/cooldown remain tuning assumptions.

## DEFERRED
- Ranged regular enemies and ranged weapons, complex boss types, future towers/theme sectors, multiplayer shared hub, server economy, live ops, guilds, PvP, cosmetic titles, full collection, additional rarities.
- Whether telegraph avoidance uses explicit reaction probability; favor physics/geometric safe spot plus movement constraints, not arbitrary perfect dodge.

## TERMINOLOGY (2026-10-22)
- Current basic attacks: **forward multi-target slash** / **single-target thrust**, using the hero as the geometry origin. A slash's cap is 3 total, not primary + 3 extras. Check nearest eligible unhit enemies at each active tick; already hit targets are never replaced or hit again by that action.
- “Cleave expansion” / “explosion + cleave” remaining in historical orb or supplement examples describes possible future modifiers only. It does not restore reduced secondary damage or approve a specific splash formula; those systems require their own later design pass.
- Canonical attack rules: `04_COMBAT.md`; presentation: `23_COMBAT_PRESENTATION_2026_10.md`; implementation and current authored values: `26_COMBAT_PROTOTYPE.md` / `Assets/Data/LongswordCombo.asset`.

## LEGACY CONFLICT RESOLUTION
- The old 100% primary / 35% nearby cleave examples are superseded by the user-approved per-swing direct multi-target design above. Historical progress notes are not current attack rules.
- Historic central-fixed prototype suggestions are **superseded** by lane-free movement.
- Historic fixed three-lane suggestions are **rejected**, not superseding free movement.
- Any older suggestion that the hero must always show their back is **rejected**; turn to target without rotating camera.
- Old 'boss doesn't move' or 'boss separate view' are superseded for first charge-boss prototype; ultimate boss roster remains open.
- Source order: this file > `23_COMBAT_PRESENTATION_2026_10.md` (camera/combat) > `04_COMBAT.md`, `05_AUTO_POSITIONING.md` and canonical GDD > older supplements/ADR. If there is a contradiction outside combat, preserve core GDD pending discussion.
