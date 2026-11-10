# SourceGit Classic UX Fork — 구현 계획 및 불변 원칙

> 문서 버전: 1.0  
> 작성 기준: 2026-11-08  
> 프로젝트: SourceGit 기반, SourceTree에 익숙한 사용자를 위한 경량 Git GUI  
> 기본 환경: Windows 11 / C# / .NET 10 / Avalonia 11  
> 상태: **실행 계획 확정, 포크·로컬 빌드·성능 측정은 아직 수행하지 않음**

## 0. 이 문서의 역할

이 문서는 코딩 에이전트의 세션이 변경되거나 컨텍스트가 압축되어도 유지해야 할 **제품 목표, 설계 제약, 범위, 단계별 게이트, 검증 기준**을 정의한다. 작업이 충돌하면 이 문서를 기준으로 판단한다. 실제 코드 구조가 본 문서의 가정과 다르면 코드를 우선 확인하고, 근거를 기록한 뒤 문서를 갱신한다. 임의로 요구사항을 삭제하지 않는다.

### 0.1 한 줄 정의

**SourceGit의 검증된 Git 기능을 유지하면서 SourceTree 방식으로 탐색·스테이징·브랜치 관리·히스토리 작업을 할 수 있도록 재구성한, Windows 우선의 경량 Git 클라이언트.**

### 0.2 성공의 의미

- SourceTree 사용자에게 익숙한 상단 작업 툴바, 좌측 탐색 트리, 중앙 작업 영역과 커밋 그래프, 하단/우측 상세 정보 및 Diff를 제공한다.
- 기존 SourceGit의 Git 커맨드 기능과 데이터 정합성을 손상시키지 않는다.
- SourceGit 원본 대비 불필요한 CPU/메모리 증가를 막고, 실제 SourceTree와 동일 조건에서 자원 사용량을 측정하여 개선 여부를 증명한다.
- 빌드 성공만으로 완료 처리하지 않는다. **실제 GUI를 열어 수동 또는 자동 조작 검증**까지 통과해야 한다.

## 1. 비가역적이지 않은 불변 원칙 (항상 준수)

1. **Git 기능 재작성 금지**: 원본 `Commands/`, 관련 모델과 Git 실행/인증/LFS 로직을 최대한 재사용한다. 버그가 확인되기 전에는 변경하지 않는다.
2. **UX 우선, 픽셀 복제 금지**: SourceTree의 정보 구조와 조작 동선을 참고하되, SourceTree 로고·아이콘·브랜드·이미지·독점 리소스는 복사하지 않는다. 독립적인 디자인 자산과 제품명을 사용한다.
3. **상태 정합성 우선**: 작업 중 브랜치·HEAD·staged/unstaged·커밋 선택의 의미가 변하면 안 된다. UI가 실제 Git 상태를 잘못 표현하는 문제는 출시 차단 버그이다.
4. **원본 애플리케이션을 침범하지 않기**: 기존 SourceGit과 별도 폴더/사용자 데이터/설정/로그를 사용한다. 초기 개발에서는 원본 README에 설명된 Windows portable `data` 디렉터리 사용을 우선 검토하고, 이후 이름·설정 경로를 정식 분리한다.
5. **기능 제거로 최적화하지 않기**: Worktree, LFS, Diff, 인증, Merge/Rebase 등의 원본 기능을 의도적으로 삭제해서 가벼워 보이게 만들지 않는다. POC에서 새 UI의 노출을 미룰 수는 있어도 기존 기능은 유지한다.
6. **성능은 측정 후 변경**: UI 포크 전의 SourceGit 및 SourceTree 성능 기준선을 기록한다. 최적화는 변경 감시·Git 프로세스·바인딩·렌더링 프로파일링 결과에 근거한다.
7. **작은 변경 단위**: 한 번에 거대한 AXAML 파일을 교체하지 않는다. 컴파일 가능한 작은 작업 단위로 분리하고 변경마다 빌드/검증한다.
8. **위험 작업은 명시적 확인**: reset --hard, discard, rebase, branch delete, force push 등은 사용자 확인과 영향 범위 표시를 유지한다. AI 에이전트는 테스트용 저장소 외의 데이터를 파괴하지 않는다.
9. **원본 추적 가능성 확보**: upstream remote, 기준 commit SHA, 적용 patch 목록, 변경 파일 목록을 유지한다. 원본 업데이트를 재적용할 수 있는 구조를 선호한다.
10. **증거 기반 완료**: 작업마다 코드 변경, 컴파일 결과, 스크린샷/테스트, 남은 오류, 측정값을 기록한다. 확인하지 못한 항목은 PASS로 기재하지 않는다.
11. **Windows 우선, 크로스플랫폼 파괴 금지**: Windows 11을 검증 기준으로 하되, 플랫폼 종속 코드는 격리한다. 원본의 macOS/Linux 빌드를 불필요하게 훼손하지 않는다.
12. **불필요한 기능 추가 억제**: AI 기능, Git 엔진 전환, 저장소 인덱스 자체 개발, 새 Diff 엔진 등의 범위 확대를 초기 단계에서 금지한다.

## 2. 원본 SourceGit 확인 결과와 개발 전제

2026-11-08 기준 공개 기본 브랜치 `master`에서 확인한 구조다. **이것은 정적 소스 확인이며 로컬 실행 결과가 아니다.** 최초 작업 시 commit SHA와 버전을 다시 기록한다.

