# Unity CLI / MCP 연결 기록 — 2026-10-23

공식 Unity CLI와 Editor 내부 Unity Pipeline을 연결했다. 이는 URP/Renderer 교체가 아니다. 게임 설계 변경은 없으며 CombatPrototype/VisualPrototype 관련 코드, 모든 씬, ProjectSettings를 수정하지 않았다.

## 설치와 구성

- 기존 CLI `1.0.0-beta.13` 확인. 재설치하지 않았다. CLI가 beta.14 업데이트를 알렸으나 이번 연결에는 필요하지 않아 업데이트하지 않았다. Editor는 프로젝트와 실행 중 인스턴스 모두 `6000.6.4f1`.
- 시작 시 사용자의 미커밋 manifest에 `com.unity.pipeline: 0.9.0-exp.1`이 이미 있었다. 공식 `unity pipeline install --project-path .`은 기존 설치를 확인했고, Editor가 이를 해석·컴파일하면서 packages-lock에 Pipeline 항목만 추가했다. 기존 의존 패키지 버전 변경/중복 설치 없음.
- Cursor 프로젝트 설정 `.cursor/mcp.json`: `unity mcp` stdio. 공식 configure로 생성 후 머신 절대경로를 `unity` PATH 명령으로 바꿨다. 현재 프로젝트 루트에서 사용하고 복수 Editor에서는 대상 경로를 명시한다.
- Codex 사용자 설정에 `unity mcp --project-path <현재 프로젝트>` 등록. `codex mcp get unity`로 enabled/stdio 확인. 저장소 밖 개인 경로는 커밋하지 않는다. Codex는 MCP 없이도 셸에서 CLI 사용 가능하다.
- 기존 AI Assistant는 그대로 보존했다. 별도 타사 MCP/AI 패키지를 추가하지 않았으며 전역 네트워크/샌드박스 정책도 변경하지 않았다.

## 실제 실행 결과

| 항목 | 결과 |
|---|---|
| Editor 연결 | 성공. localhost 포트7800에서 ready, compiling=false, 프로젝트 경로/Unity 버전 일치 |
| Console | `console get` 성공. compilationFailed=false, consoleErrors=0. 기존 AI 서비스 NoSubscription 로그5개 관찰 |
| 계층/열린 씬 | `scene get_hierarchy`, `scene list_open` 성공. JourneyPrototype, 루트 Combat Prototype, isDirty=false |
| EditMode 테스트 | `test run --mode editor --async_tests true` 실행 후 결과 파일 확인. completed, 총104/통과104/실패0/스킵0, 3.35초 |
| MCP 프로토콜 | 실제 `unity mcp` subprocess에 initialize와 tools/list 요청 성공. serverInfo=unity-mcp 1.0.0-beta.13 |
| Play Mode | 현재 JourneyPrototype에서 start → status playing 확인(frameCount=2, playerLoopTicking=true) → stop → ready/stopped 확인 |
| 캡처 | Game View PNG 1124×632 생성 및 이미지 열기 확인. `Temp/pipeline-screenshots/journey-cli-check.png` (Git 제외). 캐릭터·환경은 보이나 HUD 포함 전체 UI 캡처는 검증하지 않음 |
| 보존 확인 | 종료 후 열린 JourneyPrototype의 isDirty=false 확인. Assets와 ProjectSettings에 Git 변경 없음 |

MCP 프로토콜 검증은 독립 stdio 클라이언트로 수행했다. Cursor/Codex 앱 자체에서 재시작 후 도구를 로드하고 호출하는 검증은 **미실행**이다. 자동 PlayMode 테스트 스위트·보스 플레이/수동 Focus Guard·장시간 백그라운드 진행·모바일 화면·Windows는 **미검증**이다. 이번 Play 검증은 실행/정지와 캡처 자동화 확인 범위다.

## 오류와 해결/후속 작업

1. 최초 Pipeline 목록은 manifest만 보고 설치됨을 표시했지만 포트가 없었다. PackageCache/lock에도 없었고 Editor의 해석·컴파일이 아직 끝나지 않은 상태였다. 완료 후 서버가 시작돼 해결됐다. 설치됨만으로 연결 성공을 판단하지 않는다.
2. Codex 샌드박스의 프로세스 탐색 제한으로 PID를 못 읽었다. 해당 진단/localhost CLI 실행을 승인받아 수행했다. 영구적인 보안 완화는 하지 않았다.
3. `console get --limit 10`은 INVALID_COMMAND_ARGS였다. 실제 지원되는 `console get`으로 재실행해 성공했다. CLI/패키지 버전에 따라 인자를 조회한다.
4. 기존 Unity AI 서비스에서 NoSubscription, 과거 Editor 로그에서 cloud401/licensing404가 보였다. 로컬 Pipeline 조회·테스트·Play/캡처는 성공했으므로 이를 연결 실패 원인으로 단정하지 않는다. Unity AI 클라우드 기능이 필요할 경우에만 Hub 로그인/구독을 사용자가 점검한다.
5. 사용자는 Cursor와 Codex를 재시작해 Unity MCP 활성 상태를 확인하고 Console/계층 조회를 한 번 요청한다. Cursor에 `unity` 실행 파일 없음 오류가 나면 CLI가 있는 PATH로 Cursor를 실행하거나 개인 설정에 `command -v unity` 결과를 지정한다. 개인 절대경로를 공유 설정에 커밋하지 않는다.

다음 최소 확인은 재시작한 Cursor/Codex 각각에서 읽기 전용 Editor 상태 조회다. Editor에서 추가 씬 생성·Renderer 설정 변경은 필요 없다. 재검증 명령은 AGENTS.md 14절을 따른다.

## 공식 근거

- [Unity CLI 설치 및 사용](https://docs.unity.com/en-us/unity-cli/use-unity-cli)
- [Unity Pipeline 설치와 Editor 연결](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package)
- 설치된 공식 패키지 `Documentation~/commands/`의 Editor lifecycle, scenes, runtime, build-and-compilation 문서와 실제 명령 카탈로그를 대조했다.
- [OpenAI Codex MCP 설정](https://developers.openai.com/codex/mcp)
