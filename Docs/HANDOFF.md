# Handover — Classic layout fork of SourceGit

Status document for this fork. Read [SOURCEGIT_CLASSIC_UX_IMPLEMENTATION_PLAN.md](SOURCEGIT_CLASSIC_UX_IMPLEMENTATION_PLAN.md)
for the specification, [ACCEPTED_FEATURE_SCOPE.md](ACCEPTED_FEATURE_SCOPE.md) for the A01–A22 acceptance
list, and [DECISIONS.md](DECISIONS.md) for the architectural decisions. Current automated test status is
in [ALL_TESTS_STATUS_20261110.md](ALL_TESTS_STATUS_20261110.md); it is the authoritative "what passed"
record and it is newer than anything in this file.

## What this fork is

An opt-in "Classic" layout for SourceGit — a SourceTree-style command toolbar, navigation rail and
page arrangement — layered on the unmodified upstream repository, on top of upstream
`c0f40a2e78d81a7474628847727644fc0c56f558` (`release/v2026.23`), branch `feat/classic-layout-poc`.

* Upstream remains the source of truth. `upstream` is the official repository; `origin` is the fork.
* License is unchanged MIT, with `LICENSE` and `THIRD-PARTY-LICENSES.md` intact.
* The Classic layout is a preference (`LayoutInfo.UseClassicLayout`), applied after restart. The
  upstream layout stays the default and the recovery route.

## Architecture in five lines

1. One `ViewModels.Repository` and its existing `Histories` / `WorkingCopy` / `StashesPage` / `Watcher`
   are reused; no second repository shell and no second watcher (D01).
2. `Views/RepositoryToolbarHost.cs` constructs either the upstream toolbar or `Views/ClassicToolbar`,
   with a More flyout that hosts the upstream advanced toolbar; only one repository screen exists (D04).
3. `Views/ClassicNavigation` inserts a navigation rail above the existing branch groups; the rail
   width collapses only in Classic mode.
4. The host owns the four original toolbar shortcuts (for example `Ctrl+B`) and forwards to existing
   repository methods, clearing them on the overflow toolbar to avoid double registration (D06).
5. No new Git execution path was added: commands call existing popup VMs and existing `Commands/*`.

## Build

Requires the .NET 10 SDK (10.0.403 observed); the projects target `net10.0` and `Avalonia 11.3.22`.
A machine-wide .NET 9 on `PATH` will fail with `NETSDK1045`.

```sh
git submodule update --init --recursive          # depends/AvaloniaEdit
dotnet build src/SourceGit.csproj -c Debug
dotnet build src/SourceGit.csproj -c Release
```

Run a development instance without touching any user configuration:

```powershell
tools/Start-ClassicDev.ps1 -Configuration Debug            # build + launch
tools/Start-ClassicDev.ps1 -Configuration Release -Repository <path-to-a-fixture-repo>
```

The script points `GIT_CONFIG_GLOBAL`, `GIT_CONFIG_SYSTEM` and `GIT_CONFIG_NOSYSTEM` at empty fixture
files under `%TEMP%`, selects the SDK explicitly, creates a `data` directory next to the apphost so the
client uses portable settings, and launches `SourceGit.exe` (the apphost, so the portable data
directory resolves). `tools/Invoke-IsolatedGitEnv.ps1 '<command>'` applies the same git isolation to an
arbitrary build/test command — use it for anything that shells out to git. Rationale in D02: a merged
`git config -l` once leaked machine values into a log, so automated work never reads user or machine
git configuration.

## Test

```powershell
tools/Invoke-IsolatedGitEnv.ps1 'dotnet test tests/SourceGit.Tests/SourceGit.Tests.csproj -c Debug'
tools/Invoke-IsolatedGitEnv.ps1 'dotnet test tests/SourceGit.Tests/SourceGit.Tests.csproj -c Release'
```

`tests/SourceGit.Tests` uses xUnit + Avalonia Headless and drives real product VMs and real `git`
against disposable repositories under `%TEMP%`; the test host refuses to run if its config directory
would land outside its own output folder. Git Flow lifecycle tests are opt-in because the external
`git-flow` binary is not distributed here:

```powershell
dotnet test tests/SourceGit.Tests/SourceGit.Tests.csproj -p:GitFlowNextIntegration=true
```

Latest recorded result and its exact scope: default suite 98/98 and Git Flow opt-in 100/100, Debug and
Release, at the time of the repository-failure fix. The later narrowing of the HTTP 404 condition was
re-verified only by its targeted tests (10/10) — a full-suite re-run after that change is still owed.
See `ALL_TESTS_STATUS_20261110.md` before quoting any number.

## Known gaps (real, not closed)

GUI verification still pending, in each case because the automation driver cannot set keyboard focus
or cannot capture the required pixels — the underlying behaviour is implemented:

* Template picker apply and variable expansion driven by real keystrokes; per-action interactive rebase
  results (the action menu does not fire under UIA invoke); stash patch written to a real file.
* Staging a single selected line and reading the resulting CLI state; search-scope selection; issue-link
  sample addition; Submodule row tooltips and Tab order; signature display in commit details; the save
  action inside the external editor.
* Exact 1440×900 geometry, Light/Dark with long labels, and 125% DPI. Captures exist at 1920×1080 and at
  an actual 1462×956; the geometry gate was deliberately not claimed as passed.
* Authentication failures shown in the GUI and through Askpass, and OpenPGP verification in the GUI
  (OpenPGP signing itself is proven at CLI level with `git verify-commit`).
* Status-refresh latency and a like-for-like SourceTree comparison: NOT MEASURED. Startup/idle numbers
  in `ALL_TESTS_STATUS_20261110.md` come from a paired original/fork run, are single-machine with low
  repetition, and support no improvement or non-regression claim.

The TLS `SEC_E_UNTRUSTED_ROOT` / `SEC_E_CERT_EXPIRED` and repository-not-found error classes are
classified by `src/Models/GitFailureHints.cs`, which never copies the URL, account or token out of git
output into the message shown to the user.

## Preview release boundary

This is a technical preview of the Classic layout, not a release:

* Included and functional in normal use: the Classic shell, toolbar and navigation, default-remote
  handling, Git Flow UI guards, submodule view/update flows, commit templates, issue links, signing
  settings, and the Korean/English locale work.
* Not a claim of completion: full A01–A22 GUI/visual acceptance, DPI and pixel-geometry gates, GUI
  authentication diagnostics, and performance parity are open (see "Known gaps"). Do not describe this
  fork as feature-complete or as passing the whole acceptance ledger.
* Git Flow requires an external `git-flow` executable supplied by the user; nothing is bundled, and no
  official Windows binary was found in the upstream project.
* Upstream, third-party and MIT notices are unmodified; no proprietary assets from any other client are
  included, and the comparison studies reference public documentation only.

## Evidence convention

`artifacts/` (ignored, at the repository root) holds disposable fixture repositories, GUI screenshots,
UI-tree dumps and build logs produced during QA. Documents may reference a path there — such a path is
**unversioned local evidence** from a QA run on one machine; it is not shipped, not downloadable, and
its absence does not mean the finding was invented. The QA reports under `Docs/` that list per-run
terminal output are likewise local working files and are intentionally not versioned; the durable
records are the documents linked at the top of this file.

## Working rules that still apply

* Keep the original layout reachable; every Classic addition must be reversible by one preference.
* Never read or write user or machine git configuration from automated work; use the isolation wrappers.
* Stage explicit paths only — never `git add -A` — because `artifacts/` and per-run reports sit next to
  the source tree.
* Do not copy proprietary assets or interface text from other Git clients.