| 항목 | 확인 내용 | 영향 |
|---|---|---|
| 리포지토리 | `https://github.com/sourcegit-scm/sourcegit` | upstream 추적 |
| 라이선스 | MIT (`LICENSE`) | 저작권·라이선스 고지 유지 |
| 언어/런타임 | C#, `net10.0` | .NET 10 SDK 필요 |
| GUI | Avalonia 11.3.22 / AXAML | 화면 변경 중심 개발 가능 |
| MVVM 지원 | CommunityToolkit.Mvvm 8.4.2 | 기존 VM 활용 |
| 에디터 서브모듈 | `depends/AvaloniaEdit` | `git submodule update --init --recursive` 필요 |
| 중요한 화면 | `src/Views/Repository.axaml`, `RepositoryToolbar.axaml`, `Histories.axaml`, `WorkingCopy.axaml` | 화면별 변경 분리 |
| 핵심 VM | `src/ViewModels/Repository.cs`, `Histories.cs`, `WorkingCopy.cs` | 선택/상태 연동 점검 |
| 파일 감시 | `src/Models/Watcher.cs` | `FileSystemWatcher` 및 100ms 타이머 존재, 무작정 제거 금지 |
| Git 실행 | `src/Commands/` | 기존 기능 재활용 |
| 설정 경로 | Windows `%APPDATA%\SourceGit`, portable `data` 가능 | 포크와 원본 충돌 방지 필수 |
| Windows Git | 공식 Git for Windows 권장, MSYS Git 미지원 | 별도 설치 검증 |
| 릴리스 | Release AOT/Trimming 설정 | Debug/Release 성능 비교 시 조건 명시 |
| 기존 테스트 | 루트 `tests` 디렉터리는 확인되지 않음 | 테스트 프로젝트 추가 여부 조사 |

### 2.1 우선 조사할 파일과 사항

- `src/Views/Repository.axaml(.cs)`: 페이지 전환, 좌측 브랜치/원격 트리, 패널 너비, `Histories`/`WorkingCopy` 렌더링.
- `src/Views/RepositoryToolbar.axaml(.cs)`: 커맨드 바 바인딩과 이벤트 핸들러.
- `src/Views/Histories.axaml(.cs)` 및 `src/Views/CommitGraph.cs`: 그래프 렌더링, 리스트 선택, Diff 패널.
- `src/Views/WorkingCopy.axaml(.cs)`: staged/unstaged 표시, 파일/헝크별 조작, 커밋 메시지.
- `src/ViewModels/Repository.cs`: SelectedViewIndex, 개별 페이지 VM, refresh 호출과 cancellation.
- `src/Models/Watcher.cs`: 이벤트 필터, 디바운스, 타이머, watcher 생명주기.
- `src/ViewModels/Preferences.cs`, `src/Models/RepositoryUIStates.cs` 및 실제 설정 저장 부분: 레이아웃 모드와 위치 기억.
- `src/App.axaml.cs`, 프로젝트 메타데이터, 업데이트 로직: 제품명/경로 분리 시 영향.
- `.github/workflows/build.yml`, `global.json`, `README.md`, `THIRD-PARTY-LICENSES.md`.

**코드 조사 규칙:** 파일 이름이 실제 저장소에서 변경되었다면 경로를 강제로 만들지 말고 현재 구조를 따른다. 조사 결과는 `Docs/ARCHITECTURE_AUDIT.md`에 기록한다.

## 3. 목표 UX 사양

### 3.1 화면 구조 (Windows)

```text
┌────────────────────────────────────────────────────────────────────────────────┐
│ 저장소 탭: Repo A | Repo B                       창 제목 / 앱 버튼              │
├────────────────────────────────────────────────────────────────────────────────┤
│ [Commit] [Pull] [Push] [Fetch] [Branch] [Merge] [Stash] [Tag] [More...]       │
├────────────────────────────┬───────────────────────────────────────────────────┤
│ WORKSPACE                  │ File Status 또는 History (전환)                  │
│   File Status              │                                                   │
│   History                  │ History: 그래프·커밋 목록                         │
│   Search                   │ ────────────────────────────────────────────────  │
│ BRANCHES                   │ 선택한 커밋 정보 / 변경 파일 / Diff               │
│   main / feature/*         │                                                   │
│ REMOTES / TAGS / STASHES   │ File Status: Staged / Unstaged / Diff / Commit     │
│ WORKTREES / SUBMODULES     │                                                   │
└────────────────────────────┴───────────────────────────────────────────────────┘
```

화면은 SourceTree의 작업 모델을 목표로 하되 컴포넌트와 비주얼은 SourceGit 방식에 맞게 구현한다. 초기에는 **Windows 기본 창 기준 1440×900, 1920×1080, 125%/150% DPI**를 테스트한다. 최소 창 크기와 레이아웃 축소 동작은 P0에서 정한다.

### 3.2 UX 기능별 중요도 및 게이트

