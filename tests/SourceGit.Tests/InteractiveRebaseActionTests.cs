using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Avalonia.Headless.XUnit;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// A14 "대화형 Rebase" action semantics. Headless VM/model + real git execution evidence,
    /// deliberately separate from the Windows desktop GUI evidence in Docs/QA_MATRIX.md.
    /// </summary>
    public class InteractiveRebaseActionTests
    {
        private static Models.Commit CommitOf(string sha, string subject)
        {
            return new Models.Commit() { SHA = sha, Subject = subject };
        }

        [AvaloniaFact]
        public void LoadsRowsFromGit_ForCommitsAfterTheSelectedBase()
        {
            using var fixture = GitRepositoryFixture.Create("ir-load");
            fixture.Commit("base.txt", "base\n", "base subject");
            var on = fixture.SHA();
            fixture.Commit("a.txt", "a\n", "subject A");
            fixture.Commit("b.txt", "b\n", "subject B");
            fixture.Commit("c.txt", "c\n", "subject C");

            var vm = CreateRebase(fixture, on);
            Assert.Equal(3, vm.Items.Count);
            Assert.Equal("subject C", vm.Items[0].Commit.Subject);
            Assert.Equal("subject A", vm.Items[^1].Commit.Subject);
            Assert.Equal(vm.Items[^1].Commit.SHA, fixture.SHA("main~2"));
        }

        [Theory]
        [InlineData(Models.InteractiveRebaseAction.Pick)]
        [InlineData(Models.InteractiveRebaseAction.Edit)]
        [InlineData(Models.InteractiveRebaseAction.Reword)]
        [InlineData(Models.InteractiveRebaseAction.Drop)]
        public void ChangeAction_AppliesToEveryRow(Models.InteractiveRebaseAction action)
        {
            using var fixture = GitRepositoryFixture.Create("ir-action");
            fixture.Commit("base.txt", "base\n", "base subject");
            var on = fixture.SHA();
            fixture.Commit("a.txt", "a\n", "subject A");
            fixture.Commit("b.txt", "b\n", "subject B");
            fixture.Commit("c.txt", "c\n", "subject C");

            var vm = CreateRebase(fixture, on);
            vm.ChangeAction(new List<ViewModels.InteractiveRebaseItem>(vm.Items), action);

            foreach (var item in vm.Items)
                Assert.Equal(action, item.Action);
        }

        [Theory]
        [InlineData(Models.InteractiveRebaseAction.Squash)]
        [InlineData(Models.InteractiveRebaseAction.Fixup)]
        public void SquashAndFixup_NeverApplyToTheFirstPickedCommit(Models.InteractiveRebaseAction action)
        {
            using var fixture = GitRepositoryFixture.Create("ir-squash-guard");
            fixture.Commit("base.txt", "base\n", "base subject");
            var on = fixture.SHA();
            fixture.Commit("a.txt", "a\n", "subject A");
            fixture.Commit("b.txt", "b\n", "subject B");
            fixture.Commit("c.txt", "c\n", "subject C");

            var vm = CreateRebase(fixture, on);
            vm.ChangeAction(new List<ViewModels.InteractiveRebaseItem>(vm.Items), action);

            // Items[0] is the newest, Items[^1] the commit right after the base: it has no parent to meld into.
            Assert.Equal(action, vm.Items[0].Action);
            Assert.Equal(action, vm.Items[1].Action);
            Assert.False(vm.Items[^1].CanSquashOrFixup);
            Assert.True(vm.Items[0].CanSquashOrFixup);

            // A squash that cannot meld into anything keeps its message instead of losing it, so the
            // row turns into 'Reword'; a fixup has no message of its own to keep and stays 'Pick'.
            if (action == Models.InteractiveRebaseAction.Squash)
                Assert.Equal(Models.InteractiveRebaseAction.Reword, vm.Items[^1].Action);
            else
                Assert.Equal(Models.InteractiveRebaseAction.Pick, vm.Items[^1].Action);
        }

        [Fact]
        public void SquashRows_ExposeMessageRolesForTheTodoEditor()
        {
            using var fixture = GitRepositoryFixture.Create("ir-pending");
            fixture.Commit("base.txt", "base\n", "base subject");
            var on = fixture.SHA();
            fixture.Commit("a.txt", "a\n", "subject A");
            fixture.Commit("b.txt", "b\n", "subject B");
            fixture.Commit("c.txt", "c\n", "subject C");

            var vm = CreateRebase(fixture, on);
            vm.ChangeAction([vm.Items[0], vm.Items[1]], Models.InteractiveRebaseAction.Squash);

            // The newest squash row waits for the following rows' messages, the row that starts the
            // meld is flagged as the message owner (PendingType.Last) - that pairing decides which
            // message the app finally writes back for git's combined commit.
            Assert.Equal(Models.InteractiveRebasePendingType.Last, vm.Items[0].PendingType);
            Assert.Equal(Models.InteractiveRebasePendingType.Pending, vm.Items[1].PendingType);
            Assert.False(vm.Items[0].ShowEditMessageButton);

            vm.ChangeAction([vm.Items[2]], Models.InteractiveRebaseAction.Drop);
            Assert.Equal(Models.InteractiveRebaseAction.Drop, vm.Items[2].Action);
            Assert.False(vm.Items[2].ShowEditMessageButton);
        }

        [Fact]
        public void WriteTodoList_MapsEveryActionToItsGitKeyword()
        {
            var collection = new Models.InteractiveRebaseJobCollection()
            {
                OrigHead = "0000000000000000000000000000000000000000",
                Onto = "1111111111111111111111111111111111111111",
            };

            var actions = new Models.InteractiveRebaseAction[]
            {
                Models.InteractiveRebaseAction.Pick,
                Models.InteractiveRebaseAction.Edit,
                Models.InteractiveRebaseAction.Reword,
                Models.InteractiveRebaseAction.Squash,
                Models.InteractiveRebaseAction.Fixup,
                Models.InteractiveRebaseAction.Drop,
            };

            for (int i = 0; i < actions.Length; i++)
                collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = SHA(i), Action = actions[i], Message = "m" });

            var file = Path.Combine(Path.GetTempPath(), "sourcegit-tests-todo.txt");
            try
            {
                collection.WriteTodoList(file);
                var lines = File.ReadAllLines(file);
                Assert.Equal(actions.Length, lines.Length);
                Assert.Equal($"p {SHA(0)}", lines[0]);
                Assert.Equal($"e {SHA(1)}", lines[1]);
                Assert.Equal($"r {SHA(2)}", lines[2]);
                Assert.Equal($"s {SHA(3)}", lines[3]);
                Assert.Equal($"f {SHA(4)}", lines[4]);
                Assert.Equal($"d {SHA(5)}", lines[5]);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void WriteCommitMessage_TakesTheMessageOfTheSquashedJob()
        {
            var collection = new Models.InteractiveRebaseJobCollection();
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = SHA(1), Action = Models.InteractiveRebaseAction.Pick, Message = "keep me" });
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = SHA(2), Action = Models.InteractiveRebaseAction.Squash, Message = "other" });

            var dir = Path.Combine(Path.GetTempPath(), "sourcegit-tests-msg");
            Directory.CreateDirectory(dir);

            var done = Path.Combine(dir, "done");
            var msg = Path.Combine(dir, "msg");
            try
            {
                File.WriteAllText(done, $"p {SHA(1)} subject one\n");
                File.WriteAllText(msg, "untouched");

                collection.WriteCommitMessage(done, msg);
                Assert.Equal("keep me", File.ReadAllText(msg));
            }
            finally
            {
                Cleaner.DeleteDirectory(dir);
            }
        }

        [Theory]
        [InlineData(Models.InteractiveRebaseAction.Drop)]
        [InlineData(Models.InteractiveRebaseAction.Squash)]
        [InlineData(Models.InteractiveRebaseAction.Fixup)]
        [InlineData(Models.InteractiveRebaseAction.Reword)]
        public void GeneratedTodo_IsExecutedByGitExactlyAsTheGuiWould(Models.InteractiveRebaseAction action)
        {
            using var fixture = GitRepositoryFixture.Create("ir-exec");
            fixture.Commit("base.txt", "base\n", "base subject");
            var on = fixture.SHA();
            fixture.Commit("a.txt", "a\n", "subject A");
            var mid = fixture.Commit("b.txt", "b\n", "subject B");
            fixture.Commit("c.txt", "c\n", "subject C");
            var headBefore = fixture.SHA();
            var treeBefore = fixture.RunOutput("rev-parse", "HEAD^{tree}").Trim();

            // Same sequence the app hands to git: job list written by the product code, applied by
            // git itself with the same abbreviated-command setting the app passes on the command line.
            var editor = Path.Combine(fixture.Root, ".sg-editor.sh");
            var msgEditor = Path.Combine(fixture.Root, ".sg-msg.sh");
            var todo = Path.Combine(fixture.Root, ".sg-todo.gen");
            File.WriteAllText(editor, "#!/bin/sh\ncat .sg-todo.gen > \"$1\"\n", new UTF8Encoding(false));
            File.WriteAllText(msgEditor, "#!/bin/sh\nprintf 'reworded subject\\n' > \"$1\"\n", new UTF8Encoding(false));

            var collection = new Models.InteractiveRebaseJobCollection() { Onto = on };
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = fixture.SHA("main~2"), Action = Models.InteractiveRebaseAction.Pick, Message = "subject A" });
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = mid, Action = action, Message = "subject B" });
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = headBefore, Action = Models.InteractiveRebaseAction.Pick, Message = "subject C" });
            collection.WriteTodoList(todo);

            // The app normally plays both editor roles itself; here the todo comes from the product
            // code and git's own message editor is a script, because Environment.ProcessPath in a test
            // host is not the application binary.
            if (action == Models.InteractiveRebaseAction.Reword)
                fixture.RunWithEditors("sh .sg-editor.sh", "sh .sg-msg.sh", "-c", "rebase.abbreviateCommands=true", "rebase", "-i", on);
            else
                fixture.RunWithSequenceEditor("sh .sg-editor.sh", "-c", "rebase.abbreviateCommands=true", "rebase", "-i", on);

            switch (action)
            {
                case Models.InteractiveRebaseAction.Drop:
                    Assert.DoesNotContain("subject B", string.Join("\n", fixture.Subjects("main")));
                    Assert.False(File.Exists(Path.Combine(fixture.Root, "b.txt")));
                    Assert.True(File.Exists(Path.Combine(fixture.Root, "a.txt")));
                    Assert.True(File.Exists(Path.Combine(fixture.Root, "c.txt")));
                    break;

                case Models.InteractiveRebaseAction.Squash:
                    Assert.Equal(3, fixture.Subjects("main").Count);
                    Assert.Contains("subject B", fixture.RunOutput("log", "-1", "--format=%B", "main~1"));
                    Assert.True(File.Exists(Path.Combine(fixture.Root, "b.txt")));
                    break;

                case Models.InteractiveRebaseAction.Fixup:
                    Assert.Equal(3, fixture.Subjects("main").Count);
                    Assert.DoesNotContain("subject B", fixture.RunOutput("log", "-1", "--format=%B", "main~1"));
                    Assert.Contains("subject A", fixture.RunOutput("log", "-1", "--format=%B", "main~1"));
                    Assert.True(File.Exists(Path.Combine(fixture.Root, "b.txt")));
                    break;

                case Models.InteractiveRebaseAction.Reword:
                    var subjects = string.Join("\n", fixture.Subjects("main"));
                    Assert.Contains("reworded subject", subjects);
                    Assert.DoesNotContain("subject B", subjects);
                    Assert.Equal(4, fixture.Subjects("main").Count);
                    Assert.Equal(treeBefore, fixture.RunOutput("rev-parse", "HEAD^{tree}").Trim());
                    break;
            }
        }

        [AvaloniaFact]
        public void EditAction_StopsTheRebaseForAmending()
        {
            using var fixture = GitRepositoryFixture.Create("ir-edit");
            fixture.Commit("base.txt", "base\n", "base subject");
            var on = fixture.SHA();
            fixture.Commit("a.txt", "a\n", "subject A");
            var mid = fixture.Commit("b.txt", "b\n", "subject B");
            fixture.Commit("c.txt", "c\n", "subject C");
            var head = fixture.SHA();

            var editor = Path.Combine(fixture.Root, ".sg-editor.sh");
            var todo = Path.Combine(fixture.Root, ".sg-todo.gen");
            File.WriteAllText(editor, "#!/bin/sh\ncat .sg-todo.gen > \"$1\"\n", new UTF8Encoding(false));

            var collection = new Models.InteractiveRebaseJobCollection() { Onto = on };
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = fixture.SHA("main~2"), Action = Models.InteractiveRebaseAction.Pick });
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = mid, Action = Models.InteractiveRebaseAction.Edit });
            collection.Jobs.Add(new Models.InteractiveRebaseJob() { SHA = head, Action = Models.InteractiveRebaseAction.Pick });
            collection.WriteTodoList(todo);

            // git stops with a rebase in progress at the edited commit ("Stop for amending").
            var result = fixture.TryWithSequenceEditor("sh .sg-editor.sh", "-c", "rebase.abbreviateCommands=true", "rebase", "-i", on);

            Assert.True(Directory.Exists(Path.Combine(fixture.GitDir, "rebase-merge")), "the rebase should still be in progress");
            Assert.Contains("subject B", fixture.RunOutput("log", "-1", "--format=%s").Trim());
            Assert.Equal(fixture.RunOutput("rev-parse", mid + "^{tree}").Trim(), fixture.RunOutput("rev-parse", "HEAD^{tree}").Trim());

            // Continuing finishes the sequence that was generated by the product code.
            fixture.Run("rebase", "--continue");
            Assert.False(Directory.Exists(Path.Combine(fixture.GitDir, "rebase-merge")));
            Assert.Contains("subject C", string.Join("\n", fixture.Subjects("main")));
        }

        private static ViewModels.InteractiveRebase CreateRebase(GitRepositoryFixture fixture, string on)
        {
            var repo = TestBootstrap.OpenRepository(fixture.Root, fixture.GitDir);
            var vm = new ViewModels.InteractiveRebase(repo, CommitOf(on, "base subject"));
            Assert.True(Pump.Until(() => !vm.IsLoading), "the interactive rebase rows were not loaded");
            return vm;
        }

        private static string SHA(int seed)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 40; i++)
                sb.Append((char)('0' + ((seed * 31 + i * 7) % 10)));
            return sb.ToString();
        }
    }
}
