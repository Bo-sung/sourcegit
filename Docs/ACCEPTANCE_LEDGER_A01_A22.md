# A01–A22 수용 기준 대장 (Acceptance Ledger)

갱신: 2026-11-09 (2차 정정 + headless 검증 반영) · 브랜치 `feat/classic-layout-poc` · HEAD `c0f40a2e`
기준 정의는 `Docs/ACCEPTED_FEATURE_SCOPE.md` 를 그대로 사용한다. 이 문서 하나가 전 그룹 판정의 root다.

## 판정 등급

| 기호 | 뜻 |
|---|---|
| GUI | Windows 데스크톱 화면에서 실행하고 Git CLI로 결과를 대조해 확인 |
| HEADLESS | Avalonia Headless 회귀 테스트로 제품 VM·모델·명령 코드 + 실제 git으로 확인 (`HEADLESS_VALIDATION_20261109.md`). **GUI 증거가 아니다** |
| STATIC | 정적 검사(키·XML· diff·해시)로만 확인 |
| PART | 같은 기준의 일부만 확인, 나머지는 PART 이후 열에 사유 |
| ENV | 자동화/환경 제약으로 검증 불가 (원인은 3절) |
| UNIMPL | 제품 미구현 |
| NOT TESTED / NOT MEASURED | 각각 실행 미시도 / 수치 미측정 |

측정 환경: Windows, 화면 3840×2160 물리(150%), 앱 창 최대화 3862×2110 물리, 스크린샷 2880×1620(`coordinateSpace=window`, `scale=1`).
**NOT TESTED**: 창 1440×900 · 125% DPI 배율. **NOT MEASURED**: 렌더·스크롤 성능(`PERFORMANCE_BASELINE.md` 의 수치는 다른 환경 기준).

## 1. 판정 정정 내역 (이전에 잘못 쓴 곳)

| 항목 | 바로잡은 판정 | 근거와 이유 |
|---|---|---|
| A08 로케일 1206키 동일·중복 0·미해결 참조 0 | **STATIC** | 스크립트·XML 검사 결과로 GUI 실행 검증이 아님. 지난 판정에서 RUNTIME으로 묶어 쓴 것은 오류 |
| A08 창 전역 단축키 반응 | GUI | `Ctrl+Shift+F`·`Ctrl+Shift+P` 반응은 실제 화면에서 확인 (`CLASSIC_OVERLAY_AND_CORE_FLOWS_QA.md` §A02, `a23-46`) |
| A18 템플릿 목록·저장 | GUI | Templates 탭 렌더·추가→저장→`sourcegit.settings` 기록 (`A18_A19_A20_QA.md` §표, `a18-01/04/05`) |
| A18 피커 적용·변수 전개 | **PART(HEADLESS)** | 위 문서가 명시적으로 NOT TESTED(피커는 runtime ContextMenu, UIA invoke 미발화). 설정해 둔 것이 a18-08 설정 화면 증거였고, 적용/전개는 이번 headless 회귀로 확인 (`CommitTemplateTests` 9건) |
| A21 실패 원인 표시 | GUI(연결 실패 계열만) | 확인된 것은 `ssh: connect to host 127.0.0.1 port 1: Connection refused` + "인증은 시도되지 않았습니다" 안내 (`A03_A21_QA.md`, `a21-07`). **host key / publickey / HTTPS 401 / TLS 계열은 NOT TESTED** — 연결 자체가 실패해 인증 단계에 진입하지 못했으므로 "인증 오류 진단 전부 검증"이라 쓰면 안 됨 |
| A02 메시지 검색·결과 점프 | **GUI (이전 증거 유효)** | `Ctrl+Shift+F` → `amended` 조회 결과가 `git log --all --grep=amended` 와 일치 + 결과 클릭으로 선택 이동 (`CLASSIC_OVERLAY_AND_CORE_FLOWS_QA.md` §A02, `a23-46/49/50`). 이번 패스의 자동화 실패가 이 이전 검증을 무효화하지 않음 |
| A02 검색 범위(SHA/작성자/경로/내용/현재 브랜치) | **PART(HEADLESS)** | GUI에서는 콤보·체크박스가 UIA 미노출로 미검증; 범위 판정 로직은 `CommitSearchScopeTests` 10건으로 확인 |
| A13 리셋 soft/mixed/hard | **GUI (이전 증거 유효)** | `A12_A13_A14_HISTORY_REWRITE_REBASE_QA.md` §A13 표(`a25-32/33/36/37/39/40`) — 인덱스·워크트리·폐기 동작을 CLI로 대조. 체크아웃 없는 브랜치 리셋도 GUI PASS(`a25-41`) |
| A14 충돌 → 계속 / 중단 | **GUI (이전 증거 유효)** | 같은 문서 §리베이스: 충돌 예측 표시(`a25-66/67`), 해결→계속 reflog `rebase (continue)/(finish)`(`a25-68`~`a25-70`), 중단 후 HEAD 복원(`a25-71`). "A14 충돌 경로 ENV"라는 지난 서술은 오류 |
| A14 개별 작업 실행 | **PART(HEADLESS)** + GUI ENV | 작업 지정 → todo 생성 → git 실행 결과까지 headless로 확인; GUI의 `작업 선택` MenuFlyout 활성화는 여전히 ENV |
| A10 git-flow | **HEADLESS + CLI (해소됨)** | 지난 기록의 "영구 차단"은 오류였음이 확인됨: `gittower/git-flow-next`(Next 형식의 정본)가 Windows amd64 zip을 공식 릴리스 어셋으로 배포하며, 프로젝트 로컬(`sourcegit/artifacts/tools`, gitignore)에 설치하고 개발 프로세스 PATH로만 실행해 전 수명주기를 검증했다 (§5) |
| A11 git-flow-next 설정 편집 | **GUI + HEADLESS (구현 완료)** | 이전 "UNIMPL(읽기 전용)" 상태는 해소됨: next 키 맵을 편집·저장하면 키 맵 전체 복사 → parent/startpoint 참조 갱신 → 이전 키 삭제 순으로 쓴다. 화면 저장/재오픈과 CLI 대조까지 확인 (§6.1). 저장 버튼 막던 검증 오버블록은 GUI에서 발견해 수정 |
| 사용자 저장소 확인 절차 | 절차 위반 시인 | 이전 패스에서 격리 확인 명목으로 사용자 저장소(RuneCode)를 CLI로 조회했다. 지시(실 사용자 저장소는 읽지 말 것, 허용된 격리 확인은 설치본 preference 해시뿐)에 위반하므로 폐기하고, 이후 패스부터 해시 확인만 사용한다 |

