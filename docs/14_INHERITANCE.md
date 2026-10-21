# 14. Inheritance

## Status
Accepted Direction

## 목적
신규 장비 획득이 기존 강화 투자를 완전히 무효화하지 않도록 한다.

## 채택 방식
누적 투자 가치 기반 계승.

```text
OldInvestmentValue × InheritanceEfficiency = TransferValue
```

TransferValue를 새 장비 강화 비용 곡선에 재적용해 새 강화 단계를 계산.

## v0 효율
| 이동 | 효율 |
|---|---:|
| 동일 등급 + 동일 무기군 | 약 90% |
| 동일 등급 + 다른 무기군 | 약 75~80% |
| +1 등급 | 약 50% |
| +2 등급 | 약 30% |
| +3 등급 이상 | 약 15~20% |

정확한 수치는 시뮬레이션으로 확정.

## 무기군 간 계승
허용.

향후 신규 무기 업데이트 때 기존 유저가 +0부터 시작하지 않게 한다.
