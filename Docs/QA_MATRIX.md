# QA evidence

P0: restore PASS; Debug build PASS 0 warnings/errors; Release build PASS 0 warnings/errors; original dotnet run PASS, visible fixture repository. Logs: artifacts/p0-restore.log, p0-build-debug.log, p0-build-release.log, p0-run.log. Screenshots: p0-history-1920x1080.png, p0-file-status-1920x1080.png, p0-diff-1920x1080.png. Actual Diff screenshot visually inspected.

P0 source inspection and executable build/run gate PASS. SourceTree private screens not exported. Runtime performance baseline NOT MEASURED (see PERFORMANCE_BASELINE). P1 working vertical slice PASS; full P1 visual/DPI acceptance remains open. No overall POC completion claim.

| Test | Status | Evidence / next check |
|---|---|---|
| Baseline fixture open -> History | PASS | Original GUI graph, branch main, fixture commit visible |
| T02 modified + untracked File Status -> Diff | PASS (original) | UNSTAGED (2), readme.txt diff +1 and correct fixture content |
| T03 Stage/Unstage | PASS | GUI double-click tracked file -> CLI `M  readme.txt`; staged file double-click -> CLI ` M readme.txt`. Then GUI Stage All -> A new-file.txt + M readme.txt. qa-stage-cli.txt / qa-unstage-cli.txt |
| T05 Commit/Amend | PASS commit / NOT TESTED amend | GUI editor and Commit created 10816b0c1278c91e4b20d3d58910f530dcb7a142, subject Classic UX fixture GUI commit; GUI staged/unstaged both zero and CLI clean. qa-postcommit-log.txt |
| T06 protected checkout | NOT TESTED dirty protection / PASS clean checkout | GUI tree double-click feature/demo then main; CLI branch matched both and working tree clean. qa-checkout-cli.txt |
| T12 multiple worktrees | PASS initial smoke | GUI expands existing Worktrees group, opens fixture-worktree into second tab; main fixture has 1 unstaged, linked worktree has 0, matching CLI |
| T16 repository tab separation | PASS initial smoke | Two linked-worktree tabs retain distinct branches and counts; returning to main via tab and Ctrl+2 restores File Status badge 1 |
| T17 settings restart | PASS | GUI checkbox writes Layout.UseClassicLayout=false, active Classic remains until restart; restart restores original. Sidebar drag changed 250 -> 275.333333 DIPs, survives restart, final screenshot divider moved from ~382 -> ~420 px |
| T18 original application coexistence | PASS observed interval | Installed PID 25964 left running; separate portable data/IPC; original %APPDATA%/SourceGit/preference.json SHA256 before/after identical |
| T19 original layout recovery | PASS | Checkbox off + restart restores upstream icon rail; p1-original-recovery.png. Classic re-enabled in portable data for review |
| T20 Light/Dark and DPI | PARTIAL | Dark actual 1920x1080 PASS; Light actual 1462x956 PASS basic clipping check. Host is 150% DPI. Exact 1440x900 and 125% DPI NOT TESTED |
| T01, T04, T07–11, T13–15 | NOT TESTED | Outside completed initial evidence |

## P1 command and navigation smoke

- PASS: File Status -> readme.txt -> correct Diff, Stage/Unstage/Commit, History -> committed SHA -> Changes -> readme.txt -> correct Diff. p1-diff-1920x1080.png / p1-history-diff-1920x1080.png.
- PASS: Commit toolbar selects File Status; Branch button and Ctrl+B open existing CreateBranch popup, cancelled. Ctrl+2 returns to File Status with synchronized navigation.
- PASS: Merge button lists existing branches, feature/demo opens existing Merge popup with conflict preview, cancelled. No merge executed.
- PASS: no-remote initial toolbar visually disabled Pull/Push/Fetch. Added an empty fixture-only local bare remote; Fetch opens existing popup and executes successfully. Pull/Push/Stash/Create Tag popups open and cancel. No push, pull, stash or tag mutation executed.
- PASS: More flyout hosts existing advanced toolbar. Original commands remain reachable. Classic shortcut host restores Ctrl+B and registers original direct Fetch/Pull/Push gestures once; Ctrl+B verified, direct network gestures NOT TESTED.
- PASS: restart persisted Classic/width and original recovery. Only one Repository VM is reused per tab; Commands and Watcher unchanged by diff inspection. No CPU/memory or object-count proof claimed.
- Final Debug: `dotnet build src/SourceGit.csproj --no-restore`, PASS 0 warnings/errors, 14.02 s. Final Release: `dotnet build -c Release`, PASS 0 warnings/errors, 13.74 s. Native AOT publish NOT TESTED.
- `git diff --check` PASS. Earlier failed integration iterations were fixed; logs/review messages retain those failures. Runtime GUI tests performed by Lead, beyond worker compile claims.

## Integration checkpoint 2026-11-08 (A22 submodule + Fetch summary, Qwen fallback lead)

Source: [INTEGRATION_CHECKPOINT_20261108.md](INTEGRATION_CHECKPOINT_20261108.md). Locale integration + serial build + actual GUI smoke.

| Test | Status | Evidence / next check |
|---|---|---|
| A22 update dialog, clean submodule | PASS | `artifacts/a22-08-update-popup.png`; `대상: modules/demo` + recorded-commit/init/recursive hints in Korean, no `!!!KEY!!!` |
| A22 option text tracks init/recursive/remote checkboxes | PASS | `a22-09-update-remote-mode.png`, `a22-10-update-initoff.png` |
| A22 dirty-target warning (`수정됨:`) reflects the loaded model, no discard/reset/force wording | PASS | `a22-16-update-dirty-warning.png`; `git submodule status` identical before/after |
| A22 uninitialized target shows the init option | PASS | `a22-21-update-notinited.png` after `git submodule deinit -f modules/demo` |
| A22 execute positive: popup auto-closes only on true | PASS | `a22-32-uninit-update-success.png`; CLI shows the submodule initialized at `9de7802` |
| A22 execute failure: popup stays open, error list shown | PASS | `a22-27-uninit-update-exec.png`; `fatal: transport 'file' not allowed` for a fresh local clone |
| A22 sidebar row + hover tooltip card | PARTIAL | row PASS (`a22-15/18/23`, path + 8px dirty dot); tooltip NOT TESTED, no hover primitive in `orca computer` |
| A22 dirty submodule visible in File Status | NOT SHOWN (upstream design) | `src/Commands/QueryLocalChanges.cs:23` passes `--ignore-submodules=dirty`; CLI with the same flag matches the GUI's empty list |
| A04 Fetch single remote summary, no ref-pattern row/clipping | PASS | `a04-01-fetch-popup-fixture.png` |
| A04 Fetch All summary lists both fixture remotes + endpoints | PASS | `a04-03-fetch-popup.png` (`fixture, second`) |
| A03 add remote (GUI) writes `remote.<n>.url` + fetch refs | PASS | `a03-03-addremote-filled.png`, `a03-04-after-add.png`; CLI showed 4 `refs/remotes/qa-third/*` |
| A03 edit remote = rename + URL change | PASS | `a03-06-editremote-changed.png`; old `remote.qa-third.*` gone |
| A03 per-remote Push URL set / reset | PASS | `a03-09-pushurl-filled.png`, `a03-12-pushurl-cleared.png`; `pushurl` key added then removed |
| A03 Push popup marks the `pushURL` endpoint | PASS | `a03-22-push-popup.png` |
| A03 Fetch All restored refs deleted by CLI on two remotes | PASS | `a03-17-fetch-all-checked.png`, `a03-19-fetch-done.png` |
| A03 delete remote | PASS | `a03-20-deleteremote.png`, `a03-21-after-delete.png`; `git remote -v` back to 2 remotes |
| A03 upstream tracking from the branch menu | PASS | `a03-24-setupstream.png`; `branch.main.remote=second`, then unset |
| A03 default-remote combo selection | NOT TESTED | ComboBox dropdown items are absent from UIA and clicks dismiss them; unset field now shows "없음: …" (`a03-23-default-remote-auto.png`) |
| A03 two fetch URLs on one remote | DOCUMENTED GAP | `QueryRemotes` keeps one URL per name; multi-URL editing would be a rewrite (outside the accepted A03 wording) |
| A21 per-remote SSH key stored in fixture-local config | PASS | `a21-02-editremote-ssh.png`, `a21-03-editremote-key.png`; `remote.qa-renamed.sshkey=artifacts/ssh-fixture/id_ed25519`, nothing written to `%USERPROFILE%/.ssh` |
| A21 real SSH failure -> Git output + actionable hint, no secret echo | PASS | `a21-07-fetch-error-hint.png`; `git fetch qa-renamed` reproduces the same stderr |
| A21 host-key / publickey / HTTPS-401 / TLS classes, Askpass dialog | NOT TESTED | needs a reachable server or a credential prompt; loopback/`.invalid` endpoints and no credential entry only |
| Locale integration | PASS | en_US/ko_KR 1132 keys each, XML valid, 0 duplicates, key sets equal, 0 placeholder mismatches, 0 missing `Text.*` references, `git diff --check` PASS |
| Build after integration | PASS | Debug 12.38 s / Release 14.35 s, 0 warnings/0 errors |
| Original installed settings | PASS | `%APPDATA%/SourceGit/preference.json` SHA256 `0AD00C14…F0B9DA` identical before and after every pass |
| Tooltip text, 125% DPI, exact 1440x900, A01-A01/A05-A17 remainder | NOT TESTED | unchanged from earlier rows; do not infer PASS |