## 2. 판정

| ID | 서브기준 | 판정 | 근거 |
|---|---|---|---|
| A01 | 파일 단위 stage/unstage → 커밋 | GUI | `CLASSIC_OVERLAY_AND_CORE_FLOWS_QA.md`, CLI 대조 |
| A01 | 헝크 단위 선택·stage/unstage | GUI | `a23-23/25/26` |
| A01 | 선택한 줄 범위 staging/unstage | **GUI 자동화 대기** | 제품 UI가 **존재한다**: `UpdateSelectedChunk`(`Views/TextDiffView.axaml.cs:1040-1094`, two-side `:1230-1290`)가 `TextArea.Selection`의 시작·끝 줄로 청 범위를 만들고, 그 범위에 Added/Deleted 줄이 있으면 스테이지 팝업(`BtnStageChunk`/`BtnUnstageChunk`/`BtnDiscardChunk`, Ctrl+S/U/D)을 띄운다. `OnStageChunk`가 만든 패치를 `git apply --cache --index`로 적용하므로 헝크가 아니라 **임의 줄 범위** 단위다. 이전 "ENV/라인 단위 입력 필요"와 "헝크 단위로만" 모두 잘못된 기재이며, 절차는 확정되어 있다: 작업본 diff → 변경 줄 클릭 → Shift+Down(키보드만으로 선택) → Ctrl+S → `git diff --cached` CLI 대조 |
| A02 | 그래프·선택·세부 탭·Diff 이동 | GUI | `a27-30`, `a28-*` |
| A02 | 메시지 검색 실행·결과 점프 | GUI | `a23-46/49/50` |
| A02 | 검색 범위(SHA/작성자/경로/내용)·현재 브랜치 한정·`IsMerged` | HEADLESS | `CommitSearchScopeTests` 10건 |
| A03 | 리모트 추가/편집/삭제·Fetch All·리모트별 push URL | GUI | `A03_A21_QA.md` |
| A03 | 기본 원격 저장→재시작 유지 | **HEADLESS** | `DefaultRemotePersistenceTests` — 저장→복제 후 콜드 리로드→`Remotes.Find` 계약 확인 |
| A03 | 기본 원격 콤보의 화면 조작 | ENV | ComboBox 드롭다운 항목 UIA 미노출 |
| A04 | 대상 리모트·브랜치·추적 표시 / 오류 미은폐 | GUI | `a04-01/03`, `a21-05/07` |
| A05 | 생성·체크아웃·추적 변경·dirty 보호 | GUI | `a23-42/43` |
| A06 | 충돌 목록 → 해결 → 계속/중단 + 인덱스 상태 | GUI | `A06_A07_CONFLICT_STASH_QA.md` |
| A06 | 외부 3way 병합 양성 | NOT TESTED | `--wait` 대기 semantics를 유지한 실행 미검증 |
| A07 | 범위 표시·저장·목록·적용·적용 충돌 안내 | GUI | `a24-38/79/83` |
| A07 | 스태시 → 새 브랜치/패치 | NOT TESTED | 미시도 |
| A08 | ko/en 키 동일성·XML 유효성 | STATIC | 스크립트 검사(1206키, 중복 0) |
| A08 | 창 전역 단축키 | GUI | `Ctrl+Shift+F` 반응 |
| A08 | 요소 키보드 이동·포커스 | ENV | `focusedElementId` null, `SetFocus` 액션 없음 |
| A09 | 창/탭/Splitter/Classic 모드 복원 | GUI | 탭 2개·`Layout.*` 저장값 |
| A09 | 1440×900 · 125% DPI · 성능 | NOT TESTED / NOT MEASURED | 단일 대형 디스플레이 |
| A10 | init·start·finish 실행 | **HEADLESS + CLI** | 제품 `Commands.GitFlow` 로 init → feature start/finish → release finish(태그 `v1.0`) → hotfix start/finish까지 실제 git-flow-next v2.1.0으로 실행 (`GitFlowNextFlowTests`) + 전용 fixture CLI 대조 (§5) |
| A10 | 의존성 부재 안내 | GUI | `a10a11-23` (의존성 없음: 설정 조회·저장은 허용, 시작/완료는 차단) |
| A11 | 설정 조회·레거시 편집·영향 안내 | GUI | `a10a11-24`~`a10a11-27` |
| A11 | git-flow-next 키 편집(기준 브랜치 이름·접두사·태그 접두사) | **GUI + HEADLESS** | 화면에서 `main`→`trunk` 저장 → 키 맵 이동·참조 5곳 갱신·무관 키 보존·ref 불변 (`a29-*`), 회귀 테스트 10건 (`GitFlowNextSettingsTests`) |
| A12 | 파일 이력·리비전 내용·Blame·이전 리비전 | GUI | `a25-05/10/14/15` |
| A12 | 작업 트리 파일 Blame 진입 | GUI | `블레임 (HEAD-only)` → 블레임 창 (`a28-H3`) |
| A13 | Amend / Cherry-pick(+충돌 해결→계속) | GUI | `A12_A13_A14_…` §A13 |
| A13 | Revert(커밋 생성) / Revert 작업 트리만 | GUI | `a25-18`, `a28-G*` |
| A13 | Reset soft/mixed/hard + 브랜치만 리셋 | GUI | `a25-32/33/36/37/39/40/41` |
| A13 | 여러 커밋 동시 revert | NOT TESTED | 미시도 |
| A14 | 대화창(기준/대상/행/자동스태시/훅 건너뛰기) | GUI | `a28-9x`, `a28-E0` |
| A14 | 순서 변경 실행 | GUI | `a28-E2` — CLI로 재배치 확인 |
| A14 | 개별 작업 지정→todo→git 결과 | **HEADLESS** | `InteractiveRebaseActionTests` 14건: 6작업 todo 매핑(`p/e/r/s/f/d`), Drop·Squash·Fixup·Reword 실행 결과, Edit 정지, 부모 없는 첫 커밋의 Squash→Reword 자동 전환 |
| A14 | GUI에서 작업 선택( MenuFlyout ) | ENV | 항목활성화 미발화 (§3) |
| A14 | 충돌 → 계속 / 중단 | GUI | `a25-68`~`a25-71` (이전 증거 유효) |
| A14 | 충돌 → 건너뛰기 | NOT TESTED | 미시도 |
| A15 | 생성·푸시·트리기·단건 삭제·푸시 거부 | GUI | `A15_A16_A17_…` |
| A15 | 여러 태그 일괄 삭제 + 원격 삭제 | GUI | `a28-F3/F6` |
| A15 | 태그 체크아웃/아카이브·`원격에서도 삭제` 미체크 경로·오류 툴팁 캡처 | NOT TESTED | 미시도 |
| A16 | 추가·탐색·제거(보호/강제)·잠금·정리·빈 상태 | GUI | `a26-*`, `a27-03` |
| A17 | 터미널·탐색기·외부 diff 양성 / 미설치·잘못된 설정 음성 | GUI | `a27-*`, `Exception.*` 알림 |
| A17 | 외부 merge 양성(3way) | NOT TESTED | `--wait` semantics 유지 전제 |
| A18 | 템플릿 탭 렌더·저장 범위 | GUI | `a18-01/04/05` |
| A18 | 적용·변수 전개 | **HEADLESS** | `CommitTemplateTests` — 전개 7종 + `WorkingCopy.ApplyCommitMessageTemplate`(실제 staged 변경 기준 치환, 기존 입력 대체) + 설정 저장 템플릿 재적용. **수정 1건**: 브랜치 없는 저장소에서 `${branch_name}` NRE → `TemplateEngine.cs` 방어 |
| A19 | 패턴·치환·미리보기·저장 | GUI | `a19-14` |
| A20 | 서명 방식/키/범위·의존성 오류·fixture 커밋 | GUI | `a20-02` |
| A21 | SSH 키 설정·경로/인증 방식 표시 | GUI | `A03_A21_QA.md` |
| A21 | 연결 실패 원인 안내 | GUI | `a21-07` ("인증은 시도되지 않았습니다" 포함) |
| A21 | host key / publickey / HTTPS 401 / TLS 진단 | NOT TESTED | 연결 실패로 인증 단계 미도달 |
| A22 | 하위 저장소 열기·상태 안내·init/update/recursive·보호 | GUI | `a22-*` |