| ID | UX / 기능 | POC | MVP | 요구 동작·검증 |
|---|---|---|---|---|
| U01 | 저장소 탭과 활성 저장소 | 기존 재사용 | 개선 | 탭 전환 시 서로 다른 저장소 상태 혼선 없음 |
| U02 | 상단 툴바 (Commit/Pull/Push/Fetch/Branch/Merge/Stash) | 필수 | 완성 | 버튼이 기존 Command/VM으로 동작, 비활성 상태 정확 |
| U03 | 좌측 File Status/History 탐색 | 필수 | 완성 | 클릭/단축키로 빠르게 전환, 포커스 안정 |
| U04 | Local/Remote/Tags/Stashes/Worktrees 트리 | 필수 | 완성 | 현재 HEAD 표시, 펼침/선택/컨텍스트 메뉴 정상 |
| U05 | History 그래프와 커밋 목록 | 필수 | 완성 | 그래프·커밋 선택 정합성, 필터 유지 |
| U06 | 커밋 상세 정보/파일 목록/Diff | 필수 | 완성 | 파일 선택 때 Diff 정확, 이진 파일 안전 표시 |
| U07 | Staged/Unstaged 목록 | 필수 | 완성 | 변경 감지와 스테이징 후 표시 일치 |
| U08 | 파일 단위 Stage/Unstage/Discard | 필수 | 완성 | 확인 없는 파괴 작업 금지, 작업 후 상태 일치 |
| U09 | 헝크·라인별 Stage/Unstage | 원본 기능 유지 | 필수 | 기존 SourceGit의 가능 범위 확인 후 동작 유지 |
| U10 | Commit / Amend / 메시지 입력 | 필수 | 완성 | 공백 메시지, amend 경고, 작업 중 중복 클릭 방지 |
| U11 | 브랜치 Checkout/Create/Merge/Rebase | 기본 경로 | 필수 | 워킹트리 변경이 있을 때 보호 및 오류 전달 |
| U12 | 우클릭 메뉴·단축키 | 기본 경로 | 완성 | 작업 대상과 메뉴 명령 일치, 충돌 없음 |
| U13 | Splitter와 레이아웃 기억 | 필수 | 완성 | 다시 실행해도 너비/모드 유지 |
| U14 | 검색, 커밋 필터, 다중 선택 | 기존 수준 | 개선 | 기존 기능 회귀 없음 |
| U15 | LFS/충돌 해결/Submodule/Worktree 고급 기능 | 기존 경로 보존 | 회귀 검증 | 초기엔 새 UI 재디자인보다 기능 연결 유지 |
| U16 | 접근성·키보드·DPI·Light/Dark | 기본 | 필수 | 포커스 탐색, 텍스트 잘림, 대비 검사 |

> **POC의 완성 기준은 'SourceTree처럼 보이는 스크린샷'이 아니라 U02~U08, U10, U13이 실제 저장소에서 동작하는 상태**이다.

### 3.3 주요 작업 흐름 (사용자 관점)

**A. 변경 파일 커밋**
1. 저장소 선택 → File Status 진입 → Unstaged에서 파일 선택.
2. Diff 확인 → 파일 또는 헝크 Stage → Staged에 즉시 반영.
3. 메시지 작성 → Commit → 히스토리 상단과 워킹트리 상태 갱신.
4. Git CLI `git status --porcelain=v1` 및 `git log -1` 결과와 GUI 상태 비교.

**B. 브랜치 전환**
1. 좌측 Local Branch 선택 → 우클릭 Checkout.
2. 미커밋 변경 존재 시 원본 SourceGit의 보호 UX 유지.
3. 전환 성공 시 HEAD 표시, 히스토리, 파일 상태 갱신.
4. 원본 Git 상태와 일치하는지 검증.

**C. 커밋 탐색 및 비교**
1. History 진입 → 그래프에서 커밋 선택.
2. 메시지, 해시, 작성자, 변경 파일 표시.
3. 파일 선택 → Diff 출력 → 다른 커밋으로 바꿔도 오래된 Diff가 남지 않음.

**D. Worktree 확인**
1. 좌측 Worktrees 펼침 → 목록과 경로 표시.
2. 기존 SourceGit Worktree 관련 명령이 계속 사용 가능.
3. 서로 다른 워크트리의 브랜치·변경 파일을 섞어 보여주지 않음.

### 3.4 SourceTree와 같아야 하는 것 / 달라도 되는 것

- **같아야 하는 것:** 탐색 위치, 명령 접근성, 작업 순서, 스테이징/히스토리 정보 배치, 선택 상태 피드백, 파괴적 조작의 확인 흐름.
- **달라도 되는 것:** 정확한 픽셀, 아이콘, 창 테두리, 색상, 메뉴의 장식적 요소, 내부 구현, SourceGit 고유의 유용한 고급 기능.
- **초기 범위 제외:** SourceTree 전체 설정/플러그인/계정 UX 1:1, 새로운 Git 서비스 통합, 다국어 전면 재작업, 변경된 Git 엔진, 새로운 그래프 렌더러.

## 4. 구현 전략

### 4.1 레이어 경계

```text
Classic UX Shell (새 AXAML 및 얇은 UI 상태)
     ↓ (기존 바인딩·명령 재사용)
SourceGit ViewModels (Repository, Histories, WorkingCopy, Preferences)
     ↓
SourceGit Models / Commands / Watcher
     ↓
Git for Windows / 로컬 저장소
```

**금지:** UI 컨트롤의 click handler에서 Git CLI 문자열을 직접 조합해 실행하거나, 복수 화면에서 동일 Git 작업을 독자적으로 구현하는 방식.

### 4.2 레이아웃 전환 방식

