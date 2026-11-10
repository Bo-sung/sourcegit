# SourceTree 상세 기능 인벤토리 (Windows · Git 기준)

작성일 2026-11-08 · 소유: 본 문서와 `Docs/SOURCETREE_SOURCE_REGISTER.md`(출처 등록부) ·
참조: `Docs/CORE_FEATURE_AUDIT.md`(포크 구현 상태, I1), `Docs/UX_PARITY_MATRIX.md`(I2)
요청: "소스트리 상세한 피쳐 분석 해야해. 공식 문서 및 인터넷 뒤져서 상세한 기능 리스트업해."

## 0. 기준선 · 읽는 법

- **비교 기준**: Windows SourceTree. 이 머신에 설치·실측된 버전은 **3.4.32(2026-08-28)**. 공식 아카이브 기준 최신 공개는 **3.4.33(2026-10-31)** — 제품 페이지 링크가 3.4.32를 가리켜 스테일 상태. "3.4.33" 표기는 *참조(본 머신 미설치)*.
- **근거 등급** (상세 정의는 등록부 §0):
  `D1` 현행 공식 문서 확인 · `D2` 공식 Windows 릴리스 노트에 버전 특정 · `H` 공식이나 역사적 증거만 · `M` Mac 전용 증거 · `U` 1차 증거 없는 미확인 후보 · `S` 2차 근거(이슈/포럼)만
- **Win 확실성** 열: `Win 명시`(KB가 Windows 절 서술) / `Win 버전특정` / `Win/Mac` (공용 서술) / `Mac 전용` / `역사(Win 0.x)` / `미확인`
- **우선순위**: `1` 즉시(핵심 워크플로) · `2` 단기(자주 쓰임) · `3` 후순위 · `4` 검증만(존재 여부 확인 필요)
- **행 = 기능군(feature group)**: 한 행이 여러 원자 동작을 묶을 수 있다. 묶인 동작은 상세 열에 `[ST-xxx 통합]`으로 원 서술과 근거를 남기고, 흡수된 ID는 §27에서 재사용 금지로 관리한다. 단일 원자 동작만 담는 행도 많다. **행 수는 기능의 개수와 1:1이 아니다.**
- **fork 열**은 포크(SourceGit) 쪽 상태를 I1 어휘로 링크한 것뿐이다. **SourceTree 행의 근거 등급이 fork 구현·동작 PASS를 의미하지 않고, fork 열의 IMPL-SOURCE가 런타임 PASS를 의미하지 않는다.**
- 공식 KB는 Data Center 배너가 붙지만 본문은 데스크톱 절차다(등록부 §0). 스크린샷은 이주 이전 버전일 수 있다.
- macOS 4.x 릴리스 노트·Mac 전용 절차는 Git-Windows 범위 행에 쓰지 않고 §21(범위 밖)에만 뒀다.
- 출처 키 S50~S53(사이트맵·제품 허브)은 **수집 방법의 근거**로 등록부에만 등재되어 이 문서의 행에서는 인용되지 않는다.
- 버전 근거의 1차 출처는 **S02(3.4.33 노트, 3.3.4~3.4.33 내력)**이고, 설치된기준의 개별 파일은 **S03(3.4.32)**다. 무인 설치 KB(S20)은 상단에 "1.8 이후 무효"가 명시된 역사 문서이므로 MSI(S19)와 혼용하지 않는다.

## 1. 저장소 열기 · 북마크 · 탭 · 원격 저장소 브라우즈

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세: 옵션·상태·엣지케이스 | UI 진입점(근거 있는 곳만) | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-001 | 새 로컬 저장소 생성 | 대상 폴더 선택 후 Create. init 옵션·초기 브랜치명 등 세부 항목은 문서에 없음 | `Clone/New` ▸ `Create New Repository` ▸ Destination Path ▸ Create | Win/Mac | D1 S30 | 1 | I1 §3 |
| ST-002 | URL로 클론 | HTTPS·SSH 모두 지원. 최초 HTTPS 사용 시 Bitbucket 인증 팝업 가능 · [ST-003 통합] 웹 ▸ 앱 클론(URI 핸들러): 호스팅 웹 "Clone in Sourcetree" → `sourcetree://` → URL Actions 팝업에서 확인 후 실행. 핸들러는 보안 강화 이력(3.4.21 hardening, 3.4.33 NTLM coercion 수정) [근거 D1 S31 · D2 S02] | `Clone/New` ▸ `Clone Repository` | Win/Mac | D1 S31 | 1 | I1 §3 |
| ST-004 | 원격(호스팅) 저장소 목록에서 클론 | Hosted Repositories 뷰에서 우클릭 ▸ Clone → URL Actions 팝업 | View ▸ Show Hosted Repositories | Win 명시 | D1 S31, S27 | 1 | – |
| ST-005 | 기존 로컬 저장소 등록 | 경로 입력 시 Repository Type 자동 표시, 아니면 invalid path | `Clone/New` ▸ `Add Working Copy` 탭 ▸ Add | Win/Mac | D1 S32 | 1 | I1 §3 |
| ST-006 | 북마크 열 표시/숨김 | 사이드바 토글 · [ST-007 통합] 북마크 폴더 · 재정렬: Add Folder, 드래그로 순서 변경/폴더 이동, +/- 펼침 · [ST-020 통합] 북마크·탭 목록의 저장소 아이콘/정리: 폴더·저장소 아이콘, 로해상도에서 아이콘-이름 겹침 이슈(3.4.19) [근거 D2 S02] [근거 D1 S28] · [ST-008 통합] 북마크 삭제: 삭제 확인 팝업. (역사) 디스크에서 삭제 시 수정/미추적 파일 소실 경고 [근거 D1 S28 · H S04] | View ▸ Show/Hide Bookmarks | Win 명시 | D1 S28 | 1 | – |
| ST-009 | 다중 저장소 탭 + 탭 내비게이션 | 좌우 스크롤 화살표, 옆 폴더 버튼으로 열려 있는 저장소 목록. **Windows 전용 기능(KB 명시)** | 탭 헤더 좌우 컨트롤 | Win 전용 명시 | D1 S29 | 1 | I1 §3 |
| ST-011 | 저장소/탭 표시 이름 변경 | 필터가 걸린 New Tab 화면에서 이름 변경 실패 수정(3.4.32) = 기능 존재 · [ST-010 통합] 탭 헤더 정보: 전체 경로·저장소명·현재 브랜치·저장소 종류를 툴팁으로 표시(3.4.15) [근거 D2 S02] · [ST-012 통합] 동명 폴더 저장소 동시 열기: 같은 폴더명의 두 저장소를 동시에 못 열던 문제 수정(3.4.32) [근거 D2 S02] · [ST-013 통합] 열려 있는 저장소 목록 필터: New Tab 화면 목록 필터 / 계정 전환 후 중복 표시 수정(3.4.24) [근거 D2 S02] | New Tab 화면 / 탭 | Win 3.4.16, 3.4.32 | D2 S02 | 3 | – |
| ST-014 | 호스팅 계정 브라우즈(원격 저장소 뷰) | Edit Accounts…로 계정 추가·인증, **여러 계정 동시 등록**. 대응 호스팅 Bitbucket Cloud/Bitbucket Server/GitHub | View ▸ Show Hosted Repositories 또는 북마크 기어 아이콘 | Win 명시 | D1 S27 | 1 | I1 §3 |
| ST-015 | 지원 호스팅 서비스 범위 | 위 3종 + 3.0에서 GitHub Enterprise·VSTS(현 Azure DevOps)·GitLab(Cloud/CE/EE) 추가. GitLab 그룹 뷰·검색은 3.4.31 · [ST-016 통합] 원격 저장소 목록 필터/검색: 필터 동작(3.4.24), GitLab 저장소 미표시·미검색 수정(3.4.31) [근거 D2 S02] · [ST-222 통합] 원격 저장소 생성(호스팅): 3.0 블로그: 지원 서비스에서 새 저장소 생성·Sourcetree 설정(서비스별 기능 상이) [근거 H S42] | Edit Accounts / Remote Repositories | Win/Mac + 버전특정 | D1 S27 · H S42 · D2 S02 | 2 | – |
| ST-017 | 탐색기 셸에서 앱 열기 | Windows 탐색기 우클릭 "Open in Sourcetree"(3.4.22) | 탐색기 컨텍스트 메뉴 | Win 3.4.22 | D2 S02 | 2 | – |
| ST-018 | 저장소 신뢰(trust) 확인 | 신뢰 프롬프트 + `.git/config` 악성 설정 차단 계열(3.4.33에서 trust prompt·diff config·Hg trust 우회 보안 수정으로 존재 확인) | 저장소 최초 열기 시 프롬프트 | Win 3.4.33(참조) | D2 S02 | 1 | – |

