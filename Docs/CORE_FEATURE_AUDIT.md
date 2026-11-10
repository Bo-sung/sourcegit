# SourceTree core feature audit — read-only source investigation (2026-11-08)

Scope answer to: “Was SourceTree feature analysis really done? Are Git Flow settings, multiple remotes, and all core features implemented?”

Candid answer: the previous audit (UX_PARITY_MATRIX, FINAL_REPORT) covered Classic layout and a basic workflow vertical slice only. It was NOT an exhaustive SourceTree feature-parity audit. This document is that wider source-level audit. Source presence is NOT runtime PASS: nothing new was executed in a GUI during this task; QA status per row comes only from QA_MATRIX evidence.

Status vocabulary:
- `IMPL-SOURCE` — upstream SourceGit implementation exists in this fork (path+line below).
- `CLASSIC-REACHABLE` — reachable from the Classic layout (directly, or through toolbar `More…` flyout which hosts the original RepositoryToolbar; `src/Views/RepositoryToolbarHost.cs:32-56`).
- `TESTED` / `PARTIAL` / `NOT TESTED` — strictly from `Docs/QA_MATRIX.md`; source presence alone never becomes TESTED.
- `MISSING DEPENDENCY` — UI/VM exist but an external runtime dependency is absent on this machine.
- `MISSING UX` — configuration/data support exists but no UI path was found in source.
- `OUT OF SCOPE` — not part of this Git-only fork (Mercurial, cloud-account UI).

Official SourceTree references (primary sources only; no exact-parity claims):
- Branch management / Git Flow init–start–finish: https://support.atlassian.com/sourcetree/kb/branch-management/
- File status view: https://support.atlassian.com/sourcetree/kb/viewing-file-status-of-a-repository/
- Product page (feature overview): https://www.atlassian.com/software/sourcetree
Whether SourceTree itself exposes a given sub-dialog (e.g. Git Flow re-edit) must be confirmed by Lead in the actual GUI; this audit only states what SourceGit source does or does not offer.

## 1. Git Flow

### Implementation exists (IMPL-SOURCE)
- `src/Commands/GitFlow.cs:6` — `GitFlow : Command`. `InitAsync:14` writes `gitflow.*` config (`:37-44`) then runs external `git flow init -d` (`:46-47`); git-flow-next preset variant `:18-33`. `StartAsync:50` builds `git flow <feature|release|hotfix> start <name> [based]` (`:53-76`). `FinishAsync:79` likewise shells out to `git flow ... finish`. **Every Git Flow operation executes the external `git flow` program.**
- `src/Models/GitFlow.cs:6-11` — `GitFlowVersion { None, Legacy, Next }`. `:13-19` — `GitFlowBranchType` has **only None/Feature/Release/Hotfix** (no Bugfix, no Support). `Parse:29` reads only feature/release/hotfix prefixes (`:59-64`, `:76-80`).
- Version detection: `src/Native/OS.cs:74-78` (default `None`), background probe `UpdateGitFlowVersion:380-410` runs `"<app git executable>" flow version` (`:383-384`) and classifies `git-flow-next` vs Legacy only on exit code 0 (`:396-404`). Triggered after successful `--version` (`:370-371`).
  - Execution-path distinction: `_gitExecutable` is the **app-selected Git** (`OS.cs:49-57`; backend discovery `FindGitExecutable:195-197`; preference fallback `src/ViewModels/Preferences.cs:669-671`), **not the shell PATH**, so an early PATH-only shell probe was not authoritative. That gap is now closed by a stronger check against the exact configured executable: Lead read the portable preference `GitInstallPath = C:/Program Files/Git/bin/git.exe`, then ran that exact binary — `--version` → `git version 2.56.0.windows.4`, `flow version` → exit 1 `git: 'flow' is not a git command. See 'git --help'.` (log `../artifacts/qa-app-gitflow-dependency.log`, PowerShell NativeCommandError on `& $gitForApp flow version`, "most similar commands: reflog / show"). Per `UpdateGitFlowVersion` the probe requires exit code 0 plus non-empty stdout (`src/Native/OS.cs:393-404`), so on this machine the app’s `GitFlowVersion` resolves to **None**.
