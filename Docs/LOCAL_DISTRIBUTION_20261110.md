# 로컬 배포 준비 — Windows per-user 설치 프로그램 (2026-11-10)

상태: **Preview 패키지 생성 및 스모크 통과.** 전체 QA 릴리스가 아니며, 푸시/PR/배포는 수행하지 않았습니다.

## 1. 산출물

| 항목 | 값 |
| --- | --- |
| 설치 프로그램 | `artifacts/local-distribution-20261110/installer/SourceGitClassic-Preview-2026.23-preview-win-x64-setup.exe` |
| SHA-256 | `52c54b4419f210ed8b96db298717437f8e17a2565f5f0ddc00e99187717a930a` |
| 크기 | 38,595,337 bytes |
| 대상 | Windows 11 x64, 현재 사용자만(관리자 권한 불필요), 기본 `%LOCALAPPDATA%\Programs\SourceGitClassic` |

`artifacts/`는 checkout 밖의 gitignore된 작업 디렉터리입니다. 소스 체크아웃에 추가한 파일은
`tools/Publish-ClassicPortable.ps1`, `tools/installer/SourceGitClassic.iss`,
`tools/installer/assets/README-KO.md`, 본 문서 4개뿐입니다(커밋/스테이징은 코디네이터 담당).

## 2. 빌드 모드 (정확한 표시)

- SDK: 프로젝트 전용 .NET **10.0.403** (`%LOCALAPPDATA%\Microsoft\dotnet10`의 비공개 설치, `tools/dotnet-install.log`의 공식 설치 기록). 시스템 `dotnet`은 9.x이므로 사용하지 않았습니다.
- `dotnet publish src/SourceGit.csproj -c Release -r win-x64 --self-contained true -p:DisableAOT=true -p:DisableUpdateDetection=true -p:UseAppHost=true`
- **NativeAOT 미사용**: self-contained JIT 빌드입니다. AOT는 네이티브 링커 툴체인 확인 없이 진행하지 않았으므로 "AOT 빌드"로 표현하지 않습니다.
- 런타임 포함이므로 수신자는 .NET SDK/런타임을 설치하지 않습니다. **Git for Windows는 별도 필요**(앱이 git 실행 파일을 번들에 포함하지 않음).
- 오류·경고 0으로 publish 완료(로그 `artifacts/.../logs/publish_*.log`), 어셈블리 버전 문자열 `v2026.23-dirty`(작업 트리 dirty 상태 그대로).

## 3. 자동 업데이트 제거 검증

`DisableUpdateDetection=true`로 `DISABLE_UPDATE_DETECTION` 상수를 정의해 컴파일했습니다.
게시된 `SourceGit.dll`에서 업스트림 확인 URL 문자열 `data/version.json` 검색 결과 0건 —
업스트림 업데이트로 이 앱이 덮어써지는 경로가 빌드에 존재하지 않습니다.
`About` 화면의 웹사이트 링크는 그대로입니다.

## 4. 데이터 격리 및 기본 설정

