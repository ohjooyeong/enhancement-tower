# 강화의 탑 — Final GDD Index

## 최신 기준 안내 (2026-10-23)

> 약 4등신 캐릭터와 입체감 있는 쿼터뷰 환경으로 구성한 2.5D풍 픽셀 아트 RPG. 레인 없는 전방 중심 자유 자동전투를 사용하며 카메라 방향은 고정한다.

최신 결정은 [25](25_DECISION_REGISTER.md), View 기술 검토는 [28](28_VISUAL_DIRECTION_AND_VIEW_REVIEW.md)을 따른다. 렌더링 기술은 미확정이다. 아래 초기 문서의 자동전투는 자동 이동·공격을 뜻하며 보스의 수동 집중 방어를 금지하지 않는다. 기존 GDD 목록과 결정 이력은 보존한다.


이 문서 세트는 현재까지 논의된 《강화의 탑》의 최종 기준 설계안이다.

## 핵심 한 줄
> 죽고, 강화하고, 환생하고, 다시 올라간다.

## 게임 정체성
- 모바일 방치형 RPG
- 강화 시뮬레이션
- 자동 포지셔닝 전투
- 장비/오브/무기 조합
- 환생 기반 영구 성장
- 중세 판타지 기반 장기 확장형 세계관

## 현재 확정 방향
- 플레이어 캐릭터 레벨 없음
- 환생 조건은 최고층
- 환생 포인트(RP)로 모험가 자체를 영구 성장
- 장비/강화는 영구 성장
- 탑 진행도는 환생 시 초기화
- 마을에서는 직접 이동 가능
- 탑에서는 직접 조작하지 않고 자동 전투
- 탑 안의 자동 전투은 세계관적으로 `탑과의 동조`로 설명
- 시즌1 무기군: 장검 / 대검
- 시즌1 최고 장비 등급: 레전더리
- 시즌1 이후 단검 / 쌍검 / 창 / 활 / 석궁 등 확장 가능
- Challenge / Farming 분리
- Rage는 Farming 전용
- 서버는 경제와 핵심 성장 결과에 권위 보유

## 문서 목록
- `00_GAME_OVERVIEW.md`
- `01_WORLD_AND_TOWER.md`
- `02_CORE_LOOP.md`
- `03_HUB_AND_COMMUNITY.md`
- `04_COMBAT.md`
- `05_AUTO_POSITIONING.md`
- `06_MONSTERS.md`
- `07_TOWER_CHALLENGE.md`
- `08_FARMING.md`
- `09_RAGE.md`
- `10_ADVENTURER_GROWTH.md`
- `11_WEAPON_SYSTEM.md`
- `12_EQUIPMENT.md`
- `13_ENHANCEMENT.md`
- `14_INHERITANCE.md`
- `15_ORBS_AND_SKILLS.md`
- `16_REBIRTH.md`
- `17_BALANCE_TARGETS.md`
- `18_SERVER_ARCHITECTURE.md`
- `19_LOGGING_ANALYTICS.md`
- `20_MONETIZATION.md`
- `21_CONTENT_ROADMAP.md`
- `22_OPEN_QUESTIONS.md`
- `adr/`

## 상태 표기
- `Accepted`: 현재 기준 확정
- `Accepted Direction`: 방향 확정, 수치는 조정 가능
- `Prototype Validation`: 구조는 유력하나 실제 플레이 검증 필요
- `v0`: 최초 밸런스 가설