- 초기 POC에는 `Classic UX`를 별도의 opt-in 구성으로 도입한다.
- **권장:** 앱 시작 시 레이아웃을 선택하도록 하고 변경 후 재시작 적용. 일단 실행 중에 기존/새 뷰를 모두 생성해 두는 방식은 VM/Watcher 중복 및 메모리 증가 위험이 있다.
- 현재 `Repository.axaml`가 다수의 자식 화면과 페이지 상태를 담고 있으므로 먼저 호스팅 방식·기존 VM 재사용 가능 여부를 검증한다.
- 이미 존재하는 `Histories`, `WorkingCopy`, `BranchTree`를 그대로 호스팅한다. 새로운 기능 추가를 위해서가 아니면 원본 클래스의 공개 계약을 변경하지 않는다.
- 레이아웃 설정은 기존 환경설정 저장 구조를 먼저 분석하고, 설정 충돌을 피하는 방식으로 추가한다.
- 앱이 크래시하거나 레이아웃이 손상되면 기존 UX로 돌아갈 수 있도록 안전한 복구 경로를 둔다.

### 4.3 파일 수정 계획 (조사 후 확정)

| 범위 | 예상 변경 파일 | 원칙 |
|---|---|---|
| Shell/탐색 | `Repository.axaml(.cs)`, 신규 `Views/Classic/*` | 작은 조각으로 분리, 원본 계약 유지 |
| 툴바 | `RepositoryToolbar.axaml(.cs)` 또는 신규 Classic 툴바 | 기존 커맨드 Binding 유지 |
| History | `Histories.axaml(.cs)`와 관련 스타일 | 그래프 렌더링 코어 보존 |
| WorkingCopy | `WorkingCopy.axaml(.cs)`와 관련 스타일 | Stage/Unstage 로직 보존 |
| 설정 | Preferences / RepositoryUIStates / 리소스 | 모드·Splitter 상태 저장 |
| 자동 테스트 | 신규 테스트 프로젝트/스크립트 | repo fixture, headless UI, smoke |
| 측정 | `tools/bench/` 또는 `scripts/bench/` | 원본/포크/SourceTree 비교 가능 |

**공통 파일 소유권:** `Repository.axaml`, `Repository.cs`, `Preferences`는 복수 워커 동시 편집 금지. 선행 구조 정리 후 각 워커가 독립 파일에만 작성한다.

## 5. 개발 단계 및 인수 기준

### P0 — 원본 복제, 빌드, 기준선 (필수·선행)

**작업**
- 사용자 계정에서 GitHub 포크 준비; 로컬 `git clone --recurse-submodules <FORK_URL>` 또는 기존 원본 clone + fork remote 구성.
- `upstream`을 공식 SourceGit으로 지정하고 기준 commit SHA, 버전, 수정 전 상태 기록.
- `dotnet --info`, `git --version`, `git submodule status`, Windows SDK/환경 확인.
- `dotnet restore`, `dotnet build`, `dotnet run --project src/SourceGit.csproj` 수행.
- `Release` 빌드 결과도 확인하되, AOT/trim 특성상 Debug와 직접 성능 비교하지 않는다.
- 기존 SourceGit 개인 설정은 백업하고 포크의 사용자 데이터 경로를 분리한다.
- SourceTree와 SourceGit의 현재 UI 캡처(History, File Status, Branch context menu, Commit) 확보. 가능한 경우 사용자가 실제 사용하는 SourceTree 버전의 화면을 기준으로 삼는다.
- 현재 SourceGit의 실제 화면 및 클래스 간 바인딩을 조사하고 `Docs/ARCHITECTURE_AUDIT.md` 기록.
- 성능 측정: 아래 7장 절차대로 원본 두 앱의 기준선 확보.

**통과 기준:** 원본 SourceGit이 Windows에서 실행되고, 수정 전 기준선과 참조 UI 캡처 및 구조 분석이 저장됨. **실패 시 구조 변경 시작 금지.**

### P1 — Shell/툴바/좌측 탐색 POC

**작업**
- SourceTree 정보 구조를 반영한 Classic shell 설계·기본 구현.
- 기존 저장소 탭 유지; 상단 Commit/Pull/Push/Fetch/Branch/Merge/Stash/Tag 명령 배치.
- 좌측 File Status/History/Branches/Remotes/Tags/Stashes/Worktrees 접근.
- 주요 명령은 반드시 기존 ViewModel의 커맨드에 연결.
- 패널 Splitter 적용 및 선택·확장 상태 보존.

**통과 기준:** 창을 열고 저장소 전환·File Status/History 전환·브랜치 트리 탐색·툴바 클릭이 정상 동작. 원본 기능 삭제 없음. 스크린샷 확보. Git 데이터 변경 전후를 검증.

### P2 — History / WorkingCopy 통합

**작업**
- 커밋 그래프·목록·커밋 상세/Changed Files/Diff의 SourceTree형 배치.
- File Status에서 Staged/Unstaged 표시, 파일 선택과 Diff·Stage/Unstage/Commit 연결.
- 기존 헝크 스테이징/바이너리 Diff와 고급 기능의 연결을 유지.
- 포커스, 선택 상태, 빈 상태(Empty), 오류/로딩 상태를 정상적으로 표시.

**통과 기준:** 위 3.3의 A~C 워크플로를 UI와 Git CLI에서 대조해 전부 PASS. 실패 시 기능별 차이를 명시.