- Popups: `src/ViewModels/InitGitFlow.cs` (production/develop branch pickers `:14-28`, feature/release/hotfix/tag prefix fields `:30-59`, base-branch auto-detect `:65-79`, creates missing base branches `:115-120`), `GitFlowStart.cs`, `GitFlowFinish.cs`; views `src/Views/InitGitFlow.axaml:24-79`, `GitFlowStart.axaml`, `GitFlowFinish.axaml`.

### Reachability in Classic (CLASSIC-REACHABLE, via More only)
- No Git Flow entry exists in `src/Views/ClassicToolbar.axaml` (grep count 0).
- Path: Classic toolbar `More…` → original RepositoryToolbar Git Flow button (`src/Views/RepositoryToolbar.axaml:103` → `OpenGitFlowMenu`, `src/Views/RepositoryToolbar.axaml.cs:237-297`): if `repo.IsGitFlowEnabled()` (`src/ViewModels/Repository.cs:516-521`) → Start Feature / Start Release / Start Hotfix (`:246-275`) + Finish item when the current branch matches a prefix (`:277-291`); otherwise → Init item only (`:293-297`).
- Branch context menu shows Git Flow “finish” items for prefixed branches (`src/Views/BranchTree.axaml.cs:756-761`, `:894-899`).

### Gaps / honest limitations
- **MISSING DEPENDENCY (local machine, confirmed against the app’s own Git):** external `git flow` (or git-flow-next) is absent for both the shell PATH Git and the configured `GitInstallPath` executable `C:/Program Files/Git/bin/git.exe` (`--version` OK 2.56.0.windows.4, `flow version` exit 1; `../artifacts/qa-app-gitflow-dependency.log`). With `GitFlowVersion=None`, `Commands/GitFlow.cs` takes no preset branch and Init/Start/Finish shell out to a `git flow` subcommand that does not exist here, so they must be expected to fail at runtime. Do **not** describe Git Flow as usable or fully implemented. This is a dependency/version-detection result only: **Git Flow invocation through the app GUI remains NOT TESTED**, and “configured app Git lacks git-flow” is not the same claim as “Init was executed and failed”.
- **MISSING UX — re-edit/re-init:** once `IsGitFlowEnabled()` is true the menu shows only Start/Finish (`RepositoryToolbar.axaml.cs:244-292`); there is **no settings/re-init entry anywhere in source** (grep for InitGitFlow instantiation shows only that else-branch). An already-configured repository therefore offers no in-app way to change branches/prefixes. SourceTree comparability to be confirmed by Lead GUI check.
- **MISSING UX — bugfix/support:** config prefixes `bugfix/` and `support/` are written on init (`Commands/GitFlow.cs:24,27,40,43`) but are absent from `GitFlowBranchType` (`Models/GitFlow.cs:13-19`), from `GetGitFlowType` (`ViewModels/Repository.cs:523-536`), and from every menu. Branches under bugfix/support prefixes get no Git Flow start/finish UI.
- If detection yields `None`, no menu item is disabled in source; Init remains clickable and fails in the log popup (no pre-flight dependency check found). TESTED: **NOT TESTED** (no Git Flow GUI execution in QA).

## 2. Multiple remotes

