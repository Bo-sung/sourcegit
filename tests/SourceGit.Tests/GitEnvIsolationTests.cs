using System;
using System.IO;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// The guard that keeps this run inside its own fixture configuration. Reading or writing the
    /// machine's git configuration is out of bounds (a merged `git config -l` once put real user values
    /// into a log), so the check is asserted rather than assumed - and it is proved with a synthetic
    /// sentinel written into the fixture file, never by looking at a real one.
    /// </summary>
    /// <summary>
    /// Shares the collection of everything that installs a git config file of its own: GIT_CONFIG_GLOBAL
    /// belongs to the whole process, so two classes doing that at the same time would read each other's
    /// keys and blame the machine for it.
    /// </summary>
    [Collection("shared-git-environment")]
    public class GitEnvIsolationTests : IDisposable
    {
        private readonly GitRepositoryFixture _fixture;

        public GitEnvIsolationTests()
        {
            TestBootstrap.Prepare();
            _fixture = GitRepositoryFixture.Create("gitenv");
            _fixture.Commit("a.txt", "a\n", "first");
        }

        public void Dispose() => _fixture.Dispose();

        [Fact]
        public void GitResolvesOnlyTheFixtureConfigFiles()
        {
            // The sentinel goes into a fixture file of its own and has to come back from a child git:
            // that is what proves GIT_CONFIG_GLOBAL took effect. The same check then insists that every
            // origin the child git resolved is a fixture file or the repository's own file, reported as
            // a label so no path or value of another file is ever shown.
            Assert.Equal(string.Empty, GitEnv.Check(_fixture.Root));
        }

        [Fact]
        public void TheThreeVariablesAreSetForTheWholeRun()
        {
            // Enforced once in TestBootstrap.Prepare so no test has to remember it, and no git started
            // by the product itself escapes it either.
            Assert.Equal(GitEnv.GlobalFile, Environment.GetEnvironmentVariable("GIT_CONFIG_GLOBAL"));
            Assert.Equal(GitEnv.SystemFile, Environment.GetEnvironmentVariable("GIT_CONFIG_SYSTEM"));
            Assert.Equal("1", Environment.GetEnvironmentVariable("GIT_CONFIG_NOSYSTEM"));
            Assert.True(File.Exists(GitEnv.GlobalFile));
            Assert.True(File.Exists(GitEnv.SystemFile));
        }

        [Fact]
        public void ARepositoryWithoutItsOwnIdentityHasNoIdentity()
        {
            // The machine's global config holds a name and an address. With the isolation in place a
            // repository that sets none of its own must resolve neither, which is the cheapest way to
            // see that nothing of the user's config reaches a test repository.
            var root = Path.Combine(Path.GetTempPath(), "sourcegit-tests", "gitenv-bare-" + DateTime.Now.ToString("HHmmssfff"));
            Directory.CreateDirectory(root);
            try
            {
                Run(root, "init", "-b", "main");
                Assert.Equal(Normalize(root), Normalize(GitEnv.Output(root, "rev-parse", "--show-toplevel")));
                Assert.Equal(string.Empty, GitEnv.Get(root, "user.name"));
                Assert.Equal(string.Empty, GitEnv.Get(root, "user.email"));
            }
            finally
            {
                Cleaner.DeleteDirectory(root);
            }
        }

        private static string Normalize(string path) => path.Replace('\\', '/');

        private static void Run(string cwd, params string[] args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Native.OS.GitExecutable,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = cwd,
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            using var proc = System.Diagnostics.Process.Start(psi);
            proc.WaitForExit();
            Assert.True(proc.ExitCode == 0, "git exited with " + proc.ExitCode);
        }
    }
}
