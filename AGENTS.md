# AGENTS.md — EnhancementTower

## 0. Mission
Build a playable, mobile-first Unity 6 / C# idle action RPG **강화의 탑**. Work incrementally. Prioritize correct, testable combat over speculative features or polished placeholder art.

## 1. Mandatory reading before tasks
- Start at `docs/INDEX.md`.
- Read `docs/23_COMBAT_PRESENTATION_2026_10.md` for current camera/movement/animation/boss/encounter rules.
- Read `docs/25_DECISION_REGISTER.md` for decision status and conflicts.
- Read `docs/29_LEVEL_QUEST_SKILL_SYSTEM.md` before growth/quest/skill work. The latest level/quest/two-slot approval in25 >29 > related GDD > old supplements/ADRs applies within that scope. Character level is not weapon level, skill level, floor, or RP.
- Read related canonical docs (`04_COMBAT`, `05_AUTO_POSITIONING`, `07_TOWER_CHALLENGE`, `08_FARMING`, `11_WEAPON_SYSTEM`, `12_EQUIPMENT`, `13_ENHANCEMENT`, `15_ORBS_AND_SKILLS`, `16_REBIRTH`, `18_SERVER_ARCHITECTURE`) before touching those systems.
- If old documents conflict, priority: `25_DECISION_REGISTER` > `23_COMBAT_PRESENTATION_2026_10` for combat presentation/positioning > current topic's GDD > `supplement_*` > historical ADR. Do not silently overwrite existing design; update the register and relevant doc when user approves a change.

## 2. Locked gameplay direction
- **No fixed lanes.** In tower combat player movement is autonomous in a bounded 2D field; enemies mostly emerge from the visual forward/top. No manual joystick in the tower. Bosses retain automatic movement/attacks but allow limited manual defensive input (2026-10-23 user-approved); do not automatically solve major boss attacks.
- **Same movement and camera architecture in standard fights and bosses**. Bosses only add telegraphed attacks, charge, sidesteps, turn/reacquire, and recovery. They may end up behind the hero; no forced position swap or teleport.
- Camera has fixed **orientation** in quarter-view, no gameplay camera rotation; position may follow gently/scroll by segments. Rendering technique, initial fixed angle and projection remain undecided. The player can show front/back/side sprites according to facing; not back-view-only.
- Regular attacks use per-action geometry: forward slashes directly hit up to 3 nearest eligible enemies with equal base damage; narrow forward thrust hits 1 harder. No primary-target + reduced splash or random victim selection. Each enemy is hit once per action, with a total target budget across the active window. The prototype slash → reverse slash → thrust order and numerical values remain tunable; conditional breakthrough slash may push nearby enemies then advance only into physically open space after sustained blocking; **not** mandatory combo movement, phasing, or guaranteed escape. Tactical movement and focus targeting sample eligible candidates with seeded score-weighted probabilities; favour better choices while permitting lower-ranked legal ones. Distance is bounded, and attack windows/victim geometry stay deterministic given inputs. Do not alternate sides or reroll every frame.
- Aim and movement vectors are distinct. Hit detection is derived from logic and attack arcs, not VFX appearance or animation frames. Combat simulation is framerate independent.
- Monsters accumulate if kill rate < spawn pressure; limited lateral reposition and hard retreat limit; no infinite kiting. Player and monsters must not overlap at foot-center; limited monster-monster overlap allowed. No guaranteed perfect auto-dodge.
- Normal ranged projectiles generally handled by build/defense; elite AoE may permit limited geometry-based sidestep. Boss major attacks default to manual Focus Guard, not automatic charge avoidance. Avoid impossible/unreadable patterns.
- Challenge: finite stage (v0 ~50 enemy kills, ~5 concurrent baseline, ~90 sec test), no Rage. Clear→next; fail twice→appropriate Farming.
- Farming: persistent spawns, no per-wave intro, repeatable forward illusion/segments, offline settlement, optional encounter-intro toggle; Rage is manual and **Farming-only**.
- Hub navigation may be manual; tower combat is autonomous.
- Visual goal (2026-10-23 user-approved): **약 4등신 캐릭터와 입체감 있는 쿼터뷰 환경으로 구성한 2.5D풍 픽셀 아트 RPG. 레인 없는 전방 중심 자유 자동전투를 사용하며 카메라 방향은 고정한다.** Modern handcrafted pixel art; rendering technology remains undecided (see `docs/28_VISUAL_DIRECTION_AND_VIEW_REVIEW.md`). No final art, full renderer migration or combat rewrite before separate approval; no shiny generic dark-fantasy UI. Character level/EXP is now approved design; independent skill leveling is not newly approved.

