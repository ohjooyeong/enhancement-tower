# 08. Farming

## Status
Accepted

## 목적
시간당 재화 획득.

## 특징
- 층 클리어 없음
- 무한 몬스터 생성
- 자동 이동
- Rage 사용 가능
- 오프라인 정산

## 추천 파밍
가장 높은 층이 항상 최고 효율은 아니다.

기준:
> Rewards / Hour

## 사망
```text
사망
→ 재시도
→ 반복 사망
→ 더 안정적인 파밍 구간으로 자동 이동
```

## Spawn
강한 빌드가 몬스터 부족으로 손해 보지 않게 SpawnRate / SpawnCount / Density 확장 가능.

## 오프라인
```text
Reward = ExpectedFarmingRate × ElapsedTime × OfflineEfficiency
```

실제 몬스터 이동과 타격을 재생하지 않는다.

## 현재 플레이 흐름과의 구분
[31](31_JOURNEY_PLAY_FLOW.md)의 Supply Run은4마리 유한 재료 보충 비교 전투다. 위 지속 스폰/자동 재시도·추천 지역/오프라인 정산/Rage 구현 완료로 해석하지 않는다.