GUI geometry: dev window captured at actual 1920x1080, screenshot `scale: 1`, host 150% DPI. Another local app (Blender) held the foreground; QA raised the dev window with `SetWindowPos(HWND_TOPMOST)` + an ALT-tap `SetForegroundWindow` and restored the z-order afterwards. No benchmark was measured.



## A18 / A19 / A20 (commit template, issue links, signing) — 2026-11-09

Full table and evidence list: [A18_A19_A20_QA.md](A18_A19_A20_QA.md). Summary:

| Case | Result | Evidence |
|---|---|---|
| A18 templates tab: list, editor, repository-scope note, Git `commit.template` value | PASS | `a18-01-configure-template-tab.png`, `a18-07-template-footer.png` |
| A18 new template persisted to the repository settings file | PASS | `.git/sourcegit.settings` → `"Name": "QA A18 Template"`; editor capture `a18-05-template-editor.png` |
| A18 picker applies a template with `${branch_name}`/`${files_num}` substitution | NOT TESTED | Avalonia `ContextMenu` items do not fire `Click` through the UIA invoke path; code path unchanged upstream |
| A19 rules read from shared `.issuetracker` and local config, storage scope shown | PASS | `a19-06-rules-loaded.png`, `a19-02-fixture-issuetracker-tab.png` |
| A19 GUI rule edit persisted, other keys untouched | PASS | `git config --local issuetracker.GitLab.regex` after the GUI edit (`a19-09/10`) |
| A19 issue reference rendered as a link in history and commit details | PASS | `a19-14-commit-detail.png`, `#123` → `https://gitlab.invalid/-/issues/123` |
| A20 effective format/program shown next to the repository checkboxes | PASS | `a20-02-signing-enabled.png` (`ssh` / `ssh-keygen`), openpgp default for other repos |
| A20 checkbox persisted, GUI commit actually signed | PASS | `commit.gpgsign=true`; `git log -1` `%G?` = `G`, `Good "git" signature for qa-principal@signing.test` (`a20-04-signing-commit.png`) |
| A20 signing dependency guidance | PARTIAL | warning UI renders for a missing absolute program path or an empty `user.signingkey`; negative screenshots NOT TESTED, commit-details signature row NOT TESTED |
| Fixture isolation of signing | PASS | key, `gpg.format`, `user.signingkey`, `allowedSignersFile` only in `artifacts/signing-fixture` local config; global config and installed preference (`0AD00C14…B9DA`) unchanged |
| Locale after +11 keys | PASS | en_US/ko_KR 1124 entries each, equal key sets, 0 placeholder mismatches, 0 unresolved `Text.*` in 866 usages |
| Build | PASS | Debug 12.57 s and 18.56 s, 0 warnings / 0 errors |

Automation note: another local app keeps stealing the foreground (`window_not_focused` for `press-key`, popups dismissed instantly). Element-index actions still work; flyout/context-menu item clicks do not fire this way, which is why two A18/A19 rows stay NOT TESTED rather than failing.

## A10 / A11 GitFlow settings (2026-11-09)

Full record: [A10_A11_GITFLOW_SETTINGS_QA.md](A10_A11_GITFLOW_SETTINGS_QA.md).

| Test | Status | Evidence / next check |
|---|---|---|
| A11 legacy shape read | PASS | `a10a11-09-settings-fixed.png`, `a10a11-17-legacy-clean.png` |
| A11 edit + save writes only the listed keys | PASS | `gitflow.prefix.feature=qa-feature/` via GUI, `git show-ref` identical before/after (`a10a11-11-saved.png`) |
| A11 persistence and impact rows (branch existence, prefix match 1 → 1) | PASS | `a10a11-13-reopen.png`, `a10a11-24-editor.png`, `a10a11-27-editor-fit.png` |
| A11 validation (duplicate base branch) keeps the dialog open | PASS | `a10a11-15-validation.png` |
| A11 `git-flow-next` shape read-only + values shown | PASS | `a10a11-16-next-readonly.png`, `a10a11-28-next-display.png`, zero edit controls |
| A11 repository without git-flow keys, read-only explanation | PASS | `a10a11-21-none-shape.png` |
| A11 no branch create/rename/delete/checkout, `InitGitFlow` never called on an initialized repo | PASS | code path (`Config.SetAsync` only) + GUI save evidence above |
| A10 dependency detection and missing-dependency guard | PASS | `git flow version` exit 1; init blocked, config/refs unchanged (`a10a11-23-init-blocked.png`) |
| A10 init → feature → release (+tag) → hotfix positive flow | BLOCKED / NOT TESTED | no `git-flow` or `git-flow-next` executable for `C:\Program Files\Git\bin\git.exe`; installing one is out of scope and provenance of the classic tool could not be verified |
| Hint hook classification (`KeyFor`) | PASS | `artifacts/probes/HintProbe` 23/23; refused vs hostkey vs publickey vs HTTP 401/403/404 vs TLS vs repository vs local-transport distinct, unknown/merge-conflict/filesystem-denied add nothing |
| Dialog width vs the 560 DIP popup host | FIXED + PASS | long notes now `MaxWidth="510"`; before the fix the dialog scrolled sideways (`a10a11-10` vs `a10a11-27-editor-fit.png`) |
| Fixture restore after the pass | PASS | `.git/config` byte copy + `show-ref`/`HEAD`/`branch -a` diffs against the pre-task snapshots both empty |
| Build / locale after this pass | PASS | Debug 13.28 s, Release 11.85 s, 0 warnings/0 errors; en_US/ko_KR 1157 entries each, key sets equal, 0 duplicates, 0 placeholder mismatches; `git diff --check` PASS |
| 125% DPI, exact 1440x900, global git-flow key note | NOT TESTED | unchanged host limitation; the sentence renders from the same binding as the verified scope line |

Automation note for this pass: the More-flyout overlap described above is now **fixed**, see the next section and `a23-04-settings-no-flyout.png`. The rejected approaches stay rejected: `ContextMenu.ClosingEvent` is not a routed event and does not compile, and a Click-based `Flyout.Hide()` closes the menu that click just opened.

## Classic popup overlay fix + A01/A02/A05/A08 core flows (2026-11-09)

Full record: [CLASSIC_OVERLAY_AND_CORE_FLOWS_QA.md](CLASSIC_OVERLAY_AND_CORE_FLOWS_QA.md).