## 3. Equipment & progression invariants
- Slots: 1 weapon, 1 armor, 2 rings, 1 orb, **2 auto skills**. Unlock timing and execution scheduling remain undecided. Rage and manual boss Focus Guard are separate.
- Season 1: longsword + greatsword; weapon-specific preferred range, swing timing, arc, knockback. Future weapons are not MVP.
- Character level/kill EXP is approved (2026-10-23): capped, retained after rebirth, supports basic stats and content/skill unlocks. Lv.50 is a candidate, not a fixed cap. Combat supplies baseline EXP independently of quest completion. Enhancement remains the primary power axis; RP remains separate permanent rebirth growth. Main quests guide, growth quests add goals/rewards, achievements track long-term records. See `docs/29_LEVEL_QUEST_SKILL_SYSTEM.md` and ADR-007 (supersedes ADR-005).
- Enhancement never destroys item: success +1; blessing on success may +2; failure remains or rare regression -1; pity success bonus accumulates on failure and resets on success. Exact rates/costs remain tunable.
- Equipment inventory/economy RNG and rewards must eventually be server authoritative; **do not build backend in prototype phase**.

## 4. Unity implementation rules
- Target a Unity 6 version pinned in `ProjectSettings/ProjectVersion.txt`, same exact version across Mac and Windows. Current prototype uses URP/Universal 2D. This is an implementation fact, not a locked final renderer. Review alternatives before changing the pipeline.
- Organize `Assets/Scripts` into `Core`, `Combat`, `AI`, `Progression`, `Data`, `Presentation`, `UI`, `Infrastructure`; `Assets/Scenes`, `Assets/Prefabs`, `Assets/Art`, `Assets/Tests`.
- Use plain C# domain models for combat/balance; MonoBehaviour for composition/Unity-specific views; ScriptableObject for authored static definitions. Never mutate ScriptableObject definitions to store run/save state.
- AI state machine baseline `SEARCH → APPROACH → ATTACK → REPOSITION → SEARCH`; boss extends shared movement with telegraph/charge/reacquire/sweep and manual Focus Guard. `AVOID_TELEGRAPH` is a legacy comparison option, not the default boss response.
- Treat `CombatClock`, `AttackResolver`, `HitResolver`, `TargetSelector`, `MovementController`, `SpawnDirector`, `CombatPresentation` as separable responsibilities; names are suggestions, not an obligation to generate classes without need.
- Use object pooling only where profiling justifies it; avoid `FindObjectOfType`/scene-wide searches or allocations in hot loops; use deterministic simulated time and seeded randomness when testing.
- Keep sprite facing/depth sorting and collider foot positions explicit; visual VFX never defines combat ranges.
- Author `.meta` files via Unity; retain all `.meta` files in Git. Do not commit `Library/`, `Temp/`, `Obj/`, `Logs/`, builds or machine-specific configs.
- No OS-specific absolute paths; verify both macOS and Windows compatibility.

## 5. Work process for AI coding agents
1. Read relevant docs and existing project code; summarize scope and possible contradictions.
2. Implement **one small vertical slice** per change; do not scaffold every proposed system at once.
3. Preserve unrelated code/assets. Avoid breaking Unity serialization by renaming classes/fields without migration.
4. When Unity Editor is unavailable, clearly mark compile/runtime behavior as **not verified**; do not claim successful Play Mode tests.
5. Add EditMode tests for pure calculations when practical; make manual Play Mode verification checklist.
6. After every task report: files added/changed; tested vs untested; design assumptions; steps user must do in Unity Editor; next smallest step.
7. If a proposed implementation requires an unresolved core design choice, prefer a configurable simple default and write the assumption in docs. Ask only for blocking decisions.

