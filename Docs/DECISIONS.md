# Decisions

## D08 — User-approved feature scope expansion (2026-11-08)

All recommended first/second-priority functionality is required. Commit templates, issue links, signing settings, SSH key/authentication diagnostics and submodule navigation/update/status are also mandatory. The canonical acceptance list is ACCEPTED_FEATURE_SCOPE.md (A01–A22). Reuse existing SourceGit implementations; add missing UX only after checking actual source boundaries. Product priority groups do not rename the original P0–P4 stage gates. This decision expands scope, not the claimed completed/verified feature set.

## D01 — Keep the existing repository lifecycle

Use one `ViewModels.Repository` and its existing Histories, WorkingCopy, StashesPage and Watcher. Change layout inside the existing repository view rather than instantiating a second repository shell or rebuilding Git operations. Preserve `SelectedViewIndex`: 0 History/dashboard, 1 WorkingCopy, 2 Stashes.

## D02 — Isolated development data

Create `data` next to each Debug/Release executable before launch. Windows.GetOrCreateDirectories selects it, and IpcChannel hashes the data path into its pipe identity. Do not write to `%APPDATA%/SourceGit`. Existing installed SourceGit must remain running and untouched.

## D03 — SDK selection

Use the already installed private .NET 10.0.403 SDK explicitly; do not change machine PATH or downgrade the target. The repository targets net10.0, Avalonia 11.3.22, CommunityToolkit.Mvvm 8.4.2. Release build is separate from Native AOT publish validation.

## D04 — Classic layout opt-in

Plan: save mode in existing preference layout state and apply after restart. Keep original layout as default/recovery. Reuse branch, tag, submodule and worktree UI and menus. Keep sidebar available on File Status in Classic mode. Add a labelled navigation component and command toolbar without changing Commands or Watcher.

Implemented: LayoutInfo.UseClassicLayout is persisted; Preferences.IsClassicLayoutActive is a JsonIgnored startup snapshot. RepositoryToolbarHost constructs either upstream toolbar or ClassicToolbar with a More flyout containing the upstream advanced toolbar. Only one repository screen/VM lifecycle exists. Classic navigation is inserted above the existing branch groups; the rail width collapses only in Classic. Original layout remains the default for unset preferences.

## D06 — Preserve shortcut registration outside the overflow flyout

GUI found Ctrl+B stopped working when upstream toolbar moved into More. The persistent Classic host now owns the four original toolbar shortcuts, forwarding to existing repository methods. It clears those hotkeys on the overflow toolbar to avoid duplicate registration. Command palette and CanCreatePopup guards remain. Ctrl+B was retested successfully before opening More. No new Git execution code.

## D07 — Evidence dimensions and scope

Classic minimum is 960x600 DIPs; original minimum remains unchanged. Dark 1920x1080 screenshots are actual capture dimensions. The small Light window captured 1462x956 rather than 1440x900; keep the actual file name and defer that exact geometry gate. No manufactured baseline/performance values. Current work completes the first working vertical slice, not the P2-inclusive full POC Definition of Done.

## D05 — Limits and external writes

No remote push, PR or deployment. Local baseline has official origin plus upstream; authenticated GitHub fork preparation is separate from implementation. No proprietary assets copied. MIT and third-party notices remain intact. Codex quota UNKNOWN is not a below-threshold reading.