### P3 — UX 디테일 및 검증 자동화

**작업**
- 우클릭 컨텍스트 메뉴, 키보드 단축키, 툴팁, 더블클릭, 메시지/모달 동작 정리.
- SourceTree 참조 화면과 캡처를 비교하고 과도한 밀도/정렬/잘림 수정.
- Avalonia Headless 테스트로 핵심 Binding 및 레이아웃·키보드 이벤트 커버.
- 실제 Windows 창에서 수동 smoke 및 가능하면 UI 자동화 수행.
- Light/Dark, DPI, 최소 창 크기, 창 복구·설정 유지 검증.

**통과 기준:** 6장의 기능 회귀 매트릭스 통과. 화면 캡처·실패 목록 기록.

### P4 — 성능 계측 및 병목 개선

**작업**
- P0과 동일한 저장소·시나리오에서 SourceTree / 원본 SourceGit / 포크 3종 측정.
- UI 렌더 부하, 변경 감시, `git.exe` 중복 실행, Diff 변경, 메모리 캐시 등 프로파일링.
- 높은 부하가 확인된 경우에만 debounce/cancel, 가시 영역 갱신, 객체 캐시, UI virtualization 등을 검토.
- `Watcher.cs`의 100ms 타이머는 존재만으로 CPU 병목이라고 단정하지 않음.
- 최적화가 UI 반응 지연이나 상태 누락을 유발하지 않도록 검증.

**통과 기준:** 성능 보고서에서 비교 조건·평균/최대·반복 횟수·변경 전후 결과가 재현 가능. 기능 회귀 없음.

### P5 — 배포/업스트림 유지보수 (POC 이후)

- 포크 제품명·설정 디렉터리·앱 식별자/아이콘/업데이터를 독립적으로 조정.
- MIT 및 Third-party 라이선스 고지 유지.
- Windows x64 빌드/패키징 CI, 크래시 로그, 릴리스 노트, upstream 변경 반영 절차 정의.
- macOS/Linux는 원본 회귀 빌드만 우선 확인하며 Windows 기능 검증이 끝나기 전 별도 플랫폼 기능 개발은 하지 않는다.

### 5.1 일정·의존성 추정 (AI 구현 + 사람 검증, 계획용)

| 단계 | 예상 소요 | 선행 조건 | 산출물 |
|---|---:|---|---|
| P0 | 0.5~1.5일 | 저장소·Windows 개발 환경 | 기준선, 빌드, 구조/UX 감사 |
| P1 | 2~4일 | P0 PASS | 작동하는 Classic shell/Toolbar/Sidebar |
| P2 | 2~4일 | P1 주요 화면 연동 | History/WorkingCopy 수직 슬라이스 |
| P3 | 3~6일 | P2 Git 상태 정합성 검증 | 단축키/메뉴/회귀 테스트 |
| P4 | 2~5일 | P0 비교 데이터·P3 | 성능 개선 및 검증 리포트 |
| P5 | 1~3일 | MVP 완료 | Windows 패키징·라이선스·업스트림 절차 |

- **1차 POC 목표:** P0+P1, 즉 3~5일 수준의 최초 동작 검증. P2까지 포함한 POC는 약 1~2주를 계획 기준으로 둔다.
- **일상 사용 가능한 MVP:** 전체 약 2~4주 예상. 모델의 처리 속도보다 실제 GUI 검증과 Git 상태 회귀 테스트가 일정을 좌우한다.
- 이 수치는 특정 모델/에이전트의 실측 처리량이 아니며, 시행 전 실제 SourceGit 빌드 및 SourceTree 비교 화면이 준비되어 있다고 가정한 계획용 범위이다.
- **의존성:** P0 → P1 → P2 → P3 → P4 → P5. P0 기준선 수집 후 측정 자동화 일부를 다른 작업과 병렬화할 수 있으나 최종 성능 평가는 P3 이후.

## 6. 기능 테스트 매트릭스 (반드시 실제 Git 저장소 사용)

| ID | 상황 | 기대 결과 | 위험도 |
|---|---|---|---|
| T01 | 빈/신규 저장소 오픈 | 오류 없이 빈 상태 표시 | 보통 |
| T02 | 추적 파일 변경 및 새 파일 생성 | Staged/Unstaged 분류 정확 | 높음 |
| T03 | 파일 stage → unstage | UI와 `git status` 일치 | 매우 높음 |
| T04 | 헝크 stage → commit | 의도한 헝크만 커밋 | 매우 높음 |
| T05 | Commit / Amend | 올바른 HEAD·히스토리 반영 | 높음 |
| T06 | 미커밋 변경 있는 브랜치 checkout | Git 보호 동작과 안내 유지 | 매우 높음 |
| T07 | Branch create / rename / delete | 선택 대상 정확, 위험 작업 확인 | 높음 |
| T08 | Merge conflict 발생/해소 | 충돌 상태 표시, 해결 후 정합성 | 매우 높음 |
| T09 | Interactive rebase 취소/완료 | 원본 동작 유지 | 매우 높음 |
| T10 | Stash 생성·적용·삭제 | 변경 상태 일치 | 높음 |
| T11 | Fetch/Pull/Push 실패(오프라인 등) | 오류 표시, UI 멈춤/잘못된 성공 없음 | 높음 |
| T12 | Worktree 2개 이상 | 경로/브랜치/선택 상태 분리 | 매우 높음 |
| T13 | Git LFS 추적 파일 | 바이너리 및 포인터 안전 처리 | 높음 |
| T14 | 커밋 1만 건 이상 | 그래프·스크롤·선택 안정 | 보통 |
| T15 | 연속 파일 변경 (Unity 동작 모사) | 갱신 누락·상태 흔들림 없음 | 높음 |
| T16 | 다른 저장소 탭으로 왕복 | 상태 혼동/메모리 급증 없음 | 높음 |
| T17 | 설정 저장 후 앱 재시작 | Splitter·레이아웃 상태 보존 | 보통 |
| T18 | SourceGit 원본 앱과 함께 설치/실행 | 설정·실행 파일·로그 충돌 없음 | 높음 |
| T19 | 원본 레이아웃 복귀 | 선택된 레이아웃 사용 가능 | 보통 |
| T20 | Light/Dark + 125%/150% DPI | 텍스트 잘림·포커스 불가 없음 | 보통 |