| Test | Status | Evidence / next check |
|---|---|---|
| Popup opened from the More flyout is fully visible (no icon row over it) | FIXED + PASS | `RepositoryToolbarHost` hides the flyout when `LauncherPage.Popup` becomes non-null; `a23-04-settings-no-flyout.png` (menu path), `a23-07-apply-from-flyout.png` (toolbar icon path) |
| Cancel then reopen from the same flyout menu | PASS | `a23-05-after-cancel.png` plus a successful reopen |
| Popup keyboard reach after the flyout closes | PASS for Escape/Enter, focus posted into the dialog | `a23-08-ctrlb.png` shows the focused name field; UIA reports no focused element, so Tab order inside a long dialog stays NOT OBSERVABLE |
| Original (non-Classic) layout untouched | PASS | no `더 보기…` host at all (`a23-13-original-layout.png`); Ctrl+1 still Dashboard and Ctrl+2 still the working copy (`a23-14`, `a23-15`) |
| Classic page shortcuts follow the visible row order | PASS | Ctrl+1 파일 상태, Ctrl+2 히스토리, Ctrl+3 스태시 (`a23-12`, `a23-11`); the reference dialog rows now match (`a23-10-hotkeys-classic.png`) |
| A01 hunk (chunk) stage | PASS - existing upstream feature, first real verification | `git diff --cached -U0` = hunk A only, status `MM readme.md` (`a23-23`, `a23-24`) |
| A01 hunk unstage | PASS | `Ctrl+U` on the staged chunk restores both hunks unstaged (`a23-27`) |
| A02 amend | PASS | reflog `commit (amend)`, same parent, two commits before and after, staged hunks inside the amended commit (`a23-33`, `a23-34`) |
| A05 dirty checkout protection | PASS | dialog lists 로컬 변경 사항 with 폐기 not preselected; 취소 keeps branch and change; 확인 is refused by Git, the error is shown and the dialog stays open (`a23-42`, `a23-43`) |
| A02 history search -> commit selection | PASS | query `amended` equals `git log --all --grep=amended`; a result click selects `64cf652804...` with its message and file list (`a23-46`, `a23-49`, `a23-50`) |
| `현재 브랜치` scope checkbox, 기준 combo, sub-hunk (line range) selection, typed amend message | NOT TESTED | the first two are not UIA-selectable, the diff presenter selects whole chunks, and `type-text` never reached the commit editor |
| Build / locale / settings after this pass | PASS | Debug 15.55 s, Release 18.68 s, 0 warnings/0 errors; locales unchanged at 1157 keys each with equal key sets; `git diff --check` PASS; installed preference hash unchanged |

Incident, repaired: a coordinate probe on an unidentified More-flyout icon started a bisect in the main
fixture; it was caught in the next accessibility dump, cleared with `git bisect reset`, and the fixture
verified identical to its pre-task snapshots. Rule for future passes: identify a toolbar icon by its
menu text or element name before clicking a coordinate.

## A06 충돌 해결 / A07 스태시 (2026-11-09)

전체 기록: [A06_A07_CONFLICT_STASH_QA.md](A06_A07_CONFLICT_STASH_QA.md). 모든 항목은 전용 폐기 fixture
`artifacts/a06a07-fixture`에서 GUI 실행 + git CLI 대조로 판정했다.

| 검증 | 상태 | 근거 / 다음에 확인할 것 |
|---|---|---|
| 고급 도구모음·충돌·스태시 아이콘에 접근성 이름 부여 | FIX + PASS | `RepositoryToolbar`(15) · `WorkingCopy`(8) · `Conflict`(4) · `StashesPage`(1)에 기존 `Text.*` 키로 `AutomationProperties.Name` 지정, UIA에서 `button 이진 탐색` `button 병합` 등으로 확인 (`a24-01`, `a24-28`) |
| 병합 전 충돌 예측 | PASS | "충돌 없이/충돌이 발생합니다" 표시가 `git merge-tree` 종료코드 0/1과 일치 (`a24-06`, `a24-18`) |
| 충돌 상태 표시 | PASS | `UU conflict.txt`/`AA conflict2.txt` + `MERGE_HEAD` + 마커 ↔ 진행중 바 "병합 중 topic → main"·해결·중단·배지 (`a24-08`, `a24-11`) |
| 파일별 해결 (내 것/상대방 것) | PASS | 해결 후 내용·마커 0·unmerged 0·`MERGE_HEAD` 유지 (`a24-12`~`a24-14`) |
| 3-way 병합 편집기 | PASS | "남은 충돌 1개"→0, `저장 후 스테이징` 후 파일=선택 내용 + 인덱스 해결 (`a24-30`~`a24-33`) |
| 외부 병합 도구 미설정 | PASS | "Missing git configuration: merge.guitool" 통지, 설정 변경·가짜 성공 없음 (파일별/전체 버튼 모두) (`a24-21`, `a24-24`) |
| 충돌 후 계속 / 중단 | PASS | 계속: 2-parent `6d701a8`·reflog `commit (merge)`·clean (`a24-16`); 중단: `MERGE_HEAD` 소멸·HEAD 내용 복원·clean (`a24-25`) |
| A07 저장 범위 (추적만 / 추적안됨 포함 / 스테이징만) | PASS | 3번째 부모 유무, 파일 잔존 여부, stash 내용 = `other.txt` 하나 (`a24-44`, `a24-46`, `a24-89`) |
| A07 모드 3종 (폐기/인덱스 유지/모두 유지) | PASS | 인덱스 유지=스테이징 변경 잔존, 모두 유지=전체 잔존, 폐기=clean (`a24-94`, `a24-90`, `a24-87`) |
| A07 목록·범위 표시, 적용(±index), 적용 충돌, 삭제 확인 | PASS | stash 선택 시 파일 목록 일치, index 복원 시 스테이지 구분 복원, 적용 충돌 `UU`+마커이고 stash 유지, 삭제 전 대상 요약 통지 (`a24-47`~`a24-53`, `a24-74`~`a24-78`) |
| 스태시 모드 문자열 미번역 | FIXED + PASS | `DealWithChangesAfterStashing`을 로케일 키 해석으로 변경 + `Text.Stash.Mode.*`·`Text.Stash.Tip*` 10키 추가, 화면이 "폐기/인덱스 유지/모두 유지"로 표시 (`a24-79`) |
| 스태시→브랜치 체크아웃, 패치로 저장, 선택 파일만 스태시, 실제 외부 병합 도구, 라인 범위 선택, A15~A17 | NOT TESTED | 파일 저장 다이얼로그·외부 바이너리·UIA 미노출 컨트롤이 각각 필요 (A12~A14는 아래 항목 참조) |
| 빌드·로케일·설정 위생 | PASS | Debug 14.02s / Release 11.70s 경고 0·오류 0, en/ko 1165키 동일 집합, `git diff --check` PASS, 설치본 해시 불변 |

## A12 파일 이력·Blame / A13 되돌리기·체리픽·리셋 / A14 대화형 리베이스 (2026-11-09)

전체 기록: [A12_A13_A14_HISTORY_REWRITE_REBASE_QA.md](A12_A13_A14_HISTORY_REWRITE_REBASE_QA.md).
전용 폐기 fixture `artifacts/a12a14-fixture`(원격 없음, 작성자별 커밋은 1회성 `-c user.*` 인자)에서
GUI 실행 + git CLI 대조로 판정했고, 실제 사용자 저장소에는 리셋·리베이스·폐기·삭제를 수행하지 않았다.

