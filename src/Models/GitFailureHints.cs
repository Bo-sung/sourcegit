using Avalonia.Threading;

namespace SourceGit.Models
{
    /// <summary>
    /// Adds one actionable line to a Git failure that the client can name. Git's own
    /// output stays the evidence; this only says what to check next. It never repeats a
    /// URL, account, key or token from the message, so nothing secret can be echoed back.
    /// </summary>
    public static class GitFailureHints
    {
        // Commands report failures on a worker thread, while texts live in the Avalonia
        // resource dictionary, which owns the UI thread.
        private static string Text(string key)
        {
            return Dispatcher.UIThread.CheckAccess()
                ? App.Text(key)
                : Dispatcher.UIThread.Invoke(() => App.Text(key));
        }

        public static string For(string error)
        {
            var key = KeyFor(error);
            return string.IsNullOrEmpty(key) ? string.Empty : Text(key);
        }

        /// <summary>
        /// Classifies Git's own failure output and returns the translation key of the matching
        /// hint, or an empty string when nothing here can be named. Pure: no UI, no locale, so the
        /// classes can be checked against synthetic output. The order matters - a real SSH failure
        /// often prints several of these lines, and the first match is the one to act on: the host
        /// key check fails before authentication is tried, authentication fails before the server
        /// answers, and a connection that never opened means no credential was ever sent.
        /// </summary>
        public static string KeyFor(string error)
        {
            if (string.IsNullOrEmpty(error))
                return string.Empty;

            var text = error.ToLowerInvariant();

            if (text.Contains("transport 'file' not allowed", System.StringComparison.Ordinal))
                return "Classic.Transfer.FailLocalTransport";

            if (text.Contains("host key verification failed", System.StringComparison.Ordinal) ||
                text.Contains("remote host identification has changed", System.StringComparison.Ordinal) ||
                text.Contains("the authenticity of host", System.StringComparison.Ordinal))
                return "Classic.Transfer.FailSSHHostKey";

            if (text.Contains("permission denied (publickey", System.StringComparison.Ordinal) ||
                text.Contains("permission denied, please try again", System.StringComparison.Ordinal) ||
                text.Contains("no supported authentication methods", System.StringComparison.Ordinal))
                return "Classic.Transfer.FailSSHPermission";

            if (text.Contains("could not read username", System.StringComparison.Ordinal) ||
                text.Contains("could not read password", System.StringComparison.Ordinal) ||
                text.Contains("authentication failed for", System.StringComparison.Ordinal) ||
                text.Contains("invalid username or password", System.StringComparison.Ordinal) ||
                text.Contains("authentication required", System.StringComparison.Ordinal) ||
                text.Contains("http error 401", System.StringComparison.Ordinal) ||
                text.Contains("returned error: 401", System.StringComparison.Ordinal))
                return "Classic.Transfer.FailCredential";

            // Windows Git talks TLS through schannel, which names a broken certificate chain with a
            // Windows security status constant and only translates the sentence behind it. The
            // constant is the culture-independent part, so it is what is matched here; a connection
            // that never opened or a wrong account never carries these names, so no other class can
            // be stolen by them.
            if (text.Contains("ssl certificate", System.StringComparison.Ordinal) ||
                text.Contains("unable to get local issuer", System.StringComparison.Ordinal) ||
                text.Contains("certificate verify failed", System.StringComparison.Ordinal) ||
                text.Contains("certificate verification failed", System.StringComparison.Ordinal) ||
                text.Contains("sec_e_untrusted_root", System.StringComparison.Ordinal) ||
                text.Contains("sec_e_cert_expired", System.StringComparison.Ordinal))
                return "Classic.Transfer.FailTLSCert";

            if (text.Contains("connect to host", System.StringComparison.Ordinal) ||
                text.Contains("connection refused", System.StringComparison.Ordinal) ||
                text.Contains("connection timed out", System.StringComparison.Ordinal) ||
                text.Contains("could not resolve host", System.StringComparison.Ordinal) ||
                text.Contains("could not resolve hostname", System.StringComparison.Ordinal) ||
                text.Contains("network is unreachable", System.StringComparison.Ordinal) ||
                text.Contains("failed to connect to", System.StringComparison.Ordinal))
                return "Classic.Transfer.FailConnect";

            if (text.Contains("repository not found", System.StringComparison.Ordinal) ||
                text.Contains("does not appear to be a git repository", System.StringComparison.Ordinal) ||
                text.Contains("http error 403", System.StringComparison.Ordinal) ||
                text.Contains("http error 404", System.StringComparison.Ordinal) ||
                text.Contains("returned error: 403", System.StringComparison.Ordinal) ||
                text.Contains("returned error: 404", System.StringComparison.Ordinal) ||
                // "access denied" is also a plain Windows file-system error, so it only counts as a
                // repository problem when Git itself mentions the repository or the remote.
                (text.Contains("access denied", System.StringComparison.Ordinal) &&
                    (text.Contains("repository", System.StringComparison.Ordinal) || text.Contains("remote", System.StringComparison.Ordinal))))
                return "Classic.Transfer.FailRepository";

            // Git's own grammar for a missing remote is "repository '<path or url>' not found": the
            // quoted subject comes first and the message ends there. Match that shape instead of a bare
            // conjunction of the two words, so "file 'config' not found in repository" (a missing file)
            // and "helper not found for repository" (a missing helper) stay unclaimed. Only the shape is
            // read, never the text inside the quotes, so no URL, account, key or token can be echoed.
            foreach (var line in text.Split('\n'))
            {
                var one = line.TrimEnd('\r', ' ', '\t', '.', '!', '?');
                if (one.Contains("repository '", System.StringComparison.Ordinal) &&
                    one.EndsWith("not found", System.StringComparison.Ordinal))
                    return "Classic.Transfer.FailRepository";
            }

            return string.Empty;
        }
    }
}
