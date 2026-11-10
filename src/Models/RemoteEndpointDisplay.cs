using System;

namespace SourceGit.Models
{
    /// <summary>
    /// Authentication mode guessed from a remote endpoint. It is used only to pick
    /// the right (localized) hint in the transfer popups. It never reads credentials.
    /// </summary>
    public enum EndpointAuthMode
    {
        Unknown = 0,
        Local = 1,
        SSH = 2,
        HTTPS = 3,
        Git = 4,
    }

    /// <summary>
    /// A display-only form of a remote endpoint used by the pre-execution target
    /// summaries. It never carries URI userinfo, query or fragment, so it is safe to
    /// bind to a TextBlock. It never mutates or replaces the real endpoint, and the
    /// real git commands keep using the remote name and the original git config.
    ///
    /// Supported formats: ordinary http(s)/ssh/git URIs, `ssh://[user@]host:port/path`,
    /// SCP-like `[user@]host:path`, `file://` URIs, UNC and local Windows/POSIX paths.
    /// Anything else (unknown scheme, malformed input, embedded secrets that cannot be
    /// separated safely) is reported as not displayable instead of being shown raw.
    /// </summary>
    public class RemoteEndpointDisplay
    {
        public static readonly RemoteEndpointDisplay Invalid = new RemoteEndpointDisplay(false, EndpointAuthMode.Unknown, null, false);

        public bool IsDisplayable
        {
            get;
        }

        public EndpointAuthMode AuthMode
        {
            get;
        }

        /// <summary>Sanitized endpoint, or `null` when IsDisplayable is false.</summary>
        public string Display
        {
            get;
        }

        /// <summary>True when the original endpoint contained userinfo/query/fragment that was dropped.</summary>
        public bool HasHiddenParts
        {
            get;
        }

        public bool IsSSH => AuthMode == EndpointAuthMode.SSH;
        public bool IsHTTPS => AuthMode == EndpointAuthMode.HTTPS;
        public bool IsLocal => AuthMode == EndpointAuthMode.Local;

        private RemoteEndpointDisplay(bool displayable, EndpointAuthMode mode, string display, bool hidden)
        {
            IsDisplayable = displayable;
            AuthMode = mode;
            Display = display;
            HasHiddenParts = hidden;
        }

        public static RemoteEndpointDisplay Parse(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return Invalid;

            var raw = url.Trim();

            // Control characters or embedded newlines are never a valid endpoint, and
            // they could break a TextBlock layout, so refuse to render them.
            if (raw.IndexOfAny(new char[] { '\r', '\n', '\t', '\0' }) >= 0)
                return Invalid;

            // Windows drive path: `C:\repos\x.git` or `C:/repos/x.git`.
            if (raw.Length >= 2 && IsDriveLetter(raw[0]) && raw[1] == ':' &&
                raw.Length >= 4 && (raw[2] == '\\' || raw[2] == '/'))
                return Local(raw);

            // UNC share.
            if (raw.StartsWith(@"\\", StringComparison.Ordinal))
                return Local(raw);

            // Relative / POSIX style local paths.
            if (raw.StartsWith("./", StringComparison.Ordinal) ||
                raw.StartsWith("../", StringComparison.Ordinal) ||
                raw.StartsWith("~/", StringComparison.Ordinal) ||
                (raw.StartsWith("/", StringComparison.Ordinal) && !raw.StartsWith("//", StringComparison.Ordinal)))
                return Local(raw);

            var scheme = GetScheme(raw);
            if (scheme != null)
                return FromURI(raw, scheme);

            return FromSCPStyle(raw);
        }

        private static RemoteEndpointDisplay Local(string path)
        {
            return new RemoteEndpointDisplay(true, EndpointAuthMode.Local, path, false);
        }

        private static RemoteEndpointDisplay FromURI(string raw, string scheme)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(raw, UriKind.Absolute);
            }
            catch
            {
                return Invalid;
            }

            // Only well-known schemes are rendered. Everything else falls back to the
            // localized "hidden" summary instead of guessing what may be secret in it.
            EndpointAuthMode mode;
            int defaultPort;
            switch (scheme)
            {
                case "http":
                    mode = EndpointAuthMode.HTTPS;
                    defaultPort = 80;
                    break;
                case "https":
                    mode = EndpointAuthMode.HTTPS;
                    defaultPort = 443;
                    break;
                case "ssh":
                    mode = EndpointAuthMode.SSH;
                    defaultPort = 22;
                    break;
                case "git":
                    mode = EndpointAuthMode.Git;
                    defaultPort = 9418;
                    break;
                case "file":
                    mode = EndpointAuthMode.Local;
                    defaultPort = -1;
                    break;
                default:
                    return Invalid;
            }