| 검증 | 상태 | 근거 / 다음에 확인할 것 |
|---|---|---|
| Blame·대화형 리베이스 아이콘 접근성 이름 | FIX + PASS | `Blame.axaml`(뒤로/앞으로/이전 리비전) · `InteractiveRebase.axaml`(작업 선택·이동·메시지 편집)에 이름 부여, UIA에서 `button 뒤로` `button 작업 선택`으로 확인 (`a25-09`, `a25-56`) |
| 리셋 모드·리베이스 작업 문자열 미번역 | FIXED (리베이스만 화면 PASS) | `ResetMode`·`InteractiveRebaseActionConverters`를 로케일 키 해석으로 변경, 26키 추가(총 1193키). `피킹/수정/…`와 행 라벨은 화면 확인(`a25-56`, `a25-64`); 리셋 모드명의 한글 표시는 코드·빌드 확인에 그침 — 패스 말미 `window_not_focused`로 재촬영 못 함 (`a25-32`는 수정 전 영어 화면) |
| A12 파일 이력 = CLI `--follow` | PASS | notes.md 4행(635c89e/ae7e19a/e54ac7b/162ed67), 미변경 커밋 b59f67f 제외 (`a25-13`) |
| A12 해당 리비전 내용 | PASS | "내용" 모드가 `git show <rev>:notes.md`와 635c89e·e54ac7b에서 일치 (`a25-14`, `a25-15`) |
| A12 Blame SHA·저자 + 이전 리비전 + 이동/되돌아오기 | PASS | 라인별 저자 = `git blame`, 이전 리비전 blame = `git blame ae7e19a`, `뒤로/앞으로` 순회, SHA 클릭 시 메인이 해당 커밋으로 이동 (`a25-05`, `a25-10`~`a25-12`, `a25-16`) |
| A13 되돌리기(커밋) | PASS | `98aa971` 1-parent·reflog `revert: Revert "edit line3"`·내용 원복 (`a25-18`, `a25-19`) |
| A13 체리픽 깨끗한 경우 | PASS | `d2d36a4` parent `98aa971`, `side.txt` 추가 (`a25-20`, `a25-21`) |
| A13 체리픽 충돌 → 중단 / 해결→계속 | PASS | `UU`+`CHERRY_PICK_HEAD`+마커+진행중 바, 중단 시 원복; `상대방 것 사용`→`계속` → `2ebe741` + `(cherry picked from …)` (`a25-26`~`a25-31`) |
| A13 리셋 soft/mixed/hard 3원 구분 | PASS | porcelain `1 M.` / `1 .M` / clean + `ORIG_HEAD` 복구 경로, hard는 미커밋 변경 폐기·확인은 리셋 대화로 유지 (`a25-32`~`a25-40`) |
| A13 체크아웃 없는 브랜치 리셋 | PASS | `lab`만 162ed67→635c89e, HEAD는 `reset-lab` 유지 (`a25-41`) |
| A14 대화형 편집 창·충돌 예측·옵션 | PASS | `대상 브랜치/기준` 헤더, `#1~#4` 행, `리베이스 시 충돌이 발생합니다` = `git merge-tree` 충돌 1건, 오토스태시·훅 건너뛰기 노출 (`a25-44`, `a25-66`) |
| A14 재정렬 실행 | PASS | 드래그로 `#4,#3,#1,#2` → 시작 → 실행순 r2,r1,r3,r4 (`25af6cd→4ee1886→f9b734e→dad1ea0`), reflog `rebase (pick)/(finish)` (`a25-64`, `a25-65`) |
| A14 충돌 계속 / 중단 | PASS | 계속: reflog `rebase (continue)`→`70b6f29`+해결 내용; 중단: `rebase (abort)`→`89cc491` 복원·`rebase-merge` 소멸 (`a25-70`, `a25-71`) |
| A14 개별 작업(수정/스쿼시/픽스업/삭제) 실행 | NOT TESTED | MenuFlyoutItem이 UIA Invoke/ExpandCollapse 미지원 + 대화창(별도 창)에 키 이벤트 미전달; 6종 노출·한글 이름·이니셜 정의까지는 확인 (`a25-52`) |
| A14 `건너뛰기`, Blame 워크트리 진입 | NOT TESTED | 미실행 (A15~A17은 아래 항목 참조) |
| 빌드·로케일·설정 위생 | PASS | Debug 18.23s / Release 14.41s 경고 0·오류 0, en/ko 1193키 동일 집합·미해상 참조 0, `git diff --check` PASS, 한자·가나 0, 설치본 해시 불변, `RuneCode` reflog 클론 1건뿐(무변경) |
| A15 다중 선택 삭제, 태그 체크아웃/아카이브, Delete 키 삭제 | NOT TESTED | 복수 선택 modifier 클릭·포커스 필요 (`a26-64`) |

## A15 태그 반영 / A16 워크트리 / A17 외부 도구 (2026-11-09)

전체 기록: [A15_A16_A17_TAGS_WORKTREES_TOOLS_QA.md](A15_A16_A17_TAGS_WORKTREES_TOOLS_QA.md).
폐기 fixture `artifacts/a15-fixture`(로컬 bare `remote.git` 포함)와 `artifacts/a16-fixture`에서
GUI 실행 + CLI 대조로 판정했고, network push·전역 설정 변경·비픽스처 삭제는 하지 않았다.

| 검증 | 상태 | 근거 / 다음에 확인할 것 |
|---|---|---|
| A15 태그 생성(경량/주석) + 조건부 입력란 | PASS | `v1.2.0`=type commit·HEAD 일치, `ann/1.0`=type tag·`cat-file tag`과 동일, `경량`에서 메시지 램 숨음 (`a26-48`, `a26-50`) |
| A15 태그 트리 표시 | PASS | `ann (1)`·`rel (1)` 폴더 + 플랫 항목, 토글로 원복 (`a26-66`) |
| A15 태그 푸시 대상·반영 | PASS | `태그를 원격에 푸시`(태그/원격/`모든 원격에 푸시`) → bare `tag -l`에 `v1.1.0` 등장, 생성 시 체크박스도 `refs/tags/<name>` 1건만 푸시 (`a26-54`, `a26-56`, `a26-52`) |
| A15 푸시 반영: 이미 동일 SHA / 다른 SHA로 존재 | PASS | 전자는 오류 없이 종료·bare 불변, 후자는 `오류` 통지이고 bare `a0597c2` 유지(무음 덮어쓰기 없음) (`a26-63`, `a26-79-*`) |
| A15 태그 삭제(로컬 전용 기본) | PASS | `원격 저장소에서도 삭제` 기본 해제 → 로컬만 소멸, bare에 `v1.2.0` 잔존 (`a26-57`) |
| A16 워크트리 추가·입력 차단 | PASS(차단) | 기본 경로가 픽스처 내부, 중복 브랜치·비어있지 않은 경로에서 `확인` 차단 및 `worktree list` 무변경; 오류 문구는 아이콘 툴팁이라 화면 미확보 (`a26-09`~`a26-11`) |
| A16 추가·브랜치 체크아웃·내비게이션 | PASS | `wt1`/`feat/wt1` 생성, `topic`을 워크트리로 체크아웃, 행 더블클릭 → 탭 `wt1`(브랜치 `feat/wt1`)이고 원저장소 HEAD·status 불변 (`a26-15`, `a26-18`, `a26-20`) |
| A16 제거 가드 / `--force` / 형제 격리 | PASS | 오염 워크트리는 `--force` 없이 오류 통지·전부 잔존, `--force` 시 제거되나 브랜치 `topic` 보존, `wt1`/`wt2` 브랜치·index 무변경 (`a26-31`~`a26-34`) |
| A16 잠금·정리(prune) 정책 | PASS | `locked` 등장/소멸, 잠금+경로 소멸 상태의 `정리`는 항목을 건드리지 않고(git 정책), 잠금 해제 후 `정리`에서 소멸 (`a26-35`, `a26-38`, `a26-39`) |
| A17 비유/병합 도구 진단 2종 | PASS + FIXED | `git 설정이 없습니다: diff.guitool`, `환경 설정의 비유/병합 도구가 올바르지 않습니다!` — `RaiseException` 6곳을 `App.Text("Exception.*")`로 지역화 (`a26-84`, `a26-88`) |
| A17 저장소 터미널 양성 실행 | PASS | 클래식 레일 신규 `터미널에서 열기` 단추 → `git-bash(27144)`→`mintty(24460)`/`bash(36352)`, mintty 창 제목이 `UCRT64:/c/Gits/…/a16-fixture/wt-repo` = 픽스처 디렉터리 (`a27-43`, `a27-44`) |
| A17 저장소 탐색기 양성 실행 | PASS | `파일 탐색기에서 열기` → Shell COM 열거에 `file:///C:/Gits/…/a16-fixture/wt-repo` 창 증가(`list-windows --app explorer`는 제목 없는 셸 창만 보여 탐지 불가), 픽스처 창만 닫고 사용자 창 불변 (`a27-41`, `a27-42`) |
| A17 외부 GUI 비유 도구로 차이 확인 | PASS | 지원 목록 `vscode`(`ExternalMerger.cs:35`)를 포터블 설정에 지정 + `--user-data-dir`을 픽스처 하위로 격리 → `Code.exe(22584)`이 `--new-window --user-data-dir=…/a17-fixture/vscode-profile --diff`로 기동, 창 제목 `a.txt - Visual Studio Code`에서 3행 추가가 녹색 하이라이트 (`a27-54-vscode-diff-only.png`). 사용자 Code 10개 프로세스·설정·창 불변 |
| A17 키보드 진입(`Ctrl+E`/`Ctrl+\``) | NOT TESTED | 코드 경로 확인(`Launcher.axaml.cs:175-183`,`:297-300`). `hotkey`가 `window_not_focused` 반환(공급자 `windows.focus:false`), `Ctrl+T` 통제 테스트도 무반응 |
| A17 외부 GUI 3way 병합 편집 | NOT TESTED | `--wait` 병합 세션을 여는 재작성 대기 상태가 생겨 이번 패스 범위에서 제외 |
| Classic `더 보기…` 표면 접근성 | NOT TESTED (환경) | 버튼 이름은 전부 부여돼 있음(`RepositoryToolbar.axaml:13-121`) — 직전 "이름 없음" 서술은 측정 오류. 공급자가 `surfaces:{menus:false,dialogs:false}` 를 보고하고 코드조립 `Flyout`의 팝업은 `list-windows`에도 없음(스크린샷엔 열려 있음, `a27-11`) |
| Classic 저장소 단추 노출(신규) | FIXED + PASS | 레일 헤더에 `파일 탐색기에서 열기`·`터미널에서 열기`(`Icons.Explore`/`Icons.Terminal`, 기존 로케일 키 재사용) 추가 → UIA에 `button`으로 노출되고 클릭이 실제 창을 열었다 (`a27-40`) |
| 워크트리 그룹 빈 상태(메인만 있는 저장소) | PASS (의도) | `Worktree.Build()`가 `Count<=1`에서 빈 목록 → 펼치면 행 0개·`(0)`·`정리` 아이콘 숨김·`+`만 표시 (`a27-03`) |
| Classic 탭 캡션 | PASS (회귀 아님) | 4개 저장소 탭 모두 `fixture`/`fixture-worktree`/`parent`/`tags-repo`로 표시. 직전의 `저장소`+원통 아이콘은 Welcome 탭의 제목·아이콘(`LauncherTabBar.axaml:117-123`)이며 `Launcher.cs`·`App.axaml.cs`는 diff 0 (`a27-01`) |
| 빌드·로케일·설정 위생 | PASS | Debug 18.38s / Release 15.93s 경고 0·오류 0, en/ko 1206키 동일 집합, `git diff --check` PASS, 한자·가나 0(업스트림 zh_*/ja_JP 제외), 설치본 해시 불변 |
| 이번 패스 접근성·지역화 수정 | FIXED + PASS | 사이드바 5개 그룹 헤더/액션 버튼에 이름 부여(`button 워크트리 추가` 등), `Validation.*` 8키·`Terminal.NotSpecified` 1키·`Exception.*` 5키 추가(en/ko 1206키 동일), ko_KR 가나 혼입 1건 수정 (`a26-07`) |
| A15 다중 태그 삭제, A17 진단 문자열 외 오류 문구 툴팁 확인 | NOT TESTED | 복수 선택 클릭 / 툴팁 오버 프리미티브 필요 |
| 빌드·로케일·설정 위생 | PASS | Debug 15.87s / Release 17.60s 경고 0·오류 0, en/ko 1206키 동일 집합, `git diff --check` PASS, 한자·가나 0(업스트림 zh_*/ja_JP 제외), 설치본 해시 불변 |

