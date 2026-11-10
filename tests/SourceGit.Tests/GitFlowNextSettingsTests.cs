using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Headless.XUnit;

using Xunit;

using GitFlowSettingsVM = SourceGit.ViewModels.GitFlowSettings;

namespace SourceGit.Tests
{
    /// <summary>
    /// A11 for the git-flow-next configuration shape. The key layout below was captured from the
    /// official git-flow-next v2.1.0 Windows artifact after `git flow init --preset=classic`
    /// (SHA-256 verified against the release checksums, kept project-local under
    /// artifacts/tools). The tests never call git-flow: they assert what this client reads and
    /// writes for that shape, and that a save is a pure configuration change.
    /// </summary>
    /// <summary>
    /// Shares the "git-flow" collection with GitFlowNextFlowTests: both hide the dependency from the
    /// process for one test, which must not overlap the lifecycle run over there (see the note there).
    /// </summary>
    [Collection("shared-git-environment")]
    public class GitFlowNextSettingsTests : IDisposable
    {
        /// <summary>`gitflow.branch.&lt;name&gt;.&lt;prop&gt;` as git-flow-next stores it.</summary>
        private static readonly (string Key, string Value)[] NEXT_KEYS = [
            ("gitflow.version", "1.0"),
            ("gitflow.initialized", "true"),

            // Base branches: production has no parent, development points at it.
            ("gitflow.branch.main.type", "base"),
            ("gitflow.branch.main.upstreamstrategy", "none"),
            ("gitflow.branch.main.downstreamstrategy", "none"),
            ("gitflow.branch.main.autoupdate", "false"),
            ("gitflow.branch.main.tag", "false"),
            ("gitflow.branch.develop.type", "base"),
            ("gitflow.branch.develop.parent", "main"),
            ("gitflow.branch.develop.upstreamstrategy", "merge"),
            ("gitflow.branch.develop.downstreamstrategy", "merge"),
            ("gitflow.branch.develop.autoupdate", "true"),
            ("gitflow.branch.develop.tag", "false"),

            // Topic branches: the whole map, not only the prefix.
            ("gitflow.branch.feature.type", "topic"),
            ("gitflow.branch.feature.parent", "develop"),
            ("gitflow.branch.feature.startpoint", "develop"),
            ("gitflow.branch.feature.upstreamstrategy", "merge"),
            ("gitflow.branch.feature.downstreamstrategy", "rebase"),
            ("gitflow.branch.feature.prefix", "feature/"),
            ("gitflow.branch.feature.autoupdate", "false"),
            ("gitflow.branch.feature.tag", "false"),
            ("gitflow.branch.feature.worktree", "false"),
            ("gitflow.branch.bugfix.type", "topic"),
            ("gitflow.branch.bugfix.parent", "develop"),
            ("gitflow.branch.bugfix.startpoint", "develop"),
            ("gitflow.branch.bugfix.prefix", "bugfix/"),
            ("gitflow.branch.bugfix.tag", "false"),
            ("gitflow.branch.release.type", "topic"),
            ("gitflow.branch.release.parent", "main"),
            ("gitflow.branch.release.startpoint", "develop"),
            ("gitflow.branch.release.prefix", "release/"),
            ("gitflow.branch.release.tag", "true"),
            ("gitflow.branch.release.tagprefix", "v"),
            ("gitflow.branch.hotfix.type", "topic"),
            ("gitflow.branch.hotfix.parent", "main"),
            ("gitflow.branch.hotfix.startpoint", "main"),
            ("gitflow.branch.hotfix.prefix", "hotfix/"),
            ("gitflow.branch.hotfix.tag", "true"),
            ("gitflow.branch.hotfix.tagprefix", "v"),
            ("gitflow.branch.support.type", "topic"),
            ("gitflow.branch.support.parent", "main"),
            ("gitflow.branch.support.prefix", "support/"),
            ("gitflow.branch.support.tag", "false"),

            // A gitflow.* key this editor does not show, and a key of another tool: both must
            // survive every save untouched.
            ("gitflow.some.future.key", "keep-me"),
            ("other.tool.setting", "keep-me-too"),
        ];

        private readonly GitRepositoryFixture _fixture;

        public GitFlowNextSettingsTests()
        {
            TestBootstrap.Prepare();

            _fixture = GitRepositoryFixture.Create("gitflow-next");
            _fixture.Commit("a.txt", "a\n", "first");
            _fixture.NewBranch("develop");
            _fixture.NewBranch("feature/login");

            foreach (var kv in NEXT_KEYS)
                _fixture.Run("config", "--local", kv.Key, kv.Value);
        }