## 2. File Status · 스테이징 · 폐기 · 무시

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-030 | File Status 뷰 열기 | 3가지 경로(탭 / **Ctrl+1** / View ▸ File Status View) | `File Status` 탭 | Win 명시 | D1 S07 | 1 | I1 §3 |
| ST-031 | 파일 상태 판정 | ①수정·추적 ②삭제 ③미추적(신규) ④최신 반영 상태 아이콘 + `git status` 대응 · [ST-043 통합] "Intent to Add" 파일 표시: 3.4.27에서 File Status에서 사라지던 문제 수정 = git intent-to-add 상태 표시 [근거 D2 S02] | File Status 목록 | Win 명시 | D1 S07 | 1 | I1 §3 |
| ST-032 | Unstaged ↔ Staged 섹션 | 체크박스.on=스테이지, off=스테이지 해제. 스테이지된 신규 파일은 `+` 표시 · [ST-037 통합] 스테이지 취소(unstage): 체크박스 해제; (역사) 최상위 메뉴 + 키보드 단축키 부여 — 키 값 문서 미기재 [근거 D1 S33 · H S04] | File Status 목록 | Win 명시 | D1 S07, S33 | 1 | I1 §3 |
| ST-033 | 파일 단위 스테이지 | 헤더 체크로 전체 스테이지, 개별 체크로 파일 단위; 파일 옵션 메뉴 ▸ Stage file · [ST-036 통합] 선택 폴더(디렉터리) 스테이지: 3.4.16에서 "Cannot Stage Selected Folders" 수정 = 기능 존재 [근거 D2 S02] | Unstaged files 체크 / 파일 우클릭 | Win 명시 | D1 S07, S38 | 1 | I1 §3 |
| ST-035 | 라인 단위 스테이징/폐기 | 제품 페이지가 "file, hunk or line" stage/discard 명시. **정확한 클릭 위치는 문서 미기재** · [ST-034 통합] 헝크 단위 스테이지: diff 뷰 "Stage hunk" [근거 D1 S07 · D1 S01] | diff 뷰(구체 위치 미확인) | Win/Mac | D1 S01 | 1 | I1 §3 |
| ST-038 | No Staging(스테이지 없음) 뷰 | "Push changes immediately" 체크박스가 No Staging View에서 비활성이었다는 수정 = 뷰 존재 | File Status 뷰 옵션 | Win 3.4.20 | D2 S02 | 2 | – |
| ST-039 | 변경 폐기(Discard) | 헝크 폐기(Discard Hunk)·파일 폐기. 라벨은 "Discard"→"Discard File Changes"(3.4.19)로 명확화, 확인 대화 존재 · [ST-040 통합] 폐기 시 줄바꿈 보존(Windows): Discard Hunk에서 Windows CRLF가 Unix로 바뀌던 문제 수정 [근거 D2 S02] | diff 뷰 / 파일 우클릭 | Win 명시 + 3.4.16/3.4.19 | D1 S07 · D2 S02 | 1 | I1 §3 |
| ST-041 | 파일 상태 필터/정렬 | 특정 상태만 보기 + 정렬 방식 선택; 트리 뷰 정렬 유지(3.4.27) · [ST-042 통합] 변경 파일 수 표시: 수정/스테이지/미스테이지 개수 표시(3.4.18) [근거 D2 S02] | File Status 상단 컨트롤 | Win 명시 + 3.4.27 | D1 S07 · D2 S02 | 2 | – |
| ST-044 | 파일 무시(ignore) | 파일 단위 Ignore(역사 0.7.6 도입). 설치 시 global ignore 목록 설치 여부 선택 — MSI는 이 프롬프트를 **하지 않음**(=소비자 설치에는 존재) · [ST-278 통합] global ignore 목록 설치: 설치 시 기본 global ignore 목록 설치 여부 선택(MSI는 프롬프트 생략) [근거 D1 S19 · H S04] | 파일 우클릭 ▸ Ignore / 설치 마법사 | 역사(Win 0.x) + D1(MSI 차이) | H S04 · D1 S19 | 2 | – |
| ST-045 | diff에서 Ignore Whitespace | Diff 탭 옵션(3.4.23). (역사) 외부 diff + ignore whitespace 조합 수정 | Diff 탭 체크 | Win 3.4.23 | D2 S02 · H S04 | 2 | I1 §3 |
| ST-046 | 파일 더블클릭 동작 설정화 | 기본 동작을 사용자화(3.4.22). 3.4.26에서 컨텍스트 "Open File"에 영향 준 회귀 수정 | Tools ▸ Options | Win 3.4.22, 3.4.26 | D2 S02 | 2 | – |
| ST-047 | 파일을 외부 편집기/탐색기에서 열기 | "Open File" 컨텍스트(3.4.26 맥락), (역사) Show In Explorer | 파일 우클릭 | 역사 + 3.4.26 | H S04 · D2 S02 | 2 | I1 §3 |

## 3. 커밋

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-060 | 커밋 실행 | Pending files에서 파일 선택 → 메시지 입력 → Commit · [ST-062 통합] Commit & Push(즉시 푸시): "Push changes immediately" 체크박스 [근거 D2 S02] | toolbar `Commit` | Win/Mac | D1 S33, S38 | 1 | I1 §3 |
| ST-061 | Amend latest commit | 체크박스로 직전 커밋에 amend. 위치는 "Push changes immediately" 뒤로 이동, 메시지 교체 거부 시 체크 유지 등 동작 다듬기 이력 · [ST-066 통합] 병합 충돌 후 커밋 메시지 보존: 충돌 해결 중 작성한 메시지 유지 관련 수정 이력 [근거 D2 S02] | Commit 대화 체크박스 | Win 버전특정 | D2 S02 | 1 | I1 §3 |
| ST-063 | 커밋 메시지 템플릿 | git commit template 지원(3.4.20 Changes) · [ST-064 통합] 커밋 메시지 편집 보조: fixed-width 폰트 옵션 + 컬럼 가이드(기본 72자, 역사적 근거) [근거 H S04] | 저장소/전역 template 설정 + Commit 대화 | Win 3.4.20 | D2 S02 | 2 | I1 §3 |
| ST-065 | 커밋 메시지 내 이슈 링크 | Commit Text Replacements: Jira/Crucible/Other 유형, regex 패턴 + `$1` 치환, 클릭 시 기본 브라우저에서 열림 | `Settings` ▸ Advanced ▸ Commit Text Replacements ▸ Add | Win 명시 | D1 S23 · H S43 | 2 | – |
| ST-068 | 커밋 GPG 서명 (Windows) | KB의 설정 화면·명령줄 예시가 **Mac(GPGTools, /Applications/SourceTree.app)** 기반. Windows 전용 UI 근거 없음 → Win은 미확인 | (Mac) Repo Settings ▸ Security "Enable GPG key signing" + 커밋 옵션 "Sign Commit" | Mac 전용 → **Win U** | M S24 / U | 4 | – |
| ST-069 | Git 훅 전용 관리 UI | pre-commit 등 훅을 실행/편집하는 SourceTree UI의 1차 근거 없음(Git 자체가 실행). 커스텀 액션으로 우회 가능(S12) | 근거 없음 | 미확인 | U | 4 | – |

## 4. 그래프 · 히스토리 · 검색 · 비교

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-080 | Log/History 뷰 열기 | 탭 / **Ctrl+2** / View ▸ Log View · [ST-095 통합] 히스토리 로딩 성능/갱신: 큰 저장소 로그 로딩 개선(3.4.19), 커밋 로딩 멈춤 수정(3.4.29) [근거 D2 S02] · [ST-081 통합] 그래프 표시 순서: Date Order / Ancestor Order 전환 [근거 D1 S08] | `Log/History` 탭 | Win 명시 | D1 S08 | 1 | I1 §3 |
| ST-082 | 원격 브랜치 표시 토글 | "Show Remote Branches" 체크. 라벨은 기본 `origin/branch_name` · [ST-093 통합] 그래프 라벨/색: 태그 라벨 표시 옵션(3.4.28), 접힌 태그/브랜치 아이콘 + 접힘 설정(3.4.22), 그래프 색상 유지(3.4.31) [근거 D2 S02] | Log 뷰 체크박스 | Win 명시 | D1 S08 | 1 | – |
| ST-083 | 브랜치 범위 필터 | All Branches / Current Branch 드롭다운 + **선택형 브랜치 필터**(3.4.27), ShowRemoteBranch/AllBranches 뷰 동작(3.4.30), 재시작 후 설정 유지(3.4.32) · [ST-084 통합] 사이드바 항목 클릭으로 이동: 브랜치/태그/원격 브랜치 클릭 → 해당 커밋으로 이동. ahead/behind 배지 표시 [근거 D1 S08] · [ST-085 통합] 사이드바 이름 필터: 브랜치·태그·리모트·서브모듈·서브트리 목록을 이름으로 필터(3.4.18) [근거 D2 S02] | Log 뷰 필터 | Win 명시 + 3.4.27/3.4.30/3.4.32 | D1 S08 · D2 S02 | 1 | – |
| ST-087 | 전체 로그(Full Log) | 검색 결과 → View in Full Log 이동(3.4.32 수정으로 존재 확인) · [ST-086 통합] 로컬 커밋 검색: 제품 페이지 "commits, file changes, branches" 검색 명시. (역사) Search 탭 도입. author 검색(3.4.19) [근거 D1 S01 · H S04 · D2 S02] | 검색/로그 뷰 | Win 3.4.32 | D2 S02 | 2 | – |
| ST-088 | Jump to Commit | 전체/부분 SHA로 즉시 이동(역사 도입), 최초 시도 실패·다이얼로그 기본 선택 관련 수정(3.4.32) · [ST-092 통합] 커밋 크기 인디케이터: History 뷰에 커밋 크기 표시(3.4.18) [근거 D2 S02] | Log 뷰 ▸ Jump to Commit | 역사(Win 0.x) + 3.4.32 | H S04 · D2 S02 | 2 | – |
| ST-089 | 파일별 커밋 이력 | "file history list"를 클릭해 해당 파일의 커밋된 변경 확인 | 파일 히스토리 목록 | Win 명시(엔터프라이즈 튜토리얼) | D1 S38 | 1 | I1 §3 |
| ST-090 | Blame | 도입: Win 0.7.5.0 "Blame support". 현행성 간접 근거: 3.4.0 "blame 기능 명칭 표기" 수정 | 파일/라인 우클릭(정확한 위치 문서 미기재) | 역사 + 3.4.0 | H S04 · D2 S02 | 2 | – |
| ST-091 | 커밋 diff / 외부 diff 실행 | Log/History 탭의 External Diff 항목(3.4.23 회귀 수정), 삭제 파일의 외부 diff 비활성(3.4.19) | 커밋 상세 우클릭 | Win 3.4.23, 3.4.19 | D2 S02 | 1 | I1 §3 |
| ST-094 | 커밋 우클릭 액션 메뉴 | Reset current branch to this commit(S16), Rebase children … interactively(S39), Cherry pick 등 | Log 뷰 커밋 우클릭 | Win/Mac | D1 S16, S39 · D2 S02 | 1 | I1 §3 |
| ST-096 | 두 커밋/브랜치 비교 대화 | 1차 근거 없음. (파일 이력·diff로 대체) · [ST-097 통합] git bisect UI: 공식 KB·제품 페이지·릴리스 노트에서 근거 발견 실패 [근거 U] · [ST-098 통합] reflog UI: 위와 같음 — 근거 발견 실패 [근거 U] | 근거 없음 | 미확인 | U | 4 | – |