## Remaining risks

Latest transfer/GitFlow build and runtime results: see [TRANSFER_GITFLOW_QA.md](TRANSFER_GITFLOW_QA.md). Both configurations build; Fetch All and Push/Pull summary updates passed actual GUI smoke, missing-dependency GitFlow toolbar init blocked with unchanged fixture refs/config/status. The Fetch ref-pattern label clipping is now removed and re-verified in the 2026-11-08 integration checkpoint above. A03 and A21 then had their own pass, see [A03_A21_QA.md](A03_A21_QA.md): remote CRUD/Fetch All/PushURL/tracking and the per-remote SSH key with a real Git/SSH failure diagnosis are PASS, while the default-remote combo selection and the host-key/publickey/HTTPS-401/TLS diagnosis classes stay NOT TESTED. A10/A11 settings (`GitFlowSettings`, legacy editable, next/unknown/none read-only) are now verified, see [A10_A11_GITFLOW_SETTINGS_QA.md](A10_A11_GITFLOW_SETTINGS_QA.md); the GitFlow **execution** flow remains BLOCKED because no git-flow extension exists on this machine, so A10 is not complete. The flyout/popup overlap that pass documented is now fixed, and A01 hunk staging, A02 amend and search, A05 dirty checkout protection and the Classic page-shortcut/help mapping have PASS evidence, see [CLASSIC_OVERLAY_AND_CORE_FLOWS_QA.md](CLASSIC_OVERLAY_AND_CORE_FLOWS_QA.md); its ledger names the criteria still open (A06, A07, A12-A14, A17 and the listed sub-rows elsewhere).

### Korean localization verification (2026-11-08)

- Pi / dgx-spark / qwen3.8-flash-next added six Classic keys and completed 96 previously missing Korean keys. Worker XML/key checks: en_US and ko_KR each 1,079 keys; zero missing, extra or duplicate Korean keys; all 17 locale XML files parse; new-entry placeholders preserved. Lead reviewed the translation diff; existing translations were retained.
- Lead Debug build `dotnet build src/SourceGit.csproj --no-restore`: PASS, 0 warnings/errors, 18.83 s. Release `dotnet build src/SourceGit.csproj -c Release --no-restore`: PASS, 0 warnings/errors, 17.61 s. Logs: `artifacts/ko-complete-build-debug.log`, `artifacts/ko-complete-build-release.log`. `git diff --check`: PASS.
- Portable Debug data only: changed locale from en_US to ko_KR while the dev app was closed; launched the fixture app and observed Korean Classic labels and preferences. Actual screenshots 1920x1080: `artifacts/ko-classic-history-1920x1080.png`, `artifacts/ko-classic-preferences-1920x1080.png`. Main labels and checkbox have readable Korean without visible clipping in these captures.
- GUI coverage is limited: all 96 translated strings were NOT individually opened; runtime language switching and a third-locale English fallback are NOT VERIFIED. Keyboard automation returned `window_not_focused` even after one restore attempt; semantic menu actions opened/closed preferences, but combo set-value returned `value_not_settable`. Do not infer a product language-switch failure from these automation errors.
- No original installed app settings were intentionally modified. This localization pass changed only the portable Debug preference; a fresh original-settings hash comparison was NOT performed in this pass (the earlier P1 comparison passed).

- Exact 1440x900 target was not achieved: normal small window on this host captured 1462x956; files use actual dimensions, no resizing/cropping fabricated. Do not mark target geometry PASS. POC Classic minimum currently 960x600 DIPs; window chrome/minimum-size behavior needs follow-up.
- Full dirty checkout, amend, hunk/line staging, conflicts/rebase, LFS, large history and event storms remain untested.
- SourceTree actual toolbar/sidebar was inspected but private workspace screenshots were not exported. A public fixture SourceTree comparison still needs capture.

## A14 순서 실행 / A15 일괄 삭제 / A13 작업트리 revert / A12 작업트리 Blame (2026-11-09)

전 그룹 판정은 단일 대장 [ACCEPTANCE_LEDGER_A01_A22.md](ACCEPTANCE_LEDGER_A01_A22.md) 으로 통합했다. 아래는 이번 패스 추가 근거.

| 항목 | 판정 | 근거 |
|---|---|---|
| 대화형 리베이스 진입 (브랜치 메뉴 `$main$을(를) 기반으로 $topic$ 대화형 리베이스`) | PASS | 별도 창 `대화형 리베이스`, 기준 `9966d2d835 main commit`·대상 `topic`·행 3개 (`a28-95-state.png`) |
| 대화형 리베이스 **순서 변경 실행** | PASS | `위로 이동` 클릭 → 행 순서 변경 → `시작` → CLI: `topic-2`(adbb44b) 가 `topic-3`(476425f) 앞에 재배치, `topic-1`(a8722b7) SHA 불변 (`a28-E2-after-reorder-button.png`) |
| 대화형 리베이스 개별 작업(수정/메시지 수정/합치기/고정/삭제) | NOT TESTED (환경) | `작업 선택` MenuFlyout 항목이 트리에는 노출되나 클릭/Invoke 미발화, 좌표 클릭은 창 활성 시 light-dismiss 로 항목 도달 실패(색점 픽셀 스캔으로 좌표 교정해 6개 슬롯 481/523/565/607/649/691 확인). 제품 키보드 단축키 `P/E/R/S/F/D` 는 ListBox 포커스 필요 → `focusedElementId` null, `SetFocus` 액션 없음 (`InteractiveRebase.axaml.cs:20-77`) |
| A15 여러 태그 일괄 삭제 + 원격 삭제 | PASS | Ctrl+Click 다중 선택 → 콘텍스트 메뉴 `선택한 2개의 태그 삭제...` → `여러 태그 삭제` 팝업(`batch/1`,`batch/2`, `원격 저장소에서도 삭제` 체크) → 확인 → 로컬·bare 원격에서 두 태그 소멸, 나머지 4개 유지 (`a28-F3/F6`) |
| A13 Revert 작업 트리만(커밋 안 함) | PASS | `되돌린 변경 사항 커밋` 체크 해제 → 실행 → HEAD `7039c70` 유지·커밋 없음·`status` = `D  t3.txt` (`a28-G3-after-worktree-revert.png`) |
| A12 작업 트리 파일 Blame 진입 | PASS (범위 유의) | 파일 상태 행 메뉴 `블레임 (HEAD-only)` → `블레임` 창(파일 `t1.txt`, 리비전 `a8722b79e6 topic-1 subject`, `이전 리비전으로 책임 추적`) (`a28-H3`). 단 `git blame <revision>` 기준이라 작업 트리의 미커밋 행은 주석 대상에 포함되지 않음 (`Commands/Blame.cs:13`) |
| A02 커밋 검색 패널 | PART | `Ctrl+Shift+F` 로 진입, `커밋 검색`·`TxtSearchCommitsBox`·`검색 기준` 콤보·`현재 브랜치에서만` 노출 (`a28-J3`). 검색 실행(Enter KeyDown)·범위 전환(콤보 드롭다운)은 UIA 포커스/표면 제약으로 NOT TESTED |
| 그래프 행 접근성 정정 | FIXED | 커밋 그래프 행은 `data item`(SHA 포함) 으로 UIA 노출 → 인덱스 우클릭으로 콘텍스트 메뉴 실행 가능(이전 "커스텀 드로잉이라 미노출" 서술은 오측). 창 캡션/스케일: `coordinateSpace=window`, `scale=1`, 창 3862×2110 물리 vs 스크린샷 2880×1620 |
| 자동화 프리미티브 정정 | INFO | `perform-secondary-action` 에는 `--value` 가 없고 `set-value` 가 별도 명령(`--value` 사용). `SetFocus`/`Invoke` 는 list item 에서 미지원(`action_not_supported`/`accessibility_error`) |
| 위생 | PASS | Debug 18.10s / Release 19.42s 경고 0·오류 0, `git diff --check` PASS, 설치본 해시 `0ad00c14…38f0b9da` 불변, dev 설정 `preference-a28.bak` 복원, fixture 원복(topic 3커밋) 및 tags-repo clean |