            var host = uri.Host;
            if (string.IsNullOrEmpty(host) && scheme != "file")
                return Invalid;

            // `uri.UserInfo` is never copied out, its presence only flags that we hid
            // something. The raw authority is scanned too because a parsed Uri may not
            // expose the userinfo at all.
            var hidden = !string.IsNullOrEmpty(uri.UserInfo) ||
                uri.Query.Length > 1 ||
                uri.Fragment.Length > 0 ||
                HasRawSecrets(raw, scheme.Length);

            var builder = new System.Text.StringBuilder(128);
            builder.Append(scheme).Append("://").Append(host);

            // The host is already written above; a `file://` URI keeps its optional share
            // host as it is (`file://server/share/repo.git`) and has no port.
            if (scheme != "file" && uri.Port > 0 && uri.Port != defaultPort)
                builder.Append(':').Append(uri.Port);

            // AbsolutePath drops both the query and the fragment.
            var path = uri.AbsolutePath;
            if (scheme == "file" && !path.StartsWith("/", StringComparison.Ordinal))
                path = "/" + path;

            return new RemoteEndpointDisplay(true, mode, builder.Append(path).ToString(), hidden);
        }

        private static RemoteEndpointDisplay FromSCPStyle(string raw)
        {
            var colon = raw.IndexOf(':');
            if (colon <= 0 || colon == raw.Length - 1)
                return Invalid;

            var authority = raw.Substring(0, colon);
            var path = raw.Substring(colon + 1);

            // A one-letter authority is a Windows drive-relative path (`C:x`), not a host.
            if (authority.Length <= 1)
                return Invalid;

            // A `:` inside the path means it is not a `[user@]host:path` endpoint. An `@`
            // inside the path cannot be told apart from a misplaced credential, so such
            // input is hidden instead of guessed.
            if (path.Length == 0 || path.IndexOf(':') >= 0 || path.IndexOf('@') >= 0)
                return Invalid;

            var at = authority.LastIndexOf('@');
            var host = at >= 0 ? authority.Substring(at + 1) : authority;
            if (host.Length == 0 || !IsSafeHost(host))
                return Invalid;

            // A syntactic query/fragment is dropped exactly like an URI query is dropped.
            var cut = path.IndexOfAny(new char[] { '?', '#' });
            var hidden = at >= 0 || cut >= 0;
            if (cut >= 0)
                path = path.Substring(0, cut);

            if (path.Length == 0)
                return Invalid;

            // The `[user@]` part is dropped, only the host and the repository path are shown.
            return new RemoteEndpointDisplay(true, EndpointAuthMode.SSH, $"{host}:{path}", hidden);
        }

        /// <summary>
        /// True when the raw authority holds a userinfo, or the endpoint holds a query
        /// or a fragment. Callers only use it to state that something was hidden.
        /// </summary>
        private static bool HasRawSecrets(string raw, int schemeLength)
        {
            var start = schemeLength + 1;
            while (start < raw.Length && raw[start] == '/')
                start++;

            var end = start;
            while (end < raw.Length && raw[end] != '/' && raw[end] != '?' && raw[end] != '#')
                end++;

            if (raw.IndexOf('@', start) >= 0 && raw.IndexOf('@', start) < end)
                return true;

            return raw.IndexOf('?', start) >= 0 || raw.IndexOf('#', start) >= 0;
        }

        private static string GetScheme(string raw)
        {
            if (raw.Length < 4)
                return null;

            var idx = raw.IndexOf(':');
            if (idx <= 0)
                return null;

            if (!char.IsAscii(raw[0]) || !(char.IsLetter(raw[0]) || raw[0] == '_'))
                return null;

            for (int i = 1; i < idx; i++)
            {
                var c = raw[i];
                if (!char.IsAscii(c) || !(char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.' || c == '_'))
                    return null;
            }

            var scheme = raw.Substring(0, idx).ToLowerInvariant();
            if (scheme.Length == 1)
                return null;

            // `host:path` (SCP-like) looks like a scheme to a plain prefix check, so a
            // candidate is a scheme only when it is known or when it is followed by `://`.
            switch (scheme)
            {
                case "http":
                case "https":
                case "ssh":
                case "git":
                case "file":
                    return scheme;
            }

            return raw.StartsWith($"{scheme}://", StringComparison.OrdinalIgnoreCase) ? scheme : null;
        }

        private static bool IsSafeHost(string host)
        {
            for (int i = 0; i < host.Length; i++)
            {
                var c = host[i];
                if (!char.IsAscii(c) || !(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '[' || c == ']'))
                    return false;
            }

            return host.IndexOf('@') < 0;
        }

        private static bool IsDriveLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }
    }
}
