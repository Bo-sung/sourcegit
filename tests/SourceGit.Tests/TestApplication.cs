using System;
using System.Collections.Generic;
using System.IO;

using Avalonia;
using Avalonia.Headless;

using Xunit;

[assembly: AvaloniaTestApplication(typeof(SourceGit.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SourceGit.Tests
{
    /// <summary>
    /// Test host builder. It configures the real application class on the headless platform:
    /// the headless lifetime is not a classic desktop lifetime, so App skips window/watcher/timer
    /// startup while its resources (themes, locales) stay the genuine ones.
    /// </summary>
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>();
            builder.UseHeadless(new AvaloniaHeadlessPlatformOptions());
            builder.LogToTrace();
            Native.OS.SetupApp(builder);
            return builder;
        }
    }

    public static class TestBootstrap
    {
        private static readonly object LOCK = new object();
        private static bool _prepared = false;

        /// <summary>Ensures portable data dirs, the en_US strings and a usable git executable for the app's own commands.</summary>
        public static void Prepare()
        {
            lock (LOCK)
            {
                if (_prepared)
                    return;

                Native.OS.SetupBasicDirectories();

                // Nothing in this run may read or write the machine's own git configuration: git is
                // pointed at fixture files outside every repository before its first command, and the
                // system file is switched off too. A merged read of the real user config once leaked
                // real values into a log, so the isolation is enforced here instead of per test.
                GitEnv.EnforceProcess();

                // Guard: a test run must never write into the installed user configuration.
                var config = Native.OS.BasicDirectories.ConfigDir ?? string.Empty;
                var output = Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;
                if (!config.StartsWith(output, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Test data dir '{config}' is not inside the test output dir '{output}'. The portable 'data' folder is missing.");

                App.SetLocale("en_US");

                var git = Native.OS.FindGitExecutable();
                if (string.IsNullOrEmpty(git) || !File.Exists(git))
                    git = "git";

                // A project-local git-flow asset has to be on this process's PATH *before* the git
                // executable is assigned, because that assignment fires the product's own git-flow
                // probe. Without that every git-flow test silently takes the "dependency missing"
                // branch and a green run would not mean the lifecycle was ever executed (E-1).
                GitFlowToolDir = FindGitFlowToolDir();
                if (!string.IsNullOrEmpty(GitFlowToolDir))
                {
                    var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    if (!path.Contains(GitFlowToolDir, StringComparison.OrdinalIgnoreCase))
                        Environment.SetEnvironmentVariable("PATH", GitFlowToolDir + Path.PathSeparator + path);
                }

                Native.OS.GitExecutable = git;
                Native.OS.CredentialHelper = string.Empty;
                HasGitFlowTool = WaitForGitFlowProbe() != Models.GitFlowVersion.None;
                _prepared = true;
            }
        }

        /// <summary>Directory that holds the project-local git-flow executable, or null.</summary>
        public static string GitFlowToolDir { get; private set; }

        private static readonly List<ViewModels.Repository> OPENED = new();

        /// <summary>
        /// Creates the product's repository view model for a test and remembers it, so the watcher
        /// (100ms timer) and the auto-fetch timer it starts are disposed with the fixture instead of
        /// outliving the deleted working tree.
        /// </summary>
        public static ViewModels.Repository OpenRepository(string root, string gitDir)
        {
            Prepare();
            var repo = new ViewModels.Repository(false, root, gitDir);
            OPENED.Add(repo);
            return repo;
        }

        /// <summary>
        /// Closes one repository view model and forgets it. A test that copies a fixture to a temporary
        /// path has to close the copy before deleting the directory: the watcher (100ms) and the
        /// auto-fetch timer of an open view model keep polling the tree, and polling a tree that was
        /// deleted is work nobody asked for (and noise in a run that is not about them).
        /// </summary>
        public static void CloseRepository(ViewModels.Repository repo)
        {
            if (OPENED.Remove(repo))
            {
                try
                {
                    repo.Close();
                }
                catch
                {
                    // Teardown must never fail a test that already passed.
                }
            }
        }

        /// <summary>Closes every repository view model the tests opened, newest first.</summary>
        public static void CloseRepositories()
        {
            for (int i = OPENED.Count - 1; i >= 0; i--)
            {
                try
                {
                    OPENED[i].Close();
                }
                catch
                {
                    // Teardown must never fail a test that already passed.
                }
            }

            OPENED.Clear();
        }

        /// <summary>True when `git flow version` answers on this process's PATH.</summary>
        public static bool HasGitFlowTool { get; private set; }

        /// <summary>
        /// Hides the git-flow asset from PATH and re-runs the product's probe, so the "dependency
        /// missing" behaviour can be asserted in the same run as the lifecycle instead of depending on
        /// how the machine happened to be configured. Disposing restores PATH and the probe result.
        /// </summary>
        public static IDisposable WithoutGitFlowTool()
        {
            Prepare();

            if (string.IsNullOrEmpty(GitFlowToolDir))
            {
                HasGitFlowTool = false;
                return new NoopScope();
            }

            var original = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var kept = new List<string>();
            foreach (var dir in original.Split(Path.PathSeparator))
            {
                if (!dir.Equals(GitFlowToolDir, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(dir))
                    kept.Add(dir);
            }

            Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, kept));
            RestartGitFlowProbe();

            return new RestoreScope(original, GitFlowToolDir);
        }

        /// <summary>
        /// Puts the git-flow tool on or off this process's PATH and hands back the PATH line that
        /// reverses it. Unlike <see cref="WithoutGitFlowTool"/> this never fires a probe, so a test
        /// decides when a request goes out and in which order - which is what a test about the order
        /// of probes needs. Returns the value to give to <see cref="RestorePath"/>.
        /// </summary>
        public static string SetGitFlowToolVisible(bool visible)
        {
            Prepare();

            var original = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (string.IsNullOrEmpty(GitFlowToolDir))
                return original;

            var kept = new List<string>();
            foreach (var dir in original.Split(Path.PathSeparator))
            {
                if (string.IsNullOrEmpty(dir) || dir.Equals(GitFlowToolDir, StringComparison.OrdinalIgnoreCase))
                    continue;
                kept.Add(dir);
            }

            if (visible)
                kept.Insert(0, GitFlowToolDir);

            Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, kept));
            return original;
        }

        /// <summary>Puts back a PATH line captured by <see cref="SetGitFlowToolVisible"/>.</summary>
        public static void RestorePath(string path)
        {
            Environment.SetEnvironmentVariable("PATH", path ?? string.Empty);
        }

        /// <summary>
        /// Fires one git-flow probe request through the product's own request path and returns its
        /// number without waiting for the answer, the way the settings page does when the user picks a
        /// Git executable. A test about ordering must not wait here.
        /// </summary>
        public static int FireGitFlowProbe() => Native.OS.RequestGitFlowProbe(Native.OS.GitExecutable);

        /// <summary>The number of the newest git-flow probe request made so far.</summary>
        public static int GitFlowProbeGeneration => Native.OS.GitFlowProbeGeneration;

        /// <summary>Puts the asset back on PATH and lets the product probe it again.</summary>
        public static void RestartGitFlowProbe()
        {
            // The probe is fired by the GitExecutable setter, and the setter ignores an identical
            // value, so the assignment has to go through a different value to reach the probe again.
            var git = Native.OS.GitExecutable;
            Native.OS.GitExecutable = string.Empty;
            Native.OS.GitExecutable = git;

            WaitForGitFlowProbe();
            HasGitFlowTool = ProbeGitFlowVersion() != Models.GitFlowVersion.None;
        }

        /// <summary>
        /// Waits for the product's git-flow state to catch up with what `git flow version` answers, and
        /// returns it. Waiting only *settles* a request that is still running: it deliberately never
        /// fires the probe again, because a helper that re-fires until the value looks right would hide
        /// exactly the defect the probe guard exists for (an older request publishing after a newer
        /// one). If the state does not converge, the caller's assertion fails and names both values.
        /// </summary>
        public static Models.GitFlowVersion EnsureGitFlowProbe(int timeoutMs = 5000)
        {
            return WaitForGitFlowProbe(timeoutMs);
        }

        /// <summary>Walks up from the test binaries looking for the project-local tool directory.</summary>
        private static string FindGitFlowToolDir()
        {
            var dir = Path.GetDirectoryName(Environment.ProcessPath);
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                var candidate = Path.Combine(dir, "artifacts", "tools", "git-flow-next");
                if (File.Exists(Path.Combine(candidate, "git-flow.exe")) || File.Exists(Path.Combine(candidate, "git-flow")))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        private sealed class NoopScope : IDisposable
        {
            public void Dispose() { }
        }

        private sealed class RestoreScope : IDisposable
        {
            private readonly string _path;
            private readonly string _dir;

            public RestoreScope(string path, string dir)
            {
                _path = path;
                _dir = dir;
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable("PATH", _path);
                RestartGitFlowProbe();
            }
        }

        /// <summary>
        /// Waits until the product's background git-flow probe reached the state an independent
        /// `git flow version` run shows, and returns that state. The probe is fired by the
        /// <see cref="Native.OS.GitExecutable"/> setter, so tests must not assume it already ran.
        /// </summary>
        public static Models.GitFlowVersion WaitForGitFlowProbe(int timeoutMs = 5000)
        {
            var expected = ProbeGitFlowVersion();
            var deadline = Environment.TickCount64 + timeoutMs;
            while (Environment.TickCount64 < deadline && Native.OS.GitFlowVersion != expected)
                System.Threading.Thread.Sleep(50);

            return Native.OS.GitFlowVersion;
        }

        /// <summary>Runs `git flow version` the way Native.OS does, as an independent reference.</summary>
        public static Models.GitFlowVersion ProbeGitFlowVersion()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(Native.OS.GitExecutable, "flow version")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using var proc = System.Diagnostics.Process.Start(psi)!;
                var rs = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && rs.Contains("git-flow-next", StringComparison.Ordinal))
                    return Models.GitFlowVersion.Next;

                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(rs))
                    return Models.GitFlowVersion.Legacy;
            }
            catch
            {
                // Treated as "not installed".
            }

            return Models.GitFlowVersion.None;
        }

        /// <summary>Reports which git and git-flow the app detected, for the QA record.</summary>
        public static string GitReport()
        {
            return $"git={Native.OS.GitExecutable} ({Native.OS.GitVersionString}) gitflow={Native.OS.GitFlowVersion}";
        }
    }
}