## Headless 회귀 검증 + 판정 정정 (2026-11-09 2차)

판정 대장 [ACCEPTANCE_LEDGER_A01_A22.md](ACCEPTANCE_LEDGER_A01_A22.md) 의 §1 정정 내역과 §2 등급을 갱신했고, 상세 근거는 [HEADLESS_VALIDATION_20261109.md](HEADLESS_VALIDATION_20261109.md) 다. 이 절의 PASS 표기는 **HEADLESS 등급**(Avalonia Headless + 제품 코드 + 실제 git)이며 데스크톱 GUI 실행 증거가 아니다.

| 항목 | 판정 | 근거 |
|---|---|---|
| A14 6작업 → git todo 매핑 | HEADLESS PASS | `WriteTodoList` 산출 줄이 `p/e/r/s/f/d <sha>` — 제품 코드·6작업 전부 (`WriteTodoList_MapsEveryActionToItsGitKeyword`) |
| A14 개별 작업 **실행 결과** | HEADLESS PASS | 제품이 만든 todo 를 git 이 실행: Drop=`b.txt`·subject 소멸, Squash=커밋 3개+병합 메시지에 subject B, Fixup=메시지는 subject A만+`b.txt` 존재, Reword=메시지 변경·`HEAD^{tree}` 불변; Edit=`rebase-merge` 정지 후 `--continue` 완료 |
| A14 첫 커밋의 Squash 금지 동작 | HEADLESS PASS (동작 발견) | `CanSquashOrFixup=false`인 첫 커밋에서 Squash는 **Reword로 자동 전환**(메시지 보존), Fixup은 Pick 유지 (`SquashAndFixup_NeverApplyToTheFirstPickedCommit`) |
| A02 검색 범위 5종 + 현재 브랜치 한정 | HEADLESS PASS | 메시지(복수 단어 `--all-match`), 작성자(대소문자 무시·기본 범위는 전 브랜치), 경로(`git log -- path` 와 동일 집합), 내용(`-G`), SHA 유효성, `현재 브랜치만` 토글의 포함/제외와 `IsMerged` (`CommitSearchScopeTests` 10건) |
| A03 기본 원격 저장→재오픈 유지 | HEADLESS PASS | `RepositoryConfigure.SaveAsync` → `.git/sourcegit.settings`, 저장소 복제 후 **콜드 리로드**에서 값 복원, 변경 없으면 파일 재쓰기 없음, 저장값이 `QueryRemotes` 의 키와 일치 (`DefaultRemotePersistenceTests` 5건) |
| A18 템플릿 적용·변수 전개 | HEADLESS PASS | 전개 7종(브랜치/개수/파일/순수파일/슬라이스/정규식/이스케이프/미지수·미완) + `WorkingCopy.ApplyCommitMessageTemplate` 이 실제 `git status` 기준 staged 2건·unstaged 1건을 구분해 메시지를 **치환** (`CommitTemplateTests` 9건) |
| A18 브랜치 없는 저장소 `${branch_name}` | **FIXED** | `TemplateEngine.GetBranchName` 의 NRE(저장소에 커밋이 아직 없어 `CurrentBranch` 가 null) → `context.branch?.Name ?? string.Empty` 1줄 방어 + 회귀 테스트 |
| 테스트 격리 | PASS | portable `data` 강제 생성 + ConfigDir 위치 검사, fixture는 `%TEMP%\sourcegit-tests` 만들고 지움(잔여 0), 설치본 preference 해시 `0ad00c14…38f0b9da` 불변, 사용자 저장소 조회 없음 |
| A10 git-flow 실행 파일 | BLOCKED (해소 가능) | 공식 배포물 없음: `petervanderdoes/gitflow-avh` archived(2023-08-17), release 1.12.3~1.10.0 에셋 0개. 시스템 설치 금지·벤더링은 범위 밖 → §5 대장 참조 |
| 빌드·테스트 | PASS | Debug/Release `src` 경고 0·오류 0(14.24s/18.13s), `dotnet test` Debug 39/39 · Release 39/39, `SourceGit.slnx` 빌드 경고 0·오류 0 |
| 정정: A08 키 검사 / A18 피커 / A21 인증 계열 / A02·A13·A14 이전 증거 | 문서 정정 | 정적 검증과 GUI RUNTIME 구분, NOT TESTED였던 항목의 승격 금지, 이전 GUI PASS의 유효 확인 — 대장 §1 |

## git-flow next 설정 편집 구현 + git-flow 실행 검증 (2026-11-09 3차)

세부 근거: [A10_A11_GITFLOW_SETTINGS_QA.md](A10_A11_GITFLOW_SETTINGS_QA.md) §"next 편집", 대장 §5/§5.1. 등급은 등급 표 그대로이며 HEADLESS와 GUI를 섞어 쓰지 않는다.

