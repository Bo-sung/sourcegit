using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Headless.XUnit;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// A18 commit templates: variable expansion (Models.TemplateEngine through Models.CommitTemplate)
    /// and the commit panel's apply path (ViewModels.WorkingCopy.ApplyCommitMessageTemplate), which is
    /// the method the template picker's click handler calls.
    /// </summary>
    public class CommitTemplateTests
    {
        private static Models.Branch Branch(string name)
        {
            return new Models.Branch() { Name = name, IsLocal = true };
        }

        private static string Eval(string content, string branch, params string[] paths)
        {
            var changes = new List<Models.Change>();
            foreach (var p in paths)
                changes.Add(new Models.Change() { Path = p, Index = Models.ChangeState.Added });

            var template = new Models.CommitTemplate() { Name = "t", Content = content };
            return template.Apply(Branch(branch), changes);
        }

        [Fact]
        public void PlainTextIsPassedThrough()
        {
            Assert.Equal("fix: something\n\nbody line", Eval("fix: something\n\nbody line", "main", "a.txt"));
        }

        [Fact]
        public void BranchAndFileVariablesAreExpanded()
        {
            Assert.Equal("feature/login", Eval("${branch_name}", "feature/login"));
            Assert.Equal("2", Eval("${files_num}", "main", "src/a.cs", "docs/b.md"));
            Assert.Equal("src/a.cs, docs/b.md", Eval("${files}", "main", "src/a.cs", "docs/b.md"));
            Assert.Equal("a.cs, b.md", Eval("${pure_files}", "main", "src/a.cs", "docs/b.md"));
        }

        [Fact]
        public void SlicedVariableListsTheRestOfTheFiles()
        {
            Assert.Equal("src/a.cs and 2 other files", Eval("${files:1}", "main", "src/a.cs", "docs/b.md", "x/y.cs"));
            Assert.Equal("a.cs, b.md", Eval("${pure_files:5}", "main", "src/a.cs", "docs/b.md"));
        }

        [Fact]
        public void RegexVariableRewritesTheValue()
        {
            Assert.Equal("feat-123-login", Eval("${branch_name/^feature\\//feat-}", "feature/123-login"));
        }

        [Fact]
        public void DollarAndBracesCanBeEscaped()
        {
            Assert.Equal("${branch_name}", Eval("\\${branch_name}", "main"));
            Assert.Equal("$100 for main", Eval("$100 for ${branch_name}", "main"));
        }

        [Fact]
        public void UnknownOrBrokenVariablesExpandToNothing()
        {
            Assert.Equal("[]", Eval("[${not_a_variable}]", "main", "a.txt"));
            Assert.Equal("keep ${unclosed", Eval("keep ${unclosed", "main", "a.txt"));
        }

        [Fact]
        public void BranchVariableToleratesARepositoryWithoutBranch()
        {
            var template = new Models.CommitTemplate() { Name = "t", Content = "wip: ${branch_name}" };
            Assert.Equal("wip: ", template.Apply(null, new List<Models.Change>()));
        }

        [AvaloniaFact]
        public async Task ApplyingATemplateUsesTheStagedChangesOfTheRepository()
        {
            TestBootstrap.Prepare();

            using var fixture = GitRepositoryFixture.Create("template-apply");
            fixture.Commit("seed.txt", "seed\n", "seed");
            fixture.Write("src/a.cs", "a\n");
            fixture.Write("docs/b.md", "b\n");
            fixture.Write("unstaged.txt", "u\n");
            fixture.Run("add", "--", "src/a.cs", "docs/b.md");

            var changes = await new Commands.QueryLocalChanges(fixture.Root).GetResultAsync();
            var repo = TestBootstrap.OpenRepository(fixture.Root, fixture.GitDir);
            var wc = new ViewModels.WorkingCopy(repo);
            wc.SetData(changes);

            Assert.Equal(2, wc.Staged.Count);
            Assert.Single(wc.Unstaged);

            wc.CommitMessage = "typed before picking a template";
            wc.ApplyCommitMessageTemplate(new Models.CommitTemplate()
            {
                Name = "files",
                Content = "chore: update ${files_num} files\n\n${files}",
            });

            // git status reports the paths in sorted order and the commit panel keeps that order.
            Assert.Equal("chore: update 2 files\n\ndocs/b.md, src/a.cs", wc.CommitMessage);
        }

        [AvaloniaFact]
        public void TemplateSavedInRepositorySettingsIsApplied()
        {
            TestBootstrap.Prepare();

            using var fixture = GitRepositoryFixture.Create("template-settings");
            fixture.Commit("seed.txt", "seed\n", "seed");

            var settings = Models.RepositorySettings.Get(fixture.GitDir);
            settings.CommitTemplates.Add(new Models.CommitTemplate() { Name = "wip", Content = "wip: ${files_num}" });
            settings.Save();

            var copy = Path_Copy(fixture.Root);
            try
            {
                var reloaded = Models.RepositorySettings.Get(Path.Combine(copy, ".git"));
                Assert.Single(reloaded.CommitTemplates);

                var repo = TestBootstrap.OpenRepository(copy, Path.Combine(copy, ".git"));
                var wc = new ViewModels.WorkingCopy(repo);
                wc.ApplyCommitMessageTemplate(reloaded.CommitTemplates[0]);
                Assert.Equal("wip: 0", wc.CommitMessage);

                // The copy is about to be deleted, and an open repository view model keeps polling it:
                // the watcher runs every 100ms and the auto-fetch timer every 5s. Closing first is what
                // stops the run from working on a directory that no longer exists.
                TestBootstrap.CloseRepository(repo);
            }
            finally
            {
                Cleaner.DeleteDirectory(copy);
            }
        }

        private static string Path_Copy(string source)
        {
            var target = Path.Combine(Path.GetTempPath(), "sourcegit-tests", Path.GetFileName(source) + "-copy-" + DateTime.Now.ToString("HHmmssfff"));
            CopyDirectory(source, target);
            return target;
        }

        private static void CopyDirectory(string source, string target)
        {
            System.IO.Directory.CreateDirectory(target);

            foreach (var file in System.IO.Directory.GetFiles(source))
                System.IO.File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)), true);

            foreach (var dir in System.IO.Directory.GetDirectories(source))
                CopyDirectory(dir, System.IO.Path.Combine(target, System.IO.Path.GetFileName(dir)));
        }
    }
}
