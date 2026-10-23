# EnhancementTower — Unity 개발 시작 팩 (2026-10-21)

## 최신 성장 설계 (2026-10-23, 문서 반영만)
캐릭터 레벨/처치 경험치를 도입하고 환생 후 레벨을 유지합니다. 레벨은 보조 능력치·콘텐츠/스킬 해금, 강화는 주 전투력 성장, RP는 별도 영구 성장입니다. 상한은 존재하며 Lv.50은 후보입니다. 퀘스트 없이도 정상 레벨업할 수 있고 자동 스킬은2슬롯으로 설계합니다. 무기 중심 전투·수동 집중 방어·Farming 전용 Rage는 유지합니다.

상세와 **아직 없는 구현 목록**은 [29_LEVEL_QUEST_SKILL_SYSTEM.md](docs/29_LEVEL_QUEST_SKILL_SYSTEM.md), 과거 무레벨 결정 대체는 [ADR-007](docs/adr/ADR-007-character-level-and-unlocks.md) 참조. 이번 작업은 Markdown만 수정하며 코드·씬·ScriptableObject를 변경하지 않습니다. 아래 CombatPrototype/View 비교가 레벨/퀘스트/자동 스킬 구현을 포함하는 것은 아닙니다.

## 현재 시각적 목표 (2026-10-23)

> 약 4등신 캐릭터와 입체감 있는 쿼터뷰 환경으로 구성한 2.5D풍 픽셀 아트 RPG. 레인 없는 전방 중심 자유 자동전투를 사용하며 카메라 방향은 고정한다.

렌더링 기술은 미확정입니다. 현재 도형 기반 View/Universal2D 설정을 최종 아트로 간주하지 않습니다. [View 검토](docs/28_VISUAL_DIRECTION_AND_VIEW_REVIEW.md)에 충돌 정리, 구현 방식 비교와 승인 후 검증 범위를 기록했습니다. 보스는 자동 접근·공격 + 수동 집중 방어이며 자동 돌진 회피는 기본 해제입니다. 아래 착수 프롬프트/설치 안내는 개발 시작 당시 기록입니다.


- `AGENTS.md`: Codex / coding agent instructions (Unity project root에 배치)
- `docs/INDEX.md`: 전체 문서 구조 및 우선순위
- `docs/25_DECISION_REGISTER.md`: 최신 결정과 미확정 구분
- `docs/23_COMBAT_PRESENTATION_2026_10.md`: 레인 없는 자유 전방 이동형 전투의 최신 기준
- `docs/00_GAME_OVERVIEW.md`~`22_OPEN_QUESTIONS.md`: 기존 전체 GDD 보존
- `docs/adr/`: 과거 의사결정 문맥; 현재 방향과 충돌하면 decision register 우선
- `.gitignore`: Unity/macOS/Windows 혼용 환경 권장 제외

## 적용
Unity 6 Universal 2D 프로젝트 `EnhancementTower`를 만든 다음, **이 패키지의 `AGENTS.md`, `docs/`, `.gitignore`를 프로젝트 최상위에 복사**하세요. 이미 `Assets/`, `Packages/`, `ProjectSettings/`가 있다면 덮어쓰지 마세요. 프로젝트는 Mac↔Windows 간 Git으로 동기화하고 `.meta`를 커밋하세요.

## Codex first prompt
`AGENTS.md와 docs/INDEX.md, docs/25_DECISION_REGISTER.md, docs/23_COMBAT_PRESENTATION_2026_10.md를 읽고, Unity 프로젝트 상태부터 확인해줘. CombatPrototype 씬에서 레인 없는 전방 자유 이동 자동전투의 첫 수직 슬라이스(플레이어 1명, 근접 몬스터 1마리, 탐색-접근-공격-피격-사망)를 최소 구현해줘. 보스/경제/서버/최종 아트는 아직 만들지 말고, 변경 파일과 Unity 에디터 확인 절차를 보고해줘.`

## Notes
최초 착수 패키지는 기획 문서만 포함했습니다. 현재는 아래 CombatPrototype 코드와 씬이 추가되었으며, 검증 범위는 구현 안내 문서에 기록합니다. 과거 GDD 내용은 그대로 유지하되 최신 전투 규칙에 대한 우선순위만 명시했습니다.

## CombatPrototype 구현
Unity 6000.6.4f1에서 `Assets/Scenes/CombatPrototype.unity`를 열고 Play합니다. 도형 기반 1~12마리 자동전투, 가까운 타겟 선택, 전방 최대 3명 베기와 강한 단일 찌르기 콤보, 개별 체력바, 고정 카메라와 Restart를 제공합니다. Inspector의 Monster Count 또는 실행 화면의 5/8/12 버튼으로 적 수를 바꿀 수 있습니다. Pressure reposition을 켜고 끄며 제한된 재배치를 비교할 수 있습니다. 이동·타겟은 점수 기반 확률로 선택하며 New seed each restart로 다른 전개/고정 재현을 비교합니다. 조건부 돌파와 적 위협에 따른 이동은 `CombatTactics.asset`에서 조절하며, Trap test로 포위 상황을 확인할 수 있습니다. 동작별 정적 데이터는 `Assets/Data/LongswordCombo.asset`에서 조절합니다. Hit sparks / Damage / Sound / Attack area로 적중 피드백과 판정 안내를 비교할 수 있습니다. `Assets/Data/CombatDamage.asset`에서 피해 범위·치명타 확률/배수·영웅/몬스터 방어력을 조절합니다. 크리티컬은 큰 분홍색 CRIT 숫자로 표시합니다. 구현 가정과 테스트 절차는 [docs/26_COMBAT_PROTOTYPE.md](docs/26_COMBAT_PROTOTYPE.md)를 참조하세요.