| 항목 | 판정 | 근거 |
|---|---|---|
| git-flow-next 실행 파일 조달 | PASS (환경 해소) | `gittower/git-flow-next` v2.1.0 `windows-amd64.zip` 공식 어셋, SHA256 `9bfbc274…b656` = 릴리스 `checksums.txt` 일치 → `artifacts/tools/git-flow-next/git-flow.exe`(gitignore) + 개발 프로세스 PATH만 |
| 제품 감지 | PASS | `git flow version` → `2.1.0 (git-flow-next)` exit 0 → `Native.OS.GitFlowVersion=Next` (`OS.cs:380-408`) |
| A10 수명주기 (init→feature→release+태그→hotfix) | HEADLESS | `GitFlowNextFlowTests` — 제품 `Commands.GitFlow` 로 init(키 61개)·feature start/finish·release finish(태그 `v1.0`)·hotfix start/finish, 무완 입력(`does-not-exist`)에서 ref·태그 불변. 도구가 없는 환경에서는 같은 테스트가 의존성 차단과 키 0건을 검증 |
| A10 수명주기 CLI 대조 | PASS | `artifacts/a29-fixture/gf` 에서와 동일한 명령 sequence 의 원시 실행 결과(태그 `v1.0`/`vurgent`, exit 5 에러에서 rev-list 해시 불변) |
| A11 next 형식 편집 가능 | **GUI** | `더 보기…` → `Git-Flow` → `Git Flow 설정...` → 편집 컨트롤 6개(`edit main`/`edit develop`/`edit feature/`/`edit release/`/`edit hotfix/`/`edit v`) + next 키 맵 안내 문구 (`a29-34-settings.png`) — 이전의 읽기 전용 표시에서 변경 |
| A11 접두사 편집 저장 | **GUI** | `feature/` → `qa/` 저장 → `gitflow.branch.feature.prefix=qa/`, 다른 60개 키 불변, ref 불변 (`a29-36/38`) |
| A11 기준 브랜치 이름 변경 | **GUI** | `main` → `trunk` 저장, 영향 표시 `main → trunk · 로컬 브랜치 없음` → `gitflow.branch.trunk.{type,upstream,downstream,autoupdate,tag}` 생성 + `develop/release/hotfix/support` 의 `parent`·`startpoint` 5곳 갱신 + `gitflow.branch.main.*` 소멸, 키 총 61개 유지, `show-ref` 완전 동일 (`a29-35/37`, a29-refs-before/after 대조) |
| A11 콜드 리로드 | GUI | 팝업 재오픈 시 `edit trunk`/`edit qa/` 복원, 키 목록에 `*.parent` 행 (`a29-60-reopen.png`) |
| A11 next 편집 회귀 (HEADLESS) | HEADLESS | `GitFlowNextSettingsTests` 10건 — 저장 전 `Check()`(화면과 동일 경로) 포함: 키 맵 리네임+참조 갱신+무관 키(`bugfix`/`support`/전략/`gitflow.some.future.key`) 보존+ref 불변, 접두사 단독 갱신, 태그 접두사 쓰기/삭제, 충돌 이름 검증, 레거시 형식 불변 |
| 발견된 버그 1: next 형식에서 저장 버튼 불가 | FIXED | `ValidateBaseBranch` 의 키 맵 충돌 검사가 **자기 필드가 저장한 이름**까지 충돌로 판정 → `Popup.Check()` 실패로 확인 버튼이 조용히 아무것도 쓰지 않음. HEADLESS 테스트는 `Sure()`만 불러 놓쳤고 GUI에서 발견. 회귀 테스트 `TheStoredValuesValidateWithoutAnyEdit` |
| 발견된 버그 2: `RepositorySettings` 해시 경합 | FIXED | `Get()` 이 `_orgHash` 를 백그라운드에서 계산해, 읽자마자 저장하면 "변경 없음"으로 판정될 수 있었다 (`CommitTemplateTests` 간헐 실패, 단독 실행 재현) → 동기 계산으로 변경 |
| 빌드·테스트 | PASS | Debug/Release 경고 0·오류 0 (당시 51건; 4차에서 75건으로 갱신 — §4차 참조) |
| 위생 | PASS | `git diff --check` PASS, 변경 46파일 CJK 0, 로케일 en/ko 1206 동일·중복 0, 설치본 해시 `0ad00c14…38f0b9da` 불변, %TEMP% 잔여 0, 시스템 설치 없음 |

## 통합 리뷰 대응 + A10 GUI 수명주기 완료 (2026-11-09 4차)

정본: `Docs/INTEGRATION_FIX_20261109.md` (판정 근거), `Docs/ACCEPTANCE_LEDGER_A01_A22.md` §7 (요약).

| 항목 | 등급 | 근거 / 정확한 결과 |
|---|---|---|
| F-1 두 기본 브랜치 동시 변경 | FIXED → HEADLESS + GUI | CLI 재현: 결과 키 60개(기대 61) + `develop.parent` 소실. 수정(production 병합 후 설정을 다시 읽기) 후 회귀 테스트 PASS, GUI 저장에서도 `gitflow.branch.dev.parent=trunk`·옛 키 0개 |
| F-2 쓰기 실패 후 병합 시작 | FIXED → HEADLESS | CLI 재현: 키 61→59(절반 복사 후 삭제). 병합의 복사/재연결/삭제 각 단계 전에 `failed` 가드. 회귀 테스트가 "차단된 저장은 키 지도를 건드리지 않음"을 단언 |
| F-3 번역 없는 키의 원문 노출 | FIXED → HEADLESS | `App.SetLocale`이 `en_US`를 폴백으로 병합. `LocaleFallbackTests` 20건(Avalonia 테스트 속성으로 실제 `App.Initialize`와 같은 사전 구성을 사용 — 일반 Fact에는 `Application.Current`가 없어 전원이 실패했던 원인) |
| F-4 Sure의 UI 스레드 접촉 | REJECTED | `Sure()` 자신의 await에 `ConfigureAwait(false)`가 없어 UI 동기화 컨텍스트로 돌아옴. `ConfigureAwait`은 UI 접촉이 없는 쓰기 헬퍼 안쪽에만 존재 |
| F-5 테스트 저장소/Watcher 미해제 | FIXED | `TestBootstrap.OpenRepository`/`CloseRepositories`(= `Watcher.Stop()`) 도입, 13개 파일의 생성 경로 교체 |
| F-6 테스트 env 미복원 | FIXED | 생성자에서 보관, Dispose에서 `GIT_EDITOR`/`GIT_MERGE_AUTOEDIT` 복구(없었으면 제거) |
| F-7 오류 분류의 로케일 의존 | REJECTED | 분류 입력은 git/ssh 원문 + Ordinal 비교. Windows 이외에서는 `Command`가 `LANG=C`/`LC_ALL=C`를 주입해 git 출력을 영어로 고정 |
| E-1 테스트 PATH / 어셋 없음 분기 | FIXED | 삽입을 `Native.OS.GitExecutable` 대입(탐색 촉발점) 이전으로 이동, `ProcessPath`에서 어셋 자동 탐색, 어셋 없음 분기를 `AMissingDependencyIsBlockedWithAnExplanation`로 분리. 어셋을 숨긴 실행: 74 통과 + 어셋 경로를 지목하는 실패 1 |
| E-3 테스트 수 | 정정 | TRX 실행 결과 기준 **75건** (10/9/5/2/14/15/20). 이전 문서의 51·64·89는 파일 단위 또는 오래된 값 |
| E-4 분류 불가 형식 문구 | FIXED | `ReadOnlyNone`(키 없음)과 `ReadOnlyUnclassified`(키는 있으나 flow 형식 아님) 분리, 회귀 테스트(AvaloniaFact) |
| A10 GUI 수명주기 6종 | RUNTIME PASS | 초기화(`gitflow.*` 58키) → feature 시작/완료 → release 시작/완료(태그 `1.0`) → hotfix 시작(`hotfix/urgent`@main)/완료(main fast-forward·develop 병합·태그 `urgent`·브랜치 삭제) (`a30-61`~`a30-69`) |
| A07 스태시→새 브랜치 | RUNTIME PASS | `gui-stash-branch` 생성·체크아웃, 스태시 적용 및 목록 제거 (`a30-97`~`a30-100`) |
| A07 스태시→패치로 저장 | 부분(쓰기 미검증) | 메뉴 항목→OS 저장 대화상자, 대화상자의 "파일 이름" 필드에 UIA 값 지정까지(`a30-142`~`a30-145`). 슬래시 경로가 "파일 이름이 올바르지 않습니다"로 거부(`a30-146`)되고 재설정 시 `element value is not settable` → 파일 생성 없이 중단(패치 파일 없음) |
| A15 태그 삭제 (원격 체크 / 체크해제) | RUNTIME PASS | 체크 표시를 화면으로 확인한 삭제는 로컬+원격에서 제거(`a30-118`~`a30-121`); 체크를 끈 삭제는 로컬만 제거 — 로컬 `v1.0 v3.0`, 원격 `v1.0 v3.0 v4.0` (`a30-122`~`a30-124`) |
| A15 태그 체크아웃 | RUNTIME PASS | 확인 팝업 문구(detached 경고) 확인 → HEAD=`7f2dd1c`(=v1.0), `## HEAD (no branch)`; 더블클릭으로 `main` 복귀 (`a30-125`~`a30-127`) |
| A15 아카이브 | RUNTIME PASS | 리비전 `v3.0`·저장 위치 필드가 있는 팝업에서 zip 생성, 항목과 내용이 `git show v3.0:…`과 일치; 검증 후 삭제 (`a30-130`~`a30-132`) |
| (일시 차단) 태그 항목 첫 시도 | ENV | 포그라운드를 무관 앱(firefox, pid 81696)이 잡아 `--restore-window` 활성화가 3회 연속 `window_not_focused` → 좌표·우회 조작 없이 중단했고, 이후 초점이 돌아와 완료로 전환 |
| 빌드·테스트 | PASS | Debug/Release 경고 0·오류 0, 75/75 × 2 configuration, 어셋 숨김 실행 74+1 |
| 위생 | PASS | `git diff --check` WARNING=0, 문서 CJK(한자·가나) 0, 설치본 해시 `0ad00c14…38f0b9da` 불변, 실행 프로세스 0, 어셋 중복 사본 삭제 |

## 2026-11-09 5차 — git-flow 배치 쓰기의 중단·롤백(R-1), 테스트 분리(R-2), 알림 집계(R-3)

