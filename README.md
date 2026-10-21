# EnhancementTower — Unity 개발 시작 팩 (2026-10-21)

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
이 아카이브는 **프로젝트 코드를 포함하지 않는 기획/착수 패키지**입니다. Unity Editor에서 실제 컴파일/실행 검증은 하지 않았습니다. 과거 GDD 내용은 그대로 유지하되 최신 전투 규칙에 대한 우선순위만 명시했습니다.
