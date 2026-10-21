# 강화의 탑 — 문서 인덱스

**Updated:** 2026-10-21. 기존 `강화의_탑_FINAL_GDD.zip`의 00~22 문서 및 ADR을 보존하고, 최신 전투 설계와 Codex 작업 가이드를 추가했다. 오래된 문서는 통째로 삭제·재작성하지 않았다.

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
5. 클리브→군집(5/8/12)→보스 직선 돌진+측면 회피 테스트.

**Do not claim the GDD numbers are final.**