## 5. 브랜치

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-110 | 브랜치 생성 (이름 검증 포함) | Branch dialog ▸ New Branch 탭 ▸ 이름 입력 ▸ Create Branch. **공백 포함 브랜치명은 지원하지 않는다**: 3.4.22 항목 `Fixed: Allowed spaces in branch names`는 "공백 허용"을 새로 추가한 Changes가 아니라 Fixed이므로, 잘못된 입력 수락을 바로잡은 **유효성 검사 수정**으로 읽는다(Git ref 이름 규칙상 브랜치명에 공백 불가). 검사 시점·메시지 문구는 문서에 없음 | toolbar `Branch` | Win/Mac | D1 S06 · D2 S02 | 1 | I1 §3 |
| ST-111 | 브랜치 삭제 | Branch dialog ▸ Delete Branches 탭, 여러 개 체크 삭제, 병합 완료되지 않은 브랜치는 "Force delete regardless of merge status" · [ST-112 통합] 원격 브랜치 삭제: (역사) Branch dialog 두 번째 탭에서 원격 브랜치 삭제 [근거 H S04] | toolbar `Branch` | Win/Mac | D1 S06 | 1 | I1 §3 |
| ST-113 | 브랜치 체크아웃 | ① Checkout 버튼 ▸ 드롭다운 선택 ▸ OK ② 브랜치 더블클릭 | toolbar `Checkout` / 사이드바 | Win/Mac | D1 S06 | 1 | I1 §3 |
| ST-114 | 원격 브랜치 추적 | 로컬 브랜치 우클릭 ▸ Track remote branch ▸ 대상 선택 | 사이드바 우클릭 | Win/Mac | D1 S06 | 1 | I1 §3 |
| ST-116 | Force push | Force Push 옵션 동작(3.4.19에서 미동작 수정 = 존재) · [ST-115 통합] 첫 푸시 시 원격 브랜치명: (역사) 최초 푸시 시 기본 원격 브랜치명 규칙 [근거 H S04] | Push 대화 체크 | Win 3.4.19 | D2 S02 | 2 | I1 §3 |

## 6. 병합 · 충돌 · 외부 diff/merge 도구

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-120 | 브랜치 병합 | 대상 브랜치로 체크아웃 → Merge 버튼 → 브랜치 선택 → OK | toolbar `Merge` | Win/Mac | D1 S06 | 1 | I1 §3 |
| ST-122 | 충돌 해결: mine / theirs | 충돌 파일에 mine/theirs 선택, 수동 resolved/unresolved 표시((역사) external merge 후 resolved/auto 표기 이슈) | Conflicts 뷰 / diff | 역사 + 3.4.23 | H S04 · D2 S02 | 1 | I1 §3 |
| ST-123 | 충돌에서 외부 병합 도구 | **Ctrl+D**로 외부 merge 도구 실행(3.4.23) | 충돌 목록 + Ctrl+D | Win 3.4.23 | D2 S02 | 1 | – |
| ST-124 | 내장 외부 도구 프리셋 | Araxis Merge, Beyond Compare, DiffMerge, KDiff3, P4Merge, TortoiseMerge, WinMerge 프리셋(도입 Win 0.7.0.0). Beyond Compare 연동은 벤더 문서로 교차 확인 · [ST-125 통합] 외부 도구 실행 파일 탐지/경로: kdiff3 위치가 레거시 경로로 초기화(3.4.33), 사용자 설치 Araxis 탐지(3.4.16), "Double-click Diff Action" 복원(3.4.27) [근거 D2 S02] · [ST-317 통합] 외부 diff/merge 도구 연동(벤더): Beyond Compare: Tools ▸ Options ▸ Diff에서 External Diff/Merge로 `bcomp.exe` 지정 [근거 D1 S47] | Tools ▸ Options ▸ Diff | 역사(Win 0.x) + 벤더 1차 | H S04 · D1 S47 | 2 | – |
| ST-126 | rebase 중 충돌 mine/theirs | 삭제한 커밋 이후 재 replay 시 mine(직전)/theirs(직후) 선택 안내 | Interactive rebase 진행 중 대화 | Win/Mac | D1 S39 | 1 | – |

## 7. 리베이스 (대화형 포함)

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-130 | 대화형 리베이스 시작 | 두 경로: ①커밋 우클릭 "Rebase children of \<sha\> interactively" ②Repository ▸ Interactive rebase · [ST-139 통합] 푸시된 커밋 리베이스 경고: 푸시 이력이 있는 커밋을 리베이스하면 이후 "pull할 변경 있음" 안내(2021 블로그 서술) [근거 D1 S39] | 커밋 우클릭 / Repository 메뉴 | Win/Mac | D1 S39 | 1 | I1 §3 |
| ST-131 | squash | 행을 드래그&드롭해 합치거나, 우클릭/하단 버튼 "squash with previous". 창 자동 크기 조절 회귀 수정(3.4.22) | Interactive rebase 대화 | Win/Mac + 3.4.22 | D1 S39 · D2 S02 | 1 | – |
| ST-132 | edit (Amend commit?) | 대상 커밋의 "Amend commit?" 체크 → 계속 시 커밋 화면으로 돌아와 수정 후 재개 | Interactive rebase 대화 체크박스 | Win/Mac | D1 S39 | 1 | – |
| ST-133 | reword | Description 열 더블클릭 → 메시지 편집 창 | Interactive rebase 대화 | Win/Mac | D1 S39 | 1 | – |
| ST-134 | delete commit | Description 더블클릭 ▸ "Delete commit". 실수는 reset으로 복구 가능하다고 명시 | Interactive rebase 대화 | Win/Mac | D1 S39 | 1 | – |
| ST-135 | reorder | "Reorder and amend" 창에서 드래그&드롭 순서 변경 | Interactive rebase 대화 | Win/Mac | D1 S39 | 1 | – |
| ST-136 | 리베이스 진행 상태 대화 | "Rebase In Progress" 대화, Continue Rebase UX 개선(3.4.22), typo 수정(3.4.23) · [ST-138 통합] 비ASCII/특수 경로 저장소: 키릴 경로 저장소에서 리베이스 실패 수정(3.4.24), 자식 커밋 리베이스 회귀 수정(3.4.27) [근거 D2 S02] · [ST-137 통합] 주석 문자 설정 준수: `core.commentchar`를 대화형 리베이스에서 존중(3.4.21) [근거 D2 S02] | 진행 중 대화 | Win 3.4.22/3.4.23 | D2 S02 | 2 | – |
| ST-140 | 일반(비대화형) rebase 대화 | 1차 근거는 "Rebase In Progress"·Continue 계열 수정(=rebase 실행 경로 존재)뿐, 진입 메뉴·옵션 목록 문서 없음 | 근거 불충분 | 부분(3.4.22+) | U/D2 혼재 | 4 | – |

## 8. 체리픽 · 되돌리기 · 리셋

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-150 | 체리픽 | 브랜치 간 커밋 복사(제품 페이지). 도입: Win 0.8.0 beta. 3.4.16 "ignore selected parent" 수정 = 병합 커밋 부모 선택 존재 | 커밋 우클릭 | Win/Mac + 역사 + 3.4.16 | D1 S01 · H S04 · D2 S02 | 1 | I1 §3 |
| ST-151 | 커밋 되돌리기(revert) | merge 커밋 revert 지원(3.4.17) · [ST-121 통합] 병합 커밋 되돌리기: 3.4.17 "Can't revert committed merge" 수정 = merge revert 지원 [근거 D2 S02] | 커밋 우클릭 | Win 3.4.17 | D2 S02 | 1 | I1 §3 |
| ST-152 | 브랜치를 커밋으로 리셋 | 체크아웃한 브랜치에서 커밋 우클릭 "Reset current branch to this commit". KB **본문 텍스트**가 soft/mixed/hard 3종을 언급하고 git-reset 문서로 연결함(텍스트 근거). 단 "Choose from the following options:" 아래 실제 컨트롤·라벨은 이미지에 있어 **육안 미확인** → 대화창 표기·기본값은 미확인 · [ST-153 통합] 히스토리 복구 수단으로서 reset: 리베이스/스태시 실패 후 reset 권고(공식 블로그), 리셋 이후 커밋 사라짐 동작 설명 [근거 D1 S39, S16] | 커밋 우클릭 | Win/Mac | D1 S16 | 1 | I1 §3 |

## 9. 태그

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-160 | 태그 생성(annotated 기본) | Tag 대화에서 이름 입력, 기본은 annotated · [ST-161 통합] Lightweight 태그: Advanced Options ▸ "Lightweight tag" 체크 [근거 D1 S11] · [ST-162 통합] 특정 커밋에 태그: 부모 커밋 기준 또는 대상 커밋 지정 [근거 D1 S11] | toolbar `Tag` 또는 사이드바 우클릭 ▸ New Tag… | Win/Mac | D1 S11 | 1 | I1 §3 |
| ST-163 | 태그 삭제 | Tag 대화 ▸ Remove Tag ▸ 태그 선택 / 사이드바 우클릭 ▸ Delete \<tag\> | toolbar `Tag` / 사이드바 | Win/Mac | D1 S11 | 1 | – |
| ST-164 | 태그 이동 | 기존 태그명을 입력 후 Advanced ▸ "Move existing tag" + 대상 커밋 선택; 미체크 시 "Move Tag" 확인 팝업 | Tag 대화 ▸ Advanced | Win/Mac | D1 S11 | 2 | – |
| ST-166 | 태그 라벨/접힘 표시 | "Show tag labels" 해제 후에도 표시되던 문제(3.4.28), 접힌 태그/브랜치 아이콘 + 접힘 설정(3.4.22), 같은 커밋에 태그/브랜치 다수일 때 표시성(3.4.21) · [ST-165 통합] 태그 목록 정렬: 내림차순 사용성(3.4.16) → 숫자 버전 정렬(3.4.23) [근거 D2 S02] | 사이드바 / Log 뷰 / Options | Win 버전특정 | D2 S02 | 3 | – |
| ST-167 | 태그를 포함한 푸시 | Push 대화의 "Push all tags" 기본값이 체크 해제(3.4.18). 푸시 시 기본적으로 `--tags` 사용(미러 리모트와 충돌하는 이유, §15) — 태그 페치/기본값 옵션은 **ST-168** 참조 | Push 대화 | Win 3.4.18 | D2 S02 · D1 S54 | 1 | I1 §3 · §15 |
| ST-168 | 태그 강제 페치 | Fetch·Pull 대화에서 force fetching tags 지원(3.4.31), "Fetch all tags" 기본값 옵션(Options ▸ Git, 3.4.15/3.4.18) · [ST-275 통합] "Fetch all tags" 기본값: Options ▸ Git의 기본 동작 설정, Pull 파라미터와 연동(3.4.15), 기본값 체크 해제(3.4.18) [근거 D2 S02] | Fetch/Pull 대화 · Options ▸ Git | Win 3.4.15/3.4.18/3.4.31 | D2 S02 | 2 | – |

