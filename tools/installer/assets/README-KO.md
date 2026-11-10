# SourceGit Classic Preview (2026.23-preview)

SourceGit의 Classic(구버전 SourceTree 스타일 레이아웃) 포크 **미리보기(Preview)** 빌드입니다.
공식 배포가 아니며, 전체 QA를 통과한 릴리스가 아닙니다.

- 소스(출처): 개인 포크 <https://github.com/Bo-sung/sourcegit> (upstream: sourcegit-scm/sourcegit, MIT)
- 빌드: Windows x64, self-contained(.NET 런타임 포함 — 별도 설치 불필요), JIT 빌드(NativeAOT 아님)
- 자동 업데이트: **컴파일 단계에서 제거**됨 (`DisableUpdateDetection=true`). 업스트림 업데이트가 이 앱을 덮어쓰지 않습니다.

## 설치 전 확인사항

1. **Git for Windows 선탑재 필요**: 이 앱은 git 실행 파일을 포함하지 않습니다.
   **먼저 Git for Windows를 설치한 뒤** 자동 인식이 안 되면 설정에서 git 경로를 지정하십시오.
   Git이 전혀 설치되어 있지 않으면 경로 지정만으로는 해결되지 않습니다.
2. **서명되지 않은 빌드**: 코드 서명이 없어 Windows SmartScreen이
   "알 수 없는 프로그램" 경고를 표시할 수 있습니다. ["추가 정보" → "실행"]으로 계속합니다.
3. 설치 관리자는 관리자 권한 없이 현재 사용자에만 설치합니다
   (기본 위치: `%LOCALAPPDATA%\Programs\SourceGitClassic`).

## 실행 전 주의 (설치 후)

- 실행 파일: `SourceGit.exe` (설치 폴더의 `data` 폴더를 함께 두어야 합니다 — 설치 관리자가 자동으로 만듭니다)
- **데이터 격리**: 설정·자산을 exe 옆 `data\` 폴더에만 저장합니다.
  기존 SourceGit의 `%APPDATA%\SourceGit`은 읽거나 쓰지 않습니다.
- 한국어(`ko_KR`)·Classic 레이아웃이 활성화된 기본 설정으로 설치됩니다(이미 설정이 있으면 덮어쓰지 않음).
- 저장소 목록·최근 기록·자격증명은 번들에 포함되지 않습니다(빈 상태로 시작).

## 알려진 제한 (Preview)

- GUI 텍스트 렌더링·키보드 포커스 관련 일부 테스트 항목이 미완료입니다
  (`Docs/ALL_TESTS_STATUS_20261110.md` 참조 — "남은 검증").
- 코드 서명 없음, 자동 갱신 없음(직접 설치 교대).
- git-flow Next 기능은 git-flow-next CLI가 별도로 필요하며(설치 프로그램에 포함되지 않음)
  라이선스 확인이 끝날 때까지 포함하지 않습니다. Git Flow 기능은 해당 CLI 설치 시에만 동작합니다.
- Avatar 등 외부 이미지·네트워크 기능은 실행 환경에 따라 동작하지 않을 수 있습니다.
- 성능·SourceTree 비교 측정 등 남은 검증 항목이 남아 있습니다(전체 QA 아님).

## 라이선스

- 원저작 MIT 라이선스는 `LICENSE.txt`에 그대로 보존되어 있습니다.
- 제3자 컴포넌트 고지는 `THIRD-PARTY-LICENSES.md`를 참조하십시오.