        public void Dispose() => _fixture.Dispose();

        [Fact]
        public void TheNextShapeIsEditableWithTheStoredNames()
        {
            var vm = Editor();
            Assert.True(vm.ShowEditor, "a next-shaped repository must be editable, not read-only");
            Assert.True(vm.ShowNextNote);
            Assert.Equal("main", vm.Production);
            Assert.Equal("develop", vm.Develop);
            Assert.Equal("feature/", vm.FeaturePrefix);
            Assert.Equal("release/", vm.ReleasePrefix);
            Assert.Equal("hotfix/", vm.HotfixPrefix);
            Assert.Equal("v", vm.TagPrefix);

            // The key list is the inspection path, so all stored keys stay visible.
            Assert.Contains(vm.Keys, x => x.Key == "gitflow.branch.develop.parent");
        }

        [Fact]
        public async Task RenamingProductionRewritesItsKeyMapAndTheReferencesToIt()
        {
            var refsBefore = Refs();
            var vm = Editor();
            vm.Production = "trunk";
            await SaveAsync(vm);

            var config = LocalConfig();
            // The map moved as a whole, values included.
            Assert.Equal("base", config["gitflow.branch.trunk.type"]);
            Assert.Equal("none", config["gitflow.branch.trunk.upstreamstrategy"]);
            Assert.Equal("none", config["gitflow.branch.trunk.downstreamstrategy"]);
            Assert.Equal("false", config["gitflow.branch.trunk.autoupdate"]);
            Assert.Equal("false", config["gitflow.branch.trunk.tag"]);
            Assert.False(config.ContainsKey("gitflow.branch.main.type"), "the old entry must be gone");

            // The development branch and the topic branches that started from production now
            // reference the new name.
            Assert.Equal("trunk", config["gitflow.branch.develop.parent"]);
            Assert.Equal("trunk", config["gitflow.branch.release.parent"]);
            Assert.Equal("trunk", config["gitflow.branch.hotfix.parent"]);
            Assert.Equal("trunk", config["gitflow.branch.support.parent"]);
            Assert.Equal("develop", config["gitflow.branch.feature.parent"]);

            // Nothing else changed, and no branch or ref was touched by the save.
            Assert.Equal("keep-me", config["gitflow.some.future.key"]);
            Assert.Equal("keep-me-too", config["other.tool.setting"]);
            Assert.Equal("true", config["gitflow.initialized"]);
            Assert.Equal("bugfix/", config["gitflow.branch.bugfix.prefix"]);
            Assert.Equal(refsBefore, Refs());
        }

        [Fact]
        public async Task TheRenamedValuesSurviveAColdReload()
        {
            var vm = Editor();
            vm.Production = "trunk";
            await SaveAsync(vm);

            // A cold reload means a new repository instance, which re-reads .git/config.
            var reloaded = new GitFlowSettingsVM(TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir));
            Assert.Equal("trunk", reloaded.Production);
            Assert.Equal("develop", reloaded.Develop);
            Assert.Equal("feature/", reloaded.FeaturePrefix);
            Assert.Equal("v", reloaded.TagPrefix);
        }

        [Fact]
        public async Task TheBranchTreeParserFollowsTheDetectedDependency()
        {
            var vm = Editor();
            vm.Production = "trunk";
            await SaveAsync(vm);

            // Models.GitFlow.Parse only reads the next form while the git-flow-next dependency was
            // detected, which is a documented dependency gate, not a bug of the editor: the editor
            // reads the keys themselves and stays correct either way (asserted above). Here the
            // parser has to agree with whatever the probe found. A QA run that puts the
            // project-local git-flow-next on PATH therefore exercises the parser for real.
            var detected = TestBootstrap.EnsureGitFlowProbe();
            Assert.Equal(TestBootstrap.ProbeGitFlowVersion(), detected);

            var flow = new Models.GitFlow();
            flow.Parse(new Commands.Config(_fixture.Root).ReadAll());

            if (detected == Models.GitFlowVersion.Next)
            {
                Assert.Equal("trunk", flow.ProductionBranch);
                Assert.Equal("develop", flow.DevelopmentBranch);
                Assert.Equal("feature/", flow.FeaturePrefix);
            }
            else
            {
                // Without the dependency the next tree is deliberately not interpreted, so the
                // client falls back to "git-flow is not configured" instead of guessing.
                Assert.Equal(string.Empty, flow.ProductionBranch);
                Assert.False(flow.IsValid);
            }
        }