- Windows 포터블 모드: exe 옆 `data\` 폴더가 있으면 그곳을 Config/Cache로 사용합니다(`src/Native/Windows.cs`). 설치 프로그램이 `[Dirs] {app}\data`로 생성하므로 **원본 SourceGit의 `%APPDATA%\SourceGit`을 읽거나 쓰지 않습니다.**
- 설치는 `data\preference.json`이 **없을 때만** 시드합니다(`seed-preference.json` 복사). JSON 키는 실제 직렬화와 일치: 최상위 `Locale` = `"ko_KR"`(`src/Models/Locales.cs`의 실제 코드), `Layout.UseClassicLayout` = `true`(`src/ViewModels/LayoutInfo.cs`). 저장소는 System.Text.Json source-generation 기본 멤버명(PascalCase)이며 실 사용자 백업 `preference.json` 키 체계로 확인했습니다.
- 저장소 목록·최근 기록·자격증명·사용자 경로는 어떤 파일에도 포함하지 않았습니다(빈 상태로 시작).
- 삭제 정책: `artifacts/local-distribution-20261110` 루트 내부에서 절대경로 검증 후에만 삭제했습니다.

## 5. 라이선스·번들 제외 항목

- 원저작 MIT: `LICENSE.txt`로 설치 폴더에 포함(원문 보존). 제3자 고지: `THIRD-PARTY-LICENSES.md` 포함.
- git-flow CLI 바이너리는 라이선스 확인이 없어 **포함하지 않았습니다.** Git Flow 기능은 사용자가 해당 CLI를 별도 설치했을 때만 동작합니다.
- PDB 3종(AvaloniaEdit, AvaloniaEdit.TextMate, SourceGit)은 스테이징에서 제외. 로그·키·픽스처·캐시·userdata 없음.
- 앱 아이콘은 기존 SourceGit `src/App.ico`(MIT 저장소 자산) 사용. 앱 이름/AppId는 원본과 구분됩니다(`SourceGit Classic Preview`, AppId `{56B78231-DB7B-43D8-9D48-4A8386E019B4}`).
- 출처: 개인 포크 `https://github.com/Bo-sung/sourcegit`(upstream sourcegit-scm/sourcegit MIT 표기 유지). 공식 SourceGit으로 홍보하지 않습니다.

## 6. 설치 도구 검증 (프로젝트 로컬, pinned)

Inno Setup, NSIS, WiX가 모두 설치되어 있지 않아(레지스트리·PATH·일반 경로 조회 결과 없음),
공식 배포본을 아티팩트/tooling 아래에만 설치·사용했습니다.

- 파일: `innosetup-6.7.3.exe`, SHA-256 `9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732`
- 출처: `https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe`(공식 jrsoftware 리다이렉트 안내)
- 검증: `https://files.jrsoftware.org/is/6/innosetup-6.7.3.exe.issig`의 file-hash와 동일 + Authenticode `Valid` (서명자 `CN=Pyrsys B.V.`)
- 설치: `/VERYSILENT /CURRENTUSER` + `/DIR=<artifacts>/tooling/InnoSetup6` (ExitCode 0). 주의: 설치 중 HKCU에 `.iss` 파일 확장자 연결(association)이 등록됐습니다(현재 사용자 전용, 제품과 무관). 이전 상태를 알 수 없어 자동 되돌리지 않으며 여기에만 공개합니다. 컴파일러는 제품 설치 프로그램에 포함되지 않습니다.

## 7. 스모크 결과 (대체 식별자, rev2)

UTC 11:57–11:59, `tools/Publish-ClassicPortable.ps1 -SkipPublish -Smoke`의 실측. 스모크는 **컴파일 타임 `SmokeMode`로 별도 AppId** `{822B4D61-…}`를 박은 별도 설치 프로그램(단축키 없음)만 사용하며, 실 제품 설치(HKCU `{56b78231-…}_is1`)존재 여부를 먼저 조회해 있으면 중단하도록 했습니다(당시 없음).

| 항목 | 값 |
| --- | --- |
| 제품 설치 프로그램 SHA-256 | `52c54b4419f210ed8b96db298717437f8e17a2565f5f0ddc00e99187717a930a` (38,595,337 B) |
| 스모크 전용(대체 ID) 설치 프로그램 SHA-256 | `68f2d3904a42a9a0657528add1314ea5517b370a7e6ad18347f60e78679d2133` |

1. fixture 설치(`/DIR`을 artifacts 내, ExitCode 0) → `data\preference.json` 시드 파싱 `Locale=ko_KR`·`Layout.UseClassicLayout=true`
2. 사용자 데이터 sentinel(`data\smoke-sentinel.txt`) 작성 후 같은 디렉터리에 **재설치** → sentinel 유지, `preference.json` 해시 동일(시드가 기존 설정을 덮어쓰지 않음 확인)
3. 설치된 exe 실제 실행 — `ExecutablePath` 확인 실패 시 킬 없이 fail-closed, fixture 내부 경로 확인 후 own PID(37976)만 종료(`finally`에서도 검증된 PID만 정리)
4. 실 사용자 `%APPDATA%\SourceGit\preference.json`는 SHA-256 해시만 기록·비교(내용 미조회). 전후 동일(`0AD00C14…`) — 원본 미변경
5. fixture의 자체 uninstaller만 실행(ExitCode 0) → `preference.json`·sentinel 보존, 제품 AppId 레지스트리 키 미생성 확인, artifacts 내 fixture 정리(스모크 트리 삭제됨)