## 3. 자동화 제약 (ENV 판정의 근거)

1. UIA 공급자: `surfaces:{menus:false, dialogs:false}`, `windows.focus:false`, `focusedElementId` 상시 null, `SetFocus` 액션 없음.
2. 코드조립 `MenuFlyout` 항목은 트리에는 보이지만 Invoke/좌표 클릭 모두 Click이 발화하지 않는다(창 활성 시 light-dismiss; 색점 픽셀 스캔으로 좌표 교정 재시도 실패). XAML `<ContextMenu>` 항목은 동작해 그 계열로 우회 검증했다.
3. 키보드: 창 전역 Hotkey만 도달. 요소 키 처리(Enter, `P/E/R/S/F/D`, ComboBox 초기키 외)는 포커스 요구로 불가.
4. `Command` 는 git 에디터를 `Environment.ProcessPath` 로 연결 → 테스트 호스트에서 재현 불가 → headless 실행 검증은 "제품이 만든 todo + 셸 에디터"로 대체(제한 명기).
5. 다른 로컬 앱(SourceTree 등)이 포그라운드를 유지하면 클릭이 `window_not_focused`로 거부된다. 포커스 강탈은 금지된 UIA 우회이므로 재시도하지 않는다.

## 4. 남은 것 (다음 3)

