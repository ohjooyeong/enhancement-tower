# AGENTS.md — EnhancementTower

## 0. Mission
Build a playable, mobile-first Unity 6 / C# idle action RPG **강화의 탑**. Work incrementally. Prioritize correct, testable combat over speculative features or polished placeholder art.

## 1. Mandatory reading before tasks
- Start at `docs/INDEX.md`.
- Read `docs/23_COMBAT_PRESENTATION_2026_10.md` for current camera/movement/animation/boss/encounter rules.
- Read `docs/25_DECISION_REGISTER.md` for decision status and conflicts.
- Read related canonical docs (`04_COMBAT`, `05_AUTO_POSITIONING`, `07_TOWER_CHALLENGE`, `08_FARMING`, `11_WEAPON_SYSTEM`, `12_EQUIPMENT`, `13_ENHANCEMENT`, `15_ORBS_AND_SKILLS`, `16_REBIRTH`, `18_SERVER_ARCHITECTURE`) before touching those systems.
- If old documents conflict, priority: `25_DECISION_REGISTER` > `23_COMBAT_PRESENTATION_2026_10` for combat presentation/positioning > current topic's GDD > `supplement_*` > historical ADR. Do not silently overwrite existing design; update the register and relevant doc when user approves a change.

## 2. Locked gameplay direction
- **No fixed lanes.** In tower combat player movement is autonomous in a bounded 2D field; enemies mostly emerge from the visual forward/top. No manual joystick in the tower.
- **Same movement and camera architecture in standard fights and bosses**. Bosses only add telegraphed attacks, charge, sidesteps, turn/reacquire, and recovery. They may end up behind the hero; no forced position swap or teleport.
- Camera is fixed **orientation**, angled-looking 2D pixel-art (quarter-view), no camera rotation; position may follow gently/scroll by segments. The player can show front/back/side sprites according to facing; not back-view-only.
- Regular attacks use per-action geometry: forward slashes directly hit up to 3 nearest eligible enemies with equal base damage; narrow forward thrust hits 1 harder. No primary-target + reduced splash or random victim selection. Each enemy is hit once per action, with a total target budget across the active window. The prototype slash → reverse slash → thrust order and numerical values remain tunable; conditional breakthrough slash may push nearby enemies then advance only into physically open space after sustained blocking; **not** mandatory combo movement, phasing, or guaranteed escape. Tactical movement and focus targeting sample eligible candidates with seeded score-weighted probabilities; favour better choices while permitting lower-ranked legal ones. Distance is bounded, and attack windows/victim geometry stay deterministic given inputs. Do not alternate sides or reroll every frame.
- Aim and movement vectors are distinct. Hit detection is derived from logic and attack arcs, not VFX appearance or animation frames. Combat simulation is framerate independent.
- Monsters accumulate if kill rate < spawn pressure; limited lateral reposition and hard retreat limit; no infinite kiting. Player and monsters must not overlap at foot-center; limited monster-monster overlap allowed. No guaranteed perfect auto-dodge.
- Normal ranged projectiles generally handled by build/defense; clearly telegraphed boss or elite AoE permits limited geometry-based sidestep. Avoid impossible/unreadable patterns.
- Challenge: finite stage (v0 ~50 enemy kills, ~5 concurrent baseline, ~90 sec test), no Rage. Clear→next; fail twice→appropriate Farming.
- Farming: persistent spawns, no per-wave intro, repeatable forward illusion/segments, offline settlement, optional encounter-intro toggle; Rage is manual and **Farming-only**.
- Hub navigation may be manual; tower combat is autonomous.
- Visual style: restrained modern handcrafted 2D pixel art; no shiny generic dark-fantasy UI, character EXP/level, or leveled skills in MVP.

## 3. Equipment & progression invariants
- Slots: 1 weapon, 1 armor, 2 rings, 1 orb, 1 auto skill. Rage separate.
- Season 1: longsword + greatsword; weapon-specific preferred range, swing timing, arc, knockback. Future weapons are not MVP.
- No character EXP/level. Rebirth Points upgrade permanent adventurer stats.
- Enhancement never destroys item: success +1; blessing on success may +2; failure remains or rare regression -1; pity success bonus accumulates on failure and resets on success. Exact rates/costs remain tunable.
- Equipment inventory/economy RNG and rewards must eventually be server authoritative; **do not build backend in prototype phase**.

## 4. Unity implementation rules
- Target a Unity 6 version pinned in `ProjectSettings/ProjectVersion.txt`, same exact version across Mac and Windows. 2D URP/Universal 2D if available in selected editor.
- Organize `Assets/Scripts` into `Core`, `Combat`, `AI`, `Progression`, `Data`, `Presentation`, `UI`, `Infrastructure`; `Assets/Scenes`, `Assets/Prefabs`, `Assets/Art`, `Assets/Tests`.
- Use plain C# domain models for combat/balance; MonoBehaviour for composition/Unity-specific views; ScriptableObject for authored static definitions. Never mutate ScriptableObject definitions to store run/save state.
- AI state machine baseline `SEARCH → APPROACH → ATTACK → REPOSITION → SEARCH`; boss add `AVOID_TELEGRAPH → REACQUIRE`, not a second incompatible movement system.
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
- Add one boss: telegraphed linear charge, lateral dodge, boss passes hero, hero rotates and re-engages.
- Add basic stage spawn/clear, minimal HUD, encounter intro skip toggle.
- Validate at portrait phone aspect ratios and performance before adding permanent progression.

## 7. Forbidden shortcuts
- Do not replace free-forward movement with 1/3/4 rigid lanes.
- Do not make boss combat change to sidescroller, chase-camera, or rotate-camera without explicit approval.
- Do not add manually controlled tower movement, player levels, skill levels, forced Rage in Challenge, or unlimited dodge/knockback stun locks.
- Do not treat examples/ranges as approved economy formulas.
- Do not hallucinate finished assets, test results, server APIs, or prior implementation.
