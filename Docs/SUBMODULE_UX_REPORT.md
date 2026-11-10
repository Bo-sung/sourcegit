# A22 Submodule status & update UX — worker report

Worker: task_1ccdaaba1a37 / ctx_9893da8ba2e0 (Pi / dgx-spark / qwen3.8-flash-next, verified from `PI_PROVIDER=dgx-spark`, `PI_MODEL=qwen3.8-flash-next`).
Workspace `C:/Gits/Tools/NewGitClient`, checkout `sourcegit`, branch `feat/classic-layout-poc`, HEAD `c0f40a2e` + uncommitted work preserved.

Status: source frozen. **BUILD NOT VERIFIED / GUI NOT VERIFIED by this worker** (Lead serializes compilation and GUI). Static checks only: both changed AXAML files parse as XML, `git diff --check` clean, no locale file touched.

## 1. Files owned and changed

| File | Change |
|---|---|
| `src/Views/SubmodulesView.axaml` | Tree-row tooltip replaced (was unreadable); one localized word added for `Normal` in the list-row detail card. Row layout, columns, selection, double-tap and context menu untouched. |
| `src/Views/SubmodulesView.axaml.cs` | **Not changed** (no code-behind change was needed). |
| `src/Views/UpdateSubmodules.axaml` | Classic-only summary block appended under the existing option grid: exact target paths, per-option effect lines, Modified/Unmerged warning. Existing controls/rows/defaults untouched. |
| `src/ViewModels/UpdateSubmodules.cs` | Reactive target summary from the already-loaded model; option properties now raise notifications; `Sure()` returns the command result instead of a hard-coded `true`. |
| `Docs/SUBMODULE_UX_REPORT.md` | This report. |

No locale, Models, Commands, Repository, Preferences, Watcher or fixture file was modified. No build, GUI launch or git mutation was run. `git status` still shows the pre-existing uncommitted set plus exactly these four paths.

## 2. What the slice does

Sidebar (tree mode, the previous gap):
* Each tree row now has a real hover card: full row path (always), plus BRANCH / SHA / URL and the localized status for submodule rows. Previously the tree tooltip was `ToolTip.Tip="{Binding Module}"`, which rendered `Models.Submodule.ToString()` — the type name `SourceGit.Models.Submodule` — i.e. an unreadable tooltip on every tree row. Evidence: old line in `git show HEAD:src/Views/SubmodulesView.axaml` (tree DataTemplate, `ToolTip.Tip="{Binding Module}"`).
* All five statuses render as localized text: `NotInited`, `Modified`, `RevisionChanged`, `Unmerged` reuse the existing `Text.Submodule.Status.*` keys; `Normal` now shows a word next to the existing green check (`Text.Classic.Submodule.Status.Normal`, new).
* Folder rows in the tree: card shows the folder path only. The module block is gated by `Module IsNotNull`, so no empty labels are emitted. The submodule detail block never depends on `ToolTip.Tip == null` behaviour.
* List mode keeps its existing card, dot, double-tap and menus; only the `Normal` word was added next to the check.
* Row indicators (goldenrod `Icons.Modified` dot bound to `IsDirty`) are intentionally unchanged per Lead instruction — no per-row badge text, no path squeezing, no duplicate indicator. Consequence kept: a `NotInited` submodule still shows no dot in the row (it is `IsDirty == false`); its state is readable in the row tooltip and in the context menu ("Open Repository"/"De-initialize" stay disabled).

