# 19. Logging & Analytics

## Status
Accepted Direction

## Economy Log
- Gold
- 강화석
- 아이템 생성/삭제
- 강화
- 계승
- 분해
- 환생
- RP
- 구매
- 거래소

기본 형태:
```text
eventId
userId
timestamp
serverVersion
sessionId
reason
before
delta
after
```

## Item Lifecycle
중요 장비는 `itemInstanceId` 보유.

```text
drop → owned → enhanced → inherited → listed → sold → dismantled
```

## Gameplay
- Challenge start/end
- Farming start/end
- 사망
- 최고층
- Rage
- 무기 교체
- 프리셋 변경
- 환생

## Auto Positioning 분석
- 평균 이동 거리
- 이동 시간 비율
- 공격 가능 거리 진입까지 시간
- 평균 동시 접촉 적 수
- 압박 탈출 횟수
- 맵 가장자리 체류
- 무기별 평균 전투 거리

## Security
- 불가능한 DPS
- 비정상 이동속도
- 중복 보상
- nonce 재사용
- 시간 변조
- 비정상 강화 요청
- 비정상 재화 증가