**SMOKE PASS** — 단 GUI 텍스트(한글 렌더링 화면 그 자체, 키보드 포커스)는 이 테스트로 검증하지 않았습니다(`GUI NOT VERIFIED`). `Docs/ALL_TESTS_STATUS_20261110.md`의 남은 검증·결함 행은 그대로 유효하며 전부 통과로 되돌리지 않았습니다.

## 8. 재현 절차

```powershell
# checkout의 tools/에서 (SDK 10.0.403과 tooling의 ISCC가 있을 때)
powershell -ExecutionPolicy Bypass -File .\Publish-ClassicPortable.ps1
# 이미 게시물이 있으면: -SkipPublish, 스모크 포함: -Smoke
```

스크립트는 첫 git/MSBuild 호출 전에 fixture `GIT_CONFIG_GLOBAL`/`GIT_CONFIG_SYSTEM`/`GIT_CONFIG_NOSYSTEM=1`을 지정해 실 사용자 git 설정을 읽지 않으며, 변경 사항은 `finally`에서 되원본 복구합니다(caller 오염 없음), `DOTNET_ROOT`를 선택 SDK로 지정합니다. 단, "artifacts 밖에는 아무 일도 없다"고 표현하지는 않습니다 — 실제 범위_disclosure는 10번 절 참조. ISCC는 컴파일 타임스탬프를 박아 넣으므로 매 재빌드마다 설치 프로그램 해시가 달라집니다. 위 해시는 2026-11-10 11:58–59Z 최종 빌드의 것입니다.

## 9. 알려진 제한 (Preview)

- 서명 없는 빌드 → SmartScreen 경고 예상. 자동 업데이트 없음(교대 설치도 수동).
- GUI 남은 검증(키보드 포커스, 실 화면 픽셀 기준 1440×900·125% DPI, 인증 오류 표시·Askpass, 성능·SourceTree 비교) 미완료 — `ALL_TESTS_STATUS_20261110.md` 기준.
- git-flow CLI 외부 필요(위 참조). Avatar 등 외부 네트워크 기능은 환경 의존.

## 10. 범위_disclosure (artifacts 밖 실제 변경)

"artifacts 밖에는 아무것도 설치하지 않는다"는 표현은 정확하지 않아 삭제했습니다. 실제 범위:

| 시점 | artifacts 밖 변경 |
| --- | --- |
| 컴파일러(tooling) 1회 설치 | `%LOCALAPPDATA%` 밖 project-local DIR 지정, HKCU `.iss` 확장자 연결 등록 — 이전 상태 알 수 없어 되돌리지 않고 공개만 |
| 제품 설치(실제 배포 시에만) | `%LOCALAPPDATA%\Programs\SourceGitClassic` 파일, HKCU 제거 키 `{56b78231-…-4a8386e019b4}_is1`, 시작 메뉴 바로가기를 HKCU에 등록 |
| 스모크(rev2) | 파일은 artifacts 내만. Inno는 설치 시 AppId별 HKCU 제거 키를 기록하므로 대체 AppId `{822b4d61-…-4321584b8d0a}_is1`가 설치 중 기록되고 fixture uninstall로 제거 standard. **실행 후 잔여 없음**(prod/smoke 키 모두 False)과 제품 키 미생성을 확인. 단축키는 SmokeMode에서 생성하지 않음 |
| 제품 설치 감지 | HKCU `{56b78231-…}_is1` 읽기 전용 조회, 존재 시 스모크 중단 — 실 설치 변경 금지 |
| 실 사용자 설정 파일 | `%APPDATA%\SourceGit\preference.json`는 존재 여부와 SHA-256 해시만 사용(내용 미조회) |