## 10. 스태시

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-170 | 스태시 생성 | 변경이 있어야 버튼 활성. 설명 입력 + **"Keep staged changes"**(스테이지된 변경 보존) · [ST-171 통합] 미추적 파일 포함: "include untracked files when stashing" 체크박스(3.4.15) [근거 D2 S02] | toolbar `Stash` | Win/Mac | D1 S10 | 1 | I1 §3 |
| ST-172 | 선택/스테이지된 파일만 스태시 | 3.4.24 Changes: selected or staged files만 stash | Stash 대화 / 파일 우클릭 | Win 3.4.24 | D2 S02 | 1 | – |
| ST-173 | 스태시 목록 확인 | 사이드바에 Stashes 열, 생성일 표시(3.4.15), 클릭 시 변경 파일/diff 표시(3.4.17 회귀 수정) | 사이드바 Stashes | Win/Mac + 3.4.15/3.4.17 | D1 S10 · D2 S02 | 1 | – |
| ST-174 | 스태시 적용 | 스태시 우클릭 ▸ Apply Stash → 파일이 변경 상태로 복구 · [ST-175 통합] 스태시 삭제: 3.4.16에서 잘못된 스태시를 삭제하던 문제 수정 = 삭제 기능 존재 [근거 D2 S02] | 사이드바 우클릭 | Win/Mac | D1 S10 | 1 | – |
| ST-176 | 스태시 부분/라인 적용 | 1차 근거 없음(전체 apply만 문서화) | 근거 없음 | 미확인 | U | 4 | – |

## 11. 패치

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-180 | 패치 적용(Apply Patch) | "Apply Patch" 대화 존재(3.4.23에서 스크롤/리사이즈 불가로 읽기 불편했다는 수정) | 메뉴 경로 문서 미기재 | Win 3.4.23(존재 근거) | D2 S02 | 2 | – |
| ST-181 | 변경을 패치로 내보내기 | 1차 근거 없음 | 근거 없음 | 미확인 | U | 4 | – |

## 12. Git Flow (세부 체크리스트)

> SourceTree의 Git Flow는 **외장 git-flow(AVV) 실행 의존**이다: 임베디드 의존성에서 제외됐고 Git for Windows 2.12.1+가 동봉, 미설치면 타이틀바 알림 + 저장소 경고, "Use System Git Flow"로 지정 (S18). 포크 측 대응: `Commands/GitFlow.cs`도 external `git flow` 실행, 이 머신의 앱 설정 Git에서는 `flow version` exit 1 (I1 §1, I3) — **비교 설명일 뿐 SourceTree 무지원 주장이 아니다.**

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-190 | Git Flow 초기화 | 초기화 대화에서 **접두사 이름 입력**(feature/release/hotfix/tag 등), 확인 후 활성화 · [ST-191 통합] 초기화 기본값/도움: "대개는 기본값 사용" + 내장 Help 참조(2012 블로그, Mac 1.5 기준) [근거 H S40] | toolbar `Git Flow` | Win/Mac | D1 S06 | 1 | I1 §1 |
| ST-192 | 컨텍스트 기반 기본 동작 + Preview | 저장소 상태(현재 브랜치)에 따라 다음 동작을 기본 선택; 모든 액션 대화에 **Preview** 창(실행 결과 예측) · [ST-202 통합] Git Flow 버튼 위치: toolbar `Git Flow` 버튼과 Repository ▸ Git Flow 메뉴 두 경로 [근거 D1 S06] | `Git Flow` 버튼 | 역사(Mac 1.5) | H S40 | 2 | – |
| ST-193 | Feature 시작 | Repository ▸ Git Flow ▸ Start New Feature, 기능 이름 입력(접두사 + 이름), 3.4.33에서 release/hotfix 브랜치명 명령 주입 보안 수정 | Repository 메뉴 / `Git Flow` | Win/Mac + 3.4.33 | D1 S06 · D2 S02 | 1 | I1 §1 |
| ST-194 | Feature 종료 | Finish Feature ▸ 드롭다운에서 선택 ▸ OK. (역사) develop에 머지, feature 브랜치 기본 삭제·보존 선택 | Repository ▸ Git Flow ▸ Finish Feature | Win/Mac + 역사 | D1 S06 · H S40 | 1 | I1 §1 |
| ST-195 | Release 시작 | Start New Release ▸ 릴리스 이름. (역사) develop 최신 커밋 기준이 기본, 다른 커밋 기준 선택 가능 | Repository ▸ Git Flow ▸ Start New Release | Win/Mac + 역사 | D1 S06 · H S40 | 1 | I1 §1 |
| ST-196 | Release 종료 | Finish Release ▸ 선택 ▸ OK. (역사) production 머지 + develop 병합 + **릴리스 태그 자동 생성** | Repository ▸ Git Flow ▸ Finish Release | Win/Mac + 역사 | D1 S06 · H S40 | 1 | I1 §1 |
| ST-197 | Hotfix 시작 | Start New Hotfix ▸ 이름. (역사) 항상 production(master) 최신 기준 | Repository ▸ Git Flow ▸ Start New Hotfix | Win/Mac + 역사 | D1 S06 · H S40 | 1 | I1 §1 |
| ST-198 | Hotfix 종료 | Finish Hotfix ▸ 드롭다운 선택 ▸ OK (release와 동일 수순). 3.4.20에서 hotfix 생성 시 silent crash 수정 | Repository ▸ Git Flow ▸ Finish Hotfix | Win/Mac + 3.4.20 | D1 S06 · D2 S02 | 1 | I1 §1 |
| ST-199 | production/develop 브랜치·접두사 **재설정** | KB에 초기화만 기술. 재설정(reconfigure) 화면의 1차 근거 없음. 포크는 InitGitFlow UI 보유(I1 §1 "Gaps") · [ST-200 통합] bugfix / support 브랜치 유형: 공식 SourceTree 문서·릴리스 노트에 등장하지 않음(git-flow AVH 확장 기능의 UI 노출 여부 미확인) [근거 U] | 근거 없음 | 미확인 | U | 4 | I1 §1 |
| ST-201 | Git Flow 의존성 알림 | git-flow / Git LFS가 없으면 저장소 진입 시 추가 경고 + 타이틀바 알림, Options ▸ Git의 "Use System Git Flow" / "Use Embedded Git" 등으로 해결(**문서가 말하는 조건부 동작**). 이 머신에서 실측으로 확인된 부재는 **git-flow뿐**(I3: 앱 설정 Git에서 `flow version` exit 1)이고, **LFS의 존재/부재는 실측하지 않았다** · [ST-328 통합] 임베디드 의존성 번들 제외: Git LFS·git-flow AVH를 설치 의존성에서 제외(시스템 Git 2.12.1+에 포함되므로) → 해당 의존성이 없을 때 알림 [근거 D1 S18] ※ "없음"이 실측된 것은 git-flow뿐(§25 / I3) | Tools ▸ Options ▸ Git | Win 명시 | D1 S18 | 1 | I1 §1 |

## 13. 다중 리모트 · Fetch/Pull/Push (세부 체크리스트)

> 주의: 이 섹션은 **저장소의 git remote 관리**다. §1의 "Hosted/Remote Repositories"(호스팅 계정 브라우즈, ST-014)와 다른 기능이고 서로 대체하지 않는다.

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-210 | 리모트 추가 | Remote details 대화: 이름 / URL·Path / 호스팅 서비스 사용자명 · [ST-213 통합] 리모트 목록 표시·정렬: 사이드바 Remotes ▸ origin 항목 정렬 문제 수정(3.4.16) = 목록 UI 존재 [근거 D2 S02] | `Settings` ▸ Add | Win/Mac | D1 S09 | 1 | I1 §2 |
| ST-211 | 리모트 편집 | Edit 버튼 → URL / Path / **Host Type** 편집 | `Settings` ▸ Edit | Win/Mac | D1 S09 | 1 | I1 §2 |
| ST-212 | 리모트 삭제 | Remove 버튼 | `Settings` ▸ Remove | Win/Mac | D1 S09 | 1 | I1 §2 |
| ST-214 | Fetch: 태그 옵션 (원격/브랜치 선택 UI는 미확인) | **근거로 확인된 것은 태그 옵션뿐**: Fetch·Pull 대화에서 force fetching tags(3.4.31), "Fetch all tags" 기본값 설정(Options ▸ Git, 3.4.15/3.4.18). **Fetch 대화의 원격·브랜치 선택 컨트롤은 1차 문서로 확인되지 않았다** — S33은 Pull의 remote·remote branch 선택과 Push의 로컬↔원격 브랜치 매핑만 기술하므로 거기서 Fetch로 옮겨 읽지 않는다. 대상 지정 방식은 Lead GUI 확인 항목 | toolbar `Fetch` | 태그 옵션만 Win/Mac + 3.4.31 | D2 S02 · D1 S33(Pull/Push 한정) | 1 | I1 §2 |
| ST-215 | Pull: 원격+브랜치 선택 | Pull 대화에서 remote repository·remote branch 선택 → OK. "fetch all tags" 설정과 명령 파라미터 연동(3.4.15), 태그 force fetch 옵션(3.4.31) | toolbar `Pull` | Win/Mac + 버전특정 | D1 S33 · D2 S02 | 1 | I1 §2 |
| ST-216 | Push: 로컬/원격 브랜치 선택 | Push 대화에서 푸시할 로컬 브랜치·대상 원격 브랜치 선택 → OK. **추적 브랜치를 선택해 사용하는 방식**(미러 리모트 KB) · [ST-221 통합] 미러 리모트 비호환(엣지): `--mirror=push` 리모트는 SourceTree 푸시(`--tags`)와 충돌 → 재작성 권장 [근거 D1 S54] | toolbar `Push` | Win/Mac + KB | D1 S33, S54 | 1 | I1 §2 |
| ST-217 | 추적 리모트 기반 동작 | Push/Pull이 브랜치의 tracking remote를 무시하던 문제 수정(3.4.33) = 추적 리모트 우선 | Push/Pull 대화 | Win 3.4.33(참조) | D2 S02 | 1 | I1 §2 |
| ST-219 | 단일 리모트의 push 전용 URL | "push URL" 전용 필드/편집 근거 없음(KB는 URL/Path/Host Type만 언급) · [ST-220 통합] "기본 remote" 개념 노출: 문서상 근거는 tracking remote/branch와 푸시 대화 선택 동작뿐 — default remote 선택 UI 미확인 [근거 U] · [ST-218 통합] 원격별 페치/prune UI: prune 관련 1차 근거 없음 [근거 U] | 근거 없음 | 미확인 | U | 4 | I1 §2 |

