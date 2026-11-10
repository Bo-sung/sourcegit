using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// A03 default-remote chooser: the stored key, the persisted record and the reload of that
    /// record are exercised with the product's own RepositorySettings/RepositoryConfigure code.
    /// The chooser's rendered list is covered by the desktop GUI evidence, not here.
    /// </summary>
    public class DefaultRemotePersistenceTests : IDisposable
    {
        private readonly GitRepositoryFixture _fixture;
        private readonly GitRepositoryFixture _second;

        public DefaultRemotePersistenceTests()
        {
            TestBootstrap.Prepare();
            _fixture = GitRepositoryFixture.Create("remote-default");
            _fixture.Commit("a.txt", "a\n", "first");

            _second = GitRepositoryFixture.Create("remote-second");
            _second.Commit("seed.txt", "seed\n", "seed");

            _fixture.Run("remote", "add", "origin", _fixture.Root);
            _fixture.Run("remote", "add", "backup", _second.Root);
        }

        public void Dispose()
        {
            _fixture.Dispose();
            _second.Dispose();
        }

        [Fact]
        public void UnsetRepositoryHasNoDefaultRemote()
        {
            var repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);
            Assert.Equal(string.Empty, repo.Settings.DefaultRemote);
        }

        [Fact]
        public async Task SavingTheSettingsPersistsTheSelectedRemote()
        {
            var repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);
            var vm = new ViewModels.RepositoryConfigure(repo);

            vm.DefaultRemote = "backup";
            await vm.SaveAsync();

            var file = Path.Combine(_fixture.GitDir, "sourcegit.settings");
            Assert.True(File.Exists(file), "the repository settings file was not written");

            var json = File.ReadAllText(file);
            Assert.Contains("DefaultRemote", json, StringComparison.Ordinal);
            Assert.Contains("backup", json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ReopenedRepositoryReadsBackTheStoredRemote()
        {
            var repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);
            var vm = new ViewModels.RepositoryConfigure(repo);
            vm.DefaultRemote = "backup";
            await vm.SaveAsync();

            // A second copy of the same repository means a cold start: the settings are read from
            // disk again instead of the in-memory cache of this process.
            var copy = Copy(_fixture.Root);
            try
            {
                var copyGit = Path.Combine(copy, ".git");
                Assert.Equal("backup", Models.RepositorySettings.Get(copyGit).DefaultRemote);

                var reopened = TestBootstrap.OpenRepository(copy, copyGit);
                Assert.Equal("backup", reopened.Settings.DefaultRemote);

                // Delete the copy only after its repository view model is closed: an open one keeps a
                // 100ms watcher and the 5s auto-fetch timer polling the tree this test is removing.
                TestBootstrap.CloseRepository(reopened);
            }
            finally
            {
                Cleaner.DeleteDirectory(copy);
            }
        }

        [Fact]
        public async Task StoredNameIsAKeyOfTheRemotesTheTransferPopupsLookUp()
        {
            var repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);
            var vm = new ViewModels.RepositoryConfigure(repo);
            vm.DefaultRemote = "backup";
            await vm.SaveAsync();

            // Fetch/Pull/Push resolve the fallback remote as Remotes.Find(x => x.Name == Settings.DefaultRemote).
            var remotes = await new Commands.QueryRemotes(_fixture.Root).GetResultAsync();
            var names = new List<string>();
            foreach (var remote in remotes)
                names.Add(remote.Name);

            Assert.Contains("backup", names);
            Assert.Contains(repo.Settings.DefaultRemote, names);
            Assert.Equal(2, names.Count);
        }

        [Fact]
        public async Task SavingWithoutChangesDoesNotRewriteTheSettingsFile()
        {
            var settings = Models.RepositorySettings.Get(_fixture.GitDir);
            settings.DefaultRemote = "origin";
            settings.Save();

            var file = Path.Combine(_fixture.GitDir, "sourcegit.settings");
            var first = File.GetLastWriteTimeUtc(file);

            // RepositorySettings computes its change-detection hash on a worker thread.
            await Task.Delay(100);

            settings.Save();
            Assert.Equal(first, File.GetLastWriteTimeUtc(file));

            settings.DefaultRemote = "backup";
            settings.Save();
            Assert.True(File.GetLastWriteTimeUtc(file) >= first);
            Assert.Equal("backup", Models.RepositorySettings.Get(_fixture.GitDir).DefaultRemote);
        }

        private static string Copy(string source)
        {
            var target = Path.Combine(Path.GetTempPath(), "sourcegit-tests", Path.GetFileName(source) + "-copy-" + DateTime.Now.ToString("HHmmssfff"));
            CopyDirectory(source, target);
            return target;
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);

            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }
    }
}
