# 18. Server Architecture

## Status
Accepted Direction

## 원칙
> 전투의 모든 Hit를 서버에 보내지 않는다.  
> 경제와 핵심 성장 결과는 서버 권위다.

## Client
- Auto Positioning
- 몬스터 이동
- 공격 애니메이션
- 타겟 선택
- 일반 전투 표현
- VFX
- 데미지 숫자

## Server
- 재화
- 아이템 생성
- 개체값
- 드랍
- 강화 RNG
- 강화 결과
- 계승
- 환생
- RP
- 구매
- 랭킹
- 거래소

## Farming Segment Snapshot 후보
- Weapon
- Weapon Type
- Enhancement
- Armor
- Ring
- Orb
- Skill
- RP Stats
- Farming Area
- Balance Version
- Positioning Profile Version

## Segment 종료 조건
- 강화
- 장비 변경
- 오브 변경
- 스킬 변경
- 환생
- 파밍 지역 변경
- RP 변경

## 강화 서버 처리
1. 소유 확인
2. 강화 단계 확인
3. 비용 확인
4. 재화 차감
5. 성공 Roll
6. 축복/역행 Roll
7. 아이템 변경
8. Audit Log
9. Response

Idempotency 필수.