## 14. 계정 · 인증 · SSH · 자격증명

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-230 | 호스팅 계정 등록/편집 | Edit Accounts… ▸ 인증 ▸ OK. 다중 계정 지원 · [ST-231 통합] 지원 계정 종류: Bitbucket Cloud/Server, GitHub(+) — GitHub Enterprise·VSTS·GitLab은 3.0에서 확장 [근거 D1 S27 · H S42] | Hosted Repositories ▸ Edit Accounts… | Win 명시 | D1 S27 | 1 | I1 §3 |
| ST-232 | OAuth + Git Credential Manager | Windows 1.10에서 OAuth·2FA 지원·GCM for Windows 도입(설치 위치도 `%localappdata%`로 이동). 3.4.32에서 회전 OAuth refresh token·Credential Manager 초대형 토큰 처리 · [ST-241 통합] NTLM/URI 핸들러 보안: 클론 시 NTLM hash 노출(3.4.24), URL 핸들러 hardening(3.4.21), URI 핸들러 NTLM coercion(3.4.33) [근거 D2 S02] | 설치 + 인증 프롬프트 | Win 명시 + 3.4.32 | D1 S21 · D2 S02 | 1 | – |
| ST-233 | API Token 인증 | **Windows ≥3.4.24** 필수. Options ▸ Authentication ▸ Bitbucket 호스트 + API Token(이메일). 만료 시 재생성·재설정 필요. App Password 대체 경로 · [ST-235 통합] GitHub 2FA: 구버전(Win <1.10 / Mac <2.4)은 PAT를 비밀번호로 사용 [근거 H S25] · [ST-234 통합] 저장된 자격증명 관리: Options의 Authentication 탭에서 저장된 비밀번호/기본 사용자명 편집·삭제(도입 0.9.0.3 beta) [근거 H S04 · D1 S37] | Tools ▸ Options ▸ Authentication | Win 명시(≥3.4.24) | D1 S37 | 1 | – |
| ST-236 | SSH 키 생성/가져오기 | **Tools ▸ Create or Import SSH Keys** → PuTTY Key Generator(Generate, passphrase 지정, 저장 위치 선택 가능) · [ST-238 통합] SSH 우선 프로토콜·URL 전환: 계정에서 SSH를 우선하도록 설정하고 클론/원격 URL을 SSH로 변경(HTTPS 저장소도 전환) [근거 D1 S36] | Tools 메뉴 | Win 명시 | D1 S36, S35 | 1 | – |
| ST-237 | Pageant(agent) 인증 | SourceTree 내장 Pageant에 개인키 로드 → passphrase 자동 인증 | Pageant / 시스템 트레이 | Win 명시 | D1 S36, S35 | 1 | – |
| ST-239 | 서버 host key trust | PuTTY 레지스트리 캐시 방식. 버튼이 없을 때 임베디드 Git로 trust 후 되돌리거나 `<설치>\tools\putty\plink.exe <host>`에서 `y` | 출력창 / plink | Win 명시 | D1 S26 | 2 | – |
| ST-240 | OpenSSH 클라이언트 + 외부 ssh-agent | OpenSSH 사용 시 `AutoStartSSHAgent` 비활성 허용(외부 ssh-agent 활용) — 3.4.32. 이전에도 ssh-agent 관련 CPU 문제 수정(3.4.24) | Options(세부 항목명 문서 미기재) | Win 3.4.32 | D2 S02 | 1 | – |
| ST-242 | HTTP 프록시 설정 | 프록시 설정이 삭제되던 문제(3.4.16) = 프록시 설정 UI 존재 | Tools ▸ Options(세부 위치 미기재) | Win 3.4.16 | D2 S02 | 3 | – |

## 15. LFS · 서브모듈 · 서브트리 · 워크트리 · 스파스

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-250 | Git LFS 지원 | "tracks large assets in one place" (제품 페이지). 동봉 LFS 버전 갱신(3.5.1 → 3.7.0) · [ST-251 통합] LFS 의존성 상태: LFS 미설치 시 알림·저장소 경고, "Use System Git LFS" 또는 임베디드 Git 업데이트로 해결 [근거 D1 S18] · [ST-252 통합] LFS 추적 패턴 관리 UI: 1차 근거 없음 [근거 U] | 저장소 사용 / Options ▸ Git | Win/Mac + 3.4.20/3.4.25 | D1 S01 · D2 S02 | 1 | I1 §3 |
| ST-253 | 서브모듈 추가 | 사이드바 우클릭 또는 Repository ▸ **Add Submodule** ▸ source URL + 저장소 내 경로 · [ST-067 통합] 서브모듈 변경을 포함 커밋/푸시: 서브모듈 미커밋·미푸시 시 확인 프롬프트(Tools ▸ Options ▸ Git에서 비활성 가능, 역사 근거) / 추가 시 file status에 staged 항목 발생 [근거 H S04, S44] · [ST-254 통합] 서브모듈 제거·소스 변경: (역사) 사이드바에서 추가/삭제/소스 변경 [근거 H S04] | 사이드바 / Repository 메뉴 | Win 명시 | D1 S15 · H S44 | 1 | I1 §3 |
| ST-255 | 서브모듈 탐색 | 사이드바 전용 섹션(첫 추가 시 나타남), 더블클릭 시 별도 저장소 창, 상태(브랜치) 표시 · [ST-256 통합] pull 후 서브모듈 갱신 / 체크아웃 시 submodule 변경: (역사) pull 후올바른 상태으로 갱신. 3.4.32에서 체크아웃 시 미스테이지 submodule 변경 폐기 실패 수정 [근거 H S04 · D2 S02] · [ST-258 통합] 서브모듈/서브트리 필터: 이름 필터(3.4.18) [근거 D2 S02] | 사이드바 | Win 명시 + 역사 | D1 S15 · H S44 | 2 | – |
| ST-257 | 서브트리 추가/링크 | **Add/Link Subtree**(서브모듈과 별도 항목), globe 버튼이 원격 저장소 브라우즈를 열어 선택 지원 | 사이드바 / Repository 메뉴 | Win 명시 | D1 S15 | 1 | – |
| ST-259 | 워크트리 관리 UI | Jira SRCTREEWIN-8594 = **Suggestion / Gathering Interest**, fixVersion 없음(29 votes, 2026-10-16 갱신). 요청 존재일 뿐 출시 증거 아님 | 근거 없음 | 미확인 | U + S S48 | 4 | – |
| ST-260 | sparse checkout UI | 공식 KB·릴리스 노트에 없음. 커뮤니티: 2019 공식팀 "지원 안 함" 답글, 2025-08 대기 글(본문 JS 렌더로 직접 확인 불가, Lead 인용) | 근거 없음 | 미확인 | U + S S49 | 4 | – |

## 16. 설정 (Tools ▸ Options)

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-270 | 전역 자동 새로고침 | 파일 변경 시 자동 갱신 · [ST-271 통합] 저장소별 자동 갱신/원격 상태: 저장소 설정 Advanced ▸ "Automatically refresh" + "Refresh remote status in background". **전역이 켜져야 로컬 override 가능** [근거 D1 S13] | Tools ▸ Options ▸ General | Win 명시 | D1 S13 | 1 | I1 §3 |
| ST-272 | 임베디드 / 시스템 Git 선택 | Options ▸ Git에서 Use Embedded Git / Use System Git · [ST-274 통합] Git 지원 켜기/끄기: Tools ▸ Options ▸ Git ▸ Enable Git / user.config `EnableGitSupport` [근거 D1 S22] · [ST-273 통합] 임베디드 Git 업데이트: Windows 전용 "Update Embedded Git"(Options ▸ Git). 임베디드 경로 `%LOCALAPPDATA%\Atlassian\SourceTree\git_local\bin\git.exe` [근거 D1 S17, S12] | Tools ▸ Options ▸ Git | Win 명시 | D1 S17 | 1 | I1 §3 |
| ST-276 | autocrlf 관리 UI | autocrlf 값 관리 UI 개선(3.4.27) · [ST-277 통합] safe.barerepository: 글로벌 git config의 `safe.barerepository` 활성화 옵션(3.4.21) [근거 D2 S02] | Tools ▸ Options | Win 3.4.27 | D2 S02 | 2 | I1 §3 |
| ST-279 | diff/폰트 설정 | diff 뷰 폰트 변경(Diff 설정), 커밋 fixed-width 폰트 + 72자 컬럼 가이드(역사) | Tools ▸ Options ▸ Diff | 역사(Win 0.x) | H S04 | 3 | – |
| ST-280 | 테마/다크 모드 (Windows) | Windows 근거는 3.4.15의 dark mode 관련 수정(컨텍스트 메뉴 안보임)뿐 — 설정 경로·시스템 연동 여부 미확인. macOS 테마 블로그는 범위 밖 · [ST-281 통합] UI 언어(로케일): 이탈리아어(3.4.24/3.4.25)·일본어(3.4.24) 번역 갱신 = 로케일 존재. 전환 UI 위치는 문서 미기재 [근거 D2 S02] | Options(미특정) | Win 약(3.4.15) / M S46 | D2 S02 · M S46 | 4 | – |
| ST-282 | 창·탭 레이아웃 저장 | 탭 상태(사이드바 확장·너비)를 닫을 때 저장, 활성 탭/열린 탭 복원(역사) | 자동 | 역사(Win 0.x) | H S04 | 3 | – |
| ST-283 | Process Log | Git/Hg 호출·장기 실행 프로세스 진단(2.4 for Windows) | 창/메뉴(현행 위치 미확인) | 역사(Win 2.4) | H S41 | 3 | – |
| ST-284 | 설정 파일로 외부 편집 | 북마크/열린 탭/계정/커스텀 액션과 저장소 `sourcetreeconfig`, Windows 파일 `bookmarks.xml`·`opentabs.xml`·`userhosts`·`user.config` | 파일 편집 | Win 명시(경로 보장 caveat) | D1 S34 · D1 S55 | 3 | – |