### Multiple remote ENTRIES — IMPL-SOURCE + CLASSIC-REACHABLE
- Model: `src/Models/Remote.cs:32-36` — per remote: `Name`, `URL`, `PushURL`, `PrivateSSHKey`, `DisableAutoFetch`; SSH detection `IsSSH:38-47`.
- Enumeration: `src/Commands/QueryRemotes.cs:15-72` lists **all** `remote.<name>.*` config entries; sorted list `:69`.
- Loading into repository: `src/ViewModels/Repository.cs:1147-1159` (refresh sets `Remotes` + `RemoteBranchTrees`).
- CRUD UI: `AddRemote` (`ViewModels/AddRemote.cs:9-51`, duplicate-name guard `:58-68`; entry points: Repository remotes header button `src/Views/Repository.axaml:350` — the Repository view is shared by Classic), BranchTree remote node context menu (`src/Views/BranchTree.axaml.cs:1099-1204`): Open in browser `:1106`, Fetch `:1119`, Prune `:1129` (`ViewModels/PruneRemote.cs`), per-remote auto-fetch toggle when global auto-fetch is on `:1142-1156` (`ViewModels/Preferences.cs:254`), Edit `:1159`, Set PushURL `:1169` (`ViewModels/SetPushURL.cs:7-26`), Delete `:1179`, Copy URL `:1189`.
- Default remote: `src/Models/RepositorySettings.cs:15`; UI `ViewModels/RepositoryConfigure.cs:29-36`, `Views/RepositoryConfigure.axaml:71-76`.
- Fetch with remote picker + **“all remotes”**: `ViewModels/Fetch.cs:14-27,50-51` (checkbox visible only when `Remotes.Count > 1`), sequential per-remote loop `:85-92`; UI `Views/Fetch.axaml:26-55`. Remote-specific Fetch from context menu passes `preferredRemote` (`Fetch.cs:47-57`).
- Pull: remote combo + branch picker, DefaultRemote fallback (`ViewModels/Pull.cs:11-102`). Push: remote + remote-branch combos and “set track” option (`ViewModels/Push.cs:33-71`, track visibility `:64`).
- Upstream tracking: branch menu Tracking → `SetUpstream` (`Views/BranchTree.axaml.cs:1061-1070`, `ViewModels/SetUpstream.cs:7`); `Commands/Branch.cs:35-40` `-u <remote>/<branch>` and `--unset-upstream`; ahead/behind badges (`Models.Branch.TrackStatus*`, rendered `Views/BranchTree.axaml.cs:112-215`).
- Per-remote SSH key stored as `remote.<name>.sshkey` (`Commands/QueryRemotes.cs:37-41`), set via Add/Edit remote SSH fields (`ViewModels/AddRemote.cs:30-45`).

### Multiple URLS on ONE remote — MISSING UX
- `Commands/QueryRemotes.cs:29-32` keeps a single string per name (`urls[name] = v`): if a remote has several fetch URLs, only the last parsed one is shown/used. `Commands/Remote.cs:39-50` `set-url` supports only plain replace / `--delete` / `--push`; **no `--add`** anywhere → the app cannot create or manage multi-URL remotes; per remote at most one fetch URL + one push URL (`:34` pushurl handling `QueryRemotes.cs:34-37`).

### TESTED status (multi-remote)
- QA exercised a **single** local bare fixture remote: Fetch executed, Pull/Push popups opened and cancelled, **no real push** (`Docs/QA_MATRIX.md` P1 command smoke). Multiple remote entries, Fetch-all, per-remote SSH keys, auto-fetch toggle, default-remote setting, and upstream tracking are all **NOT TESTED**.

## 3. Scoped core feature inventory (Git-only fork)

Legend: Source | Classic reach | QA status. Not an exhaustive SourceTree equivalence list.