1. **A14 GUI 작업 선택**: MenuFlyout 항목 활성화 수단이 없으면 ENV 고정. 대안(키보드 접근 가능 포커스, 공급자 개선) 없이 회귀 테스트로 상태·결과는 이미 HEADLESS 커버.
2. **A10**: 해소됨 — `gittower/git-flow-next` 공식 Windows 어셋을 `sourcegit/artifacts/tools/git-flow-next/`에 두고 개발 PATH로 실행 (§5). 남은 서브항목은 flow 대화창(시작/완료 팝업)의 GUI 실행이고, 제품 명령 경로의 실행 결과는 HEADLESS로 확인했다.
3. **GUI 잔여 서브항목**: A06·A17 외부 3way merge 양성, A07 스태시→패치(OS 저장창 경로 입력 차단), A21 인증 계열 4종, 1440×900·125% DPI. — A07 스태시→브랜치, A15 태그 체크아웃·아카이브·원격미체크 삭제는 5차에 GUI 확인됨. A01 선택 줄 범위 staging은 제품 UI가 있어 GUI 자동화 대기(위 표), A13 다중 커밋 revert는 수용 기준 밖 보충(제품 경로는 단일 커밋 팝업 — `ViewModels/Revert.cs:18`, `Commands/Revert.cs:7`).

## 5. A10 git-flow 실행 파일 조사와 해소 (2026-11-09)