| 항목 | 판정 | 실제 관찰 |
|---|---|---|
| R-1 복사 단계가 깨지면 새 이름 아래 키가 남지 않음 | **PASS (headless)** | 다중 값 키 규칙(`exit 5`)로 복사 쓰기 하나를 확정 실패 → 첫 실패에서 중단 + 복사분 삭제 (`ACopyThatCannotFinishLeavesNoKeysUnderTheNewName`) |
| R-1 참조 재연결이 깨지면 옛 값으로 복구 + 잔존 키 명시 | **PASS (headless)** | 이미 옮긴 참조는 원값 복구, 복구는 `--unset-all` 실패로 남고 **알림 한 건**에 실패 키·git 원문·정리 커맨드가 함께 표기 (`AReferenceThatCannotBeRepointedIsMovedBackAndNamedInOneSummary`) |
| R-1 옛 키 삭제 단계가 깨지면 옛 맵 복구 후 재시도 성공 | **PASS (headless)** | 삭제 실패 → 옛 키 되쓰기 + 참조 원복 → 같은 팝업 재시도로 완전 저장 (`ADeleteThatCannotFinishRestoresTheOldMapAndSucceedsOnRetry`) |
| R-1 온전한 실패(config.lock)에서 저장 상태 불변 | **PASS (headless)** | `ABlockedWriteStartsNoRenameAndLeavesTheStoredMapUntouched` |
| R-2 기본 `dotnet test`는 도구 없이 통과 / opt-in만 실도구 | **PASS** | 기본 77/77(어셋 없음에도), opt-in 78/78, opt-in+어셋 없음은 77 통과 + 명시 실패 1건(경로·버전 보고) |
| R-3 실패 알림이 키 단위 중복 없이 한 번 | **PASS (headless)** | `KeyBatch`가 `RaiseError=false`+`ErrorReceiver`로 수집, `SendNotification`은 저장당 1회 |
| git-flow probe 결과 경합 (제품) | **PASS (수정)** | probe 번호 가드로 최신만 반영, 초기화 시점 `None` 쓰기 제거; opt-in 수명주기가 config 키 부재로 실패하던 현상 소멸 |
| 픽스처 toplevel 가드 (하니스) | **PASS** | git이 보고하는 작업트리가 요청 디렉터리와 다르면 쓰기 전 중단 |
| 빌드 | **PASS** | Debug·Release 각각 경고 0 / 오류 0 |

실측: Debug 기본 77 통과/77(어셋 있음)·77/77(어셋 없음), Debug opt-in 78/78, opt-in+어셋 없음 78 중
77 통과·1 실패(수명주기, 의존성 메시지), Release 기본 77/77. 테스트 수는 TRX/콘솔 결과 기준.
A01 선택 줄 범위 스테이징은 제품 UI가 존재하므로 **GUI 자동화 대기**로 정정(§5.1 절차), 이번 세션의
GUI 시도는 코디네이터 지시로 중단. A13 다중 커밋 revert는 수용 기준 밖 보충 유지.

## 6차 — 최종 통합 리뷰 N-1 ~ N-7 (2026-11-09)

| 확인 | 판정 | 근거 |
|---|---|---|
| N-1 롤백이 대상 이름의 기존 키를 보존 | **PASS (수정)** | 복사 단계에서 기존 값 복원 목록에 추가; 반대로 되돌리면 회귀 테스트 실패 |
| N-2 쓰기 범위를 `--local`로 분리 | **PASS (수정)** | `ReadLocalAll`/`ReadLocalAllAsync` 도입, 상속 전용은 읽기 전용; GUI 양쪽 상태 실측 |
| N-3 검사 세대 = 요청 순서 | **PASS (수정)** | 세대 배정을 호출지로 이동; 역전 재현은 git PATH 조회 특성상 불가 → 불변식 검사로 고정 |
| N-4 복제 리포지토리 닫기 | **PASS (수정)** | `CloseRepository` + 삭제 전 호출 2곳 |
| N-5 집계 문구 정확화 | **PASS (수정)** | 시도한 키만 표기; 반대로 되돌리면 회귀 테스트 실패 |
| N-6 정리 커맨드 인용 | **PASS (수정)** | `CleanupLine` 단일화; 인용을 빼면 회귀 테스트 실패 |
| N-7 읽기 실패 전파 | **PASS (수정)** | 읽기도 ErrorReceiver로 신고, 실패 시 쓰기 0건; 추적 빼면 회귀 테스트 실패 |
| 빌드 | **PASS** | src Debug·Release 경고 0/오류 0 |

실측(6차 당시): Debug 기본 83/83, opt-in 84/84, Release 기본 83/83. 이 표의 수는 7차 §10.3의
88/90으로 대체된다. GUI는 `artifacts/a33A-*`(편집 가능 상태),
`artifacts/a33B-*`(상속 전용 = 읽기 전용) 저장. 그 외 A01/A06/A07/A13/A17/A21 항목의 상태는 §5·§8 그대로.

---

## 7차 — 설정 격리 하니스·프로브 스케줄링·부분 중복 (2026-11-09)

근거 문서: `ISOLATION_AND_PROBE_SCHEDULING_FIX_20261109.md`.

| 항목 | 판정 | 근거 |
|---|---|---|
| Git 프로세스의 설정 격리(3변수 강제) | PASS | `Prepare()`가 첫 Git 실행 전에 강제. 합성 sentinel 왕복 + 원천 라벨 검증 (`GitEnvIsolationTests` 3건). §9의 83/84 진술은 아래 실측으로 대체 |
| 원천 라벨이 fixture/저장소 밖을 지목하면 실패 | PASS | `foreign-outside-the-repository (<basename>):N` 라벨. 경로·값은 출력하지 않음 |
| 프로브 요청 시각 캡처 | PASS (결정적) | `AnAnswerFromASupersededRequestIsRefused` — answer source 시임으로 도착 순서를 테스트가 결정. generation을 body에서 발급하면 FAIL |
| 늦은 답 거부(최신만 게시) | PASS (결정적) | 같은 테스트의 게시 2회 호출. 규칙 제거 시 FAIL |
| 실제 프로브 완료 순서 | PASS (opt-in) | `ARealProbeSettlesOnTheNewestRequest` — 의존성 숨김/표시 교대 10회 |
| 항목이 두 파일에 나뉜 rename | PASS (수정) | `ARenameOfAnEntryThatIsSplitAcrossFilesChangesNothing` — 쓰기 0건·알림 1건. 가드 제거 시 FAIL |
| 목적지가 윗쪽에도 있는 rename 롤백 | PASS | `ARenameUndoneLeavesNothingUnderANameThatAlsoExistsAbove` |
| 격리 미강제 감지 | PASS | `EnforceProcess()` 주석 처리 시 `GitEnvIsolationTests` 실패 |

원복(anti-fix) 민감도 실측: 4종 전부 대상 테스트가 **FAIL** (스크립트 `artifacts/probes/proof_a34.py`,
`proof_a34_pub.py`. 각 실행 후 파일 SHA-256 원복 확인 포함).

실측(TRX): Debug 기본 88/88, Debug opt-in 90/90, Release 기본 88/88, Release opt-in 90/90,
tool 없는 가정(PATH에서 tool 디렉터리 제외) 기본 88/88. 파일은 `artifacts/probes/a34-*.trx`.
src Debug·Release 빌드 경고 0/오류 0.

남은 한계: 빌드 시점 `git describe`의 환경은 호출 셸에 의존(SDK 10 `Exec`은 `EnvironmentVariable`
미지원, 실측 `MSB4064`); 태그 접두어 변경은 윗쪽 파일 전용 topic branch에 닿지 않되 덮어쓰는 키는
만들지 않음; 실사용자 설정 변경 여부는 확인하지 않음(확인이 곧 금지된 읽기).

표기 점검(7차): CJK 탐지 범위를 U+3000–303F(CJK 구두점)까지 넓혀 Docs/·src/·tests/·tools/를 다시 훑었다.
남은 건Docs 0건. 지난 라운드 문서에 섞였던 `〔ST-xxx 통합〕`·`。` 202곳은 `[ST-xxx 통합]`·`.`으로
정규화했다(`SOURCETREE_FEATURE_INVENTORY.md`, `A06_A07_CONFLICT_STASH_QA.md`).
`src/Models/Locales.cs`의 `简体中文`·`繁體中文`·`日本語`는 **상위 저장소 원본**의 언어 선택기 표기로
미수정 상태이므로 교체 대상이 아니다(여기만 한자가 남는 것이 정상).
