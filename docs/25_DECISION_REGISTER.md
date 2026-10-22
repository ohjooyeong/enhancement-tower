# Decision register (current, 2026-10-21)

## LOCKED DIRECTION (design-level; prototype validation still required)
- Portrait-mobile-first 2D handcrafted pixel-art, forward-looking quarter-view, camera orientation static (position can track modestly).
- Lane-free autonomous movement in bounded area; primary monster arrival from ahead, not omnidirectional bullet-hell. Neither tower game mode has manual player movement.
- **Boss uses same camera/movement/targeting core**; adds warnings, side-step, charge, turn-around and recovery. Passing positions is permitted naturally, not scripted swap. Four-direction visual fallback can be prototyped; animation quality needs review.
- Primary slash front-diagonal; cleave facing-dependent wide forward arc, around 180° candidate. Lunge is not guaranteed basic combo.
- Monster-monster mild overlap; monster-player foot center separation; no endless backward kite; at retreat limit re-engage and fight.
- Intro: initial Challenge entry may briefly establish hero location and enemies. User toggle to disable + skip, no full 50 enemy objects in advance; persistent Farming omits recurring intro. Visual ahead silhouettes are optional enhancement; gradual spawns are first implementation.
- Challenge finite; Farming endless + offline settlement; Rage manual Farming-only; same art/scene logic with different flow rules.
- Weapon/armor/rings2/orb/auto skill loadout; no character EXP/level and no skill leveling at MVP; RP rebirth progression.
- Enhancement no item destruction; pity/regression/blessing logic as defined in enhancement GDD.

## PROTOTYPE ASSUMPTIONS, NOT LOCKED NUMBERS
- 60-sec challenge / 50 kills / concurrent ~5 (stress-test 8 and 12), 1-second contact interval per monster, starting ASPD 1.0–1.1/s, S1 cap ~1.5/s.
- Diagonal slash arc and forward 180° cleave exact angle/range, hit-stop frames, knockback meters, boss charge speed, AI search/reposition cooldown, boundary shape and camera follow threshold.
- Boss collision/path/telegraph geometry and four-vs-eight-direction animation output.
- Intro time/skip details and Farming scrolling illusion vs walking to next segment.
- Economy rates, rebirth point curve, equipment upgrade and item counts, Day30 top 200 floors targets are balancing hypotheses.

- 2026-10-22 user-approved prototype: attack knockback may temporarily overlap/pass through other monsters. Arena walls and hero foot-center separation remain enforced; no attack-cancel stun or hero phasing is implied. Distance/cooldown remain tuning assumptions.

## DEFERRED
- Ranged regular enemies and ranged weapons, complex boss types, future towers/theme sectors, multiplayer shared hub, server economy, live ops, guilds, PvP, cosmetic titles, full collection, additional rarities.
- Whether telegraph avoidance uses explicit reaction probability; favor physics/geometric safe spot plus movement constraints, not arbitrary perfect dodge.

## LEGACY CONFLICT RESOLUTION
- Historic central-fixed prototype suggestions are **superseded** by lane-free movement.
- Historic fixed three-lane suggestions are **rejected**, not superseding free movement.
- Any older suggestion that the hero must always show their back is **rejected**; turn to target without rotating camera.
- Old 'boss doesn't move' or 'boss separate view' are superseded for first charge-boss prototype; ultimate boss roster remains open.
- Source order: this file > `23_COMBAT_PRESENTATION_2026_10.md` (camera/combat) > `04_COMBAT.md`, `05_AUTO_POSITIONING.md` and canonical GDD > older supplements/ADR. If there is a contradiction outside combat, preserve core GDD pending discussion.