* 제품 감지는 `git flow version` 실행 결과 기반 (`Native/OS.cs` `UpdateGitFlowVersion`) — 출력에 `git-flow-next`가 있으면 `Next`, 그 외 문자열이면 `Legacy`.
* 레거시 계보 `petervanderdoes/gitflow-avh`: **archived**, 마지막 push 2023-08-17, release 어셋 0개 → 공식 Windows 배포물 없음.
* Next 계보 **`gittower/git-flow-next`** (Go 재구현, 476★, 비archived, 마지막 push 2026-09-08): v2.1.0 릴리스에 `git-flow-next-v2.1.0-windows-amd64.zip` (1,552,272 B) + `checksums.txt` 공식 어셋 존재.
* 설치: `sourcegit/artifacts/tools/git-flow-next/git-flow.exe` (zip 내용물은 `git-flow.exe` 단 하나 → `git flow` 서브커맨드로 해석됨). 압축 SHA256 `9bfbc2746ea647fe664e0a5be375f7d7e144412d5031064f60efb1f8fbe1b656` = 릴리스 `checksums.txt` 의 windows-amd64 값과 **일치** (출처 검증).
* 격리: `artifacts/.gitignore`에 `tools/` 추가 → 산출물에 들어가지 않음, 시스템/GIT 설치 없음, PATH는 개발 프로세스(명령 한 번)에만 부여.
* 감지 확인: `git flow version` → `2.1.0 (git-flow-next)` exit 0 → 제품 감지는 `Next`.
* fixture (`artifacts/a29-fixture/gf`, 로컬 전용) 실제 실행 결과:
  | 명령 | 결과 |
  |---|---|
  | `flow init --preset=classic --main=main --develop=develop --feature=feature/ --bugfix=bugfix/ --release=release/ --hotfix=hotfix/ --support=support/ --tag=v` | `Git flow has been initialized`, `gitflow.*` 키 61개 (`.type/.parent/.startpoint/.prefix/*strategy/.autoupdate/.tag/.worktree`) |
  | `flow feature start login` → `finish` | `feature/login` 생성(develop 기준) → develop 병합 후 브랜치 삭제 |
  | `flow release start 1.0` → `finish` | `release/1.0` → main 병합, **태그 `v1.0` 생성**, child base `develop` 자동 갱신 |
  | `flow hotfix start urgent` → `finish` | `hotfix/urgent` → main 병합, 태그 `vurgent`, develop 갱신 |
  | `flow release finish does-not-exist` | exit 5 `Error: start point branch 'does-not-exist' does not exist`, `rev-list --all` 해시·HEAD **불변** |
* 제품 코드 경로 검증: `GitFlowNextFlowTests` — 위와 같은 수명주기를 `Commands.GitFlow.InitAsync/StartAsync/FinishAsync` 로 실행해 같은 결과(태그 `v1.0`, 무효 입력에서 ref 보존)를 확인. 의존성이 없는 환경에서는 같은 테스트가 `GitFlowAvailability.GuardOrNotify` 차단·키 0건·ref 불변을 대신 검증한다.

## 5.1 A11 git-flow-next 편집 구현 (2026-11-09)

* next 키의 실측 형태(위 init 결과)를 그대로 읽기/쓰기 대상으로 사용했다. 기준 브랜치는 **키 맵**이고 다른 항목이 `parent`/`startpoint`로 이름을 참조한다.
* 저장 순서: (1) `gitflow.branch.<old>.*` 전부를 `gitflow.branch.<new>.*`로 복사 → (2) `.parent`/`.startpoint` 값 중 이전 이름을 가리키는 것 갱신 → (3) 마지막으로 옛 키 삭제. **첫 실패에서 중단**하고 이미 쓴 것은 되돌리며, 되돌리지 못한 키는 집계 알림에 정리 커멘드와 함께 명시한다(§8 A10-A11-06 — 중간 실패를 조용히 반쪽 설정으로 넘어가게 두지 않는다).
* 접두사는 단일 키(`gitflow.branch.<topic>.prefix`), 태그 접두사는 태그를 만드는 항목의 `tagprefix`(값이 비면 키 삭제 = "접두사 없음"의 next 표기)로 쓴다. `bugfix`/`support`·전략·`autoupdate` 등 화면에 없는 키는 손대지 않는다.
* 브랜치 생성·이름 변경·삭제·체크아웃은 여전히 일절 없고, 충돌하는 이름(다른 키 맵이 이미 쓰는 이름)은 검증으로 막는다. 단 **자기 자기가 stored 값과 같은 이름은 충돌이 아니다** — 이것을 충돌로 처리해 저장 버튼이 막히는 회귀가 GUI에서 발견됨 (`ValidateBaseBranch`).
* 회귀: `GitFlowNextSettingsTests` 10건 — 편집 가능·저장 전 `Check()`(화면과 동일 경로) 통과, 키 맵 리네임+참조 갱신+무관 키 보존+ref 불변, 콜드 리로드, 접두사 단독 갱신, 태그 접두사 쓰기/삭제, 충돌 검증, legacyshape 유지.

## 6. 위생 확인 (이 패스)