테스트 결과는 `Docs/QA_MATRIX.md`에 PASS/FAIL/NOT TESTED, 재현 절차, 환경, 로그/스크린샷 링크를 기록한다. 사용자 실제 작업 저장소에서는 파괴적 테스트를 금지하고 별도 fixture 저장소를 사용한다.

## 7. 성능 검증 계획

### 7.1 측정 대상

- **대상 A:** SourceTree 현재 설치 버전.
- **대상 B:** 공식 SourceGit 동일 시점 릴리스/동일 SHA 빌드.
- **대상 C:** 포크 Release 빌드 (가능하면 AOT 여부와 publish 옵션까지 동일).
- Windows 11 동일 PC, 동일 Git for Windows, 동일 저장소, 동일 환경에서 실행.

### 7.2 측정 시나리오

1. 앱 시작 및 저장소 열기: 첫 화면 렌더까지 시간.
2. 유휴: 저장소 오픈 후 60초 안정화 → 5분 측정.
3. 파일 변경: 약 100/1000건의 작은 파일 변경 이벤트를 fixture에서 재생.
4. 대형 History: 커밋 1만 건 이상에서 로딩/스크롤/선택.
5. Diff: 큰 텍스트 파일과 여러 수정 파일을 전환.
6. Worktree: 2~3개를 열어두고 변경 감시.
7. 장시간 사용: 가능하면 30분 반복 작업 및 메모리 추이.

### 7.3 지표 및 방법

- 프로세스별 `Private Bytes`, `Working Set`, 최대치, 유휴 평균/피크.
- `CPU time` 증가량을 실제 경과 시간으로 나눈 CPU 사용량. **논리 CPU 전체 기준 퍼센트인지 단일 코어 기준인지 반드시 표기**.
- 자식 `git.exe` 프로세스의 개수·누적 CPU·중복 실행 여부를 구분.
- UI 응답 지연(입력→화면 반영), 초기 로딩 시간, 파일 갱신 지연(중앙값/P95).
- 최소 3회 반복 후 중앙값 보고. Debug와 Release/AOT 혼용 금지. 백신/인덱싱/Unity Editor 실행 조건 기록.
- 측정 자동화 스크립트는 `tools/bench/`에 두고 시나리오, 사용한 저장소, 원시 결과(CSV/JSON), 결과 요약(MD)을 남긴다.

### 7.4 정량 목표 (목표치이지 현재 검증된 성능이 아님)

- **POC 비회귀 게이트:** 같은 조건의 원본 SourceGit 대비 유휴 메모리 및 CPU 중앙값이 각각 +10% 이상 악화되지 않도록 목표. 초과 시 프로파일링하거나 이슈로 남기고 원인 규명 전 배포 보류.
- **MVP 최적화 목표:** SourceTree 대비 자원 사용량을 20% 이상 줄이는 것을 검증 목표로 설정하되, 달성 가능 여부를 P0 측정 후 재평가.
- **기능 게이트:** 최적화 때문에 스테이징/상태 동기화 실패나 변경 이벤트 누락이 한 건이라도 발생하면 해당 최적화 롤백.

### 7.5 Unity 프로젝트 관련 주의점

- `.gitignore`에 `Library/`, `Temp/`, `Obj/`, `Logs/` 등 프로젝트 정책상 추적하지 않는 디렉터리가 적절히 제외됐는지 조사.
- `.meta` 쌍, 큰 이진 파일, Git LFS, 대량 수정 이벤트, 긴 경로, Worktree 레이아웃을 fixture에 포함.
- 원본 Unity 저장소의 비공개 파일을 외부 벤치마크/이슈/로그에 유출하지 않는다.
- 대형 저장소에서 성능 이슈가 발생해도 UI만의 문제인지 Git CLI 및 파일 시스템/백신 영향인지 분리한다.

## 8. AI 에이전트 운용

### 8.1 기본 도구 결정

