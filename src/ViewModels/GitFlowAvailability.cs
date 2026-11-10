namespace SourceGit.ViewModels
{
    /// <summary>
    /// Shared dependency preflight for git-flow operations.
    ///
    /// <c>Native.OS.GitFlowVersion</c> starts as <c>None</c> and is only filled later by a
    /// background probe that runs after the git version is read, so <c>None</c> means
    /// "unavailable OR not detected yet". It must never be reported as a confirmed
    /// "git-flow is not installed".
    /// </summary>
    public static class GitFlowAvailability
    {
        /// <summary>Translation key used by the preflight notification.</summary>
        public const string NotificationKey = "GitFlow.DependencyUnavailable";

        /// <summary><c>true</c> only when a git-flow dependency (legacy or next) was actually detected.</summary>
        public static bool IsAvailable => Native.OS.GitFlowVersion != Models.GitFlowVersion.None;

        /// <summary>
        /// Preflight a git-flow operation: returns <c>true</c> when the caller may continue.
        /// Otherwise it explains the missing/un-detected dependency in the repository notification
        /// area and returns <c>false</c> with no side effect at all, so callers must run this before
        /// locking the watcher, creating the command log, creating branches or writing configuration.
        /// </summary>
        public static bool GuardOrNotify(Repository repo)
        {
            if (IsAvailable)
                return true;

            repo?.SendNotification(App.Text(NotificationKey), true);
            return false;
        }
    }
}
