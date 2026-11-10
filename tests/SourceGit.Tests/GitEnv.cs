using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SourceGit.Tests
{
    /// <summary>
    /// Keeps this test run away from the machine's own git configuration. <c>GIT_CONFIG_GLOBAL</c> and
    /// <c>GIT_CONFIG_SYSTEM</c> are pointed at fixture files outside every repository and
    /// <c>GIT_CONFIG_NOSYSTEM</c> switches the system file off too, because reading or writing the real
    /// user or machine config is out of bounds for these tests - a failed read of it once leaked real
    /// values into a log, and a green run must not depend on what the machine happens to hold.
    ///
    /// The files live under the tests' own fixture root in <c>%TEMP%</c>, never inside a repository, so
    /// they cannot be mistaken for a fixture's configuration and cannot be committed anywhere.
    /// </summary>
    public static class GitEnv
    {
        /// <summary>Directory holding the isolation files, created on first use.</summary>
        public static string Root
        {
            get
            {
                var dir = Path.Combine(Path.GetTempPath(), "sourcegit-tests", "git-env");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>The stand-in for a user's global config: empty unless a test writes into it.</summary>
        public static string GlobalFile => Path.Combine(Root, "global.gitconfig");

        /// <summary>The stand-in for the machine's system config: stays empty.</summary>
        public static string SystemFile => Path.Combine(Root, "system.gitconfig");

        /// <summary>
        /// Turns the isolation on for this process, so every git that any test or the product itself
        /// starts inherits it. Idempotent; <see cref="RestoreProcess"/> puts the previous values back.
        /// </summary>
        public static void EnforceProcess()
        {
            Touch(GlobalFile);
            Touch(SystemFile);

            Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", GlobalFile);
            Environment.SetEnvironmentVariable("GIT_CONFIG_SYSTEM", SystemFile);
            Environment.SetEnvironmentVariable("GIT_CONFIG_NOSYSTEM", "1");
        }

        /// <summary>Puts the three variables back as this process had them.</summary>
        public static void RestoreProcess(string previousGlobal, string previousSystem, string previousNoSystem)
        {
            Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", previousGlobal);
            Environment.SetEnvironmentVariable("GIT_CONFIG_SYSTEM", previousSystem);
            Environment.SetEnvironmentVariable("GIT_CONFIG_NOSYSTEM", previousNoSystem);
        }

        /// <summary>
        /// Hands git a specific file as the config "above this repository" for the duration of the scope,
        /// and puts the empty one back afterwards. A test that needs inherited git-flow keys uses this
        /// instead of touching the machine's real config file.
        /// </summary>
        public static IDisposable WithGlobalConfig(string content)
        {
            // A file of its own per scope: two tests sharing one file would read each other's keys, and
            // a test that forgot to clean up would leave a key the next one took for the machine's own.
            var path = Path.Combine(Root, "scope-" + Guid.NewGuid().ToString("N").Substring(0, 10) + ".gitconfig");
            File.WriteAllText(path, content ?? string.Empty, new UTF8Encoding(false));
            return new Scoped(path);
        }

        /// <summary>
        /// True when a git started from this process resolves its global config to the fixture file and
        /// its system config to nothing. Proved with a synthetic sentinel written into the fixture file,
        /// so the check itself never reads anything real.
        /// </summary>
        public static bool IsInEffect(string repository) => Check(repository).Length == 0;

        /// <summary>
        /// The same question as <see cref="IsInEffect"/>, answered with what went wrong instead of a
        /// bare false: a guard that only says "not isolated" is no use to whoever has to fix it.
        /// </summary>
        public static string Check(string repository)
        {
            // Both names are made up here, so the check proves the wiring without ever reading a config
            // that belongs to the machine: the sentinel has to come back from a child git, and every
            // origin git resolved has to be a fixture file or a file inside the repository.
            var key = "probe" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var value = "value-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            using var scope = WithGlobalConfig("[isolation]\n\t" + key + " = " + value + "\n");

            var read = Get(repository, "isolation." + key);
            if (!string.Equals(read, value, StringComparison.Ordinal))
                return read.Length == 0 ? "the fixture key did not come back at all" : "a different value came back";

            var origins = ShowOrigins(repository);
            return origins == "isolation-files-only" ? string.Empty : origins;
        }

        /// <summary>
        /// Reads one key the way a child git would see it. Empty when nothing holds the key - which is
        /// also how a failed read looks, so this is only used for the isolation check itself.
        /// </summary>
        public static string Get(string repository, string key) => Output(repository, "config", "--get", key);

        /// <summary>Runs one git command in a directory and returns its trimmed stdout.</summary>
        public static string Output(string repository, params string[] args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Native.OS.GitExecutable,
                WorkingDirectory = repository,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            using var proc = Process.Start(psi);
            var rs = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return rs.Trim();
        }

        /// <summary>
        /// A label for the config files a child git resolved: `isolation-files-only` when every origin
        /// was one of the two fixture files or a file inside the repository itself. Anything else is
        /// reported as a count and a kind only - never as a path or a value, because what another user's
        /// or the machine's file holds must not reach a log by way of proving it was not read.
        /// </summary>
        public static string ShowOrigins(string repository)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Native.OS.GitExecutable,
                Arguments = "config --list --show-origin",
                WorkingDirectory = repository,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var foreign = 0;
            var kind = "origin";
            using (var proc = Process.Start(psi))
            {
                var root = Path.GetFullPath(repository).Replace('\\', '/').TrimEnd('/');

                foreach (var line in proc.StandardOutput.ReadToEnd().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                {
                    // git separates the origin from the key with a tab, and a Windows path carries
                    // colons of its own, so the tab is the only safe split point.
                    var tab = line.IndexOf('\t');
                    if (tab <= 0)
                        continue;

                    var origin = line.Substring(0, tab).Trim();

                    // Keys the environment handed to git (GIT_CONFIG_KEY_n or a -c style override) are
                    // not files, and the system file is already ruled out by GIT_CONFIG_NOSYSTEM.
                    if (origin.StartsWith("command line", StringComparison.OrdinalIgnoreCase) ||
                        origin.StartsWith("GIT-CONFIG-ENV", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!origin.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                    {
                        foreign++;
                        kind = "non-file";
                        continue;
                    }

                    var path = Normalize(Unquote(origin.Substring("file:".Length).Trim()));

                    // git puts a config file's name in quotes when it would otherwise be ambiguous, so
                    // the quotes come off before anything is compared.
                    // git names the repository's own file relative to the repository (`file:.git/config`)
                    // and the files above it with an absolute path, so a relative name resolves here.
                    if (!path.Contains(':', StringComparison.OrdinalIgnoreCase))
                        path = Normalize(Path.GetFullPath(Path.Combine(repository, path)));

                    if (IsFixturePath(path) ||
                        path.StartsWith(Normalize(root) + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreign++;
                    kind = "outside-the-repository (" + Path.GetFileName(path) + ")";
                }

                proc.WaitForExit();
            }

            return foreign == 0 ? "isolation-files-only" : "foreign-" + kind + ":" + foreign;
        }

        private static void Touch(string path)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, string.Empty, new UTF8Encoding(false));
        }

        /// <summary>Drops the quotes git puts around a path it had to escape.</summary>
        private static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = value.Substring(1, value.Length - 2);

            return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        /// <summary>
        /// A path in one shape: forward slashes, no doubled ones. git prints a Windows config path with
        /// every separator doubled (`file:C://Users//...`), so comparing two of them as written would
        /// call the fixture's own file a stranger.
        /// </summary>
        private static string Normalize(string path)
        {
            return string.Join('/', path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>Whether a path is one of the fixture's own config files under its directory.</summary>
        private static bool IsFixturePath(string path)
        {
            var dir = Normalize(Root);
            return path.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase) &&
                    path.Substring(dir.Length + 1).IndexOf('/') < 0;
        }

        /// <summary>
        /// Points GIT_CONFIG_GLOBAL at one file and, on the way out, puts back whatever the process had
        /// before and deletes the file it wrote - so a test cannot leave a key behind that the next one
        /// would read as though it came from the machine.
        /// </summary>
        private sealed class Scoped : IDisposable
        {
            private readonly string _path;
            private readonly string _keep;

            public Scoped(string path)
            {
                _path = path;
                _keep = Environment.GetEnvironmentVariable("GIT_CONFIG_GLOBAL");
                Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", path);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", _keep ?? string.Empty);

                try
                {
                    File.Delete(_path);
                }
                catch
                {
                    // A leftover fixture file under %TEMP% is not worth failing a test over.
                }
            }
        }
    }
}