        [Fact]
        public async Task ChangingAPrefixWritesOnlyItsOwnKey()
        {
            var before = LocalConfig();
            var vm = Editor();
            vm.FeaturePrefix = "topic/";
            await SaveAsync(vm);

            var after = LocalConfig();
            Assert.Equal("topic/", after["gitflow.branch.feature.prefix"]);

            foreach (var kv in before)
            {
                if (kv.Key == "gitflow.branch.feature.prefix")
                    continue;

                Assert.True(after.TryGetValue(kv.Key, out var value), $"lost {kv.Key}");
                Assert.Equal(kv.Value, value);
            }
        }

        [Fact]
        public async Task TheTagPrefixIsWrittenToEveryBranchThatTags()
        {
            var vm = Editor();
            vm.TagPrefix = "ver-";
            await SaveAsync(vm);

            var config = LocalConfig();
            Assert.Equal("ver-", config["gitflow.branch.release.tagprefix"]);
            Assert.Equal("ver-", config["gitflow.branch.hotfix.tagprefix"]);
            // `feature` has tag=false, so it must not get a tagprefix key.
            Assert.False(config.ContainsKey("gitflow.branch.feature.tagprefix"));
        }

        [Fact]
        public async Task ClearingTheTagPrefixRemovesTheKeys()
        {
            var vm = Editor();
            vm.TagPrefix = string.Empty;
            await SaveAsync(vm);

            // An absent key is how git-flow-next stores "tag without a prefix".
            var config = LocalConfig();
            Assert.False(config.ContainsKey("gitflow.branch.release.tagprefix"));
            Assert.False(config.ContainsKey("gitflow.branch.hotfix.tagprefix"));
        }

        [Fact]
        public void RenamingOntoAnotherBranchKeyIsRejected()
        {
            var vm = Editor();
            var ctx = new ValidationContext(vm) { MemberName = nameof(vm.Production) };

            // `feature` already owns a key map, so it cannot become the production branch.
            Assert.NotEqual(ValidationResult.Success, GitFlowSettingsVM.ValidateBaseBranch("feature", ctx));
            Assert.NotEqual(ValidationResult.Success, GitFlowSettingsVM.ValidateBaseBranch("develop", ctx));
            Assert.Equal(ValidationResult.Success, GitFlowSettingsVM.ValidateBaseBranch("trunk", ctx));
        }

        [Fact]
        public async Task SavingWithoutChangesWritesNothing()
        {
            var before = File.ReadAllText(Path.Combine(_fixture.GitDir, "config"));
            var vm = Editor();
            await SaveAsync(vm);
            Assert.Equal(before, File.ReadAllText(Path.Combine(_fixture.GitDir, "config")));
        }

        [Fact]
        public void TheLegacyShapeIsStillDetectedAsEditable()
        {
            using var legacy = GitRepositoryFixture.Create("gitflow-legacy");
            legacy.Commit("a.txt", "a\n", "first");
            legacy.Run("config", "--local", "gitflow.branch.master", "master");
            legacy.Run("config", "--local", "gitflow.branch.develop", "develop");
            legacy.Run("config", "--local", "gitflow.prefix.feature", "feature/");

            var repo = TestBootstrap.OpenRepository(legacy.Root, legacy.GitDir);
            var vm = new GitFlowSettingsVM(repo);
            Assert.True(vm.ShowEditor);
            Assert.False(vm.ShowNextNote);
            Assert.Equal("master", vm.Production);
        }

        /// <summary>
        /// The product's own save path: the popup validates first (LauncherPage.ProcessPopupAsync
        /// calls Popup.Check(), which runs ValidateAllProperties) and only then calls Sure(). A test
        /// that called Sure() directly would miss a validation error that blocks the save button.
        /// </summary>
        private static async Task SaveAsync(GitFlowSettingsVM vm)
        {
            Assert.True(vm.Check(), "the popup blocks the save because a field reports a validation error");
            Assert.True(await vm.Sure());
        }

