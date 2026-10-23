# 강화의 탑 — 문서 인덱스

**Updated:** 2026-10-22. 기존 `강화의_탑_FINAL_GDD.zip`의 00~22 문서 및 ADR을 보존하고, 최신 전투 설계와 Codex 작업 가이드를 추가했다. 오래된 문서는 통째로 삭제·재작성하지 않았다.

## Priority
1. `25_DECISION_REGISTER.md`: 확정/가설/보류, 충돌 우선순위
2. `23_COMBAT_PRESENTATION_2026_10.md`: 2026-10 최신 전투 뷰·이동·조우·보스·원거리·타격감
3. Canonical numbered docs `00`~`22` (보존한 GDD)
4. `adr/` 기존 결정 배경; `supplement_` 추가 메모 (현행 규칙과 충돌 시 현행 기준 우선)
5. `24_DEVELOPMENT_START_GUIDE.md`: 환경 설치, `../AGENTS.md`: 코딩 에이전트 지침

## GDD areas
- 00/01/02: 개요·세계관·코어 루프
- 03: 마을/커뮤니티
- 04/05/06/07/08/09: 전투·자동 포지셔닝·몬스터·도전·파밍·레이지
- 10/11/12/13/14/15/16: 성장·무기·장비·강화·계승·오브/스킬·환생
- 17/18/19/20/21/22: 밸런스·서버·분석·과금·로드맵·미결정
- 23: **최신 전투 통합 설계** (기본 및 보스 동일 시점, 자유 전방 이동)
- 24: 설치·개발 시작
- 25: 확정 사항과 추가 결정 필요 항목

## Start development today
1. 프로젝트 만들고 정확한 Unity 에디터 버전 고정, Git 구성.
2. `AGENTS.md`, `INDEX.md`, `25_DECISION_REGISTER.md`, `23_COMBAT_PRESENTATION_2026_10.md` 읽기.
3. `Assets/Scenes/CombatPrototype.unity`에 placeholder 1 Player + 1 Monster 구현.
4. 타겟→접근→4방향 시선→타격/피격/사망 검증.
5. 동작별 다중 베기/찌르기 콤보→군집(5/8/12)→보스 직선 돌진+수동 집중 방어·재교전 테스트(자동 측면 회피는 과거 비교 옵션).

**Do not claim the GDD numbers are final.**

## Current attack design (2026-10-22)

- 설계 규칙: `04_COMBAT.md` / `25_DECISION_REGISTER.md`. 베기는 전방 가까운 적 **총 최대 3명**, 찌르기는 좁은 전방 **1명에게 강한 피해**. 주 대상+약한 주변 피해/랜덤 선택 방식은 폐기.
- 콤보별 범위·대상 수·타격 시간·넉백·표현을 구분한다. 현재 사선 → 역베기 → 찌르기와 수치는 실험값.
- 기본 콤보는 고정하고 지속 막힘에서만 조건부 돌파. 재배치는 적별 위협/준비 상태·벽·재교전 거리를 평가한다. 이동 방향·거리와 조준 타겟은 유효 후보의 확률 가중 선택이며 시드로 재현한다.
- 정적 수치: `Assets/Data/LongswordCombo.asset` / `Assets/Data/CombatTactics.asset` / `Assets/Data/CombatDamage.asset`. 피해 범위·개별 치명타·방어 차감은 조절 가능한 프로토타입 수치다. 조절/검증 방법: `26_COMBAT_PROTOTYPE.md`.

## Prototype implementation
- `26_COMBAT_PROTOTYPE.md`: 1~12마리 자동전투·동작별 전방 다중 베기/단일 찌르기 콤보·공격 넉백·압박 재배치·5/8/12 밀집 비교와 타격 피드백/선택형 판정 범위 표시와 Unity 수동 테스트 절차. 일반/빠른 약체/강한 근접 혼합 및 `FastMonster.asset` 조절, `PrototypeStage.asset` 기반 전방 순차 소환/처치/시간 제한 포함.

## Current boss direction (2026-10-23)
보스는 자동 접근·공격 + 제한적 수동 집중 방어로 검증한다. 기본 돌진 자동 회피는 해제하며, 일반전/파밍은 자동 유지. 세계관 근거는01의 동조/의식적 집중, 최신 승인 내용은25, 실행 방법은26을 따른다.

## Current visual goal (2026-10-23 사용자 승인)

> 약 4등신 캐릭터와 입체감 있는 쿼터뷰 환경으로 구성한 2.5D풍 픽셀 아트 RPG. 레인 없는 전방 중심 자유 자동전투를 사용하며 카메라 방향은 고정한다.

렌더링 기술은 미확정이다. [28_VISUAL_DIRECTION_AND_VIEW_REVIEW.md](28_VISUAL_DIRECTION_AND_VIEW_REVIEW.md)에 문서 충돌, 현재 Assets/View 구조, 기술 비교와 승인 후 검증 항목을 정리했다. [27_FIRST_ASCENT_STORYBOOK.md](27_FIRST_ASCENT_STORYBOOK.md)는 저장소에 추가된 초반 서사/UX **제안**이며, 등장 동기·NPC·장면 전체를 승인된 구현으로 간주하지 않는다. 그 문서의 이전 `2D 수제 도트` 표현도 최신25의 시각적 목표를 따른다.