| Area | Source evidence | Classic | QA |
|---|---|---|---|
| Clone / open / history-of-repos | `Commands/Clone.cs`, `Views/Clone.axaml`, `Views/LauncherPage.axaml`, `Views/Welcome.axaml` | Launcher (unchanged by fork) | open PASS (fixture/P0) |
| File status, stage/unstage file-level | `ViewModels/WorkingCopy.cs:9ff`, `Commands/UpdateIndexInfo.cs` | File Status page PASS | T03 PASS |
| Hunk/chunk stage/unstage | `Views/TextDiffView.axaml.cs:1564-1643` (`BtnStageChunk/BtnUnstageChunk`, Ctrl+S/U) | via Diff view | U09 NOT TESTED |
| Commit / amend | `Commands/Commit.cs`; amend `ViewModels/WorkingCopy.cs:87-100,257`, auto-cancel on head move `Repository.cs:162-163` | Commit toolbar PASS | commit PASS / amend NOT TESTED |
| Diff (file/commit/inline) | `Commands/Diff.cs`, `Views/TextDiffView`, `Views/Diff.axaml` | PASS | P0/P1 PASS |
| History graph, commit search | `Views/Histories.axaml`, `Views/CommitSearchView.axaml`, `Commands/QueryCommits.cs` | History page | PASS smoke; search NOT TESTED |
| Blame | `Commands/Blame.cs`, `Views/Blame.axaml`, `Views/BlameCommandPalette.axaml` | via file menus | NOT TESTED |
| File history / revision file view | `Commands/QueryFileHistory.cs`, `Views/FileHistories.axaml`, `Commands/SaveRevisionFile.cs` | via history/file menus | NOT TESTED |
| Branch / checkout / create / delete | `Commands/Branch.cs`, `ViewModels/CreateBranch.cs`, checkout `Commands/Checkout.cs` | Branch button + Ctrl+B | clean checkout PASS, dirty-protection NOT TESTED |
| Merge / conflict resolution | `Commands/Merge.cs`, `MergeTree.cs`, `Views/MergeConflictEditor.axaml.cs` | Merge button (popup PASS, cancelled) | merge execution NOT TESTED |
| Rebase / interactive rebase | `Commands/Rebase.cs`, `InteractiveRebase.cs`, `Views/Rebase.axaml`, `Views/InteractiveRebase.axaml` | More → history menus | NOT TESTED |
| Cherry-pick / revert / reset | `Commands/CherryPick.cs`, `Revert.cs`, `Reset.cs`; `Views/CherryPick.axaml`, `Revert.axaml`, `Reset.axaml`, `ResetWithoutCheckout.axaml` | More / commit menus | NOT TESTED |
| Bisect | `Commands/Bisect.cs`, `Views/BisectStateIndicator.cs`, `Views/RepositoryToolbar.axaml` | More flyout | NOT TESTED |
| Stash / apply / drop / clear / checkout-as-branch | `Commands/Stash.cs`; `Views/StashesPage.axaml`, `StashChanges.axaml`, `ApplyStash.axaml`, `DropStash.axaml`, `ClearStashes.axaml`, `CheckoutBranchFromStash.axaml` | Stashes page PASS (popup open/cancel) | mutation NOT TESTED |
| Tags (create/delete/push/batch delete) | `Commands/Tag.cs`; `Views/CreateTag.axaml`, `DeleteTag.axaml`, `DeleteMultipleTags.axaml`, `PushTag.axaml`, `TagsView.axaml` | Tags group in tree | popup open/cancel only |
| Worktrees | `Commands/Worktree.cs`; `Views/AddWorktree.axaml`, `RemoveWorktree.axaml`, `PruneWorktrees.axaml`, `CheckoutAsWorktree.axaml`, `CheckoutRemoteBranchAsWorktree.axaml` | tree group + tabs | T12/T16 PASS initial smoke |
| Submodules | `Commands/Submodule.cs`; `Views/SubmodulesView.axaml`, `AddSubmodule.axaml`, `UpdateSubmodules.axaml`, `DeinitSubmodule.axaml`, `DeleteSubmodule.axaml`, `ChangeSubmoduleUrl.axaml`, `SetSubmoduleBranch.axaml`, `MoveSubmodule.axaml` | tree group | NOT TESTED |
| Remotes (multi) / auth | Section 2 above | tree + Repository view | single-remote Fetch only |
| Git LFS | `Commands/LFS.cs`, `IsLFSFiltered.cs`; `Views/LFSFetch.axaml`, `LFSPush.axaml`, `LFSPull.axaml`, `LFSLocks.axaml`, `LFSPrune.axaml`, `LFSTrackCustomPattern.axaml`; detect via pre-push hook `ViewModels/Repository.cs:538-550` | More / toolbar | NOT TESTED |
| Git Flow | Section 1 above | More → toolbar Git Flow button (`src/Views/RepositoryToolbar.axaml:103`; absent from `ClassicToolbar.axaml`) | GUI invocation NOT TESTED; external `git flow` missing for PATH Git **and** for the configured `GitInstallPath` exe (`qa-app-gitflow-dependency.log`) |
| SSH keys / credentials | per-remote `sshkey` (§2), global askpass `Views/Askpass.axaml` (credential prompts), HTTPS host validation `Models/HTTPSValidator` | Add/Edit remote, popups | NOT TESTED |
| GPG / commit signing | repo-level `ViewModels/RepositoryConfigure.cs:175-181,279-281`; global `Views/Preferences.axaml.cs:146-201`; signature display `Commands/QueryCommitSignInfo.cs` | Repository/Preferences config | NOT TESTED |
| Discard / clean / ignore | `Commands/Discard.cs`, `Clean.cs`; `Views/Discard.axaml`, `Cleanup.axaml`, `AddToIgnore.axaml` | File Status menus | NOT TESTED |
| Shortcuts | Classic: Ctrl+B branch, Ctrl+↓ Fetch, Ctrl+⇧↓ Pull, Ctrl+⇧↑ Push, Ctrl+1/2/3 pages (`Views/RepositoryToolbarHost.cs:36-44`, `Views/ClassicToolbar.axaml.cs`, `Views/StealHotKey.cs`); hunk Ctrl+S/U | PASS Ctrl+B / others NOT TESTED (QA) |
| Localization | 17 catalogs `src/Resources/Locales/` incl. `ko_KR.axaml`; Classic mostly uses `DynamicResource`; **hardcoded** `Content = "More…"` `Views/RepositoryToolbarHost.cs:48` | — | owned by localization worker |
| Mercurial / cloud-account UI | none found in this Git-only fork (`grep` for hg/Mercurial = 0 hits; no OAuth account UI; remote “open in browser” URL mapping only, `Models/Remote.cs:66-90`) | — | OUT OF SCOPE |

