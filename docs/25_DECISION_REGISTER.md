# Decision register (current, 2026-10-22)

## 2026-10-23 USER-APPROVED level / quest / two auto skills — current

- 캐릭터 레벨/몬스터 처치 경험치를 도입한다. 레벨 상한은 존재하며 **Lv.50은 우선 검토 후보**다. 환생 후에도 레벨을 유지한다. 기본 능력치의 보조 성장과 콘텐츠·스킬 해금이 역할이며 장비 강화가 주요 전투력 성장축이다. RP는 별도 환생 영구 성장으로 유지한다.
- 기본 경험치는 전투로 획득하며 퀘스트 완료 여부로 정상적인 레벨업을 막지 않는다. 메인 퀘스트는 세계관·시스템 안내, 성장 퀘스트는 일반 플레이 목표·추가 경험치 또는 보상, 업적은 장기 기록·선택적 보상을 담당한다. 보상량/해금 레벨/경험치 곡선은 미확정이다.
- 장착 구조는 무기1/갑옷1/반지2/오브1/**자동 스킬2슬롯**이다. 무기가 기본 공격·선호 거리·템포·넉백 등 전투 스타일의 중심이며 오브와 자동 스킬이 빌드를 보완한다. 두 슬롯의 해금 시점·동시 장착 제한·발동 우선순위·재사용 대기 규칙은 미확정이다. 별도 스킬 레벨/전직/PvP 시스템의 신규 도입은 승인하지 않았다.
- 첫 시각적 공격 스킬 후보는 **전방 검기 방출**이다. 검을 휘둘러 전방으로 검기가 날아가 적을 공격하는 방향이며 이름/범위/관통 수/피해/쿨다운/강화 단계별 이펙트 변화 및 공용·무기 전용 분류는 미확정이다. 후보 등록이며 구현 완료가 아니다.
- 자동 스킬은 보스의 **수동 집중 방어**를 자동 발동/자동 회피/자동 무적으로 대체하지 않는다. Rage는 Farming 전용 별도 수동 기능이다. 기존 레인 없는 자동 이동·고정 카메라·기본 공격 판정과 비주얼 목표를 유지한다.
- 이번 승인은 **문서 설계 변경만**이다. 레벨/경험치/퀘스트/자동 스킬2개 실행, ScriptableObject 구조, Unity 씬, 기존 전투 코드를 변경하지 않는다. 구현 차이는 [29의 미충족 목록](29_LEVEL_QUEST_SKILL_SYSTEM.md#7-현재-구현과-신규-요구사항의-차이)에 분리했다.
- 상세 기준은 [29_LEVEL_QUEST_SKILL_SYSTEM.md](29_LEVEL_QUEST_SKILL_SYSTEM.md), 배경은 [ADR-007](adr/ADR-007-character-level-and-unlocks.md)이다. [ADR-005](adr/ADR-005-no-character-level.md)의 무레벨 결정과 이전 자동 스킬1슬롯을 대체한다. 이번 범위에서는 본 절 >29 > 관련 GDD > 옛 supplement/ADR 순으로 해석한다. 기존 전투 표현은23, 비주얼 기술 검토는28, 실제 구현 이력은26을 따른다. 구현 이력은 최신 설계 완료의 근거가 아니다.

## 2026-10-23 USER-APPROVED visual goal — current

> 약 4등신 캐릭터와 입체감 있는 쿼터뷰 환경으로 구성한 2.5D풍 픽셀 아트 RPG. 레인 없는 전방 중심 자유 자동전투를 사용하며 카메라 방향은 고정한다.

- 확정 대상은 **시각적 목표**다. 현대적인 수제 도트, 약4등신 인물, 바닥·벽·기둥·그림자·전후 가림으로 깊이와 높낮이가 읽히는 공간을 지향한다. 평평한 도형 화면은 현재 검증용 View이며 최종 목표가 아니다.
- **렌더링 기술 선택은 미확정**이다. 2D 스프라이트/레이어 기반, 2D 캐릭터+3D 환경, 3D 기반 픽셀 표현 중 어느 것도 이번 승인으로 채택하지 않는다. 현재 Universal2D 설정은 구현 현황이며 영구 기술 제약이 아니다.
- 카메라는 플레이 도중 방향/기울기를 회전시키지 않는다. 고정된 초기 각도, 투영 방식, 논리 좌표의 View 투영과 PPU/기준 해상도는 후속 비교에서 결정한다. 일반전/보스는 같은 카메라·이동 구조를 쓴다.
- 기존 순수 C# 고정 틱 전투, 레인 없는 자유 자동 이동, 밀집/발 중심 충돌/넉백, 보스 돌진·재교전과 **수동 집중 방어**를 유지한다. 자동 돌진 회피를 기본으로 복구하지 않는다.
- 높낮이는 우선 시각적 표현이다. 실제 경사 이동/다층 충돌/고저차 피해·사거리/점프는 승인된 전투 기능이 아니다. View의 높이로 전투 좌표를 바꾸지 않는다.
- 아래의 초기 `2D handcrafted pixel-art` 표현은 이전 명칭이다. 최신 목표는 위 문장으로 통일하며, 과거2D 표현을 평면 아트 또는 2D 렌더러만 허용하는 결정으로 해석하지 않는다. 기존 결정 이력은 보존한다.
- 기술 검토와 문서 충돌 목록: [28_VISUAL_DIRECTION_AND_VIEW_REVIEW.md](28_VISUAL_DIRECTION_AND_VIEW_REVIEW.md). 최종 아트/전체 렌더링 전환/전투 코드 재작성은 별도 승인 전 진행하지 않는다.


## LOCKED DIRECTION (design-level; prototype validation still required)
- Portrait-mobile-first 2D handcrafted pixel-art, forward-looking quarter-view, camera orientation static (position can track modestly).
- Lane-free autonomous movement in bounded area; primary monster arrival from ahead, not omnidirectional bullet-hell. Neither tower game mode has manual player movement. Boss encounters allow the explicit defensive input approved on 2026-10-23.
- **Boss uses same camera/movement/targeting core**; adds warnings, side-step, charge, turn-around and recovery. Passing positions is permitted naturally, not scripted swap. Four-direction visual fallback can be prototyped; animation quality needs review.
- 2026-10-22 USER-APPROVED: replace primary-target + reduced splash with attack-specific direct multi-target swings. Slashes select up to 3 nearest enemies in the hero's forward geometry with equal base damage; thrust selects 1 in a narrow forward shape with higher damage. No random victim selection. Each enemy can be hit once per swing, with a shared total target budget across the active window. Attack variants have different timing, shape, damage, knockback and presentation. No mandatory body lunge.
- Prototype combo order (diagonal slash → reverse slash → thrust), angles, damage multipliers and timings remain tunable assumptions, not a locked three-hit roster for every weapon.
- 2026-10-22 USER-APPROVED: preserve the fixed baseline combo and add a conditional breakthrough slash only after sustained crowd/wall blocking. Push first, then advance into collision-checked open space; no hero phasing, forced escape or attack-cancel invulnerability. Movement decisions use enemy threat, distance, attack readiness, walls and re-engagement value; no scripted left/right alternation or perfect dodge. Thresholds, weights, push/advance lengths and cooldown remain tunable.
- 2026-10-22 USER-APPROVED UPDATE: valid AI movement routes and focus targets use seeded weighted probability, favouring better scores without always selecting the best. Movement can choose a shorter or longer bounded step. Obstacles, retreat limits, cooldowns and action locks remain hard constraints. Attack victim selection inside the swing remains nearest-first geometry, not random damage lottery. Movement temperature 0 restores best-route selection and target temperature 0 restores the prior nearest-focus selection; default temperatures and eligibility bands are prototype tuning values.
- Monster-monster mild overlap; monster-player foot center separation; no endless backward kite; at retreat limit re-engage and fight.
- Intro: initial Challenge entry may briefly establish hero location and enemies. User toggle to disable + skip, no full 50 enemy objects in advance; persistent Farming omits recurring intro. Visual ahead silhouettes are optional enhancement; gradual spawns are first implementation.
- Challenge finite; Farming endless + offline settlement; Rage manual Farming-only; same art/scene logic with different flow rules.
- Current loadout: weapon1/armor1/rings2/orb1/auto skills2. Character level/EXP supports stats and unlocks, persists through rebirth; equipment enhancement remains primary and RP remains separate. See the current approval above; independent skill leveling is not newly approved.
- Enhancement no item destruction; pity/regression/blessing logic as defined in enhancement GDD.

- 2026-10-22 USER-APPROVED: add configurable minimum/maximum damage, critical chance/multiplier and per-defender DEF to the prototype. Victim geometry and target caps remain unchanged. Each eligible hit rolls independently using a seeded damage stream separate from tactical choices; equal slash base coefficients do not guarantee identical final damage.

## PROTOTYPE ASSUMPTIONS, NOT LOCKED NUMBERS
- Finite encounter prototype: initial 5, one arrival per 1.2s, max 8 living, total 50, limit 90s (user-adjusted from 60s on 2026-10-22). This single-arrival refill is a provisional alternative to ~5-enemy waves for pressure testing. Occupied spawn points defer arrivals; no catch-up burst. Clear requires no pending reservations and no living enemies. Death/timeout freeze the encounter; no floor/reward/auto-retry flow yet.
- Fast melee prototype: shared ground AI/foot collision, lower HP/damage, higher approach speed, shorter windup; user feedback led to .16s windup/.32s recovery and threat 1.35 to add contact pressure; configurable `FastMonster.asset`. Normal → Fast → Normal → Heavy is an authored comparison roster, not final stage composition. No flying/phasing or new hero attack-speed progression is implied.
- Damage variance 85–115%, hero/monster critical chance 15%/5%, critical multiplier 1.5, DEF hero/normal/heavy 1/0/3. Integer uniform roll → action/critical multipliers → rounding → flat DEF subtraction → minimum 1 is provisional, not the final progression formula.
- 90-sec challenge / 50 kills / concurrent ~5 (stress-test 8 and 12), 1-second contact interval per monster, starting ASPD 1.0–1.1/s, S1 cap ~1.5/s.
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

## 2026-10-23 charge-boss prototype assumptions
- First implementation uses fixed warning/path, existing swept foot collision, stop-and-hit once on blocked charge (2.5x), recovery 1.1s/cooldown4s, maximum sidestep .8 and full boss knockback resistance. Warning1.1s/speed5/distance3.2 are configurable. These are prototype defaults, not newly locked design. Boss comparison mode does not yet include stage timeout/rewards. See 26_COMBAT_PROTOTYPE.md.

## 2026-10-23 USER-APPROVED boss pressure cycle
- User approved strengthening the first boss with melee pressure → fixed linear charge → short punish window → turn/reacquire → forward sweep → melee, plus faster tempo below half HP. Keep the existing camera, bounded foot collision, action locks and no guaranteed dodge.
- Prototype defaults superseding the earlier recovery/cooldown defaults: post-charge opening .55s, reacquire up to2s, sweep150 degrees/range1.55x/windup.4s/active.16s/recovery.65s/damage1.8x (one hit per action). Normal melee1.5x; scene boss base damage1.6x normal, movement1.55, normal windup/recovery.32/.48s. Initial charge wait3s; later wait3.4s. Half HP: warning.8x, melee timing.85x, sweep windup.32s, later charge wait2.6s. Timing is captured when an action begins. These numbers remain tuning assumptions, not final balance.

## 2026-10-23 USER-APPROVED limited boss intervention
- Boss encounters require player attention as a gameplay direction: automatic approach/basic attacks continue, while responding to major attacks belongs to the player. No joystick, manual basic attacks, camera change, or Farming Rage in bosses.
- This supersedes automatic charge sidestepping as the default boss response. Ordinary combat/Farming retain autonomous play. Idle boss victory is not a guaranteed feature; neither is mandatory defeat regardless of equipment. Do not implement an arbitrary input-check instant kill.
- First configurable-code prototype: one Focus Guard button. A press queues a command for the next simulation tick; .45s duration, 4s cooldown from activation, incoming boss damage reduced by70% after damage/DEF calculation, rounded up, minimum1. It can overlap an attack without cancelling it; no movement, invulnerability, counterattack or automatic timing. Early presses waste the window. Numbers and the choice of guard over dodge are validation assumptions.
- Keep the previous auto-dodge available only through an explicit simulation comparison option (`manualDefense: false`), not the default screen behavior. Restart creates fresh input/timer state.
- World explanation extends the existing synchronization in01: trained instinct handles ordinary combat; a guardian's deliberate major attack requires conscious focus to reinforce that response. This is an extension, not loss of synchronization or tower mind control. A separately named storybook was not located; reconcile with it when its source is supplied.
- Future auto-climb must pause at an uncleared boss for deliberate entry, rather than silently continuing unattended retries. Boss entry UX, repeated clears/offline handling and rewards remain unimplemented and require their own slice.

## 2026-10-23 USER-APPROVED View comparison implementation
- User authorized development of the minimal comparison after the28 review. Implement the 2D pixel-placeholder character + simple3D environment candidate alongside the original2D View, keeping the same simulation instance during view switches.
- This authorizes a comparison slice, not final art, a permanent renderer choice or full project migration. Fixed40-degree Orthographic camera, code-drawn24x48/40PPU silhouettes and simple walls/pillars are tuning assumptions.
- A Universal Renderer is appended as a camera-selectable option in the existing RP asset; default Renderer2D/quality selections remain unchanged. The comparison scene selects it only for the hybrid View. Authored material references retain the required shaders for builds.
- Pure combat code, definitions, existing scene settings and manual Focus Guard are preserved. The new environment has no colliders and no actual height gameplay. Rendering/Editor validation status and user steps are recorded in26.

## Superseded growth/loadout wording (history retained)
- 이전 문구: `no character EXP/level and no skill leveling at MVP; RP rebirth progression`. 이 중 캐릭터 무레벨/무경험치와 RP만으로 모험가 성장을 설명한 부분은2026-10-23 레벨 도입 결정으로 대체한다. 스킬 자체 레벨 도입은 별도 승인하지 않았다.
- 이전 자동 스킬1슬롯은2슬롯으로 대체한다.15/21의 오래된 오브2슬롯은 기존 오브1슬롯 기준과 충돌하던 표기이며 이번 변경으로 오브가2개가 되는 것은 아니다. 오브1/자동 스킬2로 정합성을 맞춘다.
- ADR-005 원문은 삭제하지 않고 Superseded 상태와 ADR-007 링크를 둔다.26의 과거 구현·테스트 기록과28의 View 검토는 그대로 보존한다.

## 2026-10-23 Milestone 2 equipment/enhancement implementation approval
- User authorized a small playable equipment/enhancement slice after the first combat prototype. Build weapon/armor definitions and runtime instances, equip-to-combat stats, success/failure handling and an observable retry comparison; do not implement levels, quests, two-skill execution, new weapon styles or new combat patterns in this task.
- Use configurable local prototype rates/costs and linear stat bonuses; preserve no destruction, +1 success/+2 blessing, unchanged failure/rare -1 regression and accumulating pity reset on success. Defaults are test assumptions, not final economy formulas.
- Local session-only wallet/gear and a finite material-test encounter may support the comparison; they are not authoritative economy, persistent saves, full Farming or offline settlement. No backend is added. Gear applies when creating a new combat run, not by mutating an active Fighter or static SO definitions.
- Scope, default values, input-to-damage flow, implementation limits and verification are recorded in [30_EQUIPMENT_ENHANCEMENT_PROTOTYPE.md](30_EQUIPMENT_ENHANCEMENT_PROTOTYPE.md). Earlier growth documentation-only scope remains history for level/quest/skill work, not a prohibition on this separately requested equipment slice.