## 17. 커스텀 액션

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-300 | 커스텀 액션 정의 | Add ▸ 실행할 스크립트/명령 + 인수 입력 · [ST-306 통합] 편집 저장 검증 / 종료코드 신뢰: 편집 후 저장 검증 오류(3.4.25), "Trust Exit Code" 툴팁 오표기(3.4.27) = 종료코드 신뢰 옵션 존재 [근거 D2 S02] · [ST-302 통합] 실행 Git 선택: Embedded Git 또는 System Git 중 선택(임베디드 경로 명시) [근거 D1 S12] | Tools ▸ Options ▸ Custom Actions | Win 명시 | D1 S12 | 1 | – |
| ST-301 | 실행 창/출력 옵션 | 별도 창에서 실행 여부, 전체 출력 표시 여부(사용자 선택) | Custom Actions 편집 창 | Win 명시 | D1 S12 | 2 | – |
| ST-303 | 커스텀 액션 실행 | Actions ▸ Custom Actions ▸ 액션 선택 후 실행, 출력 확인 | Actions 메뉴 | Win 명시 | D1 S12 | 1 | – |
| ST-304 | 커스텀 액션 단축키 | 3.4.27 Changes: Shortcut Support for Custom Actions | Options ▸ Custom Actions | Win 3.4.27 | D2 S02 | 2 | – |
| ST-305 | 인수 토큰($BRANCH 등) | `$BRANCH` 치환이 항상 정상 동작하지 않던 수정(3.4.32) = 토큰 존재. **다른 토큰($FILE 등)은 별도 입증 필요** | Custom Actions 인수 필드 | Win 3.4.32 | D2 S02 | 3 | – |

## 18. 외부 연동 (터미널 · 탐색기 · 호스팅 · PR · CI)

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-310 | 터미널 열기 | Terminal 버튼. 저장소 탭이 있으면 저장소 폴더 기준, 없으면 SourceTree 폴더 기준 | toolbar `Terminal` | Win 명시 | D1 S14 | 1 | I1 §3 |
| ST-311 | Git Bash / cmd 선택 | "Use Git Bash as default terminal" 체크(기본은 임베디드 Git 환경의 Git Bash), Hg는 cmd만 | Tools ▸ Options ▸ Git | Win 명시 | D1 S14 | 1 | – |
| ST-313 | 탐색기 ▸ SourceTree로 열기 | "Open in Sourcetree" 셸 컨텍스트 메뉴(3.4.22) · [ST-312 통합] Windows 탐색기에서 표시: (역사) Show In Explorer가 working copy 파일에서 동작 [근거 H S04] | 탐색기 우클릭 | Win 3.4.22 | D2 S02 | 2 | – |
| ST-315 | PR 생성 = 호스팅 웹로 이동 | "Create pull requests: 푸시 후 호스팅 서비스의 **웹 UI** 새 PR로 이동". 인앱 PR 리뷰·대시보드 근거 없음 · [ST-019 통합] 호스팅 웹으로 이동: 현재 저장소를 호스팅 서비스에서 바로 열기(3.0 블로그 "single click access") · [ST-314 통합] 호스팅 서비스 웹으로 이동: 현재 저장소를 호스팅 서비스에서 여는 single-click access(3.0) [근거 H S42] [근거 H S42] | 푸시 후 / 컨텍스트 | Win/Mac(2018) | H S42 (+ in-app은 U) | 2 | – |
| ST-316 | Pipelines 빌드 상태 | Bitbucket Cloud Pipelines 빌드 상태를 SourceTree에서 표시(2017). 플랫폼·현행 지원 여부 미특정 | 빌드 상태 표시 위치 미확인 | 미확인(2017 블로그) | H S45 | 4 | – |

## 19. 설치 · 업데이트 · 엔터프라이즈 (Windows)

| ID | 기능군 (원자 동작; 병합분은 상세에 […통합]) | 상세 | UI 진입점 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|---|
| ST-320 | 소비자 EXE 설치 | `%localappdata%\SourceTree` 사용자 단위 설치(관리자권한 불요), 임베디드/시스템 Git·Hg 설치 선택 가능 · [ST-322 통합] 점진(단계적) 롤아웃: 신규 설치자가 기능 부분 배포/모니터링 지원 [근거 H S21] | 설치 마법사 | Win 명시 | D1 S21 · D1 S19(대조) | 1 | – |
| ST-321 | 앱 내 업데이트 확인 | Help ▸ Check for Updates | Help 메뉴 | Win 명시 | D1 S21, S37 | 1 | – |
| ST-323 | Enterprise MSI 무인 설치 | `msiexec /i … ACCEPTEULA=1 INSTALLDIR=<path>`(2.5+), 마법사 또는 관리 소프트웨어로 배포 | MSI / msiexec | Win 명시 | D1 S19 | 1 | – |
| ST-324 | MSI와 EXE의 동작 차이 | MSI: 설치 후 자동 실행 안 함 / Git·Hg 다운로드 및 선택 불가 / global ignore 프롬프트 없음 / 업데이트 알림 비활성 / 등록 옵션 추가, `%ProgramFiles(x86)%\Git`, `%ProgramFiles(x86)%\Mercurial` 자동 탐지 | MSI | Win 명시 | D1 S19 | 1 | – |
| ST-325 | 중앙 업데이트 관리 | 조직 차원에서 롤아웃 시점 관리(엔터프라이즈 페이지 + MSI의 업데이트 알림 비활성) | 관리자 정책 | Win 명시 | D1 S38, S19 | 2 | – |
| ST-326 | 버전·포맷 유통 | Windows EXE(3.4.33=2026-10-31 … 3.4.0=2020-12-15), Enterprise MSI 동일 버전 체계, Mac은 ZIP(4.2.20) — 아카이브에서 이전 버전 다운로드 | Download Archives | Win 명시 | D1 S05 | 2 | – |
| ST-327 | 최종 사용자 등록 | 첫 실행 시 회사 Bitbucket Server 또는 Atlassian 계정으로 등록(IE11+ 필요 명시) | 첫 실행 마법사 | Win 명시 | D1 S19 | 2 | – |

## 20. 단축키 · 접근성 · DPI (키 값은 출처 있는 것만)

| ID | 키/기능 | 상세 | Win 확실성 | 근거 | 우선 | fork |
|---|---|---|---|---|---|---|
| ST-340 | **Ctrl+1** File Status 뷰 | View ▸ File Status View와 동일 동작 | Win 명시 | D1 S07 | 1 | – |
| ST-341 | **Ctrl+2** Log/History 뷰 | View ▸ Log View와 동일 동작 | Win 명시 | D1 S08 | 1 | – |
| ST-342 | **Ctrl+D** 충돌 시 외부 merge | 3.4.23 Changes. (역사 0.x: external diff 단축키로 소개) | Win 3.4.23 + 역사 | D2 S02 · H S04 | 1 | – |
| ST-343 | **Ctrl+Enter** 커밋 | 3.4.17 "shortcut doesn't work anymore (commit the changes)" 수정 = 동작 존재 | Win 3.4.17 | D2 S02 | 2 | – |
| ST-344 | **Esc** 메시지 창 닫기 | 3.4.24 Changes(설정 항목으로 제공) · [ST-285 통합] Esc로 메시지 창 닫기: Esc 단축키 옵션(3.4.24) [근거 Win 3.4.24] | Win 3.4.24 | D2 S02 | 3 | – |
| ST-345 | **F5** 강제 새로고침 | (역사) results 전부 초기화하도록 수정 · [ST-346 통합] **Alt→F→X** 종료: (역사) 메뉴 액셀러레이터 추가 [근거 H S04] · [ST-347 통합] unstage 단축키: (역사) 메뉴 + 키보드 단축키 부여 — **키 값은 문서에 없음** [근거 H S04] | 역사(Win 0.x) | H S04 | 3 | – |
| ST-348 | 모니터별 DPI | (역사, SourceTree for Windows 2.0) "Pixel-perfect per-monitor DPI support" | 역사(Win 2.0) | H S41 | 2 | – |
| ST-350 | 키보드 포커스/내비게이션 | (역사) 기동 시 로그 뷰 포커스 초기화 → 키로 탐색 · [ST-349 통합] 저해상도 대응: 로해상도에서 저장소 폴더 아이콘-이름 겹침 수정 [근거 D2 S02] | 역사(Win 0.x) | H S04 | 3 | – |
| ST-351 | 스크린리더/WCAG 적합성 | 공식 접근성 적합성 문서·성적표 근거 없음(Atlassian 접근성 정책 페이지는 미확인) | 미확인 | U | 4 | – |

## 21. 범위 밖 (별도 표기 — Git-Windows 포크 범위에서 제외)

### 21-A. Mercurial (Hg) — 이 포크 범위 밖

| ID | 기능 | 근거 | 등급 |
|---|---|---|---|
| ST-900 | Stash의 Hg 상당 기능이 **Shelve**(Hg 저장소 열면 Shelve로 표시) | S10 | D1 |
| ST-901 | Mercurial 지원 켜기/끄기(Options ▸ Mercurial ▸ Enable Mercurial, user.config `EnableHgSupport`) | S22 | D1 |
| ST-902 | Hg는 Terminal이 Windows cmd로만 실행 | S14 | D1 |
| ST-903 | Git Flow 상당의 Hg Flow 서술 | S40 | H |
| ST-904 | 서브리포지토리(`.hgsub`) 지원 | S44 | H |
| ST-905 | Hg ZIP이 시작 시 반복 다운로드/추출되던 문제(spellcheck 활성 시) | S02 (3.4.25) | D2 |

### 21-B. macOS — Git-Windows 인벤토리 집계에서 제외

