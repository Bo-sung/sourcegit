# Architecture audit — P0

Baseline: `c0f40a2e78d81a7474628847727644fc0c56f558`, 2026-11-08, Windows 11 build 26200. Initial working tree clean, official origin/master. Local branch `feat/classic-layout-poc`; official upstream remote added. Submodule AvaloniaEdit `ddf00a91dc0da29b85c79b85c9583860213a99ff` initialized.

## Current visible structure and binding

`LauncherPage.axaml` hosts a 36-unit toolbar and a ContentControl with one Repository view for Repository data. RepositoryToolbar is a separate DataTemplate. Repository view has a 49-unit icon rail, existing sidebar with BranchTree/Tags/Submodules/Worktrees, splitter, and right page. SelectedViewIndex 0 = dashboard/history, 1 = working copy, 2 = stashes. The fixture screenshot confirms graph/commit rows, current main marker, local branches and staged/unstaged views.

Repository.Open creates one Watcher, Histories, WorkingCopy and StashesPage. It sets initial page from ShowLocalChangesByDefault and calls RefreshAll. Close cancels refresh tasks, persists repository state/commit message and disposes watcher/auto-fetch timer. Classic must not call Open again or create replacement VMs.

Histories manages commit selection/search, history filters and details. Existing Histories view includes CommitGraph/commit list and CommitDetail. WorkingCopy owns staged/unstaged selection, DiffContext, staging and committing flags. WorkingCopy view's context menus and double-tap handlers already call StageChangesAsync/UnstageChangesAsync. Reuse those views and retain their Repository ancestor.

## Commands and refresh flow

RepositoryToolbar's existing handlers call FetchAsync/PullAsync/PushAsync/StashAllAsync; branch/tag buttons use CreateNewBranch/CreateNewTag. Repository methods check CanCreatePopup and show existing popup VMs. Merge supports existing branch/commit/tag sources and runs validation in the popup. A new toolbar must select an existing branch and open `ViewModels.Merge`, never construct Git CLI commands.

Commands directory owns Git execution/query operations, credentials and existing logs. No change authorized to this layer. Watcher combines filesystem/git metadata observation (separate watchers for linked worktrees), tracks dirty timestamps and uses a 100 ms timer plus lock count. Tick triggers targeted branch/worktree/working copy/commit/tag/stash refreshes; repository queries run asynchronously, cancellation suppresses stale work and UI updates are dispatched to the UI thread. Timer frequency alone is not evidence of a performance defect. No Watcher change planned.

## Settings, identity and reuse boundary

Preferences uses source-generated JSON and atomic preference_tmp.json -> preference.json replacement. LayoutInfo carries launcher bounds, sidebar width/collapse and pane widths. RepositoryUIStates persists per-repository selection/expansion/filter/layout state. Add opt-in mode to existing layout state, cache active mode at process startup and apply preference changes after restart. Keep one Repository visual tree and original mode.

Native.Windows.GetOrCreateDirectories tests `data` beside Environment.ProcessPath before `%APPDATA%/SourceGit`. IpcChannel hashes the cache directory for its Windows pipe name and lock file. Development data directory exists before first run. Installed SourceGit PID 25964 and development PID 48640 coexist. Never close installed original. App metadata/updater and notices are upstream at P0; future release identity separation remains P5. No branding assets copied.

## P0 observed results and P1 boundary

Restore PASS. Original Debug build PASS (0 warnings/errors, 21.20 s), original Release build PASS (0 warnings/errors, 22.09 s); this is build, not Native AOT publish. dotnet run PASS and fixture File Status -> readme.txt Diff visually verified. Captures in artifacts/p0-*.png. SourceTree 3.4.32 is running with private workspaces; private source content is not captured. Its visible toolbar/workspace navigation was inspected and official references are linked in UX_PARITY_MATRIX.

Lead owns Repository.axaml(.cs), Launcher/Page, Preferences/LayoutInfo, toolbar host, integration and docs. Pi/Qwen produced separate ClassicToolbar and ClassicNavigation files. Existing History, WorkingCopy, BranchTree and all tree handlers are unchanged. Implemented Classic minimum 960x600 DIPs, upstream mode unchanged; target captures 1440x900 and 1920x1080 physical pixels. Actual first-run captures are Dark 1920x1080 and Light 1462x956 at host 150% scaling; exact smaller target and 125% remain open. See QA_MATRIX for real GUI + CLI acceptance and deferred gates.
