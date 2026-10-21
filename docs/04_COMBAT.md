# 04. Combat

## Status
Accepted Direction

탑 안에서는 직접 이동 조작을 하지 않는다.

캐릭터는 타겟 탐색, 자동 접근, 공격, 이탈, 재배치를 수행한다.

## 기본 흐름
```text
SEARCH
→ MOVE TO RANGE
→ ATTACK
→ REPOSITION
→ REPEAT
```

## 공격 판정
FPS와 독립해야 한다.

```text
if currentTime >= nextAttackTime:
    Attack()
    nextAttackTime += attackInterval
```

## 기본 공격
장검 기준:
- 주 대상 100%
- 주변 적에게 클리브

초기 예:
- 주변 약 3체
- 클리브 약 35%

클리브도 각 대상 DEF를 적용한다.

## 접촉 피해
근접 몬스터가 플레이어와 접촉하면 일정 주기로 피해.
- 약 1초 Tick 후보
- 몬스터별 개별 Tick

## 공격속도
정식 성장축.

Season 1 가설:
- 시작 약 1.0~1.1회/s
- 상한 약 1.5회/s

Season 2 후보:
- 약 2.0회/s

## 추가 공격
- 별도 확률형 성장 가능
- 짧은 딜레이 후 추가타
- 추가타가 추가타를 다시 발동하지 않음

## 생존 스탯
- HP
- DEF
- 회피

피해 경감은 장비 특성으로 우선 제공.