| ID | 기능 | 근거 | 등급 |
|---|---|---|---|
| ST-910 | Repository Browser(Window ▸ Show Repository Browser / Dock) | S28 | D1(Mac) |
| ST-911 | 원격 저장소 뷰 명칭 "Remote View" | S27 | D1(Mac) |
| ST-912 | Command+1 / Command+2 단축키 | S07, S08 | D1(Mac) |
| ST-913 | Preferences ▸ … (Custom Actions/General/Git/Terminal) | S12, S13, S14, S17 | D1(Mac) |
| ST-914 | Reset to Embedded Git, `/Applications/SourceTree.app/Contents/Resources/git_local/bin` | S17 | D1(Mac) |
| ST-915 | Repo Settings ▸ Security ▸ GPG 서명 활성화 + "Sign Commit" | S24 | D1(Mac) — Win은 ST-068 = U |
| ST-916 | SSH 키 단일 생성 제한 / Accounts ▸ Edit 절차 | S36 | D1(Mac) |
| ST-917 | macOS 다크 테마 구현 설계 | S46 | H(Mac) |
| ST-918 | Mac 4.2.20(2026-11-01) ZIP 유통 | S05 | D1(Mac) |

## 22. 간극 · 미확인 (성능/품질 주장 없이, 확인 필요 목록)

| # | 항목 | 현재 판단 | 확인 방법(파괴적 조작 없음) |
|---|---|---|---|
| G1 | Windows GPG 커밋 서명 UI | Mac KB만 존재(ST-068) | Windows 저장소 ▸ Settings ▸ Security 탭 존재 여부 실물 확인(Lead GUI) |
| G2 | 네이티브 워크트리 UI | 요청 이슈만 존재, fixVersion 없음(ST-259). **문서에 근거가 없다는 사실만으로 미출시를 단정하지 않는다** | 공식 릴리스 노트·KB 재검색 + Lead GUI에서 워크트리 메뉴 확인 → 두 결과가 모두 "없음"일 때만 미출시로 등급 확정 |
| G3 | sparse checkout | 공식 KB/RN에 근거 없음 + 커뮤니티 답변만(ST-260). 문서 부재만으로 미출시 확정 금지 | 3.4.33 Options·Repository 메뉴 확인 후 판정 |
| G4 | 스태시 부분/라인 적용 | 전체 apply만 문서화(ST-176) | Apply Stash 대화 옵션 확인 |
| G5 | 리모트 push 전용 URL / default remote 선택 / prune | Remote details는 URL·Path·Host Type만(ST-219에 통합됨) | Settings ▸ Add/Edit, Fetch 대화 체크박스 확인 |
| G6 | 인앱 PR 리뷰 | "PR은 호스팅 웹로 이동"만 근거(ST-315) | 3.4.33에 리뷰 패널 존재 여부 |
| G7 | bisect / reflog UI | 1차 근거 발견 실패(ST-096에 통합됨). 메뉴 1회 확인으로는 부재를 확정하지 않는다 | Repository/커밋 우클릭 메뉴 확인 + 공식 RN 전체 grep → 확인 대기 유지 |
| G8 | Git Flow reconfigure, bugfix/support 유형 | 초기화·start/finish만 문서화(ST-199). **미발견 ≠ 부재** → 스코프 제외 아님, 확인 대기 | Git Flow 대화 전체 탭/우클릭 확인 + RN grep 후 판정 |
| G9 | 커밋 비교(두 커밋/브랜치) 대화 | 근거 없음(ST-096) | Log 뷰 우클릭 메뉴 확인 |
| G10 | 다크/테마·언어 전환 설정 위치 | Windows 근거가 약함(ST-280에 통합됨) | Options 탭 enumeration |
| G11 | 툴바/메뉴 전체 키 값 | 확인된 키는 Ctrl+1/2, Ctrl+D, Ctrl+Enter, Esc, F5, Alt-F-X뿐 | Help ▸ Keyboard Shortcuts(존재 시) 확인 |
| G12 | Pipelines/빌드 상태 현행성 | 2017 블로그 후 갱신 근거 없음(ST-316) | 3.4.33에서 Bitbucket Cloud 저장소로 확인 |
| G13 | 인증서 trust 관리 | 관련 KB에 SourceTree UI 단계 없음(등록부 §4.5) | 사설 저장소 검사 금지 — 문서 접근 불가로 남김 |
| G14 | 기능 완성률 수치 | **산출하지 않음.** 근거 등급별 계정은 §25 참조 | – |

## 23. 워크플로 수용 시나리오 (소모성 fixture, 파괴적 실행 없음)

각 시나리오는 별도 임시 저장소 fixture에서 검증한다(기존 `artifacts/fixture*` 재사용, 사용자 저장소 사용 금지). 기대 결과는 반드시 "관찰 가능한 상태"로 쓴다.

| # | 시나리오 | 절차 요약 | 기대 결과 | 관련 ID |
|---|---|---|---|---|
| W1 | 클론→변경→스테이지→커밋→푸시 | fixture bare 리모트에서 클론, 파일 수정, 체크박스 스테이지, 메시지 커밋, Push 대화에서 브랜치 선택 | diff/로그에서 커밋 확인, 리모트에 반영, Push 대화에 추적 리모트 표기 | ST-002, ST-033, ST-060, ST-216 |
| W2 | 헝크/라인 스테이지 | 한 파일에 2개 헝크 변경 → 1개만 Stage hunk | Unstaged/Staged 양쪽 존재, 미스테이지 시 원상복구 | ST-033, ST-035, ST-032 |
| W3 | Discard | 미스테이지 파일 discard 확인 | 파일이 마지막 커밋 상태로, diff 비어있음 | ST-039 |
| W4 | stash / keep staged / 선택만 | ①전체 stash ②Keep staged changes ③3.4.24 selected-only | Stashes 열에 항목(생성일), working copy 정리, Apply 후 복구 | ST-170, ST-172, ST-173, ST-174 |
| W5 | 태그 life-cycle | annotated 생성 → 특정 커밋에 생성 → lightweight → Move → Remove | 사이드바 Tags 반영, Move 확인 팝업 | ST-160, ST-163, ST-164, ST-166 |
| W6 | 대화형 리베이스 | 4개 로컬 커밋에서 squash/reword/delete/reorder + edit | 로그가 기대 순서/메시지로 누락 없이 재작성, edit 시 커밋 화면 정지 | ST-130, ST-131, ST-132, ST-133, ST-134, ST-135 |
| W7 | reset / revert / cherry-pick | reset hard/mixed/soft, merge 커밋 revert, 다른 브랜치 커밋 체리픽 | HEAD 이동, revert 커밋 생성, cherry-pick 부모 선택 가능 | ST-150, ST-151, ST-152 |
| W8 | 충돌 → 외부 도구 | 두 브랜치 충돌 유도 → mine/theirs → Ctrl+D 외부 merge | resolved/unresolved 상태 전이, 종료 후 resolved 처리 | ST-122, ST-123, ST-126 |
| W9 | Git Flow (git-flow 존재 시) | init(prefix 지정) → feature start/finish → release start/finish(태그 확인) → hotfix | Preview 문구, 브랜치 생성/삭제, release 태그 생성 | ST-190, ST-193, ST-194, ST-195, ST-196, ST-197, ST-198 |
| W10 | Git Flow 의존성 없음 | git-flow 없는 Git에서 저장소 열기 | 타이틀바 알림/저장소 경고, Git Flow 메뉴 동작 불가 사유 표시 | ST-201, I3 (LFS는 미실측 → ST-250은 별도 확인) |
| W11 | 다중 리모트 | 리모트 추가→Edit→Remove, fetch/pull/push 대상 선택 | Remote details 필드(이름/URL·Path/Host Type), push 대화에 추적 리모트 | ST-210, ST-211, ST-212, ST-215, ST-216, ST-217 |
| W12 | 인증 | API Token(Win ≥3.4.24)으로 원격 브라우즈→클론, PuTTY 키+Pageant로 SSH 클론 | Options ▸ Authentication 저장, SSH 클론 성공 | ST-233, ST-236, ST-237 |
| W13 | 커스텀 액션 | `git status --short` 액션 정의 → Actions ▸ Custom Actions 실행(+3.4.27 단축키) | 출력 표시, `$BRANCH` 치환 확인 | ST-300, ST-301, ST-303, ST-304, ST-305 |
| W14 | 자동 갱신 | 전역 자동 갱신 ON + 저장소 Advanced 백그라운드 원격 상태 ON | 파일 변경 후 목록 갱신, ahead/behind 갱신 | ST-270 |
| W15 | MSI 무인 설치(가상/별도 머신) | `msiexec /i Sourcetree.msi ACCEPTEULA=1 INSTALLDIR=…` | 자동 실행 없음·업데이트 알림 없음·Git 다운로드 단계 없음 | ST-323, ST-324, ST-325 |

## 24. 로드맵 (순서 = 이 인벤토리 기반 우선순위)

범위 표기는 §(섹션)과 실제 surviving ID로만 쓴다. **한 번의 GUI 메뉴 확인으로 "없음"을 확정하지 않는다** — 부재 확정에는 공식 문서/릴리스 노트 부재 확인 + 실제 화면 확인이 모두 필요하고, 둘 중 하나라도 부족하면 "확인 대기"로 남긴다.

1. **P1 코어 회귀**: §2 `ST-030`~`ST-047`(파일 상태/스테이지/폐기/무시, 12행), §3 `ST-060`~`ST-069`(커밋, 6행), §4 `ST-080`~`ST-096`(로그/필터/검색/비교, 10행), §5 `ST-110`,`ST-111`,`ST-113`,`ST-114`,`ST-116`(브랜치), §13 `ST-210`~`ST-217`(리모트·Fetch/Pull/Push) — I1 §3의 IMPL-SOURCE 항목과 1:1 대조, 실제로 조작 확인된 것만 QA_MATRIX에서 TESTED로 승격.
2. **P1 충돌·재작성**: §6 `ST-120`,`ST-122`,`ST-123`,`ST-124`,`ST-126` / §7 `ST-130`~`ST-136`,`ST-140` / §8 `ST-150`,`ST-151`,`ST-152`.
3. **P1 의존성 실측**: `ST-201`(Git Flow external dependency) — I3 로그로 **git-flow 부재만** 확인됨. `ST-250`(Git LFS)은 **이 머신에서 존재/부재를 실측하지 않았으므로** 별도 확인 항목으로 남긴다. 경고 UI 자체는 Lead GUI 확인.
4. **P2 스태시/태그/패치**: §10 `ST-170`,`ST-172`,`ST-173`,`ST-174`,`ST-176` / §9 `ST-160`,`ST-163`,`ST-164`,`ST-166`,`ST-167` / §11 `ST-180`,`ST-181`.
   (§9 태그군은 `ST-168` 태그 강제 페치 포함)
