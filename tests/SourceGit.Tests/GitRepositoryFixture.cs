using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using Avalonia.Threading;

namespace SourceGit.Tests
{
    /// <summary>A disposable, synthetic git repository built with the real git binary.</summary>
    public sealed class GitRepositoryFixture : IDisposable
    {
        /// <summary>Working tree root.</summary>
        public string Root { get; }

        /// <summary>`.git` directory of this repository (not a worktree common dir).</summary>
        public string GitDir { get; }

        private GitRepositoryFixture(string root, string label)
        {
            Root = root;
            GitDir = Path.Combine(root, ".git");

            Run("init", "-b", "main");

            // Every mutation below has to land in this directory and nowhere else. A fixture path
            // that only *looks* like a repository (an ordinary folder inside another repository)
            // would send `git config`, `commit` and friends to that enclosing repository, so the
            // toplevel git reports is checked against the directory that was asked for before
            // anything is written.
            var top = RunOutput("rev-parse", "--show-toplevel").Trim().Replace('\\', '/').TrimEnd('/');
            var want = root.Replace('\\', '/').TrimEnd('/');
            if (!top.Equals(want, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Fixture '{label}' is not its own repository: git resolves the toplevel to '{top}' instead of '{want}'. Refusing to write into the enclosing repository.");

            Run("config", "user.name", "SourceGit Test");
            Run("config", "user.email", "test@sourcegit.invalid");
            Run("config", "commit.gpgsign", "false");
            Run("config", "core.autocrlf", "false");
            Run("config", "gc.auto", "0");
        }

        public static GitRepositoryFixture Create(string name)
        {
            TestBootstrap.Prepare();

            var root = Path.Combine(Path.GetTempPath(), "sourcegit-tests");
            var dir = Path.Combine(root, name + "-" + DateTime.Now.ToString("HHmmssfff"));
            Directory.CreateDirectory(dir);
            return new GitRepositoryFixture(dir, name);
        }

        public string SHA(string rev = "HEAD")
        {
            return RunOutput("rev-parse", rev).Trim();
        }

        public void Write(string file, string content)
        {
            var target = Path.Combine(Root, file.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(target, content, new UTF8Encoding(false));
        }

        public string Commit(string file, string content, string message, string author = null)
        {
            Write(file, content);
            Run("add", "--", file);

            if (string.IsNullOrEmpty(author))
                Run("commit", "-m", message);
            else
                Run("commit", "--author=" + author, "-m", message);

            return SHA();
        }

        public void NewBranch(string name, string rev = "HEAD")
        {
            Run("branch", name, rev);
        }

        public void Checkout(string name)
        {
            Run("checkout", name);
        }

        public List<string> Subjects(string range = "--all")
        {
            var result = new List<string>();
            foreach (var line in RunOutput("log", "--format=%s", range).Split('\n'))
            {
                var t = line.Trim();
                if (t.Length > 0)
                    result.Add(t);
            }

            return result;
        }

        /// <summary>Independent verification through plain git (stdout is returned).</summary>
        public string RunOutput(params string[] args)
        {
            return Execute(null, args);
        }

        /// <summary>Plain git that must succeed.</summary>
        public void Run(params string[] args)
        {
            Execute(null, args);
        }

        /// <summary>Plain git with an editor override, e.g. a prepared rebase todo list.</summary>
        public string RunWithSequenceEditor(string sequenceEditor, params string[] args)
        {
            return RunWithEditors(sequenceEditor, null, args);
        }

        /// <summary>Same, with a commit-message editor as well (git opens it for 'reword' and 'squash').</summary>
        public string RunWithEditors(string sequenceEditor, string messageEditor, params string[] args)
        {
            return Execute(EditorEnv(sequenceEditor, messageEditor), args);
        }

        /// <summary>Same as RunWithSequenceEditor but returns the exit code (a stopped rebase is not an error).</summary>
        public (string Output, int ExitCode) TryWithSequenceEditor(string sequenceEditor, params string[] args)
        {
            return RunCore(EditorEnv(sequenceEditor, null), args);
        }

        private static Dictionary<string, string> EditorEnv(string sequenceEditor, string messageEditor)
        {
            return new Dictionary<string, string>()
            {
                { "GIT_SEQUENCE_EDITOR", sequenceEditor },
                { "GIT_EDITOR", string.IsNullOrEmpty(messageEditor) ? "true" : messageEditor },
            };
        }

        private string Execute(IDictionary<string, string> extraEnv, string[] args)
        {
            var (stdout, exit) = RunCore(extraEnv, args);
            if (exit != 0)
                throw new InvalidOperationException($"git {string.Join(' ', args)} exit={exit} in {Root}\n{stdout}");
            return stdout;
        }

        private (string, int) RunCore(IDictionary<string, string> extraEnv, string[] args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Native.OS.GitExecutable,
                WorkingDirectory = Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
            psi.Environment["GCM_INTERACTIVE"] = "never";

            if (extraEnv != null)
            {
                foreach (var kv in extraEnv)
                    psi.Environment[kv.Key] = kv.Value;
            }

            using var proc = Process.Start(psi);
            var output = proc.StandardOutput.ReadToEnd();
            var error = proc.StandardError.ReadToEnd();
            proc.WaitForExit(120000);

            return (output + (string.IsNullOrEmpty(error) ? string.Empty : "\n" + error), proc.ExitCode);
        }

        public void Dispose()
        {
            // Close the repository view models that were opened for this fixture first: each one owns
            // a 100ms watcher and the 5s auto-fetch timer, and both would keep polling a directory
            // that no longer exists (integration review F-5).
            TestBootstrap.CloseRepositories();
            Cleaner.DeleteDirectory(Root);
        }
    }

    internal static class Cleaner
    {
        public static void DeleteDirectory(string dir)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (!Directory.Exists(dir))
                        return;

                    foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                        File.SetAttributes(file, FileAttributes.Normal);

                    Directory.Delete(dir, true);
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }

    internal static class Pump
    {
        /// <summary>Runs Avalonia dispatcher jobs until the condition holds; the app posts query results to the UI thread.</summary>
        public static bool Until(Func<bool> condition, int timeoutMS = 20000)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeoutMS)
            {
                Dispatcher.UIThread.RunJobs();
                if (condition())
                    return true;

                Thread.Sleep(10);
            }

            Dispatcher.UIThread.RunJobs();
            return condition();
        }
    }
}