- **상위 오케스트레이터:** 사용 가능한 고성능 추론 모델. 범위 관리/설계 승인/통합 리뷰를 담당한다. 기존 Orca 오케스트레이션을 사용 중이면 재사용한다.
- **구현 워커:** Pi + 사용자가 선택한 `Qwen3.8-Flash-Next`를 우선한다. 실제 Pi/provider의 모델 명칭과 접근 가능 여부를 `pi --list-models` 또는 해당 provider 카탈로그에서 **탐색 후** 설정한다. **모델 ID/엔드포인트를 추측하여 입력하지 않는다.**
- **대안:** OpenCode의 Build/Plan/Explore/서브에이전트 구성을 사용해도 된다. OpenCode와 Pi를 동시에 동일 트리에 연결하는 것은 피한다.
- 모델보다 테스트·소유권·범위 분할을 우선한다. 인지적 부담이 큰 상태관리/메모리 문제는 고성능 모델의 검토를 받는다.

### 8.2 권장 워커 분해

| 담당 | 범위 | 동시 편집 가능 조건 |
|---|---|---|
| Lead | 분석/계획/통합/게이트 | 공통 파일과 승인 총괄 |
| Worker A | 신규 Classic 툴바/메뉴 뷰 | 독립 파일 분리 후 |
| Worker B | 신규 Classic 사이드바/탐색 뷰 | 독립 파일 분리 후 |
| Worker C | History/Commit Detail 레이아웃 | `Histories` 파일 소유권 독점 |
| Worker D | WorkingCopy/Staging 레이아웃 | `WorkingCopy` 파일 소유권 독점 |
| Reviewer | diff/컴파일/테스트/성능 결과 검토 | 읽기 전용 권장 |

- 처음부터 4워커를 실행하지 않는다. **P0, Classic shell 설계/소유권 분리까지는 단일 통합 워커**가 안정적이다.
- 병렬화는 서로 다른 파일 단위로만. 같은 AXAML/VM에 여러 워커의 패치를 동시에 적용하지 않는다.
- 브랜치 또는 Git Worktree 분리는 안전한 개발 단위일 때만 사용. 빌드 자원 경합을 피하고, 최종 통합 및 GUI 실행 검증은 직렬화한다.
- 각 작업 종료 시 변경 파일·명령 출력·스크린샷·문제·다음 작업을 기록한 짧은 핸드오프 남긴다.

### 8.3 모델 실패 시 정책

- 사용자가 지정한 Qwen 모델을 호출할 수 없다면 카탈로그/설정 원인을 먼저 확인한다.
- 이름이 비슷한 다른 모델을 몰래 선택하지 않는다. 정확한 모델이 연결되지 않으면 `BLOCKED: MODEL_NOT_AVAILABLE`과 재현 로그를 남긴다.
- 도구 호출 능력이 없는 endpoint이면 사용 가능한 호환 API/API 모드 확인 후 연결한다.
- 실패한 에이전트 실행을 무한 반복하지 않는다. 동일 오류 재시도는 원인을 바꿨을 때만 한다.

## 9. 리스크와 대응

| 리스크 | 심각도 | 방지책 |
|---|---|---|
| 거대한 Repository.axaml 동시 수정·충돌 | 높음 | 선행 분리 및 파일 소유권 고정 |
| 새 레이아웃에서 VM 중복 생성·Watcher 중복 | 높음 | 기존 VM 인스턴스 공유/생명주기 검사, 한 모드만 생성 |
| 설정 경로·업데이터가 원본 SourceGit과 충돌 | 매우 높음 | portable 또는 앱 아이덴티티 별도 분리, 원본 설정 백업 |
| GUI와 실제 Git 상태 불일치 | 매우 높음 | 테스트 fixture에서 `git status` / `log` 검증 |
| 메모리 최적화 때문에 캐시 갱신 누락 | 높음 | 계측→국소 변경→회귀테스트 |
| Release AOT/Trim에서만 오류 | 높음 | Debug 및 Release 스모크 각각 수행 |
| SourceTree 상표·이미지 자산 복제 | 보통~높음 | 조작 방식만 참조, 원본 자산 사용 금지 |
| 사용자 실제 저장소 손상 | 매우 높음 | 테스트 전용 저장소·수정 전 snapshot·작업 승인 |
| 코드와 가이드 불일치 / upstream 변경 | 보통 | 기준 SHA 기록, 파일명 우선 실측, 단계별 문서 갱신 |
| 작업량 증가·기능 추가 요구 | 높음 | POC와 MVP Scope Gate 고정, 별도 백로그 |

## 10. 작업 브랜치/기록/인수인계 규칙

- 브랜치 예시: `feat/classic-layout-poc` (실제 포크/기준 브랜치 확인 후 생성).
- 독립 작업별 작은 커밋 권장. 커밋 메시지에 구현 범위 및 테스트 근거 포함.
- `Docs/ARCHITECTURE_AUDIT.md`: SourceGit 코드 매핑과 UI 재사용 설계.
- `Docs/UX_PARITY_MATRIX.md`: SourceTree 참조 화면·행동과 현재 구현 사이 차이.
- `Docs/QA_MATRIX.md`: 기능/시각/윈도우 실제 조작 테스트.
- `Docs/PERFORMANCE_BASELINE.md`: 테스트 환경·세 가지 비교 대상·원시 데이터 링크.
- `Docs/DECISIONS.md`: 중요한 설계 변경과 이유, 포기한 대안.
- `Docs/HANDOFF.md`: 최신 완료·진행·차단·다음 작업, 기준 SHA.
- 스크린샷/측정 결과는 적절한 `artifacts/`에 저장하되 비공개 저장소 이름과 경로는 익명화한다.

### 10.1 매 단계 보고 형식