Update Submodules popup (`UpdateSubmodules.axaml` + `.cs`), Classic mode only:
* **Targets** line: exactly the paths the command will receive — every loaded path when `All submodules` is checked, otherwise the selected path. Updates reactively when `UpdateAll` or the ComboBox selection changes. When the list is empty or nothing is selected, a single warning line replaces the path list.
* **Guidance** lines (one per relevant state, no implementation prose):
  * default → update to the commit recorded in the superproject;
  * `EnableRemote` on → the other line replaces it (update to the submodule's remote-tracking branch);
  * `EnableInit` on and visible → uninitialized targets are initialized first; off and visible → they are left uninitialized; when the init checkbox itself is hidden, neither line is shown (that matches the constructor, which forces `EnableInit=false` for an already-initialized pre-selected module);
  * `EnableRecursive` on/off → nested submodules included / not included.
* **Warning**: when the current targets already carry `Modified` or `Unmerged` state, the state is listed with the exact paths and the text states that this dialog does not discard, reset or force anything. No flag is auto-changed and no extra git operation is started.
* Defaults unchanged (`All` on, `Init` on, `Recursive` from settings, `Remote` off), same `Commands.Submodule.UpdateAsync` call, no new git process, no new focusable control (tab order unchanged).

## 3. Source evidence (semantics actually used)

| Claim | Evidence |
|---|---|
| Status enum and `IsDirty` | `src/Models/Submodule.cs` — `Normal/NotInited/RevisionChanged/Unmerged/Modified`, `IsDirty => Status > NotInited`. |
| Status comes from `git submodule status` prefixes | `src/Commands/QuerySubmodules.cs:22` (`Args = "submodule status"`), `:45` `-`→NotInited, `:48` `+`→RevisionChanged, `:51` `U`→Unmerged, default→Normal; regex `^([U\-\+ ])([0-9a-f]+)\s(.*?)(\s\(.*\))?$`. |
| `Modified` is a second, narrower query | `QuerySubmodules.cs:122-141`: only modules still `Normal` are re-checked with `--no-optional-locks status --porcelain -- <paths>`; a match sets `Modified`. |
| Branch/URL come from `.gitmodules` | `QuerySubmodules.cs:67` `config --file .gitmodules --list`. |
| One SHA per module only — no "expected vs actual" pair exists | `src/Models/Submodule.cs` has a single `SHA`, filled from the `git submodule status` line (`QuerySubmodules.cs` `module = new Models.Submodule() { Path = path, SHA = sha }`). Therefore the UI labels it only `SHA` (existing key `Text.CommitDetail.Info.SHA`) and never says expected/actual. |
| Executed command and flags unchanged | `src/Commands/Submodule.cs:46-65` builds `submodule update [--init] [--recursive] [--remote] -- <quoted paths>`; `UpdateSubmodules.Sure()` passes the same four values as before. |
| Model is loaded elsewhere, not by this popup | `src/ViewModels/Repository.cs:1267-1317` (`RefreshSubmodules` → `Commands.QuerySubmodules`), `:914-917` (`MarkSubmodulesDirtyManually`). The popup only reads `repo.Submodules`. |

### bool failure handling (reported, command layer untouched)

`Commands.Submodule.UpdateAsync` returns `Task<bool>` (`src/Commands/Submodule.cs:46`, `return await ExecAsync()` at `:65`), and `LauncherPage.ProcessPopupAsync` closes the popup only when `Sure()` returns `true` (`src/ViewModels/LauncherPage.cs:92-96`). The previous `UpdateSubmodules.Sure()` awaited the command, discarded the result and returned `true` unconditionally (`git show HEAD:src/ViewModels/UpdateSubmodules.cs` lines 96-98 and 102), so a failed `git submodule update` still closed the dialog. The five sibling submodule VMs (`AddSubmodule`, `DeinitSubmodule`, `MoveSubmodule`, `ChangeSubmoduleUrl`, `SetSubmoduleBranch`, `DeleteSubmodule`) all `return succ`.
Fix applied **inside the file this worker owns** (`src/ViewModels/UpdateSubmodules.cs`, `return succ;`) to match that convention; `src/Commands/**` was not edited.

## 4. Property/notification map and null/empty cases

Raised by `ObservableValidator` (`Popup : ObservableValidator`).

| Property | Raised when | Notes |
|---|---|---|
| `UpdateAll`, `SelectedSubmodule`, `EnableInit`, `EnableRecursive`, `EnableRemote` | setter (`SetProperty`) | `SelectedSubmodule`, `EnableInit`, `EnableRecursive`, `EnableRemote` were plain `{ get; set; }` auto-properties before; the ComboBox/CheckBox two-way bindings now actually receive notifications. |
| `TargetPaths`, `HasTargets`, `ModifiedTargetPaths`, `UnmergedTargetPaths`, `HasModifiedTargets`, `HasUnmergedTargets`, `HasDirtyTargets` | `RefreshTargets()`, called by `UpdateAll`, `SelectedSubmodule` and once at the end of the constructor | `SelectedSubmodule` is assigned before the constructor sets `EnableRecursive`; `RefreshTargets()` re-runs at the end so the first paint is consistent. |
| `Submodules`, `HasPreSelectedSubmodule`, `IsEnableInitVisible` | never (unchanged from before) | `IsEnableInitVisible` is still constructor-only, so behaviour is identical to HEAD. |

Null/empty cases:
* `_updateAll == true` and `repo.Submodules.Count == 0` → `targets.Count == 0` → `TargetPaths` is `""`, `HasTargets == false`, the "no target" line is shown, both warning lists are empty, and `Sure()` keeps its existing early `return true` (no git call).
* `_updateAll == false` with `SelectedSubmodule == null` (possible if the ComboBox is cleared) → same empty path.
* `ModifiedTargetPaths` / `UnmergedTargetPaths` are `""` unless a target carries that status; their containers are gated by `HasModifiedTargets` / `HasUnmergedTargets`, and the whole warning block by `HasDirtyTargets`.
* Tree tooltip: `Module == null` (folder row) → detail block hidden, `Module?.X` bindings yield empty/`false` (`ToShortSHA` maps null to `""`, `src/Converters/StringConverters.cs:69-70`), so the tooltip shows just the path.
* Presentation only reads the already-loaded list; it is a snapshot taken when the popup is constructed. If the watcher refreshes submodules while the dialog is open, the summary can be stale until the dialog is reopened. Nested (second-level) dirty state is **not** detected — `git submodule status` and the porcelain re-check in `QuerySubmodules` cover this repository's own gitlinks only.

Localization: all new strings are `DynamicResource` in AXAML; nothing is passed through `App.Text` and no localized string is cached in the ViewModel, so a runtime locale switch repaints the new text.

## 5. Proposed keys for Lead (12 new, `Text.Classic.Submodule.*`)

No locale file was edited. None of these keys exists in any locale today (verified by grep over `src/Resources/Locales/`). Suggested insertion point: right after `Text.Classic.Navigation` (alphabetical block, en_US `:142` / ko_KR `:146`).

| Key | English | Korean |
|---|---|---|
| `Text.Classic.Submodule.Targets` | `Targets:` | `대상:` |
| `Text.Classic.Submodule.NoTarget` | `No target path. Nothing will be updated.` | `대상 경로가 없습니다. 업데이트되지 않습니다.` |
| `Text.Classic.Submodule.ModeRecorded` | `Default: each target is updated to the commit recorded in this repository.` | `기본: 각 대상은 이 저장소에 기록된 커밋으로 업데이트됩니다.` |
| `Text.Classic.Submodule.ModeRemote` | `Remote-tracking branch: each target is updated to its remote-tracking branch instead of the recorded commit.` | `원격 추적 브랜치: 각 대상이 기록된 커밋 대신 원격 추적 브랜치로 업데이트됩니다.` |
| `Text.Classic.Submodule.InitOn` | `Uninitialized targets are initialized first (--init).` | `초기화되지 않은 대상은 먼저 초기화됩니다 (--init).` |
| `Text.Classic.Submodule.InitOff` | `Uninitialized targets are left as they are.` | `초기화되지 않은 대상은 그대로 유지됩니다.` |
| `Text.Classic.Submodule.RecursiveOn` | `Nested submodules of the targets are included (--recursive).` | `대상의 중첩된 서브모듈까지 포함됩니다 (--recursive).` |
| `Text.Classic.Submodule.RecursiveOff` | `Only the target paths are updated; nested submodules are not included.` | `대상 경로만 업데이트되며 중첩된 서브모듈은 포함되지 않습니다.` |
| `Text.Classic.Submodule.WarnDirty` | `Some targets already have local changes. This dialog does not discard, reset or force them.` | `일부 대상에 이미 로컬 변경이 있습니다. 이 대화 상자는 이를 삭제하거나 초기화하거나 강제 적용하지 않습니다.` |
| `Text.Classic.Submodule.WarnModified` | `Modified:` | `수정됨:` |
| `Text.Classic.Submodule.WarnUnmerged` | `Unmerged:` | `병합되지 않음:` |
| `Text.Classic.Submodule.Status.Normal` | `clean` | `정상` |

Until Lead adds them, every new string renders as Avalonia's missing-resource fallback (`!!!KEY!!!`); the AXAML is still loadable. Existing keys reused (already present in all locales that matter, en/ko verified): `Text.Submodule.Branch`, `Text.Submodule.URL`, `Text.Submodule.Status.{NotInited,Modified,RevisionChanged,Unmerged}`, `Text.CommitDetail.Info.SHA`, `Text.UpdateSubmodules*`.

## 6. Fixture QA steps for Lead (read-only until the OK to mutate)

Fixtures: `artifacts/submodule-fixtures/parent` (initialized, clean `modules/demo`), `artifacts/submodule-fixtures/uninitialized` (same committed gitlink, not initialized), `artifacts/submodule-fixtures/child` (bare-ish local source, `main`). Add `parent` once; do not mutate `child`. Verify against `git -C <fixture> submodule status` after each GUI action.

1. **Clean / Normal** — open `parent`, sidebar → SUBMODULES, tree mode. Row shows the submodule icon with no dot (unchanged). Hover: card shows `modules/demo`, BRANCH, SHA `9de7802` (or current gitlink), URL, and the check icon + `clean`. List mode shows the same card. Expect `git submodule status` prefix `' '` only.
2. **Uninitialized / NotInited** — open `uninitialized`. Row has no dot; the tooltip card shows the `not initialized` badge; context menu keeps "Open Repository" and "De-initialize" disabled; double-click does nothing (unchanged). Right-click → Update: dialog shows target `modules/demo`, the checkbox in the combo, `Initialize as needed` checked and visible, the `InitOn` guidance line, and no Modified/Unmerged warning. Run OK, then confirm `git submodule status` prefix changed from `-` to `' '`/`+` and the dialog closed.
3. **Modified** — in `parent`, edit `modules/demo/readme.txt` in the editor (fixture is disposable), refresh, open Update Submodules from the toolbar: target `modules/demo`, warning block visible with `Modified:` and the path, `WarnDirty` sentence present, no checkbox changed by itself. Cancel. Then set an unstaged change inside the submodule that changes the recorded gitlink (`+` prefix) to see `revision changed` in the tooltip and `Modified`-vs-`RevisionChanged` distinction (warning shows only for `Modified`/`Unmerged` — a `+` prefix alone is not a local-change warning by design).
4. **Update all / remote / recursive reactivity** — open the dialog with `All submodules` checked → all loaded paths listed; untick it → the list collapses to the single selected path; change the ComboBox selection → the path list changes immediately. Toggle `--remote`: the guidance line switches from `ModeRecorded` to `ModeRemote`. Toggle `--recursive`: the line switches between `RecursiveOn`/`RecursiveOff`. Tick `All`, press OK, then compare with `git -C parent submodule status` / `git -C parent status` and the log window command line (`submodule update --init --recursive -- "modules/demo"` shape).
5. **Failure path** — run an update that fails (e.g. point `modules/demo`'s URL at a missing path with Change URL, then update). Expected now: the log stays open **and the dialog stays open** (`Sure()` returns the command result), consistent with the other submodule dialogs. Confirm the user can cancel without losing the log.
6. **Non-Classic mode** — with Classic UX off, the new popup summary must be invisible and the dialog must look exactly as before; the sidebar tooltip change applies in both modes (no Classic condition on the sidebar).
7. **Regression** — tree expand/collapse, row selection, double-click into a submodule repo, all context-menu items, `Ctrl+F` search inside the submodule list, and Korean/English switching with the dialog and tooltip open.

## 7. Known limits / risks (honest list)

* `BUILD NOT VERIFIED`, `GUI NOT VERIFIED` by this worker. The tree tooltip relies on the same "control inside `ToolTip.Tip` inherits the item DataContext" idiom already used by this file's list template and 13 other views, and on Avalonia's `?.` null-conditional binding path (already used in `ClassicToolbar.axaml:182`, `RepositoryToolbar.axaml:139`). Both need one GUI look.
* `Normal` in the tree/list **row itself** is still indicated only by the absence of a dot; the word `clean` appears in the tooltip card. That is Lead's "keep the sidebar compact" trade-off. A row-level compact indicator for `NotInited` remains open work.
* The warning is only as good as the loaded model: `Modified` is set only for modules whose `submodule status` prefix was `' '`, and only for this repository's own gitlinks — nested/dirty grandchildren are not detected, and the UI does not claim otherwise.
* Popup summary is a construction-time snapshot; long-open dialogs do not re-read the model.
* `SelectedSubmodule`/`EnableInit`/`EnableRecursive`/`EnableRemote` are now change-notifying; nothing else binds to them, so no other screen is affected.