일반(빨강)·빠른 약체(노란 작은 마름모)·강한 근접(보라 큰 직사각형)을 섞어 비교합니다. **Include fast foes**로 빠른 적을 켜고 끄면 전투가 재시작됩니다. `Assets/Data/FastMonster.asset`에서 빠른 적의 체력/공격력 배율과 접근/공격 준비 속도를 조절합니다. Mixed Threats를 끄면 전부 일반 적입니다. 영웅 콤보/공속 성장 설정은 추가하지 않았습니다.


**Stage run**은 처음 5마리로 시작해 화면 상단에서 순차 합류하는 한 판입니다. 기본 총50/생존상한8/합류1.2초/제한90초는 `Assets/Data/PrototypeStage.asset`에서 조절합니다. Kills·Pending·Time과 CLEAR/DEFEAT/TIME UP을 확인하고 Restart로 재시작합니다. 5/8/12 버튼은 기존 군집 비교로 돌아갑니다. 빠른 적은 낮은 체력 대신 약 .48초 준비+회복으로 근접 압박을 주도록 조정했습니다.

## View 비교 실행

`Assets/Scenes/CombatViewComparison.unity`를 열고 Play하면 임시4등신 픽셀 캐릭터+3D환경을 볼 수 있습니다. **Switch view (same run)**으로 기존2D 표시와 같은 전투를 비교하고, 하단1/5/8/12·Stage run·Charge boss/FOCUS GUARD를 사용합니다. 기존 CombatPrototype 씬과 전투 규칙은 보존했습니다. 최종 아트/렌더러 선택 전 비교용이며 실행·검증 범위는 [26](docs/26_COMBAT_PROTOTYPE.md)의 View 비교 항목을 참조하세요.

쿼터뷰의 **Sword motion**을 켜면 영웅이 검을 쥐고 팔·상체를 함께 움직여 베는 임시 표현을 볼 수 있습니다. **1 foe**에서 준비/휘두름/회복을 비교하세요. 끄면 같은 전투에서 기존 막대 표현으로 돌아갑니다. 공격 속도·판정·피해와 집중 방어는 그대로입니다.

## Milestone 2 장비·강화 실행
`Assets/Scenes/EquipmentPrototype.unity`를 열고 Play하세요. 작업대에서 무기/방어구를 장착·강화한 뒤 **Apply equipment / retry**로 실제 ATK·DEF·최대HP 변화와 전투 결과를 비교합니다. **Equipment / forge**로 돌아가며, 재료가 부족하면 **Material test (1 foe)**로 보충할 수 있습니다. 장비/재료는 Play 중 재도전에 유지되고 Play 종료 후 초기화됩니다. 정식 저장/서버/파밍은 아직 없으며 [30 안내](docs/30_EQUIPMENT_ENHANCEMENT_PROTOTYPE.md)에 조절 값과 수동 검증 방법을 기록했습니다.

방어력은 피해를 직접 빼지 않고 `K/(K+DEF)`로 비율 경감합니다. `Assets/Data/CombatDamage`의 Defense Constant 초기값20은 비교용이며, [방어 공식](docs/04_COMBAT.md)과 [장비 검증 절차](docs/30_EQUIPMENT_ENHANCEMENT_PROTOTYPE.md)를 참고하세요. 몬스터 공격력은 유지했고 자동 회복 포션은 아직 구현하지 않았습니다.

플레이 흐름을 보려면 **Assets/Scenes/JourneyPrototype.unity**를 열고 Play하세요. 캠프에서 장착·강화→스테이지 선택→일반/러시/중장갑/보스→결과·강화·재도전으로 이어집니다. 재료가 부족하면 Supply Run을 이용하세요. [31 안내](docs/31_JOURNEY_PLAY_FLOW.md)에 화면/보상·일시정지 규칙과 검증 절차를 정리했습니다. 진행은 Play 세션 동안만 유지됩니다.

Journey의 주 흐름은 **CHALLENGE FLOOR → 층간 이동 → 일반층 자동 진행 → 수동 보스 입장**입니다. 막히면 FARM MATERIALS·강화 후 같은 층으로 돌아옵니다. 전투 능력치 패널은 캠프로 옮겼으며 전투 카메라를 HUD와 분리했습니다. 재료 보충은 아직 유한 전투이고 진행은 Play 세션 한정입니다.
