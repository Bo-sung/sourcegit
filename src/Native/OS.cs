using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace SourceGit.Native
{
    public static partial class OS
    {
        public class Directories
        {
            public string ConfigDir { get; set; } = string.Empty;
            public string CacheDir { get; set; } = string.Empty;
        }

        public interface IBackend
        {
            void SetupApp(AppBuilder builder);
            void SetupWindow(Window window);

            Directories GetOrCreateDirectories();
            string FindGitExecutable();
            string FindTerminal(Models.ShellOrTerminal shell);
            List<Models.ExternalTool> FindExternalTools();

            void OpenTerminal(string workdir, string args);
            void OpenInFileManager(string path);
            void OpenBrowser(string url);
            void OpenWithDefaultEditor(string file);

            bool SupportSetSid();
            string GetSetSidExecutable();
            void TerminateProcess(Process proc);
        }

        public static Directories BasicDirectories
        {
            get;
            private set;
        } = new();

        public static string GitExecutable
        {
            get => _gitExecutable;
            set
            {
                if (_gitExecutable != value)
                {
                    _gitExecutable = value;
                    UpdateGitVersion();
                }
            }
        }

        public static string GitVersionString
        {
            get;
            private set;
        } = string.Empty;

        public static Version GitVersion
        {
            get;
            private set;
        } = new Version(0, 0, 0);

        public static Models.GitFlowVersion GitFlowVersion
        {
            get;
            private set;
        } = Models.GitFlowVersion.None;

        /// <summary>
        /// How many git-flow probe requests this process has made. The number is handed out where a
        /// request is made, not where the probe finally runs, and that is what makes the newest request
        /// own the answer: a probe that starts late carries an older number than one that was asked
        /// later, so the late one cannot publish over the newer one.
        /// </summary>
        public static int GitFlowProbeRequests
        {
            get => Volatile.Read(ref _gitFlowProbeIndex);
        }

        public static string CredentialHelper
        {
            get;
            set;
        } = "manager";

        public static string ShellOrTerminal
        {
            get;
            set;
        } = string.Empty;

        public static string ShellOrTerminalArgs
        {
            get;
            set;
        } = string.Empty;

        public static List<Models.ExternalTool> ExternalTools
        {
            get;
            set;
        } = [];

        public static int ExternalMergerType
        {
            get;
            set;
        } = 0;

        public static string ExternalMergerExecFile
        {
            get;
            set;
        } = string.Empty;

        public static string ExternalMergeArgs
        {
            get;
            set;
        } = string.Empty;

        public static string ExternalDiffArgs
        {
            get;
            set;
        } = string.Empty;

        public static bool UseSystemWindowFrame
        {
            get => OperatingSystem.IsLinux() && _enableSystemWindowFrame;
            set => _enableSystemWindowFrame = value;
        }

        static OS()
        {
            if (OperatingSystem.IsWindows())
                _backend = new Windows();
            else if (OperatingSystem.IsMacOS())
                _backend = new MacOS();
            else if (OperatingSystem.IsLinux())
                _backend = new Linux();
            else
                throw new PlatformNotSupportedException();
        }

        public static void SetupBasicDirectories()
        {
            BasicDirectories = _backend.GetOrCreateDirectories();
        }

        public static void SetupApp(AppBuilder builder)
        {
            _backend.SetupApp(builder);
        }

        public static void SetupExternalTools()
        {
            ExternalTools = _backend.FindExternalTools();
        }

        public static void SetupForWindow(Window window)
        {
            _backend.SetupWindow(window);
        }

        public static void LogException(Exception ex)
        {
            if (ex == null)
                return;

            var crashDir = Path.Combine(BasicDirectories.CacheDir, "crashes");
            if (!Directory.Exists(crashDir))
                Directory.CreateDirectory(crashDir);

            var time = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var file = Path.Combine(crashDir, $"{time}.log");
            using var writer = new StreamWriter(file);
            writer.WriteLine($"Crash::: {ex.GetType().FullName}: {ex.Message}");
            writer.WriteLine();
            writer.WriteLine("----------------------------");
            writer.WriteLine($"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            writer.WriteLine($"OS: {Environment.OSVersion}");
            writer.WriteLine($"Framework: {AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName}");
            writer.WriteLine($"Source: {ex.Source}");
            writer.WriteLine($"Thread Name: {Thread.CurrentThread.Name ?? "Unnamed"}");
            writer.WriteLine($"App Start Time: {Process.GetCurrentProcess().StartTime}");
            writer.WriteLine($"Exception Time: {DateTime.Now}");
            writer.WriteLine($"Memory Usage: {Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024} MB");
            writer.WriteLine("----------------------------");
            writer.WriteLine();
            writer.WriteLine(ex);
            writer.Flush();
        }

        public static string FindGitExecutable()
        {
            return _backend.FindGitExecutable();
        }

        public static bool TestShellOrTerminal(Models.ShellOrTerminal shell)
        {
            return !string.IsNullOrEmpty(_backend.FindTerminal(shell));
        }

        public static void SetShellOrTerminal(Models.ShellOrTerminal shell)
        {
            ShellOrTerminal = shell != null ? _backend.FindTerminal(shell) : string.Empty;
            ShellOrTerminalArgs = shell.Args;
        }

        public static Models.DiffMergeTool GetDiffMergeTool(bool onlyDiff)
        {
            if (ExternalMergerType < 0 || ExternalMergerType >= Models.ExternalMerger.Supported.Count)
                return null;

            if (ExternalMergerType != 0 && (string.IsNullOrEmpty(ExternalMergerExecFile) || !File.Exists(ExternalMergerExecFile)))
                return null;

            return new Models.DiffMergeTool(ExternalMergerExecFile, onlyDiff ? ExternalDiffArgs : ExternalMergeArgs);
        }

        public static void AutoSelectExternalMergeToolExecFile()
        {
            if (ExternalMergerType >= 0 && ExternalMergerType < Models.ExternalMerger.Supported.Count)
            {
                var merger = Models.ExternalMerger.Supported[ExternalMergerType];
                var externalTool = ExternalTools.Find(x => x.Name.Equals(merger.Name, StringComparison.Ordinal));
                if (externalTool != null)
                    ExternalMergerExecFile = externalTool.ExecFile;
                else if (!OperatingSystem.IsWindows() && File.Exists(merger.Finder))
                    ExternalMergerExecFile = merger.Finder;
                else
                    ExternalMergerExecFile = string.Empty;

                ExternalDiffArgs = merger.DiffCmd;
                ExternalMergeArgs = merger.MergeCmd;
            }
            else
            {
                ExternalMergerExecFile = string.Empty;
                ExternalDiffArgs = string.Empty;
                ExternalMergeArgs = string.Empty;
            }
        }

        public static void OpenInFileManager(string path)
        {
            _backend.OpenInFileManager(path);
        }

        public static void OpenBrowser(string url)
        {
            if (!IsSafeBrowserTarget(url))
            {
                Models.Notification.Send(null, $"Blocked unsafe URL: {url}", true);
                return;
            }

            _backend.OpenBrowser(url);
        }

        private static bool IsSafeBrowserTarget(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute) &&
                Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (
                    uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps ||
                    uri.Scheme == Uri.UriSchemeFtp
                );
        }

        public static void OpenTerminal(string workdir)
        {
            if (string.IsNullOrEmpty(ShellOrTerminal))
            {
                Models.Notification.Send(workdir, App.Text("Terminal.NotSpecified"), true);
                return;
            }

            var cwd = string.IsNullOrEmpty(workdir) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : workdir;
            var args = ShellOrTerminalArgs;
            if (!string.IsNullOrEmpty(args))
                args = args.Replace("$FOLDER_NAME", Path.GetFileName(cwd.Trim().TrimEnd('/')));

            _backend.OpenTerminal(cwd, args);
        }

        public static void OpenWithDefaultEditor(string file)
        {
            _backend.OpenWithDefaultEditor(file);
        }

        public static string GetAbsPath(string root, string sub)
        {
            var fullpath = Path.Combine(root, sub);
            if (OperatingSystem.IsWindows())
                return fullpath.Replace('/', '\\');

            return fullpath;
        }

        public static string GetRelativePathToHome(string path)
        {
            if (OperatingSystem.IsWindows())
                return path;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var prefixLen = home.EndsWith('/') ? home.Length - 1 : home.Length;
            if (path.StartsWith(home, StringComparison.Ordinal))
                return $"~{path.AsSpan(prefixLen)}";

            return path;
        }

        public static bool SupportSetSid()
        {
            return _backend.SupportSetSid();
        }

        public static string GetSetSidExecutable()
        {
            return _backend.GetSetSidExecutable();
        }

        public static void TerminateProcess(Process proc)
        {
            _backend.TerminateProcess(proc);
        }

        private static void UpdateGitVersion()
        {
            if (string.IsNullOrEmpty(_gitExecutable) || !File.Exists(_gitExecutable))
            {
                GitVersionString = string.Empty;
                GitVersion = new Version(0, 0, 0);
                GitFlowVersion = Models.GitFlowVersion.None;
                return;
            }

            var start = new ProcessStartInfo();
            start.FileName = _gitExecutable;
            start.Arguments = "--version";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;

            try
            {
                using var proc = Process.Start(start)!;
                var rs = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(rs))
                {
                    GitVersionString = rs.Trim();

                    var match = REG_GIT_VERSION().Match(GitVersionString);
                    if (match.Success)
                    {
                        var major = int.Parse(match.Groups[1].Value);
                        var minor = int.Parse(match.Groups[2].Value);
                        var build = int.Parse(match.Groups[3].Value);
                        GitVersion = new Version(major, minor, build);
                        GitVersionString = GitVersionString.Substring(11).Trim();
                    }

                    // Update git flow version in background to avoid blocking the UI.
                    RequestGitFlowProbe(_gitExecutable);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>The newest git-flow probe request's number; an answer may only publish while its
        /// own number is still the newest one.</summary>
        public static int GitFlowProbeGeneration => Volatile.Read(ref _gitFlowProbeIndex);

        /// <summary>
        /// Asks one git which git-flow it has, without waiting for the answer. The number a request
        /// carries is taken here, where the request is made, and travels with it: if the number were
        /// handed out when the task body finally started, a request that started earlier but was slow
        /// to get a thread would come out as the newer one, and that is precisely the ordering this
        /// guard is about (final review N-3). Asking also invalidates the previous answer, because an
        /// answer describes the git it came from and that question is open again.
        /// </summary>
        /// <returns>The request's number, for a caller that wants to follow it.</returns>
        internal static int RequestGitFlowProbe(string git)
        {
            var generation = Interlocked.Increment(ref _gitFlowProbeIndex);
            GitFlowVersion = Models.GitFlowVersion.None;
            Task.Run(() => PublishGitFlowProbe(generation, GitFlowProbeRun(git)));
            return generation;
        }

        /// <summary>
        /// How the probe finds its answer. The product asks the git executable; a scheduling test puts
        /// its own answer source here so it can decide which request answers first - a real git cannot
        /// be asked to answer out of order on demand, so without this seam the ordering rule would only
        /// ever be tested by luck. Nothing else about the request changes.
        /// </summary>
        internal static Func<string, Models.GitFlowVersion> GitFlowProbeRun = RunGitFlowProbe;

        /// <summary>Runs <c>git flow version</c> for one executable and names what it found.</summary>
        private static Models.GitFlowVersion RunGitFlowProbe(string git)
        {
            var found = Models.GitFlowVersion.None;

            var start = new ProcessStartInfo();
            start.FileName = git;
            start.Arguments = "flow version";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;

            try
            {
                using var proc = Process.Start(start)!;
                var rs = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(rs))
                {
                    if (rs.Contains("git-flow-next", StringComparison.Ordinal))
                        found = Models.GitFlowVersion.Next;
                    else
                        found = Models.GitFlowVersion.Legacy;
                }
            }
            catch
            {
                // git itself is there; only git flow is missing or unreadable.
            }

            return found;
        }

        /// <summary>
        /// Publishes an answer and says whether it counted. Two requests can be in flight when the git
        /// executable is set twice in a row (the tests hide and show the dependency that way, and a
        /// user who picks another git does it too), and the thread pool does not keep their order. An
        /// answer whose request is no longer the newest is dropped, because it describes a git this
        /// client has moved on from - showing it would advertise a git-flow the git in use does not
        /// have, or hide one it does have.
        /// </summary>
        internal static bool PublishGitFlowProbe(int generation, Models.GitFlowVersion found)
        {
            if (generation != Volatile.Read(ref _gitFlowProbeIndex))
                return false;

            GitFlowVersion = found;
            return true;
        }

        [GeneratedRegex(@"^git version[\s\w]*(\d+)\.(\d+)[\.\-](\d+).*$")]
        private static partial Regex REG_GIT_VERSION();

        private static IBackend _backend = null;
        private static string _gitExecutable = string.Empty;
        private static int _gitFlowProbeIndex = 0;
        private static bool _enableSystemWindowFrame = false;
    }
}