        [Fact]
        public async Task RenamingBothBaseBranchesInOneSaveKeepsEveryReferenceOnTheNewNames()
        {
            // Regression (integration review F-1): the two renames share one save. Copying the
            // development map from the snapshot taken before the production rename would store the
            // pre-rename `parent` (the old production name) into the new entry and then delete the
            // reference the first rename had just updated - a dangling name that only shows up when
            // git-flow runs.
            var refsBefore = Refs();
            var vm = Editor();
            vm.Production = "trunk";
            vm.Develop = "dev";
            await SaveAsync(vm);

            var config = LocalConfig();
            Assert.Equal("base", config["gitflow.branch.trunk.type"]);
            Assert.Equal("base", config["gitflow.branch.dev.type"]);

            // The point of the test: the renamed development branch points at the renamed production
            // branch, not at a name that no longer exists.
            Assert.Equal("trunk", config["gitflow.branch.dev.parent"]);

            // And everything that referenced the old development name follows it.
            Assert.Equal("dev", config["gitflow.branch.feature.parent"]);
            Assert.Equal("dev", config["gitflow.branch.feature.startpoint"]);
            Assert.Equal("dev", config["gitflow.branch.bugfix.parent"]);
            Assert.Equal("dev", config["gitflow.branch.release.startpoint"]);
            Assert.Equal("trunk", config["gitflow.branch.release.parent"]);
            Assert.Equal("trunk", config["gitflow.branch.hotfix.parent"]);

            // Neither old key map survives, nothing outside them changed, and no ref was touched.
            Assert.DoesNotContain(config.Keys, k => k.StartsWith("gitflow.branch.main.", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(config.Keys, k => k.StartsWith("gitflow.branch.develop.", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("keep-me", config["gitflow.some.future.key"]);
            Assert.Equal("keep-me-too", config["other.tool.setting"]);
            Assert.Equal(refsBefore, Refs());
        }

        [Fact]
        public async Task ABlockedWriteStartsNoRenameAndLeavesTheStoredMapUntouched()
        {
            // Regression (integration review F-2): git refuses to write the local config while
            // .git/config.lock exists - the same failure a full disk or a parallel git process gives.
            // A rename must not begin after such a failure, because a copy without the reference
            // update and the delete would leave the old and the new key map both claiming type=base.
            var lockFile = Path.Combine(_fixture.GitDir, "config.lock");
            File.WriteAllText(lockFile, string.Empty);

            var refsBefore = Refs();
            var before = LocalConfig();
            try
            {
                var vm = Editor();
                vm.Production = "trunk";
                vm.Develop = "dev";
                Assert.False(await vm.Sure(), "a save whose writes are all blocked has to report failure");
            }
            finally
            {
                File.Delete(lockFile);
            }

            var after = LocalConfig();
            Assert.DoesNotContain(after.Keys, k => k.StartsWith("gitflow.branch.trunk.", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(after.Keys, k => k.StartsWith("gitflow.branch.dev.", StringComparison.OrdinalIgnoreCase));

            foreach (var kv in before)
                Assert.Equal(kv.Value, after.TryGetValue(kv.Key, out var v) ? v : null);

            Assert.Equal(refsBefore, Refs());

            // And the same save succeeds once the writer can work again.
            var retry = Editor();
            retry.Production = "trunk";
            retry.Develop = "dev";
            await SaveAsync(retry);

            var recovered = LocalConfig();
            Assert.Equal("trunk", recovered["gitflow.branch.dev.parent"]);
            Assert.DoesNotContain(recovered.Keys, k => k.StartsWith("gitflow.branch.main.", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(refsBefore, Refs());
        }

        [Fact]
        public async Task ACopyThatCannotFinishLeavesNoKeysUnderTheNewName()
        {
            // Regression (rereview R-1, copy phase): the copy loop used to carry on with the keys
            // after one of them failed, so a rename that hit one unwritable key left the keys it had
            // already written standing under the new name next to the untouched old map.
            SeedTwoValues("gitflow.branch.trunk.autoupdate", "left", "over");

            var refsBefore = Refs();
            var vm = Editor();
            vm.Production = "trunk";
            Assert.False(await vm.Sure(), "a rename whose copy failed has to report failure");

            var config = LocalConfig();
            Assert.False(config.ContainsKey("gitflow.branch.trunk.type"), "the copied map must not stay under the new name");
            Assert.False(config.ContainsKey("gitflow.branch.trunk.upstreamstrategy"));
            Assert.False(config.ContainsKey("gitflow.branch.trunk.downstreamstrategy"));

            // The one key under the new name is the key that was already there and could not be
            // overwritten. This save did not create it, so undoing must not delete it either.
            Assert.Equal("over", config["gitflow.branch.trunk.autoupdate"]);

            // The entry that was renamed is where it always was, references included.
            Assert.Equal("base", config["gitflow.branch.main.type"]);
            Assert.Equal("main", config["gitflow.branch.develop.parent"]);
            Assert.Equal("main", config["gitflow.branch.hotfix.parent"]);
            Assert.Equal(refsBefore, Refs());
        }

        [AvaloniaFact]
        public async Task AReferenceThatCannotBeRepointedIsMovedBackAndNamedInOneSummary()
        {
            // Regression (rereview R-1 reference phase + R-3): the copy had finished when the rewrite
            // of a back-reference failed, so the keys the copy wrote and the references that were
            // already moved all have to go back, and the whole save has to be reported once.
            SeedTwoValues("gitflow.branch.release.parent", "main", "main");

            var refsBefore = Refs();
            var vm = Editor();
            vm.Production = "trunk";

            using (var raised = new Notifications(_fixture.Root))
            {
                Assert.False(await vm.Sure(), "a rename that could not repoint its references has to report failure");

                // R-3: the commands of one save run without their own notification, the popup
                // reports the save once with the reason git gave.
                Assert.Single(raised.Sent);
                Assert.True(raised.Sent[0].IsError);
                Assert.True(raised.Sent[0].Message.Contains("gitflow.branch.release.parent", StringComparison.Ordinal), "the summary has to name the key: " + raised.Sent[0].Message);
                Assert.True(raised.Sent[0].Message.Contains("multiple values", StringComparison.Ordinal), "the summary has to quote git: " + raised.Sent[0].Message);
                Assert.True(raised.Sent[0].Message.Contains("git config --local --unset-all", StringComparison.Ordinal), "the summary has to name the cleanup command: " + raised.Sent[0].Message);
            }

            var config = LocalConfig();
            Assert.False(config.ContainsKey("gitflow.branch.trunk.type"), "the copy is undone together with the references");
            Assert.Equal("base", config["gitflow.branch.main.type"]);
            Assert.Equal("main", config["gitflow.branch.develop.parent"]);   // a reference that was moved has to be moved back
            Assert.Equal("main", config["gitflow.branch.support.parent"]);
            Assert.Equal("main", config["gitflow.branch.release.parent"]);
            Assert.Equal(refsBefore, Refs());
        }

        [AvaloniaFact]
        public async Task ADeleteThatCannotFinishRestoresTheOldMapAndSucceedsOnRetry()
        {
            // Regression (rereview R-1, delete phase + safe retry): the old keys are dropped last,
            // and when one of them cannot be dropped the keys already dropped have to come back,
            // because both maps standing together is the half rename the guards exist to avoid.
            SeedTwoValues("gitflow.branch.main.tag", "false", "false");

            var refsBefore = Refs();
            var vm = Editor();
            vm.Production = "trunk";

            using (var raised = new Notifications(_fixture.Root))
            {
                Assert.False(await vm.Sure(), "a rename that could not drop the old entry has to report failure");
                Assert.Single(raised.Sent);
                Assert.True(raised.Sent[0].Message.Contains("gitflow.branch.main.tag", StringComparison.Ordinal), "the summary has to name the key that stayed: " + raised.Sent[0].Message);
            }

            var config = LocalConfig();
            Assert.False(config.ContainsKey("gitflow.branch.trunk.type"), "the new entry must not survive an unfinished rename");
            Assert.Equal("base", config["gitflow.branch.main.type"]);   // the keys that were already dropped have to be back
            Assert.Equal("none", config["gitflow.branch.main.upstreamstrategy"]);
            Assert.Equal("false", config["gitflow.branch.main.autoupdate"]);
            Assert.Equal("main", config["gitflow.branch.develop.parent"]);   // the references point at the map that still exists
            Assert.Equal(refsBefore, Refs());

            // A retry has to work: the fields still hold the name the user asked for, and cleaning
            // up the key the summary names is all that is missing. The summary is the way to see
            // that, so the fields must not be treated as saved by a failed save.
            _fixture.Run("config", "--local", "--unset-all", "gitflow.branch.main.tag");
            _fixture.Run("config", "--local", "gitflow.branch.main.tag", "false");
            Assert.True(await vm.Sure(), "the same popup has to be able to finish once the key is clean");

            var saved = LocalConfig();
            Assert.Equal("base", saved["gitflow.branch.trunk.type"]);
            Assert.False(saved.ContainsKey("gitflow.branch.main.type"));
            Assert.Equal("trunk", saved["gitflow.branch.develop.parent"]);
        }

        [AvaloniaFact]
        public async Task KeysThatCannotBeClassifiedAreListedAndNeverEdited()
        {
            // Integration review E-4: with no detected dependency and no gitflow.initialized, the
            // parser cannot say whether the stored keys are the legacy or the next shape. The old note
            // claimed the repository had no git-flow keys while the key list below it was populated.
            _fixture.Run("config", "--local", "--unset", "gitflow.initialized");

            using var hidden = TestBootstrap.WithoutGitFlowTool();
            var vm = Editor();

            Assert.False(vm.ShowEditor, "an unclassifiable key set must not be editable");
            Assert.True(vm.HasKeys);
            Assert.Contains(vm.Keys, x => x.Key == "gitflow.branch.develop.parent");
            Assert.False(string.IsNullOrWhiteSpace(vm.LimitationState));
            Assert.DoesNotContain("no git-flow keys", vm.LimitationState, StringComparison.OrdinalIgnoreCase);

            // A save in that state is a no-op whatever the fields hold.
            vm.Production = "trunk";
            Assert.True(await vm.Sure());
            Assert.Equal("base", LocalConfig()["gitflow.branch.main.type"]);
            Assert.Equal("main", LocalConfig()["gitflow.branch.develop.parent"]);
            Assert.False(LocalConfig().ContainsKey("gitflow.branch.trunk.type"), "an unclassifiable shape must not be written to");
        }

        [AvaloniaFact]
        public async Task ARenameThatIsUndonePutsBackWhatAlreadyLivedUnderTheNewName()
        {
            // Regression (final review N-1): undoing used to delete everything it saw under the
            // destination name, including a key that was stored there before the save started. The
            // collision check only knows names that own a key map, so a single key left under
            // `gitflow.branch.trunk.` (`autoupdate`, a strategy, ...) is a reachable destination, and
            // deleting it would drop a setting this save never made.
            _fixture.Run("config", "--local", "gitflow.branch.trunk.autoupdate", "keep");
            SeedTwoValues("gitflow.branch.main.tag", "false", "false");   // the last phase breaks here

            var refsBefore = Refs();
            var vm = Editor();
            vm.Production = "trunk";

            using (var raised = new Notifications(_fixture.Root))
            {
                Assert.False(await vm.Sure(), "a rename whose delete failed has to report failure");
                Assert.Single(raised.Sent);
            }

            var config = LocalConfig();
            // The key the rename only borrowed comes back with the value it held; the copy had
            // overwritten it with the old entry's value, so restoring is not the same as leaving it.
            // xunit has no "because" on Equal, so the checks that need a wording read as True checks.
            Assert.True(config.TryGetValue("gitflow.branch.trunk.autoupdate", out var borrowed) && borrowed == "keep",
                    "a key the rename only borrowed has to come back with the value it held");
            Assert.False(config.ContainsKey("gitflow.branch.trunk.type"), "the map this save created has to be gone");
            Assert.False(config.ContainsKey("gitflow.branch.trunk.upstreamstrategy"));
            Assert.True(config.TryGetValue("gitflow.branch.main.type", out var old) && old == "base",
                    "the old entry has to stand where it was");
            Assert.Equal(refsBefore, Refs());
        }

        [AvaloniaFact]
        public async Task KeysHeldOnlyByAFileAboveTheRepositoryAreReportedAndNeverWrittenLocally()
        {
            // Regression (final review N-2): the popup showed the merged config but planned its writes
            // from the merged read too, so a flow that lives only in the user's or the system's file
            // would have been copied into the repository's file - where the inherited values still win
            // over it, and where a rollback cannot undo what git precedence keeps shadowing.
            using var other = GitRepositoryFixture.Create("gitflow-parent");
            other.Commit("a.txt", "a\n", "first");

            var stored = string.Join("\n", new[]
            {
                "[gitflow]",
                "\tversion = 1.0",
                "\tinitialized = true",
                "",
                "[gitflow \"branch.main\"]",
                "\ttype = base",
                "",
                "[gitflow \"branch.develop\"]",
                "\ttype = base",
                "\tparent = main",
                "",
                "[gitflow \"branch.feature\"]",
                "\ttype = topic",
                "\tparent = develop",
                "\tprefix = feature/",
            });

            // The fixture's stand-in for the config "above this repository" - never a config file that
            // belongs to the machine, and it goes back to empty when the scope ends.
            using var above = GitEnv.WithGlobalConfig(stored);

            var refsBefore = RefsOf(other);
            var repo = TestBootstrap.OpenRepository(other.Root, other.GitDir);
            var vm = new GitFlowSettingsVM(repo);

            // The keys are listed and the shape is recognised, because reading tells the truth
            // about them; only editing them is not this window's to do.
            Assert.True(vm.HasKeys, "keys inherited from above have to be shown");
            Assert.False(vm.Writable, "a flow that this repository does not hold cannot be edited here");
            Assert.False(vm.ShowEditor);
            Assert.True(vm.ShowLimitation);

            // A change has to be asked for, or the save ends as "nothing to do" before it gets to
            // the point this test is about.
            vm.Production = "trunk";

            using (var raised = new Notifications(other.Root))
            {
                Assert.False(await vm.Sure(), "a save cannot rename entries the repository's own file does not hold");
                Assert.Single(raised.Sent);
                Assert.Contains("above this repository", raised.Sent[0].Message);
            }

            var local = LocalConfigOf(other);
            Assert.DoesNotContain(local.Keys, k => k.StartsWith("gitflow.", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(refsBefore, RefsOf(other));
        }

        /// <summary>
        /// The partial case of the same rule: the entry this save renames is held partly in the
        /// repository's own file and partly in a file above it. Writing would move the local half and
        /// leave the half above naming a branch that no longer exists, and no rollback can join those
        /// two again - so an entry that is not wholly here is not renamed at all.
        /// </summary>
        [AvaloniaFact]
        public async Task ARenameOfAnEntryThatIsSplitAcrossFilesChangesNothing()
        {
            // "main" exists here in every key the editor shows, and one more key of the same branch
            // lives in the file above - which is exactly what a team config looks like when a
            // repository started overriding a few of its keys.
            using var above = GitEnv.WithGlobalConfig("[gitflow \"branch.main\"]\n\taudit = kept-uphere\n");

            var refsBefore = Refs();
            var before = LocalConfig();

            var vm = Editor();
            vm.Production = "trunk";

            using (var raised = new Notifications(_fixture.Root))
            {
                Assert.False(await vm.Sure(), "an entry that is only partly here must not be half-moved");
                Assert.Single(raised.Sent);
                Assert.Contains("above this repository", raised.Sent[0].Message);
            }

            // Not copied, not renamed, not deleted: the file is exactly as it was.
            Assert.Equal(before, LocalConfig());
            Assert.Equal(refsBefore, Refs());
        }

        /// <summary>
        /// A rename that fails and is undone under a name that also exists in a file above: what this
        /// save wrote locally has to be gone, because anything it leaves behind would quietly take
        /// precedence over the inherited entry it was supposed to undo.
        /// </summary>
        [AvaloniaFact]
        public async Task ARenameUndoneLeavesNothingUnderANameThatAlsoExistsAbove()
        {
            using var above = GitEnv.WithGlobalConfig("[gitflow \"branch.trunk\"]\n\taudit = kept-uphere\n");

            // The copy under the new name succeeds; the reference update that follows is what fails,
            // so the undo has to remove a whole copied map. The order matters: git reports the last
            // value of a multi-valued key, and that has to be the branch being renamed, or the save
            // would find no reference to update and never reach the failing write.
            SeedTwoValues("gitflow.branch.develop.parent", "other", "main");

            var refsBefore = Refs();
            var vm = Editor();
            vm.Production = "trunk";

            using (var raised = new Notifications(_fixture.Root))
            {
                Assert.False(await vm.Sure());
                Assert.Single(raised.Sent);
            }

            var config = LocalConfig();
            foreach (var key in config.Keys)
            {
                Assert.False(key.StartsWith("gitflow.branch.trunk.", StringComparison.OrdinalIgnoreCase),
                        "a name this save created has to be empty again after the undo, found " + key);
            }

            Assert.True(config.TryGetValue("gitflow.branch.main.type", out var type) && type == "base",
                    "the entry that was renamed has to stand where it was");
            Assert.Equal(refsBefore, Refs());
        }

        [AvaloniaFact]
        public async Task ASaveThatStoppedNamesWhatItTriedAndNotWhatItNeverReached()
        {
            // Regression (final review N-5): the summary used to list keys whose write was skipped
            // because an earlier one had failed, which told the user those saves were attempted and
            // refused. Only the key that git actually turned down belongs in that line.
            SeedTwoValues("gitflow.branch.main.tag", "false", "false");

            var vm = Editor();
            vm.Production = "trunk";
            vm.ReleasePrefix = "rel/";

            using (var raised = new Notifications(_fixture.Root))
            {
                Assert.False(await vm.Sure());
                Assert.Single(raised.Sent);
                Assert.Contains("gitflow.branch.main.tag", raised.Sent[0].Message);
                Assert.DoesNotContain("gitflow.branch.release.prefix", raised.Sent[0].Message);
            }

            // The prefix that was never attempted still holds what was stored, not what was typed.
            Assert.Equal("release/", LocalConfig()["gitflow.branch.release.prefix"]);
        }

        [Fact]
        public void ACleanupLineShowsWhatItWouldRun()
        {
            // Regression (final review N-6): the summary hands the user a command to run. A ref name
            // may hold `$` or a backtick, so the key has to be quoted the way a shell wants or the
            // line would run something other than what it shows.
            Assert.Equal(
                "git config --local --unset-all 'gitflow.branch.main.tag'",
                GitFlowSettingsVM.CleanupLine("gitflow.branch.main.tag"));

            Assert.Equal(
                @"git config --local --unset-all 'gitflow.branch.ma'\''in.tag'",
                GitFlowSettingsVM.CleanupLine("gitflow.branch.ma'in.tag"));
        }

        [AvaloniaFact]
        public async Task AConfigThatCannotBeReadStopsTheSaveAndPassesOnWhatGitSaid()
        {
            // Regression (final review N-7): a failed read was read as "no keys", and the save went on
            // to write. Without a snapshot nothing is known about what a write would overwrite, so the
            // only correct action is to write nothing and hand git's reason to the user.
            var vm = Editor();
            vm.Production = "trunk";

            File.AppendAllText(Path.Combine(_fixture.GitDir, "config"), "\nnot config syntax\n");

            var raw = File.ReadAllText(Path.Combine(_fixture.GitDir, "config"));
            using (var raised = new Notifications(_fixture.Root))
            {
                Assert.False(await vm.Sure(), "a save that could not read the config must not claim success");
                Assert.Single(raised.Sent);
                Assert.Contains("could not be read", raised.Sent[0].Message);
            }

            // Nothing of the save reached the file: only the line that broke the read is in there.
            Assert.Equal(raw, File.ReadAllText(Path.Combine(_fixture.GitDir, "config")));
        }

        [Fact]
        public void TheStoredValuesValidateWithoutAnyEdit()
        {
            // Regression: the key-map collision check must not treat the name the field itself
            // stores as a collision, or the save button would be blocked in a valid repository.
            Assert.True(Editor().Check());
        }

        /// <summary>The repository's own config file of any fixture.</summary>
        private static Dictionary<string, string> LocalConfigOf(GitRepositoryFixture fixture)
        {
            var rs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in fixture.RunOutput("config", "--local", "-l").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                    rs[parts[0]] = parts[1];
            }

            return rs;
        }

        private static string RefsOf(GitRepositoryFixture fixture)
        {
            var sb = new StringBuilder();
            foreach (var line in fixture.RunOutput("show-ref").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                sb.AppendLine(line);

            return sb.ToString();
        }

        /// <summary>Builds the settings VM over a freshly read repository instance.</summary>
        private GitFlowSettingsVM Editor()
        {
            var repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);
            return new GitFlowSettingsVM(repo);
        }

        /// <summary>
        /// Makes git refuse to write one single key while every other key stays writable: git will
        /// not replace a key that holds more than one value with one value. That is how these tests
        /// fail a chosen write in the middle of a save with nothing but git's own behaviour.
        /// </summary>
        private void SeedTwoValues(string key, string first, string second)
        {
            _fixture.Run("config", "--local", "--add", key, first);
            _fixture.Run("config", "--local", "--add", key, second);
        }

        /// <summary>
        /// Collects what a save reports, so "one save, one notification" can be asserted. Only the
        /// notifications of this fixture's repository are counted, because other test classes report
        /// from their own repositories at the same time.
        /// </summary>
        private sealed class Notifications : IDisposable
        {
            private readonly string _group;

            public readonly List<Models.Notification> Sent = new();

            public Notifications(string repository)
            {
                // Repository.SendNotification reports the repository path the way Repository stores
                // it, with forward slashes and no trailing one.
                _group = repository.Replace("\\", "/").TrimEnd('/');
                Models.Notification.Raised += OnRaised;
            }

            public void Dispose() => Models.Notification.Raised -= OnRaised;

            private void OnRaised(Models.Notification n)
            {
                if (n.Group == _group)
                    Sent.Add(n);
            }
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

        /// <summary>Every ref of the repository, to prove a save never touches branches or tags.</summary>
        private string Refs()
        {
            var sb = new StringBuilder();
            foreach (var line in _fixture.RunOutput("show-ref").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                sb.AppendLine(line);

            return sb.ToString();
        }
    }
}