## 4. Risk-prioritized next checks (for Lead GUI pass)
1. **Git Flow invocation (high, dependency already settled):** the app Git executable was verified as `C:/Program Files/Git/bin/git.exe` and lacks `git-flow`, so re-checking the Git path is no longer needed. Remaining check is the GUI behaviour of More → Git Flow → Init with `GitFlowVersion=None`: confirm the popup still appears (no dependency pre-flight gate found in source) and that the log records a `git flow init` failure rather than a silent success. Installing `git-flow` is out of scope.
2. **Git Flow re-edit gap (high):** after a successful init, verify no settings/re-init item appears (source predicts Start/Finish only) and record SourceTree’s actual capability for comparison.
3. **Multi-remote entry (high):** add a 2nd fixture-only remote; verify BranchTree entries, Fetch popup “all remotes”, DefaultRemote selection, per-remote Fetch. No push to real remotes.
4. **Multi-URL single remote (medium):** set a second fetch URL via CLI, confirm SourceGit shows exactly one URL and `set-url` overwrite behavior (§2).
5. **Amend + dirty checkout + hunk stage (medium):** flagged NOT TESTED in QA_MATRIX rows T05/T06/U09.
6. **Upstream tracking & set-track-on-push (medium):** `SetUpstream` popup + Push track checkbox.
7. **LFS / submodules / signing / blame / search (low, breadth):** each IMPL-SOURCE only.

Out-of-scope notes: Mercurial support and cloud-account (GitHub/Bitbucket login) UI are not part of this Git-only fork unless later source proves otherwise. This inventory is scoped, not an exhaustive SourceTree equivalence audit; the web/doc-based SourceTree feature inventory is owned separately in [SOURCETREE_FEATURE_INVENTORY.md](SOURCETREE_FEATURE_INVENTORY.md) (another worker, source register SOURCETREE_SOURCE_REGISTER.md). Version note for comparisons: the locally inspected install is SourceTree **3.4.32**, while the latest published Windows build in that inventory is **3.4.33 (31 Oct 2026)**; do not treat local GUI observations as current-release behaviour without re-checking the newer release notes.