* Debug/Release 빌드 경고 0·오류 0, `dotnet test` **75건** (TRX 실행 결과에서 클래스별로 센 수치: CommitSearchScope 10 · CommitTemplate 9 · DefaultRemotePersistence 5 · GitFlowNextFlow 2 · GitFlowNextSettings 14 · InteractiveRebaseAction 15 · LocaleFallback 20) — Debug/Release 전부 75/75. 어셋을 숨긴 실행은 74 통과 + 실패 1(어셋 경로를 지목하는 환경 메시지)
* 이 패스 코드 변경: `src/ViewModels/GitFlowSettings.cs`(next 편집 구현 + 검증 오버블록 수정), `src/Models/RepositorySettings.cs`(백그라운드 해시 예약 삭제), `src/Views/GitFlowSettings.axaml`(next 안내 문구), 로케일 `GitFlow.Settings.ReadOnlyNext` → `NextEditor` 키 이름 변경(en/ko 동수 1206), `tests/SourceGit.Tests/GitFlowNextSettingsTests.cs`·`GitFlowNextFlowTests.cs` 신규 + `TestApplication.cs`(git-flow 탐지 대기)
* `RepositorySettings.Get`의 백그라운드 해시 예약은 실제 경합이었다: 방금 읽은 설정을 바로 저장하면 "변경 없음"으로 판정되어 쓰기가 조용히 사라질 수 있었고, `CommitTemplateTests`가 이 경합으로 간헐 실패했다(단독 실행에서는 재현) → 동기 해시로 수정해 3회 연속 통과
* 설치본 preference 불변: SHA256 `0ad00c14f7c2c3746a9c18dad55ea17757d194c29692de1a0575dede38f0b9da` — 이 패스에서 허용된 유일한 사용자 영역 확인
* 테스트 격리: portable `data` 디렉터리 강제 + ConfigDir 위치 검사(실패 시 테스트 실패), fixture는 %TEMP%\sourcegit-tests 생성/삭제, 실행 후 잔여 0
* git-flow 도구는 `sourcegit/artifacts/tools/`(gitignore에 `tools/` 규칙) 안에만 두고 PATH는 개발 프로세스에만 부여; 시스템 설치·전역 git 설정·자격값·키 저장소 접근 없음(§1의 과거 위반 시인 포함)
* `git diff --check` PASS, 변경 파일 CJK(한자·가나) 0 — 자동 스캔 (upstream `ja_JP`/`zh_CN`/`zh_TW`/`Locales.cs`는 이 패스에서 손대하지 않음)

## 7. 통합 리뷰 대응 (2026-11-09 4차)

정본: `Docs/INTEGRATION_FIX_20261109.md`. 요약:

* **수정**: F-1(한 저장에서 두 기본 브랜치 이름 변경 시 부모 참조 유실) · F-2(쓰기 실패 후 병합 시작 → 절반 복사) ·
  F-3(번역 없는 키가 `Text.*` 원문으로 보이던 것 → `en_US` 병합 폴백) · F-5(테스트가 여는 `Repository`/Watcher 미해제) ·
  F-6(테스트가 `GIT_EDITOR`/`GIT_MERGE_AUTOEDIT` 미복원)
* **기각(근거 명시)**: F-4(`Sure()`은 UI 스레드 컨텍스트에서 실행되며 그 await에는 `ConfigureAwait(false)`가 없다) ·
  F-7(git/ssh 원문만 Ordinal로 분류하고, Unix에서는 `LANG`/`LC_ALL=C`을 주입해 출력을 영어로 고정)
* **증거 정정**: E-1(테스트 PATH 삽입을 git-flow 탐색 시점 이전으로 이동, 어셋 자동 탐색, 어셋 없음 분기를 별도 테스트로 분리,
  어셋 경로 표기 정정) · E-2(어셋 실재 확인 및 중복 사본 삭제) · E-3(테스트 수를 TRX 실행 결과로 세어 75건으로 정정) ·
  E-4(분류 불가 키 목록과 "키 없음" 문구를 분리)
* **GUI**: A10 수명주기 6종(초기화/feature 시작·완료/release 시작·완료/hotfix 시작·완료), A11 동시 이름 변경,
  A07 스태시→새 브랜치, A15 태그 삭제(원격 체크/체크해제 양쪽), A15 태그 체크아웃(detached), A15 아카이브(zip)가
  RUNTIME PASS. A07 스태시→패치는 OS 저장 대화상자의 경로 입력이 슬래시 표기를 거부해 **쓰기 미검증**으로 남겼다
  (대화상자 진입과 필드 값 지정까지는 확인). 근거 표는 `Docs/INTEGRATION_FIX_20261109.md` §4.
* **픽스처 경로 주의**: `sourcegit/artifacts/a15-fixture` 등은 제품 저장소 작업트리 안의 일반 디렉터리여서
  그 안의 git 명령은 제품 저장소에 닿는다. 유출 흔적은 없음(합성 태그/브랜치/config 0, worktree 1개, reflog 2건).
  flow/태그/워크트리 GUI 테스트는 격리 경로(`sourcegit/artifacts/fixture`, `NewGitClient/artifacts/a30-fixture/*`)만 사용.

---

## 8. 5차 재리뷰 대응 (2026-11-09, R-1 ~ R-5)

정본: `Docs/INTEGRATION_REREVIEW_20261109.md`, 대응 상세: `Docs/INTEGRATION_REREVIEW_FIX_20261109.md`.