### 커밋 메시지 규칙 (2026-10-22 사용자 지정)
- 앞으로 작성하는 커밋은 **영문 유형 접두사 + 한글 설명**으로 쓴다. 본문도 한글로 쓰되 파일명·클래스명·API 등 고유한 기술 식별자는 원문을 유지한다.
- 제목 형식은 `type: 한글 변경 내용`으로 통일한다. 유형은 `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `chore`, `build`, `ci` 중 적절한 것을 사용한다.
- 제목은 무엇이 달라졌는지 구체적이고 간결하게 쓴다. 예: `feat: 이동과 타겟 선택에 확률 가중치 적용`, `fix: 구석에서 공격이 멈추는 문제 해결`, `docs: 전투 설계와 구현 안내 갱신`.
- 본문이 필요한 변경은 이유와 검증 결과·미검증 항목을 한글로 적는다. 단순한 변경에는 불필요한 본문을 붙이지 않는다.
- 검증과 문서 정리가 끝난 의미 있는 단위로 커밋하며, 사용자 변경이나 관련 없는 파일을 섞지 않는다.
- 기존 커밋 메시지는 별도 요청 없이 재작성하지 않는다. 원격 푸시는 사용자가 요청할 때만 한다.

## 6. First milestone (no server, store, final graphics)
- `CombatPrototype` scene with placeholder sprites, fixed-angle camera, one hero, one melee monster.
- Target/approach, facing 4 dirs, attack timing, front diagonal slash and damage, hit reactions, death.
- Validate forward multi-target slash / reverse slash / single-target thrust, then 5→8→12 enemies stress test and overlap/retreat-limit behavior.
- Add one boss: telegraphed linear charge, manual Focus Guard, collision-safe movement and turn/re-engage when positions naturally cross. Preserve the legacy sidestep comparison without restoring it as default.
- Add basic stage spawn/clear, minimal HUD, encounter intro skip toggle.
- Validate at portrait phone aspect ratios and performance before adding permanent progression.

## 7. Forbidden shortcuts
- Do not replace free-forward movement with 1/3/4 rigid lanes.
- Do not make boss combat change to sidescroller, chase-camera, or rotate-camera without explicit approval.
- Do not add manually controlled tower movement, unapproved independent skill leveling, forced Rage in Challenge, or unlimited dodge/knockback stun locks. Do not make auto skills replace manual boss Focus Guard. PvP and class-change systems remain deferred.
- Do not treat examples/ranges as approved economy formulas.
- Do not hallucinate finished assets, test results, server APIs, or prior implementation.

## 8. Current growth documentation scope (2026-10-23)
This change authorizes Markdown updates only. Do not implement level/EXP calculations, quest progress/rewards, two-skill execution, ScriptableObject schema changes, Unity scene changes, or a combat rewrite as part of it. Implementation requires a separate task; report the current gaps rather than presenting approved design as shipped behavior. Preserve prior ADRs and prototype validation history.

## 9. Milestone 2 equipment scope (2026-10-23)
The user separately authorized weapon/armor equip + enhancement + real combat stat comparison. See `docs/30_EQUIPMENT_ENHANCEMENT_PROTOTYPE.md`. Use static definitions and separate session state, configurable prototype numbers and no backend. The preceding documentation-only restriction was for level/quest/skill design; those systems remain outside this equipment task. Preserve existing scenes/user settings, combat rules and manual boss Focus Guard. Do not treat the finite material test as implemented persistent Farming or local RNG as final economy authority.

## 10. Ratio defense (2026-10-23 user-approved)
Use shared ratio mitigation `max(1, round(raw * K / (K + target DEF)))` after existing raw action/critical rounding; midpoint away from zero. Replace flat subtraction in all combat, preserving seeded rolls and manual Focus Guard afterward. Configurable finite positive K defaults to20 as a prototype assumption, not final balance. Do not assume diminishing reduction percentages imply diminishing absolute effective-health gains. Potion/monster attack/armor growth balance remains separate; do not restore flat subtraction. See docs/04_COMBAT.md and docs/30_EQUIPMENT_ENHANCEMENT_PROTOTYPE.md.

## 11. Journey UI slice (2026-10-23 user-approved)
A separate JourneyPrototype scene connects uGUI camp/equip/enhance feedback, four staged encounters, result/retry and finite supply combat. See docs/31_JOURNEY_PLAY_FLOW.md. Preserve original scenes, ratio defense K20 and manual boss Focus Guard. Screens switch within one scene; paused combat resumes with its original stats, new entry resets the fight and snapshots gear. Rewards settle only once on clear. Route/reward values are prototype assumptions. No automatic ascent, potion, manual dodge, new patterns, persistence/backend or full Farming in this slice.

## 12. Minimal ascent and compact HUD (2026-10-23 user-approved)
Journey now prioritizes target-floor entry, a vertical floor map and opaque entry transition; ordinary floor1/2 clears continue to the next floor after a short result pause, stoppable by camp/map navigation. Floor4 boss entry remains manual. This narrowly supersedes the earlier no-automatic-ascent scope; failure retries/full Farming stay deferred. Preserve the blocked floor through finite material runs and enhancement. Keep combat stats in camp/results; separate the small top floor strip/bottom HUD from the camera viewport without changing orientation, logic geometry or spawns. See docs/31_JOURNEY_PLAY_FLOW.md.
