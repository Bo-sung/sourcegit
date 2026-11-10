using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// A10 - the git-flow lifecycle through the product's own <see cref="Commands.GitFlow"/>.
    ///
    /// The lifecycle needs the real dependency, so the test first asks the same question the
    /// client asks (`git flow version`):
    ///
    /// * git-flow-next is reachable on PATH (the QA runs put the checksum-verified artifact from
    ///   artifacts/tools on the *process* PATH only) - the whole lifecycle is executed for real.
    /// * it is not reachable - what this client must do is block with an explanation and change
    ///   nothing, which is asserted instead of pretending a flow ran.
    ///
    /// Only the first of those needs the downloaded artifact, so it carries
    /// `[Trait("Requires", "git-flow-next")]` and is left out of the default `dotnet test` run; the
    /// dependency guard below always runs. Everything happens in a synthetic repository under
    /// %TEMP%; nothing is pushed anywhere.
    /// </summary>
    /// <summary>
    /// Hiding the dependency means changing this process's PATH, so the two classes that do it
    /// share the xunit collection "git-flow" and never overlap: a lifecycle run that happened while
    /// another test had the tool hidden would fail for a reason that has nothing to do with the code.
    /// </summary>
    [Collection("shared-git-environment")]
    public class GitFlowNextFlowTests : IDisposable
    {
        private readonly GitRepositoryFixture _fixture;
        private readonly string _prevEditor;
        private readonly string _prevMergeAutoEdit;

        public GitFlowNextFlowTests()
        {
            TestBootstrap.Prepare();

            _prevEditor = Environment.GetEnvironmentVariable("GIT_EDITOR");
            _prevMergeAutoEdit = Environment.GetEnvironmentVariable("GIT_MERGE_AUTOEDIT");

            // git-flow opens an editor for tag/merge messages. Only this test process is affected, and
            // Dispose() puts the previous values back.
            Environment.SetEnvironmentVariable("GIT_EDITOR", "true");
            Environment.SetEnvironmentVariable("GIT_MERGE_AUTOEDIT", "no");

            _fixture = GitRepositoryFixture.Create("gitflow-flow");
            _fixture.Commit("a.txt", "a\n", "first");
            _fixture.NewBranch("develop");
        }

        public void Dispose()
        {
            // The editor variables belong to the whole process; leaving them set would silently change
            // commit-message and rebase behaviour for whatever runs next in this process (F-6).
            Environment.SetEnvironmentVariable("GIT_EDITOR", _prevEditor);
            Environment.SetEnvironmentVariable("GIT_MERGE_AUTOEDIT", _prevMergeAutoEdit);
            _fixture.Dispose();
        }

        /// <summary>
        /// The real lifecycle, driven through the product's commands. It needs the git-flow-next
        /// dependency, which is a downloaded, gitignored artifact, so the default `dotnet test` run
        /// leaves this test out (see the `VSTestTestCaseFilter` note in the project file) and
        /// `AMissingDependencyIsBlockedWithAnExplanation` asserts what the client does without it.
        /// Run the dependency-backed test with
        /// `dotnet test tests/SourceGit.Tests -p:GitFlowNextIntegration=true`.
        /// </summary>
        [Fact]
        [Trait("Requires", "git-flow-next")]
        public async Task TheLifecycleRunsThroughTheProductCommands()
        {
            var repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);

            // The dependency gate is asked here rather than only at start-up: an opt-in run on a
            // machine without the artifact has to say so, and the product's background probe losing
            // a race must not be reported as "the dependency is missing". The reference run of
            // `git flow version` is the question the client itself asks.
            if (TestBootstrap.ProbeGitFlowVersion() == Models.GitFlowVersion.None)
            {
                Assert.Fail($"git-flow is not reachable through the test git ({TestBootstrap.GitReport()}); looked in '{TestBootstrap.GitFlowToolDir ?? "project-local asset not found"}'. The lifecycle cannot run; AMissingDependencyIsBlockedWithAnExplanation covers the case without the dependency.");
                return;
            }

            // `Commands.GitFlow.InitAsync` chooses the next-style command line from the product's own
            // probe, so that state has to be settled before the lifecycle starts: while it still says
            // "no git-flow" the client would initialise the legacy key layout instead, which is a
            // different feature and not what this test is about.
            Assert.Equal(Models.GitFlowVersion.Next, TestBootstrap.EnsureGitFlowProbe());

            var flow = new Commands.GitFlow(_fixture.Root);

            // --- init: the client writes the next-style key tree through the tool itself ---
            Assert.True(await flow.InitAsync("main", "develop", "feature/", "release/", "hotfix/", "v"), "git flow init failed");

            var config = LocalConfig();
            Assert.Equal("true", config["gitflow.initialized"]);
            Assert.Equal("base", config["gitflow.branch.main.type"]);
            Assert.Equal("main", config["gitflow.branch.develop.parent"]);
            Assert.Equal("feature/", config["gitflow.branch.feature.prefix"]);
            Assert.Equal("v", config["gitflow.branch.release.tagprefix"]);

            // The stored tree is what the settings editor and the branch tree read.
            var settings = new ViewModels.GitFlowSettings(repo);
            Assert.True(settings.ShowEditor);
            Assert.Equal("main", settings.Production);
            Assert.Equal("develop", settings.Develop);

            // --- feature start / finish ---
            _fixture.Checkout("develop");
            _fixture.Commit("dev.txt", "dev\n", "develop change");

            Assert.True(await flow.StartAsync(Models.GitFlowBranchType.Feature, "login", null), "feature start failed");
            Assert.Contains("refs/heads/feature/login", Refs(), StringComparison.Ordinal);

            _fixture.Commit("login.txt", "login\n", "feature work");
            Assert.True(await flow.FinishAsync(Models.GitFlowBranchType.Feature, "login", false, false, false), "feature finish failed");
            Assert.DoesNotContain("refs/heads/feature/login", Refs(), StringComparison.Ordinal);
            Assert.Equal("develop", _fixture.RunOutput("rev-parse", "--abbrev-ref", "HEAD").Trim());

            // --- release start / finish, with the version tag ---
            Assert.True(await flow.StartAsync(Models.GitFlowBranchType.Release, "1.0", null), "release start failed");
            _fixture.Commit("release.txt", "release\n", "release prep");

            var beforeTags = Tags();
            Assert.True(await flow.FinishAsync(Models.GitFlowBranchType.Release, "1.0", false, false, false), "release finish failed");
            var releaseTag = TagAdded(beforeTags);
            Assert.Equal("v1.0", releaseTag);
            Assert.Contains("refs/heads/main", Refs(), StringComparison.Ordinal);

            // --- hotfix start / finish ---
            Assert.True(await flow.StartAsync(Models.GitFlowBranchType.Hotfix, "urgent", null), "hotfix start failed");
            _fixture.Commit("hotfix.txt", "hotfix\n", "hotfix work");
            Assert.True(await flow.FinishAsync(Models.GitFlowBranchType.Hotfix, "urgent", false, false, false), "hotfix finish failed");
            Assert.NotEqual(beforeTags, Tags());

            // --- invalid input must preserve every ref ---
            var refsBefore = Refs();
            var tagsBefore = Tags();
            Assert.False(await flow.FinishAsync(Models.GitFlowBranchType.Release, "does-not-exist", false, false, false));
            Assert.Equal(refsBefore, Refs());
            Assert.Equal(tagsBefore, Tags());
        }

        /// <summary>
        /// The probe is fired by the GitExecutable setter and answers on a background thread, so two
        /// requests can be in flight at once. What reaches the client has to be the answer of the
        /// newest request: a probe that started while the dependency was hidden must not publish after
        /// one that started with it visible, or the client offers git-flow commands for a dependency
        /// the running git does not have (final review N-3). The generations are handed out where the
        /// request is made, which is what this pins down.
        /// </summary>
        /// <summary>
        /// Which of two probes the thread pool answers first is not something a test can ask of a real
        /// git, so the answer source is put under the test's control: both probes park until the test
        /// releases them, and the test plays the two publications out in the order it picks. That makes
        /// the ordering rule decidable instead of a matter of luck (final review N-3). No flow runs and
        /// no tool output is invented here; evidence that a flow really runs stays in the opt-in
        /// lifecycle test.
        /// </summary>
        [Fact]
        [Trait("Kind", "Scheduling")]
        public void AnAnswerFromASupersededRequestIsRefused()
        {
            TestBootstrap.Prepare();

            var parked = new CountdownEvent(2);
            var release = new ManualResetEventSlim(false);
            var real = Native.OS.GitFlowProbeRun;
            Native.OS.GitFlowProbeRun = git =>
            {
                parked.Signal();
                release.Wait(TimeSpan.FromSeconds(20));
                return Models.GitFlowVersion.Legacy;
            };

            try
            {
                var git = Native.OS.GitExecutable;
                var older = Native.OS.RequestGitFlowProbe(git);
                var newer = Native.OS.RequestGitFlowProbe(git);

                // Both requests must be waiting before the test publishes on their behalf, otherwise a
                // probe would answer on its own in the middle of the test.
                Assert.True(parked.Wait(TimeSpan.FromSeconds(10)), "the two probes did not both start");

                // The number travels with the request; it is not handed out when the task gets a thread.
                Assert.True(older < newer, "a later request must own the newer number, saw " + older + " and " + newer);

                // Asking again invalidates the answer that was standing: it described a question that is
                // open once more, so nothing about git-flow is known until the new answer lands.
                Assert.Equal(Models.GitFlowVersion.None, Native.OS.GitFlowVersion);

                // The newest answer lands, the older answer that arrives late is refused.
                Assert.True(Native.OS.PublishGitFlowProbe(newer, Models.GitFlowVersion.Next));
                Assert.False(Native.OS.PublishGitFlowProbe(older, Models.GitFlowVersion.Legacy),
                        "an answer for a superseded request must not be published");
                Assert.Equal(Models.GitFlowVersion.Next, Native.OS.GitFlowVersion);
            }
            finally
            {
                release.Set();
                Native.OS.GitFlowProbeRun = real;

                // The parked probes answer Legacy, so the real git is asked once more for the state the
                // rest of the run depends on.
                TestBootstrap.RestartGitFlowProbe();
            }
        }

        /// <summary>
        /// The same rule through the real probe: hide the dependency and fire a request that answers "no
        /// git-flow" without waiting for it, then show it and fire a newer request. Whichever answer
        /// arrives last, the newest request's answer has to be the one that stands.
        /// </summary>
        [Fact]
        [Trait("Requires", "git-flow-next")]
        public void ARealProbeSettlesOnTheNewestRequest()
        {
            TestBootstrap.Prepare();

            if (string.IsNullOrEmpty(TestBootstrap.GitFlowToolDir))
                return;

            var path = TestBootstrap.SetGitFlowToolVisible(false);
            try
            {
                var git = Native.OS.GitExecutable;
                for (int round = 0; round < 10; round++)
                {
                    // The older request goes out with the dependency hidden, so its answer is "no
                    // git-flow". It is deliberately not waited for: being late is the point.
                    Native.OS.RequestGitFlowProbe(git);

                    // The newer request goes out with the dependency shown, and its answer is the one
                    // that has to stand, whichever answer the thread pool delivers last.
                    TestBootstrap.SetGitFlowToolVisible(true);
                    Native.OS.GitExecutable = string.Empty;
                    Native.OS.GitExecutable = git;

                    // Waiting settles the newer request only; the harness never re-fires a probe to
                    // make a value look right, which would hide the defect this is about.
                    Assert.Equal(Models.GitFlowVersion.Next, TestBootstrap.WaitForGitFlowProbe(8000));

                    // Let the late answer arrive before hiding the dependency again, so the next round
                    // starts with both requests' answers actually delivered.
                    TestBootstrap.WaitForGitFlowProbe(8000);
                    TestBootstrap.SetGitFlowToolVisible(false);
                }
            }
            finally
            {
                TestBootstrap.RestorePath(path);
                TestBootstrap.RestartGitFlowProbe();
            }
        }

        /// <summary>
        /// The other half of the same contract, asserted in the same run rather than depending on how
        /// the machine happens to be configured: with the dependency hidden, every entry point stops
        /// before it locks the watcher, creates a branch or writes a key.
        /// </summary>
        [Fact]
        public void AMissingDependencyIsBlockedWithAnExplanation()
        {
            using var hidden = TestBootstrap.WithoutGitFlowTool();
            Assert.False(TestBootstrap.HasGitFlowTool);

            var repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);
            var refsAtStart = Refs();
            Assert.Equal(Models.GitFlowVersion.None, Native.OS.GitFlowVersion);
            Assert.False(ViewModels.GitFlowAvailability.IsAvailable);
            Assert.False(ViewModels.GitFlowAvailability.GuardOrNotify(repo));
            Assert.Equal(refsAtStart, Refs());
            Assert.False(HasGitFlowKeys());
        }

        private bool HasGitFlowKeys()
        {
            foreach (var kv in LocalConfig())
            {
                if (kv.Key.StartsWith("gitflow.", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private Dictionary<string, string> LocalConfig()
        {
            var rs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in _fixture.RunOutput("config", "--local", "-l").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                    rs[parts[0]] = parts[1];
            }

            return rs;
        }

        private string Refs() => RefsOf("show-ref");

        private string RefsOf(string subcommand)
        {
            var sb = new StringBuilder();
            foreach (var line in _fixture.RunOutput(subcommand).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                sb.AppendLine(line.Trim());

            return sb.ToString();
        }

        private string Tags()
        {
            var lines = new List<string>();
            foreach (var line in _fixture.RunOutput("tag", "-l").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                lines.Add(line.Trim());

            lines.Sort(StringComparer.Ordinal);
            return string.Join("|", lines);
        }

        /// <summary>The tag that <paramref name="before"/> did not have yet.</summary>
        private string TagAdded(string before)
        {
            foreach (var tag in Tags().Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!before.Contains(tag, StringComparison.Ordinal))
                    return tag;
            }

            return string.Empty;
        }
    }
}