```text
[Stage] P0 / P1 / P2 / P3 / P4
[Baseline] upstream SHA / fork SHA / branch
[Implemented] 완료 항목 (UX ID 링크)
[Changed] 변경 파일과 변경 이유
[Build] Debug/Release 실행 명령 및 결과
[Tests] PASS / FAIL / NOT TESTED + 근거
[UI] 화면 캡처 경로와 비교 결과
[Performance] 원본과의 비교 수치 또는 아직 미측정 표기
[Risks] 발견된 문제와 되돌리기 방법
[Next] 다음 1~3개 작업
```

### 10.2 컨텍스트 압축/세션 이전 시

새 에이전트가 시작되면 반드시 (1) 본 문서 0~1장, (2) `Docs/HANDOFF.md`, (3) `git status` 및 최근 커밋, (4) 현재 단계의 게이트를 확인한다. 작업 상태를 추정하지 않는다.

## 11. 시작 절차 (Windows PowerShell 예시)

> 다음은 사용자에게 GitHub 포크가 이미 존재한다고 가정한 예시다. URL은 본인 포크로 바꿔야 한다. 기존 개인 SourceGit 폴더에 덮어쓰지 않는다.

```powershell
# 기본 툴 확인
git --version
dotnet --info
pi --version
pi --list-models

# 사용자 포크를 만든 후 실행
$forkUrl = "https://github.com/YOUR_GITHUB_ID/sourcegit.git"
git clone --recurse-submodules $forkUrl
Set-Location sourcegit
git remote add upstream https://github.com/sourcegit-scm/sourcegit.git
git remote -v
git submodule status
git rev-parse HEAD

git switch -c feat/classic-layout-poc

dotnet restore
dotnet build

# 기존 SourceGit 설정과 충돌하지 않게 Debug 실행파일 옆에 portable 데이터 폴더 준비
$exe = Get-ChildItem -Path .\src\bin\Debug -Filter SourceGit.exe -Recurse | Select-Object -First 1
if (-not $exe) { throw "빌드 결과 SourceGit.exe를 찾지 못했습니다." }
New-Item -ItemType Directory -Path (Join-Path $exe.Directory.FullName "data") -Force | Out-Null

dotnet run --project src/SourceGit.csproj
```

- GitHub 포크 이름/URL, 사용 가능한 모델 ID, 로컬 빌드 상태는 실행 과정에서 조회한다.
- 빌드 중 문제가 발생하면 별도 기록 후 원인 해결을 우선한다. **P0 성공 없이 P1 코드 수정으로 넘어가지 않는다.**
- 사용자가 인증정보를 메시지나 Git 이슈에 붙여넣게 요구하지 않는다. 기존 provider/Git Credential Manager 설정을 이용한다.
- `git remote add upstream`에서 기존 remote 오류가 발생하면 목록을 확인하고 올바른 URL인지 점검하며 무조건 덮어쓰지 않는다.

## 12. 권장 구현 종료 기준 (Definition of Done)

**POC 종료**
- P0, P1, P2 통과.
- U02~U08/U10/U13 핵심 경로 동작.
- T02/T03/T05/T06/T12/T16/T17은 PASS, 나머지는 NOT TESTED로 정확하게 명시 가능.
- SourceTree 참조 화면 2종 이상과 포크 스크린샷 비교.
- Git 명령 계층 기능 훼손 없음, 중요한 오류가 문서화됨.
- 최소 유휴 CPU/메모리 비회귀 측정 존재.

**MVP 종료**
- P3, P4 통과, 위험도 '매우 높음' 테스트 전부 PASS.
- 파괴적 작업 확인과 오류 메시지 정상.
- 원본 SourceGit 대비 비회귀 및 SourceTree 대비 개선 수치 확보(미달성 시 근거와 대응 계획).
- 원본 설정과 공존, 라이선스 충족, Windows 패키지 재현.

## 13. 참고 자료 (기준 링크)

- SourceGit 원본 및 README: https://github.com/sourcegit-scm/sourcegit
- SourceGit 원본 라이선스: https://github.com/sourcegit-scm/sourcegit/blob/master/LICENSE
- SourceGit 빌드 CI: https://github.com/sourcegit-scm/sourcegit/blob/master/.github/workflows/build.yml
- SourceGit 변경 감시: https://github.com/sourcegit-scm/sourcegit/blob/master/src/Models/Watcher.cs
- SourceGit 메인 Repository UI: https://github.com/sourcegit-scm/sourcegit/blob/master/src/Views/Repository.axaml
- Atlassian SourceTree 브랜치 관리: https://support.atlassian.com/sourcetree/kb/branch-management/
- Atlassian SourceTree 파일 상태: https://support.atlassian.com/sourcetree/kb/viewing-file-status-of-a-repository/
- Avalonia Headless 테스트: https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform
- Pi 원본: https://github.com/badlogic/pi-mono
- OpenCode 에이전트 설정: https://opencode.ai/docs/agents
- Qwen3.8-Flash-Next 모델 원본: https://github.com/QwenLM/Qwen3.8-Flash-Next

---

**결정 요약:** SourceGit 기능 재사용 / Windows 우선 / SourceTree의 동선·정보 구조만 구현 / Pi+Qwen 워커 기본 / P0→P1→P2→P3→P4 게이트 / 빌드와 실제 GUI 조작·성능 수치를 모두 결과로 남긴다.
