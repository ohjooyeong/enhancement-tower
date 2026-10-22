# Development Start Guide

목표:
> 오늘 개발 환경을 설치하고 `EnhancementTower` 프로젝트를 열어 빈 Scene을 Play하는 것까지 완료한다.

## 1. 설치
필수:
1. Unity Hub
2. Unity 6.3 LTS 계열 Editor
3. Android Build Support
4. Android SDK & NDK Tools
5. OpenJDK
6. Git
7. VS Code 또는 Rider
8. Node.js
9. Codex CLI

Android 프로토타입이 먼저라면 iOS Build Support는 나중에 추가해도 된다.

## 2. Unity 설치
Unity Hub에서 Unity 6.3 LTS를 설치하고 다음 모듈을 함께 선택한다.
- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

별도 JDK/NDK를 섞기보다 처음에는 Unity Hub가 설치한 도구를 그대로 쓴다.

## 3. 프로젝트 생성
Unity Hub:
```text
Projects
→ New project
```

권장:
```text
Project Name: EnhancementTower
Template: Universal 2D / 2D URP 계열
Location: ~/Projects/EnhancementTower
```

## 4. 첫 Scene 실행
기본 Scene을 `Assets/Scenes/CombatPrototype.unity`로 저장한다.

Hierarchy 최소 구성:
```text
Main Camera
Player
```

Player는 빈 GameObject여도 된다.
Play를 눌러 Console Error 없이 실행되면 1차 성공.

## 5. 폴더
```text
Assets/
├── Art/
├── Audio/
├── Prefabs/
├── Scenes/
├── ScriptableObjects/
└── Scripts/
    ├── Character/
    ├── Combat/
    ├── Movement/
    ├── Monsters/
    ├── Weapons/
    ├── Equipment/
    ├── UI/
    ├── Data/
    └── Common/

docs/
AGENTS.md
```

## 6. Git
```bash
cd ~/Projects/EnhancementTower
git init
```

Unity `.gitignore`에서 최소 다음을 제외한다.
```text
Library/
Temp/
Logs/
Obj/
Build/
Builds/
UserSettings/
```

첫 커밋:
```bash
git add .
git commit -m "chore: initialize Unity project"
```

## 7. Codex
Node/npm 준비 후:
```bash
npm install -g @openai/codex
codex --version
```

프로젝트 루트에서:
```bash
cd ~/Projects/EnhancementTower
codex
```

화면 안내에 따라 ChatGPT 계정으로 로그인한다.

## 8. 첫 Codex 프롬프트
```text
AGENTS.md와 docs/를 읽어라.

이 프로젝트는 Unity 기반 모바일 방치형 RPG "강화의 탑"이다.
현재 목표는 Combat Prototype이다.

아직 코드를 수정하지 말고 다음만 제안해라.
1. CombatPrototype Scene 구성
2. 필요한 GameObject
3. C# 클래스 구조
4. ScriptableObject 데이터 구조
5. Player와 Monster의 최소 상태
6. Auto Positioning 구현 순서
7. 5개의 작은 개발 단계

과도한 아키텍처를 만들지 말고,
첫 번째 목표는 장검 플레이어가 적에게 자동 접근해 적정 거리에서 멈추는 것이다.
```

이후:
```text
1단계만 구현해.

Player와 Monster를 만들고,
Player가 가장 가까운 Monster를 Target으로 선택한 뒤
preferredRange까지 자동 접근하는 것까지만 구현한다.

공격, 데미지, 강화, UI는 아직 구현하지 않는다.

작업 후:
- 변경 파일
- Unity Editor에서 내가 해야 할 Inspector 작업
- 테스트 방법
을 알려줘.
```

## 9. 첫 프로토타입 순서

2026-10-22 기준 기존 클리브 단계는 아래 직접 타격 콤보로 갱신했다. 현재 구현 범위와 Unity 확인 절차는 `26_COMBAT_PROTOTYPE.md`, 정확한 에디터 버전은 `ProjectSettings/ProjectVersion.txt`를 따른다.
P0. Project boots
P1. Player + Monster 표시
P2. Target Selection
P3. Auto Positioning
P4. Auto Attack
P5. Damage / HP / Death
P6. 전방 최대 3명 직접 베기 / 강한 단일 찌르기와 동작별 콤보 (`04_COMBAT.md`)
P7. 5마리 Spawn
P8. Pressure / Reposition
P9. 장검 vs 대검 Positioning 비교

## 10. 오늘의 완료 기준
```text
[ ] Unity Hub 설치
[ ] Unity 6.3 LTS 설치
[ ] Android 모듈 설치
[ ] EnhancementTower 생성
[ ] CombatPrototype Scene 저장
[ ] Play 성공
[ ] Git init
[ ] 첫 commit
[ ] docs 복사
[ ] AGENTS.md 배치
[ ] Codex 설치/로그인
[ ] Codex가 docs를 읽고 구현 계획 제시
```