5. **P2 인증/계정**: §14 `ST-230`,`ST-232`,`ST-233`,`ST-236`,`ST-237`,`ST-239`,`ST-240`,`ST-242` (API Token ≥3.4.24, OpenSSH 외부 ssh-agent 3.4.32).
6. **P2 Git Flow parity**: §12 `ST-190`,`ST-192`,`ST-193`,`ST-194`,`ST-195`,`ST-196`,`ST-197`,`ST-198`의 prefix·Preview·release 태그 생성까지. `ST-199`(reconfigure, bugfix/support)는 **근거 미발견 상태일 뿐이고 "SourceTree에 없음"이 확정된 것이 아니므로** 스코프에서 제외하지 않고 G8의 확인 대기 항목으로 유지한다(확인 전 구현 착수/포기 결정 금지).
7. **P3 설정/연동**: §16 `ST-270`,`ST-272`,`ST-276`,`ST-279`,`ST-280`,`ST-282`,`ST-283`,`ST-284` / §17 `ST-300`,`ST-301`,`ST-303`,`ST-304`,`ST-305` / §18 `ST-310`,`ST-311`,`ST-313`,`ST-315`,`ST-316` / §20 `ST-340`~`ST-351`.
8. **P4 검증 전용**: G1~G13은 항목마다 (a) 공식 문서·릴리스 노트 재검색, (b) 필요한 경우 Lead GUI 확인 2단계를 거쳐 등급을 승격/강등한다. **1회 메뉴 확인으로 전역 부재를 확정하지 않고**, 자동 스코프 확대·축소도 하지 않는다. 판정이 끝나지 않은 항목은 "확인 대기" 그대로 남긴다.
9. **범위 제외(포크 기준, 근거 있음)**: §21-A Mercurial, §21-B macOS — Git 전용 포크 범위라서 제외하며, SourceTree 기능 부재를 주장하지는 않는다.

## 25. 집계 (실제 개수 결과 — 스크립트로 행을 세어 확인)

- 집계 기준: 표의 데이터 행(줄이 `| ST-` 로 시작)만 개수. §20 단축키 행 9건은 컬럼이 7개, §21 범위 밖 행은 4개 컬럼이지만 모두 ID 접두로 구분해 별도 개수했다.

### 25-A. 행 수

| 구분 | 행 수 |
|---|---|
| Git-Windows 인벤토리 (§1~§20) — **기능군 행 수** | **139** |
| 범위 밖 Mercurial (§21-A) | 6 |
| 범위 밖 macOS (§21-B) | 9 |
| **총 표 행** | **154** |

행 수는 **기능군** 개수이며 원자 동작 수와 같지 않다(§0). 근거가 없는 항목 12건은 UNCONFIRMED로 분리 표기했고, 없는 기능을 채워 넣지 않았다. 병합으로 흡수된 ID는 §27에서 재사용 금지로 관리한다.

### 25-B. 분류별 (in-scope 139)

| 분류 | 행 수 |
|---|---|
| 1 저장소/북마크/탭/원격 브라우즈 | 11 |
| 2 File Status/스테이징/폐기/무시 | 12 |
| 3 커밋 | 6 |
| 4 그래프/히스토리/검색/비교 | 10 |
| 5 브랜치 | 5 |
| 6 병합/충돌/외부 도구 | 5 |
| 7 리베이스 | 8 |
| 8 체리픽/리버트/리셋 | 3 |
| 9 태그 | 6 |
| 10 스태시 | 5 |
| 11 패치 | 2 |
| 12 Git Flow | 10 |
| 13 다중 리모트·Fetch/Pull/Push | 8 |
| 14 계정/인증/SSH | 8 |
| 15 LFS/서브모듈/서브트리/워크트리/스파스 | 6 |
| 16 설정 | 8 |
| 17 커스텀 액션 | 5 |
| 18 외부 연동 | 5 |
| 19 설치/엔터프라이즈 | 7 |
| 20 단축키/접근성/DPI | 9 |

### 25-C. 근거 등급별 (in-scope 139 — 근거 등급 열만 계수, 한 행에 여러 등급 가능 → 합 168 > 139)

| 등급 | 행 수 |
|---|---|
| D1 현행 공식 문서 확인 | 84 |
| D2 공식 Windows 릴리스 노트에 버전 특정 | 47 |
| H 공식이나 역사적 증거만 | 22 |
| M Mac 전용 증거 | 2 |
| U 1차 증거 없는 미확인 후보 | 11 |
| S 2차 근거(이슈/포럼) | 2 |

- 우선순위별(in-scope 139): **1** 80 · **2** 39 · **3** 8 · **4** 12 (합 139)
- U 태그 11건 = "근거 미발견"이며 **부재 확정 아님**: ST-068(Windows GPG, G1) · ST-069(훅 UI, G1) · ST-096(두 커밋/브랜치 비교·bisect·reflog, G7/G9) · ST-140(비대화형 rebase) · ST-176(스태시 부분 적용, G4) · ST-181(패치 내보내기) · ST-199(Git Flow 재설정·bugfix/support, G8) · ST-219(prune·pushURL·default remote, G5) · ST-259(워크트리, G2) · ST-260(sparse, G3) · ST-315(인앱 PR 리뷰, G6). ST-351은 WCAG 적합성 **판정**만 미확인(기능 자체는 D1).

### 25-D. 출처

- 등록부 등재: **1차 출처 53건** = 제품 페이지·Enterprise·아카이브 4 + 공식 Windows 릴리스 노트 3 + 공식 KB 32 + Bitbucket 공식 문서 2 + 공식 블로그 8 + 사이트맵·허브 3 + 외부 도구 벤더 1 · **2차 출처 2건**(Jira SRCTREEWIN-8594, 커뮤니티 토론) · **내부 참조 3건**(I1~I3). ※ 등록부 표 행 실개수: §1 표 53행, §2 표 2행, §3 표 3행.
- 직접 접근 성공: 등록부 등재 URL 중 community(S49)를 제외한 전부 HTTP 200. S49는 JS 렌더링으로 본문 확인 실패(등록부 §4).

## 26. 등록부 정합성 메모

- 등록부: `Docs/SOURCETREE_SOURCE_REGISTER.md` (S01~S55 + I1~I3). 본문에서 `[Sxx]`/`Sxx`로 참조한 키는 전부 등록부에 존재해야 하며, 미등록 URL은 사용하지 않았다.
- 이 문서는 SourceTree 기능의 **문서상 존재**를 근거 등급과 함께 정리한 것이다. 포크의 구현·런타임 상태는 `Docs/CORE_FEATURE_AUDIT.md`(I1)와 `Docs/QA_MATRIX.md`가 정본이며, 여기서 어떤 PASS도 주장하지 않는다.
- 기능 완성률(%)은 근거 없는 수치이므로 산출하지 않는다.

## 27. 통합·유보 ID (행 병합 내역 — ID 재사용 금지)

같은 동작을 행으로 쪼갠 항목과 부수적인 버전 메모성 항목을 **기능군**으로 정리하면서, 정보를 잃지 않도록 흡수 대상의 원 서술·근거를 생존 행의 상세 열에 그대로 옮겼다. 병합된 ID는 아래 표에 남기고 **다시 쓰지 않는다**. 병합된 행의 사실·근거는 그대로 생존 행 상세에 `[ST-xxx 통합] …  [근거 …]` 형식으로 보존된다.

**규모: 생존 행 57건에 ID 77건을 흡수** (생존 행의 상세 열에서 `[ST-xxx 통합]` 태그로 확인 가능). ST-168(태그 강제 페치)는 근거가 충분해 원래 대로 **독립 행으로 되돌렸다**(§9). 스크립트 검증: 생존/흡수 열에 중복 없음, 흡수 ID가 행으로 남아 있는 경우 없음, 최종 생존 ID 전부 실재.

| 생존 ID | 흡수한 ID |
|---|---|
| ST-011 | ST-010, ST-012, ST-013 |
| ST-015 | ST-016, ST-222 |
| ST-006 | ST-007, ST-008, ST-020 |
| ST-002 | ST-003 |
| ST-039 | ST-040 |
| ST-041 | ST-042 |
| ST-033 | ST-036 |
| ST-031 | ST-043 |
| ST-035 | ST-034 |
| ST-032 | ST-037 |
| ST-061 | ST-066 |
| ST-063 | ST-064 |
| ST-253 | ST-067, ST-254 |
| ST-060 | ST-062 |
| ST-080 | ST-081, ST-095 |
| ST-082 | ST-093 |
| ST-088 | ST-092 |
| ST-083 | ST-084, ST-085 |
| ST-087 | ST-086 |
| ST-116 | ST-115 |
| ST-111 | ST-112 |
| ST-124 | ST-125, ST-317 |
| ST-151 | ST-121 |
| ST-136 | ST-137, ST-138 |
| ST-130 | ST-139 |
| ST-160 | ST-161, ST-162 |
| ST-166 | ST-165 |
| ST-168 | ST-275 |
| ST-174 | ST-175 |
| ST-170 | ST-171 |
| ST-190 | ST-191 |
| ST-192 | ST-202 |
| ST-210 | ST-213 |
| ST-216 | ST-221 |
| ST-233 | ST-234, ST-235 |
| ST-232 | ST-241 |
| ST-236 | ST-238 |
| ST-255 | ST-256, ST-258 |
| ST-250 | ST-251, ST-252 |
| ST-276 | ST-277 |
| ST-344 | ST-285 |
| ST-272 | ST-273, ST-274 |
| ST-270 | ST-271 |
| ST-044 | ST-278 |
| ST-300 | ST-302, ST-306 |
| ST-313 | ST-312 |
| ST-320 | ST-322 |
| ST-201 | ST-328 |
| ST-345 | ST-346, ST-347 |
| ST-350 | ST-349 |
| ST-219 | ST-218, ST-220 |
| ST-096 | ST-097, ST-098 |
| ST-199 | ST-200 |
| ST-280 | ST-281 |
| ST-152 | ST-153 |
| ST-315 | ST-019, ST-314 |
| ST-230 | ST-231 |


**사슬(생존 행이 나중에 다른 행에 흡수된) 사례는 최종 생존 행으로 평탄화했다**: ST-020 → ST-007 → ST-006, ST-314 → ST-019 → ST-315. 최종 생존 ID만 표에 나타나며, 각 ID는 흡수 열과 생존 열에 동시에 나타나지 않는다(스크립트 검증).