| 식별자 | 내용 | 판정 | 근거 |
|---|---|---|---|
| A10-A11-06 | 병합 저장이 키 쓰기가 중간에 깨져도 반쪽 이름 변경을 남기지 않음: **첫 실패에서 중단 + 복사분 롤백 + 잔존 키 명시** | **PASS (headless)** | 3건의 회귀 테스트(복사/참조 재연결/삭제 단계별 실패 주입) + 기존 온전한 실패 테스트. 실패 주입은 git의 "다중 값 키는 단일 값으로 덮어쓸 수 없음(`exit 5`)" 규칙 — 목·타이밍 없음 |
| A10-A11-03 | 기본 `dotnet test`는 외부 도구 없이 통과, 실도구 수명주기는 opt-in | **PASS** | `[Trait("Requires","git-flow-next")]` + `VSTestTestCaseFilter`; 기본 77/77(어셋 부재 포함), opt-in 78/78 |
| A10-A11-04 | 실패 알림이 키 개수만큼 중복되지 않고 한 번의 집계 | **PASS (headless)** | `Command.ErrorReceiver` + `KeyBatch`, 알림 1건에 실패 키·잔존 키·git 원문 |
| (제품 회귀) | git-flow 탐색 probe의 결과 경합: 최신 probe만 반영, 초기화 시점 `None` 덮어쓰기 제거 | **PASS (수정)** | `Native.OS`의 `_gitFlowProbeIndex`; opt-in 수명주기가 `gitflow.branch.release.tagprefix` 부재로 실패하던 현상 소멸 |
| (하니스) | 픽스처로 지정한 경로가 실제로 독립 저장소인지(git toplevel) 쓰기 전에 확인하고 다르면 중단 | **PASS** | `GitRepositoryFixture.Create()` 가드. `sourcegit/artifacts/a15|a16|a17|a27|a28|a29-fixture`·`ssh-fixture`는 제품 작업트리 안의 일반 디렉터리로 확인됨 |
| (문서 정정) | A01 선택 줄 범위 스테이징은 제품에 UI가 **존재** → `ENV`가 아니라 **GUI 자동화 대기** | **정정** | `TextDiffView.UpdateSelectedChunk` → `PatchGenerator` → `git apply --cache --index`; `INTEGRATION_REREVIEW_FIX_20261109.md` §5.1 참조 |
| (문서 정정) | 지난 4차의 F-4 "성립하지 않음" → **지적대로 수정됨**이 정확 | **정정** | `INTEGRATION_FIX_20261109.md` §2 정정 |
| (문서 정정) | "Windows의 git은 지역화되지 않는다" → **이 설치는 gettext 카탈로그가 없어 영어** | **정정** | `git version --build-options` = `gettext: enabled`, `share/locale` 없음, `LC_ALL=de_DE/fr_FR` 측정 |

실측 실행 수치(TRX/콘솔 기준; 지난 4차의 75에서 R-1 회귀 3건 추가, 수명주기는 기본 실행에서 제외):

| 구성 | 총 | 통과 | 실패 |
|---|---|---|---|
| Debug 기본 (어셋 있음) | 77 | 77 | 0 |
| Debug 기본 (어셋 디렉터리를 옮겨 없앰) | 77 | 77 | 0 |
| Debug opt-in (`-p:GitFlowNextIntegration=true`, 어셋 있음) | 78 | 78 | 0 |
| Debug opt-in (어셋 없음) | 78 | 77 | 1 — 수명주기, 어셋 경로와 git/git-flow 상태를 출력 |
| Release 기본 | 77 | 77 | 0 |
| Debug/Release `src` 빌드 | — | — | 경고 0 / 오류 0 |

제품 저장소 위생 재확인: HEAD `c0f40a2e78d81a7474628847727644fc0c56f558`, 로컬 `gitflow.*` 키 0,
stash 0, worktree 1, reflog 2, 태그는 upstream 릴리스 태그만(테스트 생성 태그 없음).

§7의 "75개"는 위 표로 대체한다(구식 진술).

## 9. 최종 통합 리뷰 N-1 ~ N-7 반영 (2026-11-09 6차)

상세 근거는 `INTEGRATION_FINAL_FIX_20261109.md`. R-1 ~ R-4는 통과로 확인되었다.

| 항목 | 판정 | 근거 |
|---|---|---|
| N-1 롤백이 기존 키를 삭제 | **PASS (수정)** | 복사 단계가 대상 키의 기존 보유를 구분해 되돌림/삭제를 결정. 반대 되돌림 실험에서 회귀 테스트 실패 확인 |
| N-2 병합 스냅샷 vs `--local` 쓰기 | **PASS (수정)** | 쓰기 스냅샷을 `config --local -l`로 분리, 상속 전용 설정은 읽기 전용 + 저장 시 write 0건. GUI 양쪽 상태 스크린샷 확보 |
| N-3 probe 세대 번호 배정 위치 | **PASS (수정, 한계 명시)** | 요청 지점으로 이동 + 요청 수 공개. 스케줄링 역전 자체는 git의 PATH 조회 때문에 결정적 재현 불가 → 불변식 회귀망으로 대체 |
| N-4 복제 리포지토리 미해제 | **PASS (수정)** | `CloseRepository` 추가, 두 테스트에서 디렉터리 삭제 전에 닫음 |
| N-5 미시도 키를 실패로 표기 | **PASS (수정)** | 건너뛴 키는 목록에 남기지 않음. 반대 되돌림 실험에서 회귀 테스트 실패 확인 |
| N-6 정리 커맨드 인용·이스케이프 | **PASS (수정)** | `GitFlowSettings.CleanupLine` 단일화( 홑따옴표 이스케이프 포함). 반대 되돌림 실험에서 회귀 테스트 실패 확인 |
| N-7 설정 읽기 실패 은폐 | **PASS (수정)** | 읽기도 `ErrorReceiver`로 신고, `ReadFailed`면 쓰기 0건 + git 원문 인용. 반대 되돌림 실험에서 회귀 테스트 실패 확인 |

실측: Debug 기본 83/83, Debug opt-in 84/84, Release 기본 83/83, src Debug·Release 빌드 경고 0/오류 0.
GUI(제품 UI): 로컬 키 보유 저장소는 편집기 표시(범위 줄 로컬 28·전역 0), 키가 윗쪽 파일에만 있는
저장소는 읽기 전용 안내(범위 줄 로컬 0·전역 28) — `artifacts/a33A-06-15-settings-open.png`,
`artifacts/a33B-06-15-settings-open.png`.

§8의 77/78 실측 표는 §4·§9의 83/84로 대체된다(구식 진술).

## 10. 설정 격리·스케줄링·부분 중복 (2026-11-09 7차)

상세 근거는 `ISOLATION_AND_PROBE_SCHEDULING_FIX_20261109.md`.

### 10.1 확인된 것

| 항목 | 판정 | 근거 |
|---|---|---|
| Git 설정 격리 불변조건 | **PASS** | `Prepare()`가 첫 Git 실행 전에 3개 변수를 fixture로 강제. 합성 sentinel 왕복 + 원천 라벨 검증 3건. `EnforceProcess()` 무력화 시 테스트 실패(민감) |
| N-3 요청 시각 캡처·게시 규칙 | **PASS (결정적)** | request/run/publish 3분할. 답 도착 순서를 테스트가 정하는 UNIT/HEADLESS 테스트 + 실제 프로브 루프. 원복 2종 모두에서 실패(민감) |
| N-1 목적지 기존 값 보존 | **PASS** | 6차 확인 유지. 추가로 목적지가 윗쪽 파일에도 있는 경우 롤백 후 잔여 0건을 테스트로 고정 |
| N-2 항목이 두 파일에 나뉜 경우 | **PASS (수정)** | 이름 바꾸는 항목의 키가 local/윗쪽에 나뉘어 있으면 쓰기 0건으로 거부. 반대 되돌림에서 회귀 테스트 실패(민감) |

### 10.2 확인하지 못한 것(가정 금지)

- **빌드 시점 `git describe`**: SDK 10의 `Exec`은 `EnvironmentVariable`을 지원하지 않아(실측
  `MSB4064`) 프로젝트 파일 안에서 자식 환경만 격리할 수 없다. 이번 라운드의 모든 실행은 격리 셸에서
  시작해 상속받았지만, 클린 체크아웃에서 평범한 셸로 빌드하면 그 사용자의 환경을 읽는다 — 예외로 남긴다.
- 태그 접두어 변경이 **윗쪽 파일에만 정의된 topic branch**에는 닿지 않는다(거기서 한 칸 아래인 로컬에
  덮어쓰는 키를 만들지 않기 때문). 유실이 아니라 도달 범위 문제이며 `설정 범위` 표시가 이를 안내한다.
- 실사용자·기계 설정의 변경 여부는 **확인하지 않았다**(확인은 곧 금지된 읽기이므로). 6차 사고의
  "전송·저장 없음" 진술은 취소했고, ledger에도 사고를 불변조건 위반으로만 남긴다.

### 10.3 실측 (TRX)

| 실행 | 값 |
|---|---|
| Debug 기본 | 88/88 (`artifacts/probes/a34-default-debug.trx`) |
| Debug opt-in | 90/90 (`a34-integration-debug.trx`) |
| Release 기본 | 88/88 (`a34-default-release.trx`) |
| Release opt-in | 90/90 (`a34-integration-release.trx`) |
| tool 없는 가정 기본 | 88/88 (`a34-noasset-default.trx`) |

§9의 83/84 진술은 §10.3으로 대체된다(구식 진술).
